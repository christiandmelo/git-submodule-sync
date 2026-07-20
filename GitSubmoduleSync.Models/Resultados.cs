namespace GitSubmoduleSync.Models;

public sealed record ResultadoEtapa(string Nome, bool Sucesso, TimeSpan Duracao, string? Detalhe = null);

public sealed record ResultadoExecucao(
  IReadOnlyList<ResultadoEtapa> Etapas,
  int Compilados, int Pulados, int Falharam, int Bloqueados,
  TimeSpan Total)
{
  public bool Sucesso => Falharam == 0 && Bloqueados == 0;
}
