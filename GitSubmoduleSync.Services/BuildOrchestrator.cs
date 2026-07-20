using System.Collections.Concurrent;
using System.Diagnostics;
using GitSubmoduleSync.Models;

namespace GitSubmoduleSync.Services;

public sealed record ProgressoBuild(int OndaAtual, int TotalOndas, int Concluidos, int Total);

public sealed class BuildOrchestrator
{
  private static readonly string[] ExtensoesRelevantes = { ".cs", ".resx", ".csproj", ".config" };
  private static readonly string[] PastasExcluidas = { "bin", "obj", "packages", ".git", "node_modules" };

  private readonly MsBuildService _msBuildService = new();

  public async Task<ResultadoExecucao> ExecutarAsync(
    Grafo grafo, SyncProfile perfil, string msbuild, bool ignorarIncremental,
    IProgress<LogEvent> log, IProgress<ProgressoBuild> progresso, CancellationToken ct)
  {
    var geral = Stopwatch.StartNew();

    var falharam = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
    var bloqueados = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
    var recompilados = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal); // buildados de fato (não pulados)

    var compiladosCount = 0;
    var puladosCount = 0;
    var concluidos = 0;
    var total = grafo.Nos.Count;

    var grauParalelismo = perfil.GrauParalelismoBuild > 0 ? perfil.GrauParalelismoBuild : Environment.ProcessorCount;
    var usarIncremental = perfil.BuildIncremental && !ignorarIncremental;

    for (var w = 0; w < grafo.Ondas.Count; w++)
    {
      ct.ThrowIfCancellationRequested();
      var onda = grafo.Ondas[w];
      var ondaAtual = w + 1;

      var aCompilar = new List<ProjectNode>();
      foreach (var no in onda)
      {
        var deps = grafo.Arestas.TryGetValue(no.AssemblyName, out var d) ? d : Array.Empty<string>();
        var depsRuins = deps.Where(dep => falharam.ContainsKey(dep) || bloqueados.ContainsKey(dep)).ToList();

        if (depsRuins.Count > 0)
        {
          bloqueados[no.AssemblyName] = 0;
          log.Report(new LogEvent(NivelLog.Aviso, $"bloqueado por dependência: {string.Join(", ", depsRuins)}", Projeto: no.AssemblyName));
          concluidos++;
          progresso.Report(new ProgressoBuild(ondaAtual, grafo.Ondas.Count, concluidos, total));
        }
        else
        {
          aCompilar.Add(no);
        }
      }

      log.Report(new LogEvent(NivelLog.Info,
        $"onda {ondaAtual}/{grafo.Ondas.Count}: {aCompilar.Count} a compilar, {onda.Count - aCompilar.Count} bloqueado(s)"));

      using var semaforo = new SemaphoreSlim(grauParalelismo);

      var tarefasOnda = aCompilar.Select(async no =>
      {
        await semaforo.WaitAsync(ct);
        try
        {
          var deps = grafo.Arestas.TryGetValue(no.AssemblyName, out var d) ? d : Array.Empty<string>();
          var dependenciaRecompilada = deps.Any(recompilados.ContainsKey);

          if (usarIncremental && !dependenciaRecompilada && PodeSerPulado(no, grafo.BinCustomResolvido))
          {
            Interlocked.Increment(ref puladosCount);
            log.Report(new LogEvent(NivelLog.Detalhe, "pulado — sem alteração", Projeto: no.AssemblyName));
          }
          else
          {
            var resultado = await _msBuildService.CompilarAsync(no, msbuild, log, ct);
            recompilados[no.AssemblyName] = 0;

            if (resultado.Status == StatusProjeto.Compilado)
            {
              Interlocked.Increment(ref compiladosCount);
              log.Report(new LogEvent(NivelLog.Sucesso, $"compilado em {resultado.Duracao.TotalSeconds:0.0}s", Projeto: no.AssemblyName));
            }
            else
            {
              falharam[no.AssemblyName] = 0;
              log.Report(new LogEvent(NivelLog.Erro, $"falhou (exit code {resultado.ExitCode})", Projeto: no.AssemblyName));
            }
          }
        }
        finally
        {
          semaforo.Release();
          var c = Interlocked.Increment(ref concluidos);
          progresso.Report(new ProgressoBuild(ondaAtual, grafo.Ondas.Count, c, total));
        }
      });

      await Task.WhenAll(tarefasOnda); // barreira: a próxima onda só começa quando esta termina
    }

    geral.Stop();

    var etapa = new ResultadoEtapa("Build", falharam.IsEmpty && bloqueados.IsEmpty, geral.Elapsed);
    return new ResultadoExecucao(
      Etapas: new[] { etapa },
      Compilados: compiladosCount,
      Pulados: puladosCount,
      Falharam: falharam.Count,
      Bloqueados: bloqueados.Count,
      Total: geral.Elapsed);
  }

  private static bool PodeSerPulado(ProjectNode no, string binCustomResolvido)
  {
    var caminhoBin = string.IsNullOrEmpty(no.OutputPathAbsoluto) ? binCustomResolvido : no.OutputPathAbsoluto;
    if (string.IsNullOrEmpty(caminhoBin)) return false;

    var caminhoDll = Path.Combine(caminhoBin, no.AssemblyName + ".dll");
    if (!File.Exists(caminhoDll)) return false;

    var dllTime = File.GetLastWriteTimeUtc(caminhoDll);
    var dirProjeto = Path.GetDirectoryName(no.CaminhoCsproj)!;

    return !TemFonteMaisRecenteQue(dirProjeto, dllTime);
  }

  private static bool TemFonteMaisRecenteQue(string diretorio, DateTime referenciaUtc)
  {
    IEnumerable<string> arquivos;
    IEnumerable<string> subpastas;
    try
    {
      arquivos = Directory.EnumerateFiles(diretorio);
      subpastas = Directory.EnumerateDirectories(diretorio);
    }
    catch (UnauthorizedAccessException)
    {
      return false;
    }

    foreach (var arquivo in arquivos)
    {
      var ext = Path.GetExtension(arquivo);
      if (ExtensoesRelevantes.Contains(ext, StringComparer.OrdinalIgnoreCase)
          && File.GetLastWriteTimeUtc(arquivo) > referenciaUtc)
      {
        return true;
      }
    }

    foreach (var sub in subpastas)
    {
      if (PastasExcluidas.Contains(Path.GetFileName(sub), StringComparer.OrdinalIgnoreCase)) continue;
      if (TemFonteMaisRecenteQue(sub, referenciaUtc)) return true;
    }

    return false;
  }
}
