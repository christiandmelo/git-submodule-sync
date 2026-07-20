# 05 — Restore NuGet

**Entrega:** `NuGetRestoreService` + verificação de referências externas.
**Depende de:** 03.
**Protótipo validado:** [`tools/Restore-All.ps1`](../tools/Restore-All.ps1)

---

## Contrato

```csharp
public sealed class NuGetRestoreService {
  Task<ResultadoRestore> RestaurarAsync(
    SyncProfile perfil, string msbuild,
    IProgress<LogEvent> log, CancellationToken ct);

  IReadOnlyList<RefExterna> VerificarExternas(Grafo grafo);
}

public sealed record ResultadoRestore(int Total, int Sucesso, TimeSpan Duracao);
```

## Sem `nuget.exe`

O próprio MSBuild restaura `packages.config` desde o VS 16.5:

```
MSBuild.exe <solution.sln> /t:restore /p:RestorePackagesConfig=true /nologo /v:quiet
```

`RestorePackagesConfig=true` é obrigatório — sem ele o `/t:restore` só cobre `PackageReference`, e a DataPrev inteira é `packages.config`.

Isso mantém a ferramenta com **uma única dependência externa** (o MSBuild, já obrigatório para compilar): sem pasta `Tools/`, sem `nuget.exe` embarcado, sem download na primeira execução, sem caminho de ferramenta configurável.

As credenciais do feed privado vêm do `NuGet.Config` do perfil do usuário, presente em qualquer máquina com Visual Studio configurado:

```xml
<add key="customizacao-bh"
     value="https://totvstfs.pkgs.visualstudio.com/Customizacao-BH/_packaging/customizacao-bh/nuget/v3/index.json" />
```

A ferramenta **não** cria, edita nem lê esse arquivo. Se o feed não estiver configurado, o restore falha e a mensagem do MSBuild é repassada.

## Descoberta das solutions

Restaurar por **solution**, não por projeto — o `/t:restore` já cobre todos os projetos do `.sln`.

Varrer `*.sln` sob `PastaRaiz`, excluindo:

- `BuildAll.sln` na raiz (artefato do processo antigo, não é de nenhum submódulo);
- qualquer caminho contendo `node_modules` — o `dataprev_PortalMeuRH_web` tem um `.sln` de dependência C++ do `lmdb`.

**A `packages\` fica no nível do `.sln`, que nem sempre é a raiz do submódulo.** No `DataPrev_GVRVFA_win` o `.sln` está em `DataPrev_GVRVFA_win\Win\`, e é lá que a pasta é criada. Localizar por varredura, nunca por convenção de caminho.

## Execução

- **Sempre restaurar**, sem lógica de detecção. Com tudo já restaurado o custo é ~1,2s por solution (medido); a complexidade de decidir quando pular não se paga;
- Paralelizável por submódulo, já que cada um tem sua própria `packages\`;
- Falha de uma solution **não** aborta as demais (I6): registrar e seguir. Os projetos afetados falharão na etapa 06 e serão reportados lá, com o erro real do compilador.

Medido na DataPrev: **14/14 em 17,9s**. O `DataPrev_GVRVFA_win`, único sem `packages\`, baixou 22 pacotes em 8,5s.

## Verificação de referências externas

`VerificarExternas` confere se cada `RefExterna` do grafo existe em disco e reporta as ausentes.

### A ordem importa

**Rodar depois do restore, nunca antes.** Na validação, 63 referências apareceram como ausentes em 9 projetos — e **todas** eram pacotes NuGet ainda não restaurados, nenhuma era erro de configuração. Verificar antes produziria 63 falsos positivos na primeira execução e ensinaria o desenvolvedor a ignorar o aviso.

### O que fazer com o resultado

Referência ausente é `NivelLog.Aviso`, **não** erro fatal. O projeto entra na fila normalmente e, se realmente não compilar, falha na etapa 06 com o erro do compilador — que é mais informativo que um palpite prévio. O aviso existe para dar contexto quando isso acontecer:

```
[GVRVFA.Form] referência não encontrada: RM.Cst.TotvsStore.ImpPlanilhaXls.Form.dll
              esperada em ...\Win\packages\...\lib\net48\
```

## Critérios de aceite

- [ ] 14 solutions descobertas na DataPrev — `BuildAll.sln` e o `.sln` sob `node_modules` fora;
- [ ] Restore completo em menos de 30s com pacotes já em cache;
- [ ] Apagar `DataPrev_GVRVFA_win\Win\packages\` e restaurar recria a pasta com os 22 pacotes;
- [ ] Uma solution com feed inacessível não impede as outras 13;
- [ ] `VerificarExternas` após restore bem-sucedido reporta **0** ausências na DataPrev;
- [ ] Nenhuma referência a `nuget.exe` no código.
