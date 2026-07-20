using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using GitSubmoduleSync.Models;

namespace GitSubmoduleSync.Services;

public sealed record ResultadoProjeto(
  string Assembly, StatusProjeto Status, TimeSpan Duracao,
  int ExitCode, IReadOnlyList<string> Erros);

public sealed class MsBuildService
{
  private static readonly Regex RegexErroCs = new(@"\berror CS\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
  private static readonly Regex RegexAvisoCs = new(@"\bwarning CS\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

  public async Task<ResultadoProjeto> CompilarAsync(
    ProjectNode no, string msbuild, IProgress<LogEvent> log, CancellationToken ct)
  {
    var sw = Stopwatch.StartNew();

    var psi = new ProcessStartInfo
    {
      FileName = msbuild,
      Arguments = $"\"{no.CaminhoCsproj}\" /t:Build /p:Configuration=Debug /p:Platform=AnyCPU /p:BuildProjectReferences=false /nologo /verbosity:minimal /m:1 /clp:NoSummary",
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      StandardOutputEncoding = Encoding.UTF8,
      StandardErrorEncoding = Encoding.UTF8,
      UseShellExecute = false,
      CreateNoWindow = true,
    };

    using var processo = Process.Start(psi)
      ?? throw new InvalidOperationException($"Não foi possível iniciar o MSBuild para '{no.AssemblyName}'.");

    using var registroCancelamento = ct.Register(() =>
    {
      try { if (!processo.HasExited) processo.Kill(entireProcessTree: true); } catch { /* processo já pode ter saído */ }
    });

    var erros = new List<string>();
    var tarefaSaida = LerLinhasAsync(processo.StandardOutput, no.AssemblyName, log, erros);
    var tarefaErro = LerLinhasAsync(processo.StandardError, no.AssemblyName, log, erros);

    // Espera sem o CancellationToken: o cancelamento já mata o processo via registro acima,
    // o que faz WaitForExitAsync retornar normalmente. Evita derrubar a leitura assíncrona
    // do stdout/stderr no meio, que causaria deadlock de pipe se lida de forma síncrona depois.
    await processo.WaitForExitAsync(CancellationToken.None);
    await Task.WhenAll(tarefaSaida, tarefaErro);
    sw.Stop();

    ct.ThrowIfCancellationRequested();

    var status = processo.ExitCode == 0 ? StatusProjeto.Compilado : StatusProjeto.Falhou;
    return new ResultadoProjeto(no.AssemblyName, status, sw.Elapsed, processo.ExitCode, erros);
  }

  private static async Task LerLinhasAsync(StreamReader leitor, string assembly, IProgress<LogEvent> log, List<string> erros)
  {
    string? linha;
    while ((linha = await leitor.ReadLineAsync()) is not null)
    {
      if (linha.Length == 0) continue;

      if (RegexErroCs.IsMatch(linha))
      {
        lock (erros) { erros.Add(linha); }
        log.Report(new LogEvent(NivelLog.Erro, linha, Projeto: assembly));
      }
      else if (RegexAvisoCs.IsMatch(linha))
      {
        log.Report(new LogEvent(NivelLog.Aviso, linha, Projeto: assembly));
      }
      else
      {
        log.Report(new LogEvent(NivelLog.Detalhe, linha, Projeto: assembly));
      }
    }
  }
}
