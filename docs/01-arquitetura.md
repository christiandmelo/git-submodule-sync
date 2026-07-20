# 01 — Arquitetura

**Entrega:** solution com 3 projetos compilando vazios.
**Depende de:** nada.

---

## Estrutura

```
GitSubmoduleSync.sln
├── Directory.Build.props            # versão única, Nullable, ImplicitUsings, indentação
├── GitSubmoduleSync.Models\         # net8.0        — sem dependências
├── GitSubmoduleSync.Services\       # net8.0        — referencia Models
└── GitSubmoduleSync.UI\             # net8.0-windows, WinForms — referencia Services + Models
```

Fluxo de dependência estritamente unidirecional: **UI → Services → Models**. `Models` não conhece `Services`; `Services` não conhece `UI`.

**Não existe** projeto `Updater` (ver I9). **Não existe** pasta `Tools/` — o único executável externo é o MSBuild, localizado em tempo de execução (etapa 03), e o restore de NuGet é feito pelo próprio MSBuild (etapa 05).

## Camadas

### `GitSubmoduleSync.Models`

Tipos de dados e persistência de configuração. Sem I/O além da leitura e escrita do `configs.json`. Sem `Process`, sem rede.

### `GitSubmoduleSync.Services`

Toda a lógica. Cada service é uma classe independente, instanciada diretamente — **sem container de injeção de dependência**, seguindo o padrão do TestCoverageUI e proporcional ao tamanho do problema.

Comunicação com a camada de cima exclusivamente por `IProgress<LogEvent>` e retorno de tipos de resultado. Nenhum service referencia `System.Windows.Forms` nem toca em UI.

### `GitSubmoduleSync.UI`

`MainForm` (execução) e `ConfigForm` (perfis). Só orquestra e apresenta: nenhuma regra de negócio, nenhuma chamada a `Process`.

## `Directory.Build.props`

```xml
<Project>
  <PropertyGroup>
    <Version>1.0.0</Version>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <NeutralLanguage>pt-BR</NeutralLanguage>
  </PropertyGroup>
</Project>
```

Versão em **um só lugar**. O TestCoverageUI a duplica entre `.UI.csproj` e `.Services.csproj`, e o desalinhamento entre as duas é fonte de bug lá — não repetir.

## Modelo de concorrência

- Toda a execução roda em `Task` de fundo; a UI nunca bloqueia;
- O paralelismo é do **orquestrador**, não do MSBuild (que recebe `/m:1`) — ver etapa 06;
- `CancellationToken` atravessa toda a cadeia, do botão até o `Process.Kill(entireProcessTree: true)`;
- Progresso e log sobem por `IProgress<LogEvent>`, que faz o marshalling para a thread de UI.

## Critérios de aceite

- [ ] `dotnet build GitSubmoduleSync.sln` compila sem erro nem aviso;
- [ ] `GitSubmoduleSync.UI` abre uma janela vazia e fecha limpo;
- [ ] `Models` não tem referência a `Services` nem a `System.Windows.Forms`;
- [ ] `Services` não tem referência a `UI` nem a `System.Windows.Forms`;
- [ ] A versão aparece uma única vez em toda a árvore (`grep -r "<Version>"` retorna uma linha).
