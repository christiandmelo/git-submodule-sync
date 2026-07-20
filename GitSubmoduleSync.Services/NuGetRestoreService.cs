using System.Diagnostics;
using System.Text;
using GitSubmoduleSync.Models;

namespace GitSubmoduleSync.Services;

public sealed record ResultadoRestore(int Total, int Sucesso, TimeSpan Duracao);

public sealed class NuGetRestoreService
{
  public async Task<ResultadoRestore> RestaurarAsync(
    SyncProfile perfil, string msbuild, IProgress<LogEvent> log, CancellationToken ct)
  {
    var solutions = DescobrirSolutions(perfil.PastaRaiz);
    log.Report(new LogEvent(NivelLog.Info, $"{solutions.Count} solution(s) encontradas para restore."));

    var inicio = Stopwatch.StartNew();
    var grauParalelismo = perfil.GrauParalelismoBuild > 0 ? perfil.GrauParalelismoBuild : Environment.ProcessorCount;
    using var semaforo = new SemaphoreSlim(grauParalelismo);

    var sucesso = 0;
    var tarefas = solutions.Select(async sln =>
    {
      await semaforo.WaitAsync(ct);
      try
      {
        var ok = await RestaurarUmaAsync(sln, msbuild, log, ct);
        if (ok) Interlocked.Increment(ref sucesso);
      }
      finally
      {
        semaforo.Release();
      }
    });

    await Task.WhenAll(tarefas);
    inicio.Stop();

    return new ResultadoRestore(solutions.Count, sucesso, inicio.Elapsed);
  }

  private static async Task<bool> RestaurarUmaAsync(
    string sln, string msbuild, IProgress<LogEvent> log, CancellationToken ct)
  {
    var nome = Path.GetFileNameWithoutExtension(sln);
    var sw = Stopwatch.StartNew();

    var psi = new ProcessStartInfo
    {
      FileName = msbuild,
      Arguments = $"\"{sln}\" /t:restore /p:RestorePackagesConfig=true /nologo /v:quiet",
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      StandardOutputEncoding = Encoding.UTF8,
      StandardErrorEncoding = Encoding.UTF8,
      UseShellExecute = false,
      CreateNoWindow = true,
    };

    try
    {
      using var processo = Process.Start(psi);
      if (processo is null)
      {
        log.Report(new LogEvent(NivelLog.Erro, $"Não foi possível iniciar o MSBuild para restaurar.", Projeto: nome));
        return false;
      }

      var tarefaSaida = processo.StandardOutput.ReadToEndAsync(ct);
      var tarefaErro = processo.StandardError.ReadToEndAsync(ct);
      await processo.WaitForExitAsync(ct);
      var saida = await tarefaSaida;
      var erro = await tarefaErro;

      sw.Stop();

      if (processo.ExitCode == 0)
      {
        log.Report(new LogEvent(NivelLog.Sucesso, $"restaurado em {sw.Elapsed.TotalSeconds:0.0}s", Projeto: nome));
        return true;
      }

      log.Report(new LogEvent(NivelLog.Erro, $"falha no restore (exit code {processo.ExitCode}).", Projeto: nome));
      foreach (var linha in (saida + erro).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
      {
        log.Report(new LogEvent(NivelLog.Detalhe, linha, Projeto: nome));
      }
      return false;
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or IOException)
    {
      log.Report(new LogEvent(NivelLog.Erro, $"erro ao executar o restore: {ex.Message}", Projeto: nome));
      return false;
    }
  }

  public IReadOnlyList<RefExterna> VerificarExternas(Grafo grafo)
  {
    return grafo.Nos
      .SelectMany(n => n.ReferenciasExternas)
      .Where(r => !File.Exists(r.CaminhoAbsoluto))
      .DistinctBy(r => r.CaminhoAbsoluto, StringComparer.OrdinalIgnoreCase)
      .ToList();
  }

  private static List<string> DescobrirSolutions(string pastaRaiz)
  {
    var buildAllSln = Path.Combine(pastaRaiz, "BuildAll.sln");

    return Directory.EnumerateFiles(pastaRaiz, "*.sln", SearchOption.AllDirectories)
      .Where(sln => !string.Equals(sln, buildAllSln, StringComparison.OrdinalIgnoreCase))
      .Where(sln => !ContemSegmento(sln, "node_modules"))
      .ToList();
  }

  private static bool ContemSegmento(string caminho, string segmento) =>
    caminho.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)
      .Any(p => string.Equals(p, segmento, StringComparison.OrdinalIgnoreCase));
}
