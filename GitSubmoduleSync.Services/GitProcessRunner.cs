using System.Diagnostics;
using System.Text;

namespace GitSubmoduleSync.Services;

internal sealed record ResultadoGit(int ExitCode, string Saida, string Erro)
{
  public bool Sucesso => ExitCode == 0;
}

internal static class GitProcessRunner
{
  public static async Task<ResultadoGit> ExecutarAsync(string diretorio, string argumentos, CancellationToken ct)
  {
    var psi = new ProcessStartInfo
    {
      FileName = "git",
      Arguments = argumentos,
      WorkingDirectory = diretorio,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      StandardOutputEncoding = Encoding.UTF8,
      StandardErrorEncoding = Encoding.UTF8,
      UseShellExecute = false,
      CreateNoWindow = true,
    };

    using var processo = Process.Start(psi)
      ?? throw new InvalidOperationException("Não foi possível iniciar o git.");

    using var registroCancelamento = ct.Register(() =>
    {
      try { if (!processo.HasExited) processo.Kill(entireProcessTree: true); } catch { }
    });

    var tarefaSaida = processo.StandardOutput.ReadToEndAsync();
    var tarefaErro = processo.StandardError.ReadToEndAsync();
    await processo.WaitForExitAsync(CancellationToken.None);
    var saida = await tarefaSaida;
    var erro = await tarefaErro;

    ct.ThrowIfCancellationRequested();

    return new ResultadoGit(processo.ExitCode, saida, erro);
  }
}
