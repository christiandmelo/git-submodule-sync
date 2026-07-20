# Especificação — GitSubmoduleSync

Ferramenta desktop (.NET 8 / Windows Forms) para sincronizar e compilar, de forma automática e paralela, uma pasta centralizadora de customizações RM composta por múltiplos submódulos git.

Referência de arquitetura e experiência: **TestCoverageUI**.

---

## 1. Problema

Hoje, para deixar o ambiente de um cliente compilado, o desenvolvedor precisa:

1. Entrar em cada pasta de projeto (a DataPrev tem **16 submódulos** e **172 `.csproj`**);
2. Selecionar a branch manualmente;
3. Dar `get` / `pull`;
4. Abrir a solution no Visual Studio;
5. Compilar.

O ciclo completo leva **até 30 minutos** e é repetido a cada troca de contexto de cliente.

Um `.bat` (`sync-submodules.bat`, PowerShell embutido) foi criado para reduzir isso, mas resolve só parte do problema.

---

## 2. Diagnóstico do `sync-submodules.bat` atual

O script está no caminho certo conceitualmente, mas tem cinco falhas estruturais que a nova ferramenta precisa resolver — não são bugs de digitação, são decisões de desenho:

| # | Problema | Causa raiz | Efeito |
|---|---|---|---|
| 1 | Exige que a branch já esteja selecionada | `git submodule update --remote` usa a branch declarada no `.gitmodules` e deixa o submódulo em **detached HEAD** no tip remoto. Não faz checkout de branch. | O dev continua fazendo o passo manual que a ferramenta deveria eliminar. `dataprev_PortalMeuRH_win` sequer tem `branch =` no `.gitmodules` — cai no default `master`. |
| 2 | Ordem de build errada (plugin antes da base) | O grafo é inferido por heurística de nome: `$_.Name -like "*Plugin*"`. Só 1 dos 6 projetos do `dataPrev_Plugin_win` casa com esse filtro (`RM.Cst.DataPrev.Plugin`); `Const`, `IServer`, `Server`, `Form`, `CustomScript` do mesmo submódulo ficam de fora e sobem cedo demais. | Erro de compilação por DLL ausente em `Bin\Custom`. |
| 3 | Ordem excessivamente serializada quando o heurístico acerta | Ao injetar `ProjectDependencies`, faz o Plugin depender de **todos** os ~166 projetos restantes, incluindo os de teste unitário. | Mata o paralelismo do MSBuild justamente no fim da fila. |
| 4 | Nenhum restore de NuGet | Existem **13 `packages.config`** na árvore. O script não roda `nuget restore` nem `msbuild /t:Restore`. | Build quebra em `HintPath` de `..\packages\...` num clone novo. |
| 5 | Continua lento | Delega tudo a um `BuildAll.sln` de 172 projetos aberto no VS/MSBuild, sem build incremental e sem controle de paralelismo. | Não ataca a causa dos 30 minutos. |

Observação adicional: o `dotnet sln add` funciona em csproj legado, mas `dotnet build` **não** compila `net48` com `packages.config` de forma confiável. A ferramenta deve usar `MSBuild.exe` do Visual Studio.

---

## 3. Visão do produto

Uma janela. O usuário informa **a pasta centralizadora** e **a branch**. Clica em *Executar*. A ferramenta:

1. Descobre os submódulos;
2. Resolve o nome real da branch em cada um (tratando `develop` / `Develop`);
3. Faz checkout + pull de cada submódulo em paralelo;
4. Restaura pacotes NuGet;
5. Monta o **grafo real de dependências** lendo os `.csproj`;
6. Compila em **ondas paralelas**, respeitando a topologia;
7. Pula o que não mudou (build incremental);
8. Mostra log colorido em tempo real e um resumo com tempos por etapa.

O caminho da pasta e as preferências ficam salvos em perfis nomeados — igual ao `configs.json` do TestCoverageUI, um perfil por cliente (DataPrev, Energisa, Brasfels…).

---

## 4. Arquitetura

Mesma estratificação do TestCoverageUI (`UI → Services → Models`).

```
GitSubmoduleSync.sln
├── GitSubmoduleSync.Models      # SyncProfile, ProfilesConfig, ProjectNode, BuildWave, StepResult
├── GitSubmoduleSync.Services    # GitService, BranchResolver, DependencyGraphService,
│                                # NuGetRestoreService, MsBuildService, BuildOrchestrator,
│                                # ToolLocatorService
└── GitSubmoduleSync.UI          # MainForm, ConfigForm
```

Convenções herdadas: indentação de 2 espaços, `Nullable` e `ImplicitUsings` habilitados, textos/logs/commits em **pt-BR**.

**Sem auto-update.** Diferente do TestCoverageUI, esta ferramenta **não** tem `Updater.exe`, `UpdateService` nem `version.json`. A atualização será responsabilidade de um projeto centralizador de ferramentas, previsto para o futuro. Consequências no desenho:

- Manter a versão num único `Directory.Build.props` — não replicar a duplicação de versão entre `.UI.csproj` e `.Services.csproj` que existe no TestCoverageUI, que é fonte de bug lá;
- Distribuição na Fase 1 é `dotnet publish` + zip manual, sem `Release.ps1`;
- Não introduzir dependência de rede em nada fora do git dos submódulos — o app deve funcionar integralmente offline salvo pelo `fetch`/`pull`.

---

## 5. Modelo de dados

`configs.json`, ao lado do executável (`AppContext.BaseDirectory`).

```json
{
  "PerfilAtivo": "DataPrev",
  "Perfis": [
    {
      "Nome": "DataPrev",
      "PastaRaiz": "C:\\RM\\Legado\\12.1.2602\\DataPrev",
      "BranchBase": "develop",
      "BranchesFallback": ["develop", "Develop", "master", "main"],
      "Projetos": [
        { "Submodulo": "DataPrev_Abono6Dias_win",     "Branch": null },
        { "Submodulo": "DataPrev_PerfisDeAcesso_win", "Branch": null },
        { "Submodulo": "dataPrev_Plugin_win",         "Branch": "Dev_HU02_PerfisDeAcesso" }
      ],
      "PastaBinCustom": "",
      "AtualizarRepositorioPai": true,
      "IgnorarProjetosDeTeste": true,
      "BuildIncremental": true,
      "GrauParalelismoGit": 4,
      "GrauParalelismoBuild": 0,
      "SubmodulosIgnorados": [],
      "CaminhoMsBuild": ""
    }
  ]
}
```

Regras dos campos de branch:

- **`BranchBase`** — a branch que vale para todos os submódulos. É o caso normal e o único campo que o usuário precisa preencher.
- **`Projetos`** — lista materializada pelo botão **"Ler projetos"** (seção 7.1). `Branch: null` significa *"usa a `BranchBase`"* — não é um valor copiado. Assim, trocar a `BranchBase` de `develop` para `release/2602` reflete automaticamente em todos os projetos que não foram sobrescritos, sem precisar reler nada.
- **`Projetos` vazio ou ausente** — a execução funciona normalmente, usando `BranchBase` para todo mundo. Ler os projetos é opcional, nunca pré-requisito.

Campos derivados quando vazios:

- **`PastaBinCustom`** — resolvido a partir do `OutputPath` real dos `.csproj` (`..\..\..\Bin\Custom\`), que a partir de `<Raiz>\<Submodulo>\<Projeto>\` aponta para `C:\RM\Legado\12.1.2602\Bin\Custom`. Ou seja: **o `Bin` da instalação do RM, não um `bin` dentro da pasta centralizadora**. Não presumir — ler do csproj e exibir o caminho resolvido na UI para conferência.
- **`CaminhoMsBuild`** — via `vswhere.exe` (`-latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`).
- **`GrauParalelismoBuild: 0`** — `Environment.ProcessorCount`.

---

## 6. Pipeline de execução

Seis estágios. Cada um reporta início, fim, duração e status; o orquestrador aborta a cadeia se um estágio bloqueante falhar, mas **nunca** aborta o estágio inteiro por causa de um único submódulo/projeto — coleta as falhas e segue.

### 6.1 Estágio 1 — Descoberta

- Valida que `PastaRaiz` contém `.gitmodules`;
- Lê `.gitmodules` + `git submodule status` e monta a lista de submódulos;
- Detecta submódulos não inicializados (prefixo `-` no `submodule status`) e marca para `--init`.

### 6.2 Estágio 2 — Resolução de branch (resolve o problema #1)

Para cada submódulo, resolver o nome **real** da branch antes de qualquer checkout:

```csharp
Task<BranchResolution> ResolverAsync(string caminhoSubmodulo, SyncProfile perfil);
```

Algoritmo:

0. Determinar a branch **desejada**: o `Branch` do submódulo em `Projetos`, se preenchido; senão, a `BranchBase`;
1. `git ls-remote --heads origin` → lista de refs remotas reais;
2. Match **exato** com a branch desejada;
3. Se não achar, match **case-insensitive** (`StringComparer.OrdinalIgnoreCase`) — é isso que resolve `develop` vs `Develop`, comprovado no `DataPrev_PerfisDeAcesso_win`, cujo remoto só tem `origin/Develop`;
4. Se ainda não achar, percorrer `BranchesFallback` na ordem, repetindo 2 e 3. **Exceção:** se a branch veio de um override explícito do usuário, **não** aplicar fallback — pular direto para o passo 5. Silenciosamente compilar `develop` quando o dev pediu `Dev_HU02` é pior do que não compilar;
5. Se nada resolver, marcar o submódulo como **`BranchNaoEncontrada`**, registrar no log em amarelo e **pular só ele** (não abortar a execução).

O resultado carrega o nome exato do ref remoto, que é o usado no checkout. Quando o nome resolvido difere do digitado, logar explicitamente:
`[DataPrev_PerfisDeAcesso_win] branch 'develop' não existe no remoto; usando 'Develop'.`

### 6.3 Estágio 3 — Sincronização git (paralela)

Por submódulo, com `SemaphoreSlim(GrauParalelismoGit)` — o gargalo aqui é rede/servidor Azure DevOps, não CPU; 4 simultâneos é um ponto de partida razoável:

```
git -C <sub> fetch origin --prune
git -C <sub> checkout <branchResolvida>          # cria tracking local se necessário
git -C <sub> pull --ff-only origin <branchResolvida>
```

Regras:

- **Nunca** usar `git submodule update --remote` — é o que produz detached HEAD;
- Se o working tree estiver sujo, **não** fazer checkout: logar em vermelho, listar os arquivos modificados e pular o submódulo. A ferramenta não descarta trabalho do dev;
- `--ff-only`: se o pull exigir merge, reportar e pular. Resolver divergência é decisão humana;
- Antes disso tudo, opcionalmente `git pull` no repositório pai (`AtualizarRepositorioPai`), para trazer submódulos novos.

### 6.4 Estágio 4 — Restore NuGet (resolve o problema #4)

Estágio **obrigatório, não opcional** — a validação mostrou que o `DataPrev_GVRVFA_win` não tinha `packages\` alguma, e seus 12 projetos eram incompiláveis por isso.

**Não é preciso `nuget.exe`.** O próprio MSBuild restaura `packages.config` desde o VS 16.5:

```
MSBuild.exe <solution.sln> /t:restore /p:RestorePackagesConfig=true /nologo /v:quiet
```

Isso elimina do escopo a necessidade de embarcar `nuget.exe` numa pasta `Tools/` — a ferramenta passa a depender de **um único executável externo** (o MSBuild que já é obrigatório), o que simplifica distribuição e reduz a superfície de configuração. As credenciais do feed privado (`customizacao-bh` no Azure DevOps) vêm do `NuGet.Config` do perfil do usuário, já presente em qualquer máquina com Visual Studio configurado.

- Rodar por **solution**, não por projeto — o `/t:restore` já cobre todos os projetos do `.sln`;
- Executar sempre: com tudo restaurado o custo é ~1,2s por solution (medido), barato demais para justificar lógica de detecção;
- Paralelizável por submódulo, já que cada um tem seu próprio `packages\`;
- **A pasta `packages\` fica no nível do `.sln`, que nem sempre é a raiz do submódulo.** No `DataPrev_GVRVFA_win` o `.sln` está em `DataPrev_GVRVFA_win\Win\`, e é lá que a `packages\` é criada. Localizar os `.sln` por varredura, não por convenção de caminho — e ignorar `.sln` dentro de `node_modules\` (o `dataprev_PortalMeuRH_web` tem um, de uma dependência C++ do `lmdb`).

**A pasta `packages\` é do submódulo e não se mexe nela.** Nada de `repositoryPath` compartilhado, nada de `NuGet.config` novo na pasta raiz, nada de reescrever `HintPath`. Os `HintPath` de `..\packages\...` são versionados dentro de cada repositório e alterá-los sujaria o working tree de todos os submódulos — exatamente o que a regra 1 da seção 9 proíbe. O restore é somente leitura sobre a estrutura existente: baixa o que falta, no lugar onde o csproj já espera encontrar.

### 6.5 Estágio 5 — Grafo de dependências (resolve os problemas #2 e #3)

**É o núcleo técnico da ferramenta.** Substitui integralmente o heurístico `*Plugin*` e a geração do `BuildAll.sln`.

```csharp
public sealed record ProjectNode(
  string CaminhoCsproj,
  string Submodulo,
  string AssemblyName,
  string OutputPath,
  bool EhProjetoDeTeste,
  IReadOnlyList<string> DependeDe);   // AssemblyNames

public sealed class DependencyGraphService {
  IReadOnlyList<ProjectNode> Carregar(string pastaRaiz, SyncProfile perfil);
  IReadOnlyList<IReadOnlyList<ProjectNode>> OrdenarEmOndas(IReadOnlyList<ProjectNode> nos);
}
```

**Descoberta dos nós.** Varrer `*.csproj` sob `PastaRaiz`, excluindo `\bin\`, `\obj\`, `\.git\`, `\packages\`. Nota: o regex do script atual (`'\\(bin|obj|\\.git)\\'`) tem escape duplo indevido no `.git` — não replicar.

> **Validado em 20/07/2026** contra os 172 csproj da DataPrev — resultados na seção 12.

**Extração das arestas.** De cada csproj (namespace MSBuild `http://schemas.microsoft.com/developer/msbuild/2003`):

1. `<AssemblyName>` → identidade do nó;
2. `<ProjectReference Include="...">` → aresta direta (dependência **dentro** do submódulo);
3. `<Reference><HintPath>` cujo caminho resolvido cai em `Bin\Custom\` → pega o nome do arquivo sem extensão. **Se esse nome bater com o `AssemblyName` de outro nó do grafo, é uma aresta.** É assim que a dependência entre submódulos — feita via binário por causa do instalador oficial — vira dependência de build explícita.

   ⚠ **Nunca casar `HintPath` por padrão de texto (`..\..\..\Bin\Custom\`).** Sempre resolver para caminho absoluto (`Path.GetFullPath` a partir da pasta do csproj) e só então classificar. O `DataPrev_GVRVFA_win` tem um nível extra de pasta (`DataPrev_GVRVFA_win\Win\<Projeto>\`) e usa `..\..\..\..\` — quatro níveis, não três. Um match textual perderia os 12 projetos desse submódulo inteiro.

   Exemplo real, de `RM.Cst.DataPrev.Plugin.csproj`:
   ```
   ..\..\..\Bin\Custom\RM.Cst.DataPrev.Abono6Dias.Const.dll
   ..\..\..\Bin\Custom\RM.Cst.DataPrev.PerfisDeAcesso.IServer.dll
   ..\..\..\Bin\Custom\RM.Cst.DataPrev.AdmissaoDigital.Server.dll
   ```
   → o Plugin depende de 3 submódulos, **não dos 16**. E fica naturalmente no fim da topologia, sem regra de nome.

4. `HintPath` que resolve para o `Bin\` da instalação (raiz, sem `Custom`) ou para uma pasta `packages\` → dependência **externa**: produto RM instalado ou pacote NuGet. Não vira aresta; vira **pré-condição verificável**. Se o arquivo não existir, reportar com mensagem clara em vez de deixar o MSBuild cuspir centenas de `CS0246`.

   **A verificação roda depois do estágio 4 (restore), nunca antes.** Na validação, 63 referências apareceram como ausentes em 9 projetos — e *todas* eram pacotes NuGet ainda não restaurados, nenhuma era erro de configuração. Verificar antes do restore produziria 63 falsos positivos e treinaria o dev a ignorar o aviso.

**Ordenação.** Kahn, agrupando por nível: onda *N* = todos os nós cujas dependências já estão inteiramente em ondas < *N*. Projetos sem dependência entre si caem na mesma onda e compilam juntos.

**Ciclos.** Se Kahn não drenar o grafo, há ciclo. Reportar o ciclo com os nomes dos assemblies envolvidos e oferecer ao usuário compilar assim mesmo em ordem alfabética (o comportamento de hoje), marcando a execução como degradada.

**Projetos de teste.** Com `IgnorarProjetosDeTeste: true` (**padrão ligado**), excluir do grafo os csproj cujo `AssemblyName` termine em `.TesteUnitario`, `.TestesUnitarios` ou `.TesteUnitarios` (as três grafias existem na DataPrev). São 14 dos 172.

O motivo é mais forte do que só economizar tempo: **12 desses projetos têm `OutputPath = ..\..\..\Bin\`** — a raiz do `Bin` da instalação RM, junto com os binários do produto, e não o `Bin\Custom`. Compilar os testes despeja DLLs de teste dentro da instalação. Ignorá-los por padrão é higiene, não otimização.

**Verificação de `OutputPath` divergente.** Ao montar o grafo, conferir se o `OutputPath` de Debug de cada projeto **não-teste** resolve para o `Bin\Custom` canônico e avisar quando não resolver. Já existe um caso real: `RM.Cst.DataPrev.HoraIdeal.Api` tem `OutputPath = ..\..\bin\Custom\`, que resolve para `<PastaRaiz>\bin\Custom\` — **dentro da pasta centralizadora**, dois níveis acima em vez de três. A DLL nunca chega ao `Bin\Custom` da instalação. É o que explica a pasta `DataPrev\bin\Custom\` com um único arquivo solto. A ferramenta **não corrige** o csproj (é arquivo versionado do submódulo); apenas reporta em amarelo, para o time decidir.

**Cache.** Persistir o grafo em `.gss-cache\graph.json` com o hash (`SHA-256`) do conjunto `caminho + LastWriteTimeUtc` de todos os csproj. Se nada mudou, reaproveitar — evita reparsear 172 XMLs a cada execução.

### 6.6 Estágio 6 — Build paralelo e incremental (resolve o problema #5)

Para cada onda, em ordem; dentro da onda, `Parallel.ForEachAsync` com `MaxDegreeOfParallelism = GrauParalelismoBuild`:

```
MSBuild.exe <csproj>
  /t:Build
  /p:Configuration=Debug /p:Platform=AnyCPU
  /p:BuildProjectReferences=false
  /nologo /verbosity:minimal /m:1
  /clp:NoSummary
```

`/m:1` por processo: o paralelismo é gerenciado pelo orquestrador (que conhece a topologia), não pelo MSBuild.

#### `/p:BuildProjectReferences=false` é obrigatório

**Sem essa flag o build paralelo falha de forma intermitente.** Foi o achado mais importante da validação e não é intuitivo.

Por padrão, cada invocação do MSBuild compila também, em processo, todas as `ProjectReference` transitivas do projeto alvo. Como o orquestrador dispara vários projetos da mesma onda ao mesmo tempo, e projetos distintos compartilham as mesmas dependências, **dois ou mais processos passam a escrever no mesmo `obj\Debug\` do mesmo projeto de dependência** — e um mata o arquivo do outro:

```
error MSB3491: Não foi possível gravar ... IService.csproj.CoreCompileInputs.cache
               porque ele está sendo usado por outro processo
error MSB3554: Não é possível gravar no arquivo de saída ... Properties.Resources.resources
               porque ele está sendo usado por outro processo
```

Na medição, isso derrubou **10 projetos e bloqueou outros 8** — inclusive o `Plugin`. Com a flag ligada, os mesmos 158 projetos compilaram **100% limpos**. A flag é segura precisamente porque a ordenação topológica já garante que toda dependência foi compilada numa onda anterior: o orquestrador assume a responsabilidade que estava duplicada no MSBuild. Como efeito colateral, elimina-se também o trabalho redundante de recompilar as mesmas dependências dezenas de vezes.

#### Nunca usar `/t:Rebuild`

Pelo mesmo motivo, e mais um: como **todos os projetos compartilham o `Bin\Custom` como `OutputPath`**, o `Clean` implícito do `Rebuild` apaga arquivos de saída de outros projetos.

O botão **"Rebuild completo"** da UI, portanto, **não** mapeia para `/t:Rebuild`. Ele significa *"ignore a lógica incremental da ferramenta"* e continua chamando `/t:Build` — apenas sem pular nada. Essa distinção precisa estar clara para quem implementar, porque a tradução ingênua de "rebuild" para `/t:Rebuild` reintroduz a falha.

**`Debug|AnyCPU` é fixo, não configurável.** Não existe seletor de configuração na UI nem campo no perfil. O motivo é estrutural: nos csproj, `Debug|AnyCPU` tem `OutputPath = ..\..\..\Bin\Custom\` e `Release|AnyCPU` tem `OutputPath = bin\Release\`. Como as referências cruzadas entre submódulos são `HintPath` apontando para `Bin\Custom`, **compilar em Release não alimenta o diretório de onde o grafo se resolve** — a segunda onda quebraria por DLL ausente. Release é responsabilidade da pipeline oficial, e esta ferramenta é de ambiente local de desenvolvimento. Deixar isso hard-coded é intencional: um campo configurável aqui só existiria para permitir uma configuração que não funciona.

**Build incremental** (`BuildIncremental: true`) — pular um projeto quando **todas** valerem:

1. `Bin\Custom\<AssemblyName>.dll` existe;
2. Nenhum arquivo-fonte do projeto (`.cs`, `.resx`, `.csproj`, `.config`) é mais novo que a DLL;
3. Nenhuma dependência direta foi recompilada nesta execução.

Isso é o que transforma a segunda execução do dia em segundos. A UI deve ter um botão **"Rebuild completo"** que ignora o incremental — sem isso, o dev não confia na ferramenta.

**Falha em um projeto.** Marcar o projeto e **toda a sua árvore de dependentes** como `Bloqueado`, pular esses, e continuar com os ramos independentes. Ao fim, o resumo separa `Compilados` / `Pulados (sem alteração)` / `Falharam` / `Bloqueados por dependência`.

**Log.** `MSBuild.exe` com `RedirectStandardOutput/Error`, streaming linha a linha. Como cada projeto é um processo próprio e vários rodam juntos, **toda linha precisa ser prefixada com o nome do projeto** ou o log vira ruído. Parsear `error CSxxxx` / `warning CSxxxx` para colorir e alimentar o painel de erros.

Sobre a coloração: no TestCoverageUI o `MainForm` colore comparando trechos de texto das mensagens do service, o que faz mudar um texto alterar silenciosamente as cores. **Não repetir.** As mensagens devem carregar um enum `NivelLog` (`Info`, `Sucesso`, `Aviso`, `Erro`, `Detalhe`) definido no service; a UI só mapeia enum → cor.

### 6.7 `BuildAll.sln` — opcional

O `BuildAll.sln` deixa de ser necessário para compilar. Mas ainda é útil para o dev que quer abrir tudo no Visual Studio. Manter como **ação separada** ("Gerar BuildAll.sln"), agora escrevendo as seções `ProjectDependencies` a partir do grafo real — o que também conserta a ordem de build dentro do VS.

---

## 7. Interface

Janela única, mesmo espírito do TestCoverageUI (log estilo terminal, fundo escuro).

### 7.1 Tela principal

```
┌──────────────────────────────────────────────────────────────┐
│ Perfil: [DataPrev            ▾]  [Configurações…]            │
│ Pasta:  C:\RM\Legado\12.1.2602\DataPrev                      │
│ Branch base: develop      (2 projetos com branch específica) │
│ ☑ Incremental   ☐ Ignorar testes                             │
│                                                              │
│ [ Executar tudo ]  [ Só sincronizar ]  [ Só compilar ]  [■]  │
├──────────────────────────────────────────────────────────────┤
│ Onda 3 de 7  ·  47/172 projetos  ·  02:41 decorrido          │
│ [██████████████░░░░░░░░░░░░░░░░░░░░░░░░]                     │
├──────────────────────────────────────────────────────────────┤
│  Log  │  Erros (3)  │  Ordem de build  │  Resumo             │
│ ──────────────────────────────────────────────────────────── │
│ [PerfisDeAcesso] branch 'develop' não existe; usando 'Develop│
│ [Abono6Dias.Data] compilado em 1,2s                          │
│ [Absenteismo.Form] pulado — sem alterações                   │
│ [Plugin] aguardando: AdmissaoDigital.Server                  │
└──────────────────────────────────────────────────────────────┘
```

A tela principal é de **execução**, não de edição: pasta, branch base e a lista de projetos ficam todas em Configurações. Aqui só se lê o que está valendo e se aperta o botão.

### 7.2 Tela de configurações — branch por projeto

```
┌──────────────────────────────────────────────────────────────┐
│ Nome do perfil: [DataPrev                                  ] │
│ Pasta raiz:     [C:\RM\Legado\12.1.2602\DataPrev ][Procurar] │
│ Branch base:    [develop                                   ] │
│                                                              │
│                                        [ Ler projetos ]      │
│ ┌──────────────────────────────────┬───────────────────────┐ │
│ │ Projeto                          │ Branch                │ │
│ ├──────────────────────────────────┼───────────────────────┤ │
│ │ DataPrev_Abono6Dias_win          │ develop      (base) ▾ │ │
│ │ DataPrev_PerfisDeAcesso_win      │ develop      (base) ▾ │ │
│ │ dataPrev_Plugin_win              │ Dev_HU02_Perfis…    ▾ │ │
│ │ dataprev_PortalMeuRH_win         │ develop      (base) ▾ │ │
│ └──────────────────────────────────┴───────────────────────┘ │
│                            [ Restaurar todos para a base ]   │
│                                                              │
│                                    [ Salvar ]  [ Cancelar ]  │
└──────────────────────────────────────────────────────────────┘
```

Comportamento do **"Ler projetos"**:

1. Lê `.gitmodules` + `git submodule status` da pasta raiz e popula a grade;
2. Preenche a coluna Branch de todo mundo com a **branch base**, exibida com o sufixo `(base)` em cinza — indicando que é herdada, e não fixada. Ao gravar, essas linhas persistem como `Branch: null`;
3. A célula de Branch é um `ComboBox` editável, carregado sob demanda com o resultado de `git ls-remote --heads origin` daquele submódulo. Digitar é permitido (branch que ainda não subiu); escolher da lista evita erro de digitação, que é metade do problema original;
4. Alterar uma célula tira o `(base)` e marca a linha em negrito — dá para ver de relance quais projetos fogem do padrão;
5. **"Restaurar todos para a base"** volta tudo para `null`.

Regras:

- **Ler projetos é opcional.** Sem nunca clicar, a ferramenta roda com a branch base em todos os submódulos. O botão existe para quem precisa do override, não como etapa obrigatória de configuração;
- **Reler não descarta overrides.** Um segundo clique reconcilia por nome de submódulo: acrescenta os novos, remove os que sumiram do `.gitmodules` e **preserva** as branches já customizadas. Se um submódulo com override for removido do `.gitmodules`, avisar antes de descartar;
- A leitura é local (`.gitmodules`) e instantânea; só o `ls-remote` de cada combo vai à rede, e sob demanda — abrir a tela não pode disparar 16 chamadas de rede.

### 7.3 Detalhes de execução que importam

- **Botão de cancelar** ligado a um `CancellationToken` que chega até o `Process.Kill(entireProcessTree: true)`. Um build de 30 minutos sem cancelamento é inaceitável;
- **Aba "Erros"** filtrando só as linhas `error CSxxxx`, com duplo clique abrindo o arquivo na linha (`devenv /edit <arquivo> /command "edit.goto <linha>"`);
- **Aba "Ordem de build"** exibindo as ondas em `TreeView` — é o que torna o grafo auditável e permite ao dev entender por que algo foi bloqueado;
- **Aba "Resumo"** no formato do relatório final do `.bat` atual (tempo por etapa, tempo total), que é uma boa ideia e vale preservar;
- Toda a execução em `Task` de fundo; UI atualizada via `IProgress<LogEvent>`;
- Log completo gravado em `.gss-cache\logs\<timestamp>.log` para anexar em chamado.

---

## 8. Onde o tempo vai ser recuperado

**Medido em 20/07/2026**, DataPrev completa, 158 projetos (sem testes), 12 núcleos:

| Etapa | Tempo medido |
|---|---|
| Restore NuGet (14 solutions) | **17,9s** |
| Build frio — 158 projetos, `/t:Rebuild` | **120s** (2 min) |
| Build sem alterações (MSBuild decide) | **79,8s** |
| **Total, ambiente do zero** | **~2,5 min** |

Tempo por onda no build frio: 28,3s · 27,4s · 31,8s · 17,2s · 8,5s · 6,8s.

Contra os **até 30 minutos** do processo manual, isso é uma redução de aproximadamente **12×** — e sem contar os ~5 minutos de interação humana (selecionar branch, abrir VS) que desaparecem por completo.

**O incremental próprio da ferramenta ainda vale a pena.** Os 79,8s do build sem alterações são quase todos overhead de subir 158 processos MSBuild só para cada um concluir que não havia o que fazer. A lógica de skip por timestamp da seção 6.6 evita a invocação inteira, e é o que deve levar a segunda execução do dia para a casa dos segundos. A medição confirma que o ganho existe e onde ele está.

Meta revisada, agora com base em dado real: **ambiente do zero em ~3 minutos; execução sem alterações abaixo de 30 segundos** após o incremental próprio da Fase 2.

---

## 9. Tratamento de erros — regras invioláveis

1. **Nunca descartar trabalho do desenvolvedor.** Working tree sujo, stash pendente ou divergência de branch ⇒ pular o submódulo e reportar. Sem `reset --hard`, sem `clean -fd`, sem `checkout --force` — em nenhuma circunstância, nem sob opção de configuração.
2. **Falha isolada não derruba a execução.** Um submódulo sem a branch, um projeto que não compila: registrar, marcar dependentes como bloqueados, seguir.
3. **Falhar cedo em pré-condição.** MSBuild ausente, `Bin\` do RM ausente, `nuget.exe` ausente ⇒ mensagem clara antes de começar, não erro de compilação obscuro 10 minutos depois.
4. **Exit code é a fonte da verdade.** Nunca inferir sucesso do texto da saída do MSBuild.
5. **Caminhos com espaço** sempre entre aspas; a raiz `C:\RM\Legado\12.1.2602\` não tem espaço hoje, mas outros clientes terão.
6. **Uma execução por vez** — detalhado na seção 9.1.

### 9.1 Escopo de uma execução

Uma execução compila **um perfil e seus submódulos**, nada além disso. O `GrauParalelismoBuild` paraleliza projetos dentro do mesmo cliente; a ferramenta nunca combina perfis numa mesma execução.

**Sem lock e sem trava de DLL.** Todos os perfis escrevem no mesmo `Bin\Custom` da instalação RM, e sobrescrever as DLLs de outro cliente ao trocar de perfil é comportamento **esperado** — é assim que o dev troca o ambiente de trabalho hoje, e ele sabe o que está fazendo. Então:

- Sem arquivo de lock em `Bin\Custom`;
- Sem `Mutex` global entre instâncias;
- Sem diálogo de confirmação ao rodar um perfil diferente do último;
- Sem limpeza de DLLs de outros clientes, em nenhuma hipótese.

A única serialização é a natural da UI: enquanto uma execução está em andamento, os botões de execução da janela ficam desabilitados. Não é uma trava — é só não deixar o mesmo dev clicar duas vezes.

Para rastreabilidade, o cabeçalho de cada log em `.gss-cache\logs\` grava o perfil, a pasta raiz e o `Bin\Custom` usados. Se um ambiente ficar estranho, dá para ler o log anterior e ver o que foi compilado por último — informação, não bloqueio.

---

## 10. Entrega em fases

**Fase 1 — Núcleo (entrega o grosso do valor)**
Perfis + persistência · resolução de branch com variação · sync git paralelo · grafo de dependências · build em ondas · log colorido em tempo real.

**Fase 2 — Velocidade e confiança**
Build incremental · cache do grafo · restore NuGet · cancelamento · aba de erros navegável · resumo com tempos · "Ler projetos" com override de branch por submódulo.

**Fase 3 — Acabamento**
Geração opcional do `BuildAll.sln` corrigido · exportação do log · atalho para abrir a solution no VS.

O override de branch por projeto está na Fase 2, e não na 1, porque o modelo de dados já o contempla desde o início (`Branch: null` = herda a base): a Fase 1 lê e respeita o campo, só não oferece a tela para editá-lo. Quem precisar do override antes disso consegue pelo `configs.json`.

---

## 11. Decisões tomadas

Registradas aqui porque são restrições de desenho, não preferências — se alguém propuser reverter uma delas depois, o motivo está escrito.

| Tema | Decisão | Motivo |
|---|---|---|
| **Auto-update** | Fora de escopo. Sem `Updater.exe`, sem `version.json`. | Uma ferramenta centralizadora futura vai concentrar a atualização de todo o ferramental do time. Embutir aqui seria trabalho a ser jogado fora. |
| **Escopo de branch** | Branch base para todos + override opcional por projeto, materializado pelo botão "Ler projetos". | Cobre o caso normal com um campo só, sem impedir o time que trabalha com feature branch por módulo. |
| **`packages\`** | Não se mexe. Sem `repositoryPath` compartilhado, sem reescrever `HintPath`. | A pasta e os `HintPath` pertencem ao submódulo e são versionados. Alterá-los sujaria o working tree de todos os repositórios. |
| **Configuração de build** | `Debug|AnyCPU` fixo, sem seletor. | Só `Debug` escreve em `Bin\Custom`, de onde as referências cruzadas se resolvem. Release é da pipeline oficial. |
| **Múltiplos clientes** | Uma execução cobre um perfil e seus submódulos. Sem lock, sem confirmação, sem trava de DLL. | Sobrescrever o `Bin\Custom` ao trocar de cliente é o fluxo normal de trabalho e o dev tem consciência disso. Travar seria atrito sem ganho. |

### Pontos que ainda dependem de dado real

1. **`GrauParalelismoGit: 4`** é um chute inicial. O limite é o Azure DevOps, não a máquina. Instrumentar o tempo do estágio git na Fase 1 e calibrar com número real.
2. **Estimativas da seção 8** são projeções. A Fase 1 deve gravar tempo por onda e por projeto no log, para confirmar (ou corrigir) a meta de 5 min / 1 min antes de a Fase 2 otimizar no lugar errado.
3. ~~**Grafo da DataPrev.**~~ **Validado** — resultados na seção 12.

---

## 12. Resultado da validação do grafo (20/07/2026)

Protótipo do parser executado sobre `C:\RM\Legado\12.1.2602\DataPrev`, 172 csproj. **A premissa central se confirma.**

### Ondas

| | Com testes | Sem testes (padrão) |
|---|---|---|
| Projetos | 172 | 158 |
| Ondas | 7 | **6** |
| Maior onda | 52 | 51 |
| Média por onda | 24,6 | 26,3 |
| **Ciclos** | **nenhum** | **nenhum** |

Os 172 projetos foram **100% ordenados** — a topologia drena por completo, sem ciclo. Com 12 núcleos na máquina de referência e ondas de 51/35/38/22/7/5, o paralelismo é aproveitável em toda a execução: nenhuma onda é tão estreita a ponto de ociosar os núcleos, e nenhuma é serial.

### O problema da ordem do Plugin: resolvido

`RM.Cst.DataPrev.Plugin` cai naturalmente na **onda 5 de 6**, sem nenhuma regra de nome — e os demais projetos do mesmo submódulo se distribuem conforme suas dependências reais:

```
Onda 1: RM.Cst.DataPrev.Const, .CustomScript, .Form
Onda 2: RM.Cst.DataPrev.IServer
Onda 3: RM.Cst.DataPrev.Server
Onda 5: RM.Cst.DataPrev.Plugin
```

Isso mostra por que o heurístico `*Plugin*` do `.bat` falhava: `Const`, `Form` e `CustomScript` **precisam** compilar cedo, e o filtro por nome não os alcançava.

E o Plugin tem **14 dependências diretas**, não 166:

```
Abono6Dias.Const · Abono6Dias.IServer · Absenteismo.IServer
AdmissaoDigital.IServer · AdmissaoDigital.Server · DataPrev.Const
CstValeTransportePorLinha.IServer · LicencaPremio.IServer
LicencaRemunerada.IServer · OpcoesFerias.IServer · OpcoesFerias.IService
PerfisDeAcesso.Const · PerfisDeAcesso.IServer · PerfisDeAcesso.IService
```

O `.bat` fazia o Plugin depender de **todos** os outros projetos, o que o empurrava para uma onda final solitária e serializava o fim do build.

### Qualidade da resolução

**Zero** `HintPath` de `Bin\Custom` ficou sem projeto correspondente. Ou seja: **toda referência binária entre submódulos foi resolvida como aresta do grafo.** A convenção de referenciar via `bin\Custom`, imposta pelo instalador oficial, é 100% recuperável por parsing — que é exatamente a aposta desta especificação.

### Achados incidentais (não previstos na spec original)

1. **Profundidade de caminho não é uniforme.** `DataPrev_GVRVFA_win` tem uma pasta `Win\` intermediária e usa `..\..\..\..\`. Corrigido na seção 6.5: resolver caminho absoluto, nunca casar texto.
2. **12 projetos de teste gravam em `Bin\` da instalação RM.** Motivou tornar `IgnorarProjetosDeTeste` padrão ligado (seção 6.5).
3. **`RM.Cst.DataPrev.HoraIdeal.Api` tem `OutputPath` errado** — aponta para dentro da pasta centralizadora. Bug real e pré-existente no csproj do submódulo; a ferramenta reporta, não corrige.
4. **Só 3 de 16 submódulos têm `packages\` restaurada.** As 63 referências externas "ausentes" eram todas isso. Promoveu o restore a estágio obrigatório e moveu a verificação de pré-condições para depois dele (seções 6.4 e 6.5).

---

## 13. Resultado da execução real: restore + build (20/07/2026)

O pipeline completo foi executado de ponta a ponta contra a instalação real.

### Restore

`MSBuild /t:restore /p:RestorePackagesConfig=true` em 14 solutions: **14/14 OK em 17,9s**. O `DataPrev_GVRVFA_win`, único sem `packages\`, baixou 22 pacotes do feed privado em 8,5s. **Nenhum `nuget.exe` foi necessário** — ver seção 6.4.

### Build

| Cenário | Resultado | Tempo |
|---|---|---|
| Build morno (`/t:Build`, estado prévio) | 158/158 OK | 120s |
| Rebuild frio **sem** `BuildProjectReferences=false` | 140 OK · **10 falhas** · 8 bloqueados | 159s |
| Rebuild frio **com** `BuildProjectReferences=false` | **158/158 OK** | **120s** |
| Build sem alterações | 158/158 OK | 79,8s |

157 DLLs regravadas no `Bin\Custom` no rebuild frio, confirmando que o build foi real e não um encadeamento de "up-to-date".

**Zero falhas, zero bloqueios, zero ciclos, na DataPrev inteira.** Incluindo o `RM.Cst.DataPrev.Plugin`, que era o projeto que motivou o pedido.

### O que a execução revelou

1. **`/p:BuildProjectReferences=false` é obrigatório** — sem ele, 10 falhas intermitentes por colisão de `obj\Debug\`. Documentado na seção 6.6. Foi o achado de maior impacto: sem essa medição, a ferramenta seria implementada com um bug de concorrência que só apareceria em máquina rápida e sob carga, e que seria interpretado como "o build é instável".
2. **`nuget.exe` sai do escopo** — o MSBuild restaura `packages.config` sozinho, e a ferramenta passa a ter só uma dependência externa.
3. **Medir build morno como se fosse frio engana.** A primeira medição deu 120s com apenas 26 DLLs regravadas — o MSBuild havia pulado 132 projetos. O número final coincide em 120s, mas por outro motivo. Qualquer benchmark futuro precisa checar quantas DLLs foram efetivamente escritas antes de acreditar no cronômetro.
4. **O gargalo do caso incremental é o startup de processo**, não a compilação: 79,8s para não compilar nada. Justifica a lógica de skip da seção 6.6 com dado, e não com intuição.

### O que ainda não foi validado

- **Estágio git.** Branch resolution e pull paralelo não foram executados — exigiria alterar o estado dos repositórios do usuário. A lógica de `ls-remote` foi verificada por leitura (confirmado que `DataPrev_PerfisDeAcesso_win` só tem `origin/Develop`), mas o checkout em si continua não exercitado.
- **Projetos de teste.** Os 14 foram excluídos, conforme o padrão da ferramenta. Não se sabe se compilam.
- **Uma máquina só.** 12 núcleos. O formato das ondas (maior = 51) sugere que máquinas com menos núcleos escalam pior, mas linearmente — sem cliff.
- **Um cliente só.** DataPrev. Outros clientes podem ter convenções de `OutputPath` ou aninhamento diferentes; o `GVRVFA` já mostrou que variação dentro de um mesmo cliente existe.
