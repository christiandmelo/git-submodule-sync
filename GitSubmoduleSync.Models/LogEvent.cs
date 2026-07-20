namespace GitSubmoduleSync.Models;

public sealed record LogEvent(
  NivelLog Nivel,
  string Mensagem,
  string? Projeto = null,     // prefixo obrigatório quando vem de build paralelo
  string? Submodulo = null);
