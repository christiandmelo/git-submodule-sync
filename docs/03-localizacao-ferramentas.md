# 03 — Localização de ferramentas

**Entrega:** `ToolLocatorService` — encontra o MSBuild e valida o ambiente.
**Depende de:** 01.

---

## Por que é uma etapa própria

O MSBuild é a **única** dependência externa da ferramenta. Ele restaura os pacotes (etapa 05) e compila (etapa 06). Se não for encontrado, nada funciona — e o desenvolvedor precisa saber disso em segundos, não depois de sincronizar 16 repositórios.

## Contrato

```csharp
namespace GitSubmoduleSync.Services;

public sealed class ToolLocatorService {
  public string? LocalizarMsBuild(string? caminhoConfigurado = null);
  public bool GitDisponivel();
  public PreCondicoes Verificar(SyncProfile perfil);
}

public sealed record PreCondicoes(
  string? MsBuild,
  bool Git,
  bool PastaRaizExiste,
  bool GitmodulesExiste) {
  public bool Ok => MsBuild is not null && Git && PastaRaizExiste && GitmodulesExiste;
}
```

## Localização do MSBuild

Ordem de tentativa:

1. `caminhoConfigurado`, se preenchido e o arquivo existir;
2. **`vswhere.exe`**, o caminho suportado pela Microsoft:
   ```
   %ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe
     -latest -products *
     -requires Microsoft.Component.MSBuild
     -find MSBuild\**\Bin\MSBuild.exe
   ```
3. Se `vswhere` não existir ou nada retornar: falhar com mensagem acionável.

**Não** varrer `Program Files` à mão nem chutar caminhos versionados. Na máquina de referência o `vswhere` retorna:

```
C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe
```

Esse caminho contém a edição (`Professional`) e muda em máquinas com Community ou Enterprise. Hard-code aqui quebra na máquina do colega ao lado.

`-products *` é obrigatório: sem ele o `vswhere` ignora Build Tools e edições que não sejam a padrão.

### `dotnet build` não serve

Os 172 projetos da DataPrev são **.NET Framework 4.8 em csproj legado** (`ToolsVersion="14.0"`, namespace `http://schemas.microsoft.com/developer/msbuild/2003`) com `packages.config`. O SDK do .NET não compila esse formato de forma confiável, e `dotnet restore` não entende `packages.config`. O MSBuild do Visual Studio é requisito, não preferência.

## Verificação de pré-condições

Roda **antes** de qualquer trabalho e falha cedo, com mensagem que diz o que fazer:

| Falha | Mensagem |
|---|---|
| MSBuild ausente | "Visual Studio 2022 com o componente MSBuild não foi encontrado. Instale o Visual Studio ou informe o caminho do MSBuild nas configurações do perfil." |
| Git ausente | "O comando 'git' não foi encontrado no PATH. Instale o Git para Windows." |
| Pasta raiz inexistente | "A pasta '{caminho}' não existe. Verifique o perfil." |
| `.gitmodules` ausente | "A pasta '{caminho}' não é um repositório com submódulos ('.gitmodules' não encontrado)." |

A verificação de **referências externas ausentes** (DLLs do produto RM, pacotes NuGet) **não** entra aqui: ela depende do grafo e precisa rodar depois do restore — ver etapa 05.

## Critérios de aceite

- [ ] `LocalizarMsBuild()` retorna um caminho existente na máquina de desenvolvimento;
- [ ] Com `caminhoConfigurado` apontando para arquivo inexistente, cai para o `vswhere` em vez de falhar;
- [ ] `Verificar()` sobre uma pasta sem `.gitmodules` retorna `Ok == false` com a causa correta;
- [ ] Nenhum caminho de Visual Studio aparece hard-coded no código;
- [ ] A verificação completa leva menos de 1 segundo.
