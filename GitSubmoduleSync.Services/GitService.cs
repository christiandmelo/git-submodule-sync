using System.Diagnostics;
using System.Text.RegularExpressions;
using GitSubmoduleSync.Models;

namespace GitSubmoduleSync.Services;

public sealed class GitService
{
  private readonly BranchResolver _branchResolver = new();

  public async Task<IReadOnlyList<SubmoduloInfo>> DescobrirAsync(string pastaRaiz, CancellationToken ct)
  {
    var declarados = LerGitmodules(pastaRaiz);
    var status = await LerSubmoduleStatusAsync(pastaRaiz, ct);

    return declarados
      .Select(d => new SubmoduloInfo(
        d.Nome, d.Path, d.Url, d.Branch,
        Inicializado: status.TryGetValue(d.Path, out var s) && s != '-'))
      .ToList();
  }

  public async Task<ResultadoEtapa> AtualizarPaiAsync(string pastaRaiz, IProgress<LogEvent> log, CancellationToken ct)
  {
    var sw = Stopwatch.StartNew();

    var statusResultado = await GitProcessRunner.ExecutarAsync(pastaRaiz, "status --porcelain", ct);
    if (!statusResultado.Sucesso)
    {
      sw.Stop();
      log.Report(new LogEvent(NivelLog.Erro, "não foi possível verificar o status do repositório pai."));
      return new ResultadoEtapa("Repositório pai", false, sw.Elapsed, statusResultado.Erro);
    }
    if (!string.IsNullOrWhiteSpace(statusResultado.Saida))
    {
      sw.Stop();
      log.Report(new LogEvent(NivelLog.Aviso, "repositório pai com alterações não commitadas — pulado (nenhum arquivo local foi tocado)."));
      return new ResultadoEtapa("Repositório pai", false, sw.Elapsed, "WorkingTreeSujo");
    }

    var pull = await GitProcessRunner.ExecutarAsync(pastaRaiz, "pull --ff-only", ct);
    sw.Stop();

    if (pull.Sucesso)
    {
      log.Report(new LogEvent(NivelLog.Sucesso, "repositório pai atualizado."));
      return new ResultadoEtapa("Repositório pai", true, sw.Elapsed);
    }

    log.Report(new LogEvent(NivelLog.Erro, $"falha ao atualizar o repositório pai: {pull.Erro.Trim()}"));
    return new ResultadoEtapa("Repositório pai", false, sw.Elapsed, pull.Erro);
  }

  public async Task<IReadOnlyList<ResultadoSubmodulo>> SincronizarAsync(
    SyncProfile perfil, IProgress<LogEvent> log, CancellationToken ct, IProgress<ProgressoGit>? progresso = null)
  {
    var todos = await DescobrirAsync(perfil.PastaRaiz, ct);
    var alvo = todos
      .Where(s => !perfil.SubmodulosIgnorados.Contains(s.Nome, StringComparer.OrdinalIgnoreCase))
      .ToList();

    using var semaforo = new SemaphoreSlim(perfil.GrauParalelismoGit > 0 ? perfil.GrauParalelismoGit : 4);

    var concluidos = 0;
    var total = alvo.Count;
    progresso?.Report(new ProgressoGit(0, total));

    var tarefas = alvo.Select(async sub =>
    {
      await semaforo.WaitAsync(ct);
      try
      {
        return await SincronizarUmAsync(perfil, sub, log, ct);
      }
      finally
      {
        semaforo.Release();
        var c = Interlocked.Increment(ref concluidos);
        progresso?.Report(new ProgressoGit(c, total));
      }
    });

    return await Task.WhenAll(tarefas);
  }

  private async Task<ResultadoSubmodulo> SincronizarUmAsync(
    SyncProfile perfil, SubmoduloInfo sub, IProgress<LogEvent> log, CancellationToken ct)
  {
    var caminho = Path.Combine(perfil.PastaRaiz, sub.Caminho);

    if (!sub.Inicializado)
    {
      log.Report(new LogEvent(NivelLog.Info, "submódulo não inicializado — inicializando.", Submodulo: sub.Nome));
      var init = await GitProcessRunner.ExecutarAsync(perfil.PastaRaiz, $"submodule update --init \"{sub.Caminho}\"", ct);
      if (!init.Sucesso)
      {
        log.Report(new LogEvent(NivelLog.Erro, $"falha ao inicializar: {init.Erro.Trim()}", Submodulo: sub.Nome));
        return new ResultadoSubmodulo(sub.Nome, StatusSubmodulo.Erro, null, init.Erro);
      }
    }

    var projetoConfig = perfil.Projetos.FirstOrDefault(p => string.Equals(p.Submodulo, sub.Nome, StringComparison.OrdinalIgnoreCase));
    var overrideExplicito = projetoConfig?.Branch is not null;
    var branchDesejada = projetoConfig?.Branch ?? perfil.BranchBase;

    var resolucao = await _branchResolver.ResolverAsync(caminho, branchDesejada, overrideExplicito, perfil.BranchesFallback, ct);

    if (resolucao.Status == StatusSubmodulo.BranchNaoEncontrada)
    {
      log.Report(new LogEvent(NivelLog.Aviso, $"branch '{branchDesejada}' não encontrada no remoto — submódulo pulado.", Submodulo: sub.Nome));
      return new ResultadoSubmodulo(sub.Nome, StatusSubmodulo.BranchNaoEncontrada, null, branchDesejada);
    }
    if (resolucao.Status == StatusSubmodulo.Erro)
    {
      log.Report(new LogEvent(NivelLog.Erro, "não foi possível consultar o remoto (git ls-remote falhou).", Submodulo: sub.Nome));
      return new ResultadoSubmodulo(sub.Nome, StatusSubmodulo.Erro, null, null);
    }

    if (resolucao.CasouPorCase)
    {
      log.Report(new LogEvent(NivelLog.Aviso, $"branch '{branchDesejada}' não existe no remoto; usando '{resolucao.BranchResolvida}'.", Submodulo: sub.Nome));
    }
    if (resolucao.VeioDeFallback)
    {
      log.Report(new LogEvent(NivelLog.Aviso, $"branch '{branchDesejada}' não existe; usando fallback '{resolucao.BranchResolvida}'.", Submodulo: sub.Nome));
    }

    var branch = resolucao.BranchResolvida!;

    // I1 — nunca descartar trabalho do desenvolvedor: sem alteração não commitada,
    // nunca reset --hard / clean -fd / checkout --force.
    var statusResultado = await GitProcessRunner.ExecutarAsync(caminho, "status --porcelain", ct);
    if (!statusResultado.Sucesso)
    {
      log.Report(new LogEvent(NivelLog.Erro, "não foi possível verificar o status do submódulo.", Submodulo: sub.Nome));
      return new ResultadoSubmodulo(sub.Nome, StatusSubmodulo.Erro, branch, statusResultado.Erro);
    }
    if (!string.IsNullOrWhiteSpace(statusResultado.Saida))
    {
      var arquivos = string.Join(", ", statusResultado.Saida.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(5));
      log.Report(new LogEvent(NivelLog.Erro, $"working tree com alterações não commitadas — pulado: {arquivos}", Submodulo: sub.Nome));
      return new ResultadoSubmodulo(sub.Nome, StatusSubmodulo.WorkingTreeSujo, branch, statusResultado.Saida);
    }

    var fetch = await GitProcessRunner.ExecutarAsync(caminho, "fetch origin --prune", ct);
    if (!fetch.Sucesso)
    {
      log.Report(new LogEvent(NivelLog.Erro, $"falha no fetch: {fetch.Erro.Trim()}", Submodulo: sub.Nome));
      return new ResultadoSubmodulo(sub.Nome, StatusSubmodulo.Erro, branch, fetch.Erro);
    }

    var checkout = await GitProcessRunner.ExecutarAsync(caminho, $"checkout \"{branch}\"", ct);
    if (!checkout.Sucesso)
    {
      log.Report(new LogEvent(NivelLog.Erro, $"falha no checkout de '{branch}': {checkout.Erro.Trim()}", Submodulo: sub.Nome));
      return new ResultadoSubmodulo(sub.Nome, StatusSubmodulo.Erro, branch, checkout.Erro);
    }

    var pull = await GitProcessRunner.ExecutarAsync(caminho, $"pull --ff-only origin \"{branch}\"", ct);
    if (!pull.Sucesso)
    {
      log.Report(new LogEvent(NivelLog.Erro, $"divergência de branch (pull --ff-only falhou) em '{branch}': {pull.Erro.Trim()}", Submodulo: sub.Nome));
      return new ResultadoSubmodulo(sub.Nome, StatusSubmodulo.DivergenciaDeBranch, branch, pull.Erro);
    }

    log.Report(new LogEvent(NivelLog.Sucesso, $"sincronizado em '{branch}'.", Submodulo: sub.Nome));
    return new ResultadoSubmodulo(sub.Nome, StatusSubmodulo.Sincronizado, branch, null);
  }

  // --- .gitmodules ---

  private sealed record SubmoduloDeclarado(string Nome, string Path, string Url, string? Branch);

  private static readonly Regex RegexCabecalho = new(@"^\[submodule\s+""(?<nome>[^""]+)""\]\s*$", RegexOptions.Compiled);
  private static readonly Regex RegexChaveValor = new(@"^\s*(?<chave>\w+)\s*=\s*(?<valor>.+?)\s*$", RegexOptions.Compiled);

  private static List<SubmoduloDeclarado> LerGitmodules(string pastaRaiz)
  {
    var caminho = Path.Combine(pastaRaiz, ".gitmodules");
    if (!File.Exists(caminho)) return new List<SubmoduloDeclarado>();

    var resultado = new List<SubmoduloDeclarado>();
    string? nomeAtual = null;
    string? pathAtual = null;
    string? urlAtual = null;
    string? branchAtual = null;

    void Fechar()
    {
      if (nomeAtual is not null && pathAtual is not null)
      {
        resultado.Add(new SubmoduloDeclarado(nomeAtual, pathAtual, urlAtual ?? "", branchAtual));
      }
    }

    foreach (var linhaBruta in File.ReadAllLines(caminho))
    {
      var cabecalho = RegexCabecalho.Match(linhaBruta);
      if (cabecalho.Success)
      {
        Fechar();
        nomeAtual = cabecalho.Groups["nome"].Value;
        pathAtual = null;
        urlAtual = null;
        branchAtual = null;
        continue;
      }

      var kv = RegexChaveValor.Match(linhaBruta);
      if (kv.Success && nomeAtual is not null)
      {
        switch (kv.Groups["chave"].Value.ToLowerInvariant())
        {
          case "path": pathAtual = kv.Groups["valor"].Value; break;
          case "url": urlAtual = kv.Groups["valor"].Value; break;
          case "branch": branchAtual = kv.Groups["valor"].Value; break;
        }
      }
    }
    Fechar();

    return resultado;
  }

  // --- git submodule status ---

  private static async Task<Dictionary<string, char>> LerSubmoduleStatusAsync(string pastaRaiz, CancellationToken ct)
  {
    var resultado = new Dictionary<string, char>(StringComparer.OrdinalIgnoreCase);
    var status = await GitProcessRunner.ExecutarAsync(pastaRaiz, "submodule status", ct);
    if (!status.Sucesso) return resultado;

    foreach (var linha in status.Saida.Split('\n', StringSplitOptions.RemoveEmptyEntries))
    {
      if (linha.Length < 2) continue;
      var statusChar = linha[0];
      var resto = linha[1..].Trim();
      var partes = resto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      if (partes.Length < 2) continue;
      var path = partes[1];
      resultado[path] = statusChar;
    }

    return resultado;
  }
}
