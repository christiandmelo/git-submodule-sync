# 04 — Grafo de dependências

**Entrega:** `DependencyGraphService` — descobre projetos, monta o grafo e ordena em ondas.
**Depende de:** 02, 03.
**Protótipo validado:** [`tools/Test-Graph.ps1`](../tools/Test-Graph.ps1)

---

## O que este service resolve

A dependência entre submódulos **não** é declarada por `ProjectReference` — o instalador oficial exige que os módulos se referenciem por binário, via `bin\Custom`. O `.bat` atual tenta adivinhar a ordem por nome (`*Plugin*`) e erra: coloca o plugin antes da base, e ao mesmo tempo o faz depender dos 166 outros projetos, serializando o fim do build.

A dependência real **é descobrível**. Todo projeto grava sua DLL em `Bin\Custom` e referencia as dos outros pelo mesmo caminho. Cruzando `AssemblyName` com o nome do arquivo em `HintPath`, o grafo se reconstrói inteiro.

**Validado:** 172 csproj, 0 `HintPath` não resolvido, 0 ciclos, 6 ondas.

## Contrato

```csharp
public sealed record ProjectNode(
  string CaminhoCsproj,
  string Submodulo,
  string AssemblyName,
  string OutputPathAbsoluto,
  bool EhProjetoDeTeste,
  IReadOnlyList<string> ProjectReferences,   // caminhos absolutos de csproj
  IReadOnlyList<RefExterna> ReferenciasExternas);

public sealed record RefExterna(string Nome, string CaminhoAbsoluto, TipoRefExterna Tipo);
public enum TipoRefExterna { BinProdutoRM, PacoteNuGet }

public sealed record Grafo(
  IReadOnlyList<ProjectNode> Nos,
  IReadOnlyList<IReadOnlyList<ProjectNode>> Ondas,
  IReadOnlyDictionary<string, IReadOnlyList<string>> Arestas,   // assembly -> assemblies
  IReadOnlyList<string> Ciclos,
  IReadOnlyList<string> Avisos,
  string BinCustomResolvido);

public sealed class DependencyGraphService {
  Grafo Montar(SyncProfile perfil, IProgress<LogEvent>? log = null);
}
```

## Descoberta dos nós

Varrer `*.csproj` sob `PastaRaiz`, excluindo caminhos que contenham `\bin\`, `\obj\`, `\packages\`, `\.git\`, `\node_modules\`.

> O `.bat` atual usa o regex `'\\(bin|obj|\\.git)\\'`, com escape duplo indevido no `.git`. Não replicar. E `node_modules` importa: o `dataprev_PortalMeuRH_web` contém projetos C++ de uma dependência do `lmdb`.

De cada csproj (namespace `http://schemas.microsoft.com/developer/msbuild/2003`):

| Extrair | XPath | Fallback |
|---|---|---|
| `AssemblyName` | `//AssemblyName` | nome do arquivo sem extensão |
| `OutputPath` de Debug | `//PropertyGroup[contains(@Condition,'Debug')]/OutputPath` | vazio |
| `ProjectReference` | `//ProjectReference/@Include` | — |
| `HintPath` | `//Reference/HintPath` | — |

## Arestas

Duas origens:

1. **`ProjectReference`** → dependência intra-submódulo. Resolver o `Include` para caminho absoluto e casar com o nó correspondente.
2. **`HintPath` que resolve para dentro de `Bin\Custom\`** → pegar o nome do arquivo sem extensão. **Se casar com o `AssemblyName` de outro nó, é uma aresta.** É esta regra que transforma a referência binária entre submódulos em dependência de build.

`HintPath` que resolve para o `Bin\` do produto (raiz, sem `Custom`) ou para uma pasta `packages\` **não vira aresta** — vira `RefExterna`, verificada na etapa 05.

### I4 — resolver caminho, nunca casar texto

```csharp
var abs = Path.GetFullPath(Path.Combine(dirDoCsproj, hintPath));
```

Só depois classificar por `abs`. **Não** testar se o `HintPath` começa com `..\..\..\Bin\Custom\`.

O `DataPrev_GVRVFA_win` tem uma pasta intermediária (`DataPrev_GVRVFA_win\Win\<Projeto>\`) e usa `..\..\..\..\` — quatro níveis. Um match textual perderia os 12 projetos desse submódulo em silêncio, sem erro, sem aviso: eles simplesmente não teriam arestas e cairiam todos na onda 1, compilando fora de ordem.

## Ordenação em ondas

Kahn agrupando por nível: **onda N = todos os nós cujas dependências estão inteiramente em ondas anteriores**. Projetos sem dependência entre si caem na mesma onda e compilam em paralelo (etapa 06).

Resultado medido na DataPrev, sem projetos de teste:

```
Onda 1: 51    Onda 3: 38    Onda 5: 7
Onda 2: 35    Onda 4: 22    Onda 6: 5
```

O `RM.Cst.DataPrev.Plugin` cai na **onda 5 de 6**, com **14 dependências diretas**, sem nenhuma regra de nome. Os demais projetos do mesmo submódulo se espalham conforme suas dependências reais: `Const`, `CustomScript` e `Form` na onda 1; `IServer` na 2; `Server` na 3. É exatamente isso que a heurística por nome não conseguia expressar.

### Ciclos

Se Kahn não drenar o grafo, há ciclo. Reportar os assemblies envolvidos e oferecer execução degradada em ordem alfabética (comportamento equivalente ao de hoje), marcando o resultado como degradado. **Não** abortar silenciosamente.

Na DataPrev não há ciclos — mas outros clientes não foram verificados.

## Projetos de teste

Com `IgnorarProjetosDeTeste` (**padrão ligado**), excluir os csproj cujo `AssemblyName` termine em `.TesteUnitario`, `.TestesUnitarios` ou `.TesteUnitarios` — as três grafias existem na DataPrev. São 14 dos 172.

O motivo vai além de tempo: **12 desses projetos têm `OutputPath = ..\..\..\Bin\`**, a raiz do `Bin` da instalação RM, junto dos binários do produto. Compilá-los despeja DLLs de teste dentro da instalação.

## Avisos de `OutputPath` divergente

Para cada nó **não-teste**, conferir se o `OutputPath` de Debug resolve para o `Bin\Custom` canônico. Quando não resolver, emitir `NivelLog.Aviso` — **sem corrigir o csproj**, que é arquivo versionado do submódulo (I1).

Caso real na DataPrev: `RM.Cst.DataPrev.HoraIdeal.Api` tem `OutputPath = ..\..\bin\Custom\`, dois níveis acima em vez de três, resolvendo para `<PastaRaiz>\bin\Custom\` — dentro da pasta centralizadora. A DLL nunca chega à instalação. É o que explica a pasta `DataPrev\bin\Custom\` com um único arquivo solto.

## Resolução do `Bin\Custom`

`BinCustomResolvido` é o `OutputPath` absoluto **mais frequente** entre os nós não-teste — não uma constante e não um palpite a partir da profundidade de pastas. Na DataPrev, 145 projetos apontam para `C:\RM\Legado\12.1.2602\Bin\Custom` via `..\..\..\`, e 12 (o GVRVFA) chegam ao **mesmo destino** via `..\..\..\..\`. A moda dos caminhos resolvidos acerta; a moda das strings de `OutputPath` erraria.

Exibir o caminho resolvido na UI para conferência do desenvolvedor.

## Cache

Persistir o grafo em `<PastaRaiz>\.gss-cache\graph.json`, chaveado por hash SHA-256 do conjunto `{caminho + LastWriteTimeUtc}` de todos os csproj. Se o hash bater, reaproveitar — evita reparsear 172 XMLs a cada execução.

O `.gss-cache\` deve entrar no `.gitignore` do repositório pai, ou aparecerá como working tree sujo e a etapa 07 passará a pular submódulos.

## Critérios de aceite

Verificado por um harness de console sobre `C:\RM\Legado\12.1.2602\DataPrev`:

- [ ] 172 csproj descobertos (158 com `IgnorarProjetosDeTeste`);
- [ ] **0** `HintPath` de `Bin\Custom` sem projeto correspondente;
- [ ] **0** ciclos; a soma dos tamanhos das ondas é igual ao total de nós;
- [ ] 6 ondas com tamanhos 51/35/38/22/7/5;
- [ ] `RM.Cst.DataPrev.Plugin` na onda 5, com exatamente 14 dependências diretas;
- [ ] Os 12 projetos do `DataPrev_GVRVFA_win` têm arestas — se caírem todos na onda 1, o I4 foi violado;
- [ ] Aviso emitido para `RM.Cst.DataPrev.HoraIdeal.Api`;
- [ ] `BinCustomResolvido == "C:\RM\Legado\12.1.2602\Bin\Custom"`;
- [ ] Segunda execução usa o cache e leva menos de 200 ms.
