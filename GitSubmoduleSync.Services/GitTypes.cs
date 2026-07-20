using GitSubmoduleSync.Models;

namespace GitSubmoduleSync.Services;

public sealed record SubmoduloInfo(
  string Nome, string Caminho, string Url, string? BranchDeclarada, bool Inicializado);

public sealed record BranchResolution(
  string Submodulo, string? BranchResolvida,
  bool VeioDeFallback, bool CasouPorCase, StatusSubmodulo Status);

public sealed record ResultadoSubmodulo(
  string Submodulo, StatusSubmodulo Status, string? BranchResolvida, string? Detalhe);
