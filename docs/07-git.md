# 07 — Sincronização git

**Entrega:** `BranchResolver` + `GitService` — resolve a branch e sincroniza os submódulos.
**Depende de:** 02.

---

Resolve a queixa original: *"ele até faz o get, mas eu tenho que ter selecionado a branch antes"*.

> **Única etapa não exercitada na validação**, por alterar o estado dos repositórios. A lógica de resolução foi conferida por leitura contra a DataPrev real; o checkout não foi executado. Testar em clone descartável antes de rodar no ambiente de trabalho.

## Por que o `.bat` exige branch selecionada

```powershell
git submodule update --init --recursive --remote --progress $sub
```

O `--remote` usa a branch declarada no `.gitmodules` e deixa o submódulo em **detached HEAD** no tip remoto. Não faz checkout de branch — por isso o desenvolvedor continua tendo que selecionar antes. E o `dataprev_PortalMeuRH_win` sequer tem `branch =` no `.gitmodules`: cai no default `master`.

**A ferramenta não usa `git submodule update --remote`.**

## Contrato

```csharp
public sealed class BranchResolver {
  Task<BranchResolution> ResolverAsync(
    string caminhoSubmodulo, string branchDesejada, bool overrideExplicito,
    IReadOnlyList<string> fallbacks, CancellationToken ct);
}

public sealed record BranchResolution(
  string Submodulo, string? BranchResolvida,
  bool VeioDeFallback, bool CasouPorCase, StatusSubmodulo Status);

public sealed class GitService {
  Task<IReadOnlyList<SubmoduloInfo>> DescobrirAsync(string pastaRaiz, CancellationToken ct);
  Task<ResultadoEtapa> AtualizarPaiAsync(string pastaRaiz, IProgress<LogEvent> log, CancellationToken ct);
  Task<IReadOnlyList<ResultadoSubmodulo>> SincronizarAsync(
    SyncProfile perfil, IProgress<LogEvent> log, CancellationToken ct);
}
```

## Resolução de branch

Por submódulo, **antes** de qualquer checkout:

0. Branch desejada = `Branch` do submódulo em `Projetos`, se preenchido; senão `BranchBase`;
1. `git ls-remote --heads origin` → refs remotas reais;
2. Match **exato**;
3. Se falhar, match **case-insensitive** (`OrdinalIgnoreCase`);
4. Se falhar, percorrer `BranchesFallback` repetindo 2 e 3 — **exceto** quando a branch veio de override explícito do usuário;
5. Se nada resolver: `BranchNaoEncontrada`, aviso em amarelo, **pular só este submódulo** (I6).

### O passo 3 é o ponto central

O problema não é digitação — é que **os remotos divergem entre si**. Na DataPrev, `DataPrev_PerfisDeAcesso_win` só tem `origin/Develop`; todos os outros têm `origin/develop`. Sem o match case-insensitive, esse submódulo nunca sincroniza.

Quando o nome resolvido difere do digitado, logar explicitamente:

```
[DataPrev_PerfisDeAcesso_win] branch 'develop' não existe no remoto; usando 'Develop'.
```

### Por que override explícito não usa fallback

Se o desenvolvedor pediu `Dev_HU02_PerfisDeAcesso` e essa branch não existe, cair silenciosamente em `develop` compilaria **código diferente do pedido** sem que ele soubesse. Falhar é melhor: `BranchNaoEncontrada` e o submódulo fica de fora, visível no resumo.

O fallback existe para o caso genérico (`BranchBase` numa lista heterogênea de repositórios), não para sobrescrever uma escolha deliberada.

## Sincronização

Por submódulo, com `SemaphoreSlim(GrauParalelismoGit)` — o gargalo é o servidor Azure DevOps, não a CPU:

```
git -C <sub> fetch origin --prune
git -C <sub> checkout <branchResolvida>     # cria tracking local se necessário
git -C <sub> pull --ff-only origin <branchResolvida>
```

### I1 — nunca descartar trabalho

**Antes do checkout**, verificar `git status --porcelain`. Se houver alteração não commitada: **não fazer checkout**. Registrar em vermelho, listar os arquivos e pular o submódulo com `WorkingTreeSujo`.

`--ff-only` no pull: se exigir merge, reportar `DivergenciaDeBranch` e pular. Resolver divergência é decisão humana.

**Em nenhuma hipótese** `reset --hard`, `clean -fd` ou `checkout --force` — nem sob opção de configuração, nem sob confirmação do usuário. Uma ferramenta que às vezes apaga trabalho deixa de ser usada.

### Repositório pai

Com `AtualizarRepositorioPai`, rodar `git pull` na `PastaRaiz` **antes** dos submódulos, para trazer submódulos recém-adicionados ao `.gitmodules`. Mesma regra de working tree sujo.

### Submódulos não inicializados

`git submodule status` prefixa com `-` os não inicializados. Para esses, `git submodule update --init <sub>` **sem** `--remote`, e só então aplicar o fluxo de checkout normal.

## Descoberta

Ler `.gitmodules` e `git submodule status`. Retornar nome, caminho, URL, branch declarada (pode ser ausente) e estado de inicialização.

Respeitar `SubmodulosIgnorados`.

## Critérios de aceite

Em **clone descartável**, nunca no ambiente de trabalho:

- [ ] 16 submódulos descobertos na DataPrev;
- [ ] `DataPrev_PerfisDeAcesso_win` resolve para `Develop` com `CasouPorCase == true` e loga o aviso;
- [ ] `dataprev_PortalMeuRH_win`, sem `branch` no `.gitmodules`, resolve pela `BranchBase`;
- [ ] Branch inexistente com override explícito → `BranchNaoEncontrada`, **sem** cair no fallback;
- [ ] Branch inexistente vinda da `BranchBase` → tenta os fallbacks na ordem;
- [ ] Submódulo com arquivo modificado é pulado com `WorkingTreeSujo` e **permanece modificado** depois da execução;
- [ ] Submódulo com commits divergentes é pulado com `DivergenciaDeBranch`, sem merge;
- [ ] Após sincronizar, `git rev-parse --abbrev-ref HEAD` retorna o nome da branch — **nunca** `HEAD` (detached);
- [ ] Nenhuma ocorrência de `reset --hard`, `clean -f` ou `checkout --force` no código;
- [ ] Nenhuma ocorrência de `submodule update --remote` no código.
