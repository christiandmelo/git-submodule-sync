using System.Diagnostics;
using System.Text.RegularExpressions;

namespace GitSubmoduleSync.Services;

public sealed record ErroCompilacao(string Assembly, int Onda, string? Arquivo, int? Linha, string LinhaCompleta);

public sealed class DevEnvLauncherService
{
  private static readonly Regex RegexLocalizacao = new(
    @"^(?<arquivo>.+?)\((?<linha>\d+)(,\d+)?\):\s*error\s*CS\d+", RegexOptions.Compiled);

  public static (string? Arquivo, int? Linha) ExtrairLocalizacao(string linhaDeErro)
  {
    var m = RegexLocalizacao.Match(linhaDeErro);
    if (!m.Success) return (null, null);
    return (m.Groups["arquivo"].Value.Trim(), int.Parse(m.Groups["linha"].Value));
  }

  public void AbrirArquivoNaLinha(string caminhoArquivo, int linha)
  {
    try
    {
      Process.Start(new ProcessStartInfo
      {
        FileName = "devenv",
        Arguments = $"/edit \"{caminhoArquivo}\" /command \"edit.goto {linha}\"",
        UseShellExecute = true,
      });
    }
    catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or IOException)
    {
      // Visual Studio nao encontrado no PATH — duplo clique e' uma conveniencia, nao critico.
    }
  }
}
