# 02 — Modelos e configuração

**Entrega:** tipos de dados e persistência do `configs.json`.
**Depende de:** 01.

---

## Perfil

```csharp
namespace GitSubmoduleSync.Models;

public sealed class SyncProfile {
  public string Nome { get; set; } = "";
  public string PastaRaiz { get; set; } = "";

  public string BranchBase { get; set; } = "develop";
  public List<string> BranchesFallback { get; set; } = new() { "develop", "Develop", "master", "main" };
  public List<ProjetoConfig> Projetos { get; set; } = new();

  public string PastaBinCustom { get; set; } = "";   // vazio = derivar do OutputPath dos csproj
  public string CaminhoMsBuild { get; set; } = "";   // vazio = localizar via vswhere

  public bool AtualizarRepositorioPai { get; set; } = true;
  public bool IgnorarProjetosDeTeste { get; set; } = true;
  public bool BuildIncremental { get; set; } = true;

  public int GrauParalelismoGit { get; set; } = 4;
  public int GrauParalelismoBuild { get; set; }      // 0 = Environment.ProcessorCount
  public List<string> SubmodulosIgnorados { get; set; } = new();
}

public sealed class ProjetoConfig {
  public string Submodulo { get; set; } = "";
  public string? Branch { get; set; }               // null = herda BranchBase
}
```

### Regras de branch

`Branch = null` significa **"herda a `BranchBase`"** — não é uma cópia do valor. Trocar a `BranchBase` de `develop` para `release/2602` passa a valer para todos os projetos não sobrescritos, sem reler nada.

`Projetos` vazio ou ausente é estado válido: a execução usa `BranchBase` para todos. Popular a lista é opcional (etapa 09), nunca pré-requisito.

### Campos derivados

| Campo | Quando vazio |
|---|---|
| `PastaBinCustom` | Resolvido do `OutputPath` real dos csproj (etapa 04). **Não presumir `..\..\..\Bin\Custom\`** — ver I4. |
| `CaminhoMsBuild` | Localizado via `vswhere` (etapa 03). |
| `GrauParalelismoBuild` | `Environment.ProcessorCount`. |

Campos derivados **não** são gravados de volta no JSON quando resolvidos. Persistir o valor derivado congelaria um caminho que pode mudar com uma atualização do Visual Studio.

## Configuração

```csharp
public sealed class ProfilesConfig {
  public string PerfilAtivo { get; set; } = "";
  public List<SyncProfile> Perfis { get; set; } = new();

  public static string CaminhoArquivo =>
    Path.Combine(AppContext.BaseDirectory, "configs.json");

  public static ProfilesConfig Carregar();   // nunca lança; arquivo ausente ou corrompido => instância vazia
  public void Salvar();
  public SyncProfile? ObterAtivo();
}
```

`Carregar()` **nunca lança**. O TestCoverageUI chama `File.ReadAllText` sem proteção, assumindo que um `EnsureConfigExists()` rodou antes no `Program.Main` — acoplamento implícito que quebra se a ordem de inicialização mudar. Aqui, arquivo ausente, JSON inválido ou permissão negada retornam uma configuração vazia e registram o motivo; a UI mostra a tela de configuração.

Escrita **atômica**: grava em `configs.json.tmp` e usa `File.Move(tmp, destino, overwrite: true)`. Uma queda no meio da escrita não pode deixar o arquivo truncado.

## Tipos de execução

```csharp
public enum NivelLog { Detalhe, Info, Sucesso, Aviso, Erro }

public sealed record LogEvent(
  NivelLog Nivel,
  string Mensagem,
  string? Projeto = null,     // prefixo obrigatório quando vem de build paralelo
  string? Submodulo = null);

public enum StatusProjeto { Pendente, Compilando, Compilado, PuladoSemAlteracao, Falhou, BloqueadoPorDependencia }

public enum StatusSubmodulo { Pendente, Sincronizado, Pulado, BranchNaoEncontrada, WorkingTreeSujo, DivergenciaDeBranch, Erro }
```

**`NivelLog` é a única fonte da cor** (I8). A UI mapeia enum → cor e nunca inspeciona o texto. No TestCoverageUI a cor vem de comparação de trechos da mensagem, o que faz uma mudança de redação alterar silenciosamente a aparência do log.

## Resultados

```csharp
public sealed record ResultadoEtapa(string Nome, bool Sucesso, TimeSpan Duracao, string? Detalhe = null);

public sealed record ResultadoExecucao(
  IReadOnlyList<ResultadoEtapa> Etapas,
  int Compilados, int Pulados, int Falharam, int Bloqueados,
  TimeSpan Total) {
  public bool Sucesso => Falharam == 0 && Bloqueados == 0;
}
```

## Critérios de aceite

- [ ] `ProfilesConfig.Carregar()` com arquivo inexistente retorna instância vazia, sem exceção;
- [ ] `Carregar()` com JSON corrompido retorna instância vazia e registra o motivo, sem exceção;
- [ ] Round-trip: `Salvar()` seguido de `Carregar()` preserva todos os campos, inclusive `Branch = null`;
- [ ] `Branch = null` serializa como `null` no JSON — **não** como string vazia nem omitido;
- [ ] Interromper o processo durante `Salvar()` nunca deixa `configs.json` truncado.
