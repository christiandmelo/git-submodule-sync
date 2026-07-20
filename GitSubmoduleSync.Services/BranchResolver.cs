using GitSubmoduleSync.Models;

namespace GitSubmoduleSync.Services;

public sealed class BranchResolver
{
  /// <summary>Lista as branches remotas de um submódulo — usado pela UI para popular o combo de branch sob demanda.</summary>
  public async Task<IReadOnlyList<string>> ListarBranchesAsync(string caminhoSubmodulo, CancellationToken ct) =>
    await ListarBranchesRemotasAsync(caminhoSubmodulo, ct) ?? Array.Empty<string>();

  public async Task<BranchResolution> ResolverAsync(
    string caminhoSubmodulo, string branchDesejada, bool overrideExplicito,
    IReadOnlyList<string> fallbacks, CancellationToken ct)
  {
    var submodulo = Path.GetFileName(caminhoSubmodulo.TrimEnd(Path.DirectorySeparatorChar));
    var refsRemotas = await ListarBranchesRemotasAsync(caminhoSubmodulo, ct);

    if (refsRemotas is null)
    {
      return new BranchResolution(submodulo, null, VeioDeFallback: false, CasouPorCase: false, StatusSubmodulo.Erro);
    }

    var (resolvida, casouPorCase) = Casar(branchDesejada, refsRemotas);
    if (resolvida is not null)
    {
      return new BranchResolution(submodulo, resolvida, VeioDeFallback: false, CasouPorCase: casouPorCase, StatusSubmodulo.Pendente);
    }

    // Override explícito não cai no fallback genérico: se o desenvolvedor pediu uma branch
    // específica e ela não existe, cair silenciosamente na base compilaria código diferente
    // do pedido sem que ele soubesse.
    if (!overrideExplicito)
    {
      foreach (var fallback in fallbacks)
      {
        var (resolvidaFallback, casouPorCaseFallback) = Casar(fallback, refsRemotas);
        if (resolvidaFallback is not null)
        {
          return new BranchResolution(submodulo, resolvidaFallback, VeioDeFallback: true, CasouPorCase: casouPorCaseFallback, StatusSubmodulo.Pendente);
        }
      }
    }

    return new BranchResolution(submodulo, null, VeioDeFallback: false, CasouPorCase: false, StatusSubmodulo.BranchNaoEncontrada);
  }

  private static (string? Resolvida, bool CasouPorCase) Casar(string branch, IReadOnlyList<string> refsRemotas)
  {
    var exata = refsRemotas.FirstOrDefault(r => string.Equals(r, branch, StringComparison.Ordinal));
    if (exata is not null) return (exata, false);

    var porCase = refsRemotas.FirstOrDefault(r => string.Equals(r, branch, StringComparison.OrdinalIgnoreCase));
    if (porCase is not null) return (porCase, true);

    return (null, false);
  }

  private static async Task<IReadOnlyList<string>?> ListarBranchesRemotasAsync(string caminhoSubmodulo, CancellationToken ct)
  {
    var resultado = await GitProcessRunner.ExecutarAsync(caminhoSubmodulo, "ls-remote --heads origin", ct);
    if (!resultado.Sucesso) return null;

    // cada linha: "<sha>\trefs/heads/<nome-da-branch>"
    return resultado.Saida
      .Split('\n', StringSplitOptions.RemoveEmptyEntries)
      .Select(linha => linha.Split('\t', StringSplitOptions.TrimEntries))
      .Where(partes => partes.Length == 2 && partes[1].StartsWith("refs/heads/", StringComparison.Ordinal))
      .Select(partes => partes[1]["refs/heads/".Length..])
      .ToList();
  }
}
