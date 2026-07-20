# 06 — Orquestrador de build

**Entrega:** `MsBuildService` + `BuildOrchestrator` — build paralelo em ondas.
**Depende de:** 04, 05.
**Protótipo validado:** [`tools/Build-Waves.ps1`](../tools/Build-Waves.ps1)

---

É o núcleo da ferramenta e onde está o ganho de tempo. Também é onde estão as duas armadilhas que só apareceram na execução real — I2 e I3.

## Contrato

```csharp
public sealed class MsBuildService {
  Task<ResultadoProjeto> CompilarAsync(
    ProjectNode no, string msbuild,
    IProgress<LogEvent> log, CancellationToken ct);
}

public sealed record ResultadoProjeto(
  string Assembly, StatusProjeto Status, TimeSpan Duracao,
  int ExitCode, IReadOnlyList<string> Erros);

public sealed class BuildOrchestrator {
  Task<ResultadoExecucao> ExecutarAsync(
    Grafo grafo, SyncProfile perfil, string msbuild,
    bool ignorarIncremental,
    IProgress<LogEvent> log, IProgress<ProgressoBuild> progresso,
    CancellationToken ct);
}

public sealed record ProgressoBuild(int OndaAtual, int TotalOndas, int Concluidos, int Total);
```

## Linha de comando

```
MSBuild.exe <csproj>
  /t:Build
  /p:Configuration=Debug
  /p:Platform=AnyCPU
  /p:BuildProjectReferences=false
  /nologo /verbosity:minimal /m:1 /clp:NoSummary
```

Cada parâmetro tem motivo. Nenhum é decorativo.

### I2 — `/p:BuildProjectReferences=false` é obrigatório

**Sem essa flag o build paralelo falha de forma intermitente.** Foi o achado mais importante da validação e não é intuitivo.

Por padrão, cada invocação do MSBuild compila também, em processo, todas as `ProjectReference` transitivas do alvo. Como o orquestrador dispara vários projetos da mesma onda ao mesmo tempo, e projetos distintos compartilham dependências, **dois ou mais processos acabam escrevendo no mesmo `obj\Debug\`**:

```
error MSB3491: Não foi possível gravar ... IService.csproj.CoreCompileInputs.cache
               porque ele está sendo usado por outro processo
error MSB3554: Não é possível gravar no arquivo de saída ... Properties.Resources.resources
               porque ele está sendo usado por outro processo
```

Medido: **10 projetos falharam e 8 ficaram bloqueados**, inclusive o `Plugin`. Com a flag, os mesmos 158 compilaram **100% limpos**.

A flag é segura exatamente porque a ordenação topológica já garante que toda dependência foi compilada numa onda anterior — o orquestrador assume uma responsabilidade que estava duplicada no MSBuild. Como efeito colateral, elimina o trabalho redundante de recompilar as mesmas dependências dezenas de vezes.

**Este é um bug que só aparece em máquina rápida e sob carga.** Sem esta seção, quem implementar vai removê-la por parecer supérflua e passar dias atrás de um "build instável".

### I3 — nunca `/t:Rebuild`

Pelo mesmo motivo, mais um: como **todos os projetos compartilham o `Bin\Custom` como `OutputPath`**, o `Clean` implícito do `Rebuild` apaga saídas de outros projetos.

O botão **"Rebuild completo"** da UI **não** mapeia para `/t:Rebuild`. Significa *"ignore a lógica incremental da ferramenta"* (`ignorarIncremental: true`) e continua chamando `/t:Build`. A tradução ingênua de "rebuild" para `/t:Rebuild` reintroduz a falha.

### `/m:1`

O paralelismo é do orquestrador, que conhece a topologia. Deixar o MSBuild também paralelizar multiplicaria os processos e voltaria a disputar os mesmos `obj\`.

### I5 — `Debug|AnyCPU` fixo

Sem seletor na UI e sem campo no perfil. `Debug|AnyCPU` grava em `Bin\Custom`; `Release|AnyCPU` grava em `bin\Release\` e **não** alimenta o diretório de onde o grafo se resolve — a segunda onda quebraria por DLL ausente. Release é da pipeline oficial.

## Execução em ondas

```
para cada onda, em ordem:
  bloquear quem depende de algo que falhou ou foi bloqueado
  compilar o restante em paralelo (grau = GrauParalelismoBuild)
  aguardar a onda inteira antes de avançar
```

A barreira entre ondas é o que garante a corretude: nenhum projeto começa antes de suas dependências estarem em disco.

**Paralelismo por semáforo de processos**, não `Parallel.ForEachAsync` sobre trabalho síncrono — cada item é um `Process` externo, e o custo é de I/O e espera, não de CPU gerenciada. Manter no máximo `GrauParalelismoBuild` processos vivos, alimentando a fila conforme cada um termina.

**Sempre redirecionar stdout e stderr**, lendo de forma assíncrona (`ReadToEndAsync`) antes de aguardar a saída. Ler síncrono após `WaitForExit` causa deadlock quando o buffer do pipe enche — e a saída do MSBuild enche.

## Build incremental

Com `BuildIncremental` e sem `ignorarIncremental`, pular um projeto quando **todas** valerem:

1. `<BinCustom>\<AssemblyName>.dll` existe;
2. Nenhum arquivo-fonte do projeto (`.cs`, `.resx`, `.csproj`, `.config`) é mais novo que a DLL;
3. Nenhuma dependência direta foi recompilada nesta execução.

Pular significa **não lançar o processo**. É aí que está o ganho: o build sem alterações leva **79,8s** medidos, quase todos gastos em subir 158 processos MSBuild para cada um concluir que não havia o que fazer. O MSBuild já é incremental; o que custa é o startup.

## Falhas

Projeto que falha marca **toda a sua árvore de dependentes** como `BloqueadoPorDependencia`; os ramos independentes seguem (I6). O resumo separa quatro categorias — `Compilados`, `Pulados (sem alteração)`, `Falharam`, `Bloqueados por dependência` — porque a ação do desenvolvedor é diferente em cada uma.

`ExitCode != 0` é a única definição de falha (I7).

## Log

Cada projeto é um processo próprio e vários rodam juntos: **toda linha precisa do prefixo do projeto**, ou o log vira ruído ilegível. Preencher `LogEvent.Projeto`.

Parsear `error CS\d+` e `warning CS\d+` para alimentar a aba de erros (etapa 10). A cor vem de `NivelLog` (I8), nunca do texto.

## Desempenho medido

DataPrev, 158 projetos, 12 núcleos:

| Cenário | Resultado | Tempo |
|---|---|---|
| Rebuild frio **sem** I2 | 140 OK · **10 falhas** · 8 bloqueados | 159s |
| Rebuild frio **com** I2 | **158/158 OK** | **120s** |
| Build sem alterações | 158/158 OK | 79,8s |

Ondas no build frio: 28,3s · 27,4s · 31,8s · 17,2s · 8,5s · 6,8s.

> **Ao medir, conferir quantas DLLs foram efetivamente regravadas.** A primeira medição da validação deu 120s com apenas 26 das 158 DLLs escritas — o MSBuild havia pulado 132 projetos, e o número foi quase reportado como build frio. O total coincidiu com o do rebuild real por coincidência.

## Critérios de aceite

Sobre a DataPrev completa:

- [ ] 158/158 compilam com `ignorarIncremental: true`, **0** falhas e **0** bloqueios;
- [ ] Contagem de DLLs regravadas em `Bin\Custom` compatível com o número de projetos compilados;
- [ ] Segunda execução sem alterações pula ~158 projetos e leva menos de 30s;
- [ ] Tocar um `.cs` de `RM.Cst.DataPrev.Const` faz recompilar esse projeto **e** seus dependentes, incluindo o `Plugin`;
- [ ] Introduzir erro de compilação num projeto da onda 1 produz falha nele e `BloqueadoPorDependencia` nos dependentes — sem abortar os ramos independentes;
- [ ] Cancelar no meio encerra todos os processos MSBuild em até 2s, sem órfãos;
- [ ] Nenhuma ocorrência de `/t:Rebuild` no código;
- [ ] `BuildProjectReferences=false` presente em toda invocação.
