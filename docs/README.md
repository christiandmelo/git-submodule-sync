# Especificação Técnica — GitSubmoduleSync

Documentos de implementação. **A numeração é a ordem de execução**: cada etapa depende das anteriores e termina com uma verificação executável.

O documento de produto — problema, diagnóstico do `.bat` atual, decisões e resultados de validação — é a [ESPECIFICACAO.md](../ESPECIFICACAO.md) na raiz. Estes documentos assumem que aquele foi lido.

---

## Ordem de execução

| # | Documento | Entrega | Depende de |
|---|---|---|---|
| 01 | [Arquitetura](01-arquitetura.md) | Solution + 3 projetos compilando vazios | — |
| 02 | [Modelos e configuração](02-modelos.md) | `SyncProfile`, `ProfilesConfig`, enums, persistência JSON | 01 |
| 03 | [Localização de ferramentas](03-localizacao-ferramentas.md) | `ToolLocatorService` — acha o MSBuild | 01 |
| 04 | [Grafo de dependências](04-grafo-dependencias.md) | `DependencyGraphService` — ondas topológicas | 02, 03 |
| 05 | [Restore NuGet](05-restore.md) | `NuGetRestoreService` | 03 |
| 06 | [Orquestrador de build](06-orquestrador-build.md) | `MsBuildService` + `BuildOrchestrator` | 04, 05 |
| 07 | [Sincronização git](07-git.md) | `BranchResolver` + `GitService` | 02 |
| 08 | [Interface principal](08-ui-principal.md) | `MainForm` — execução e log | 06, 07 |
| 09 | [Interface de configuração](09-ui-configuracao.md) | `ConfigForm` + "Ler projetos" | 08 |
| 10 | [Acabamento](10-acabamento.md) | Cancelamento, aba de erros, resumo, `BuildAll.sln` | 09 |

### Por que esta ordem

O pipeline **headless** vem antes da UI (etapas 04–07), e dentro dele o **grafo e o build vêm antes do git**. Três motivos:

1. **O risco está no grafo e no build**, não na tela. Se a topologia ou o paralelismo não funcionassem, a UI seria trabalho perdido — por isso o protótipo dessas duas etapas já foi executado e validado contra a DataPrev real antes de escrever esta especificação.
2. **Grafo e build são verificáveis sem interface**, por um harness de console rodando sobre uma pasta real. O git não é: exercitá-lo altera o estado dos repositórios do desenvolvedor.
3. **O git é a etapa mais bem compreendida** e a de menor incerteza técnica. Deixá-la para depois não gera retrabalho.

### Marcos

- **Marco 1 — pipeline headless (etapas 01–07).** Um console consegue sincronizar, restaurar e compilar a DataPrev inteira. É aqui que está praticamente todo o valor.
- **Marco 2 — ferramenta utilizável (08–09).** O desenvolvedor abre, escolhe o perfil e clica.
- **Marco 3 — ferramenta confiável (10).** Cancelamento, navegação de erros e relatório.

---

## Invariantes

Regras que valem em **todos** os documentos. Foram derivadas da execução real contra a DataPrev (172 csproj, 16 submódulos) e violá-las reintroduz falhas já observadas.

| # | Invariante | Consequência de violar |
|---|---|---|
| I1 | **Nunca descartar trabalho do desenvolvedor.** Sem `reset --hard`, `clean -fd` ou `checkout --force`, em nenhuma opção de configuração. | Perda de código não commitado. |
| I2 | **Sempre `/p:BuildProjectReferences=false`** nas invocações do MSBuild. | Colisão de `obj\Debug\` entre processos paralelos: 10 falhas intermitentes medidas. |
| I3 | **Nunca `/t:Rebuild`.** | O `Clean` implícito apaga saídas de outros projetos no `Bin\Custom` compartilhado. |
| I4 | **Resolver caminhos com `Path.GetFullPath`, nunca casar texto** de `HintPath`/`OutputPath`. | Perda silenciosa dos 12 projetos do `DataPrev_GVRVFA_win`, que tem aninhamento extra. |
| I5 | **`Debug|AnyCPU` é fixo.** | `Release` grava em `bin\Release\` e não alimenta o `Bin\Custom` de onde o grafo se resolve. |
| I6 | **Falha isolada não aborta a execução.** Marca dependentes como bloqueados e segue. | Um submódulo problemático impede o trabalho em todos os outros. |
| I7 | **Exit code é a fonte da verdade.** Nunca inferir sucesso do texto de saída. | Falhas silenciosas. |
| I8 | **Cor de log vem de enum, nunca de comparação de texto.** | Mudar a redação de uma mensagem altera a cor sem ninguém perceber (bug real do TestCoverageUI). |
| I9 | **Sem auto-update, sem lock de execução.** | Escopo que pertence à ferramenta centralizadora futura. |

---

## Convenções de código

- **.NET 8**, Windows Forms, `Nullable` e `ImplicitUsings` habilitados;
- **Indentação de 2 espaços**;
- **pt-BR** em UI, logs, mensagens de exceção, comentários e mensagens de commit;
- Nomes de método podem misturar português e inglês, seguindo o padrão do arquivo em edição — não normalizar;
- Versão única em `Directory.Build.props`;
- Nenhuma dependência de rede além do git e do NuGet.

## Protótipos de referência

Validados contra a DataPrev real. Servem de referência de algoritmo — não de estilo, por serem PowerShell:

- [`tools/Test-Graph.ps1`](../tools/Test-Graph.ps1) — parser e ordenação topológica (etapa 04);
- [`tools/Restore-All.ps1`](../tools/Restore-All.ps1) — restore por solution (etapa 05);
- [`tools/Build-Waves.ps1`](../tools/Build-Waves.ps1) — orquestração em ondas com paralelismo (etapa 06).
