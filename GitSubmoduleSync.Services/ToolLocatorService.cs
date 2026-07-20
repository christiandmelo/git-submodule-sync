using System.Diagnostics;
using GitSubmoduleSync.Models;

namespace GitSubmoduleSync.Services;

public sealed record PreCondicoes(
  string? MsBuild,
  bool Git,
  bool PastaRaizExiste,
  bool GitmodulesExiste)
{
  public bool Ok => MsBuild is not null && Git && PastaRaizExiste && GitmodulesExiste;
}

public sealed class ToolLocatorService
{
  // vswhere é um processo externo (~300-600ms de spawn); cachear por instância evita
  // repetir o custo a cada chamada de Verificar() na mesma execução da ferramenta.
  private string? _msbuildViaVsWhereCache;
  private bool _msbuildViaVsWhereResolvido;

  public string? LocalizarMsBuild(string? caminhoConfigurado = null)
  {
    if (!string.IsNullOrWhiteSpace(caminhoConfigurado) && File.Exists(caminhoConfigurado))
    {
      return caminhoConfigurado;
    }

    if (_msbuildViaVsWhereResolvido)
    {
      return _msbuildViaVsWhereCache;
    }

    var vswhere = CaminhoVsWhere();
    if (vswhere is null || !File.Exists(vswhere))
    {
      _msbuildViaVsWhereResolvido = true;
      return _msbuildViaVsWhereCache = null;
    }

    var saida = ExecutarECapturarSaida(
      vswhere,
      "-latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\\**\\Bin\\MSBuild.exe");

    var caminho = saida?
      .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
      .FirstOrDefault();

    _msbuildViaVsWhereResolvido = true;
    return _msbuildViaVsWhereCache = (caminho is not null && File.Exists(caminho) ? caminho : null);
  }

  public bool GitDisponivel()
  {
    try
    {
      using var processo = Process.Start(new ProcessStartInfo
      {
        FileName = "git",
        Arguments = "--version",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
      });
      processo?.WaitForExit();
      return processo?.ExitCode == 0;
    }
    catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or IOException)
    {
      return false;
    }
  }

  public PreCondicoes Verificar(SyncProfile perfil)
  {
    var msbuild = LocalizarMsBuild(perfil.CaminhoMsBuild);
    var git = GitDisponivel();
    var pastaExiste = Directory.Exists(perfil.PastaRaiz);
    var gitmodulesExiste = pastaExiste && File.Exists(Path.Combine(perfil.PastaRaiz, ".gitmodules"));

    return new PreCondicoes(msbuild, git, pastaExiste, gitmodulesExiste);
  }

  public static IReadOnlyList<string> ObterMensagensFalha(PreCondicoes p, string pastaRaiz)
  {
    var mensagens = new List<string>();

    if (p.MsBuild is null)
    {
      mensagens.Add("Visual Studio 2022 com o componente MSBuild não foi encontrado. Instale o Visual Studio ou informe o caminho do MSBuild nas configurações do perfil.");
    }
    if (!p.Git)
    {
      mensagens.Add("O comando 'git' não foi encontrado no PATH. Instale o Git para Windows.");
    }
    if (!p.PastaRaizExiste)
    {
      mensagens.Add($"A pasta '{pastaRaiz}' não existe. Verifique o perfil.");
    }
    else if (!p.GitmodulesExiste)
    {
      mensagens.Add($"A pasta '{pastaRaiz}' não é um repositório com submódulos ('.gitmodules' não encontrado).");
    }

    return mensagens;
  }

  private static string? CaminhoVsWhere()
  {
    var programFilesX86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
    if (string.IsNullOrEmpty(programFilesX86))
    {
      return null;
    }
    return Path.Combine(programFilesX86, "Microsoft Visual Studio", "Installer", "vswhere.exe");
  }

  private static string? ExecutarECapturarSaida(string arquivo, string argumentos)
  {
    try
    {
      using var processo = Process.Start(new ProcessStartInfo
      {
        FileName = arquivo,
        Arguments = argumentos,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
      });
      if (processo is null) return null;

      var saida = processo.StandardOutput.ReadToEnd();
      processo.WaitForExit();
      return saida;
    }
    catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or IOException)
    {
      return null;
    }
  }
}
