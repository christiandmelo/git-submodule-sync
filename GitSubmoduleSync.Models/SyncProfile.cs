namespace GitSubmoduleSync.Models;

public sealed class SyncProfile
{
  public string Nome { get; set; } = "";
  public string PastaRaiz { get; set; } = "";

  public string BranchBase { get; set; } = "develop";
  public List<string> BranchesFallback { get; set; } = new() { "develop", "Develop", "master", "main" };
  public List<ProjetoConfig> Projetos { get; set; } = new();

  public string PastaBinCustom { get; set; } = "";   // vazio = derivar do OutputPath dos csproj
  public string CaminhoMsBuild { get; set; } = "";   // vazio = localizar via vswhere

  public bool AtualizarRepositorioPai { get; set; } = true;
  public bool IgnorarProjetosDeTeste { get; set; } = true;
  public bool BuildIncremental { get; set; } = true;

  public int GrauParalelismoGit { get; set; } = 4;
  public int GrauParalelismoBuild { get; set; }      // 0 = Environment.ProcessorCount
  public List<string> SubmodulosIgnorados { get; set; } = new();
}

public sealed class ProjetoConfig
{
  public string Submodulo { get; set; } = "";
  public string? Branch { get; set; }               // null = herda BranchBase
}
