namespace GitSubmoduleSync.Services;

public enum TipoRefExterna { BinProdutoRM, PacoteNuGet }

public sealed record RefExterna(string Nome, string CaminhoAbsoluto, TipoRefExterna Tipo);

public sealed record ProjectNode(
  string CaminhoCsproj,
  string Submodulo,
  string AssemblyName,
  string OutputPathAbsoluto,
  bool EhProjetoDeTeste,
  IReadOnlyList<string> ProjectReferences,   // caminhos absolutos de csproj
  IReadOnlyList<RefExterna> ReferenciasExternas);

public sealed record Grafo(
  IReadOnlyList<ProjectNode> Nos,
  IReadOnlyList<IReadOnlyList<ProjectNode>> Ondas,
  IReadOnlyDictionary<string, IReadOnlyList<string>> Arestas,   // assembly -> assemblies
  IReadOnlyList<string> Ciclos,
  IReadOnlyList<string> Avisos,
  string BinCustomResolvido);
