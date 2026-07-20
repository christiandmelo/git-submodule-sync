# 08 — Interface principal

**Entrega:** `MainForm` — execução e log em tempo real.
**Depende de:** 06, 07.

---

## Layout

```
┌──────────────────────────────────────────────────────────────┐
│ Perfil: [DataPrev            ▾]  [Configurações…]            │
│ Pasta:  C:\RM\Legado\12.1.2602\DataPrev                      │
│ Branch base: develop      (2 projetos com branch específica) │
│ Bin\Custom: C:\RM\Legado\12.1.2602\Bin\Custom                │
│ ☑ Incremental   ☐ Ignorar testes                             │
│                                                              │
│ [ Executar tudo ]  [ Só sincronizar ]  [ Só compilar ]  [■]  │
├──────────────────────────────────────────────────────────────┤
│ Onda 3 de 6  ·  47/158 projetos  ·  02:41 decorrido          │
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

**Tela de execução, não de edição.** Pasta, branch base e lista de projetos ficam em Configurações (etapa 09). Aqui só se lê o que está valendo e se aperta o botão.

O **`Bin\Custom` resolvido** aparece em texto: é o caminho para onde as DLLs vão, ele é derivado e não configurado, e o desenvolvedor precisa poder conferir de relance que está compilando para a instalação certa.

## Ações

| Botão | Etapas |
|---|---|
| **Executar tudo** | descoberta → git → restore → grafo → build |
| **Só sincronizar** | descoberta → git |
| **Só compilar** | restore → grafo → build |
| **■** | cancela a execução em andamento |

"Só compilar" é o caso mais frequente no dia a dia — o desenvolvedor já fez o pull pelo Visual Studio e só quer o ambiente compilado.

## Threading

- Execução em `Task` de fundo; a UI nunca bloqueia;
- Log e progresso sobem por `IProgress<LogEvent>` / `IProgress<ProgressoBuild>`, que fazem o marshalling;
- Durante a execução, os botões de ação ficam desabilitados e o de cancelar habilitado. É a única serialização — **sem lock, sem mutex** (I9);
- Cancelamento propaga até `Process.Kill(entireProcessTree: true)`. Um build de vários minutos sem cancelamento é inaceitável.

## Log

Estilo terminal, fundo escuro, monoespaçado. Cor **exclusivamente** por `NivelLog` (I8):

| Nível | Cor |
|---|---|
| `Detalhe` | cinza |
| `Info` | branco |
| `Sucesso` | verde |
| `Aviso` | amarelo |
| `Erro` | vermelho |

Nenhum `if (msg.Contains("..."))` decidindo cor. No TestCoverageUI a cor vem de comparação de trechos do texto, e mudar a redação de uma mensagem no service altera silenciosamente a aparência do log.

**Toda linha vinda do build carrega prefixo de projeto** — vários processos escrevem ao mesmo tempo e sem prefixo o log é inútil.

Buffer limitado (~5.000 linhas em memória) com auto-scroll desligável: o log completo vai para arquivo (etapa 10), a tela não precisa guardar tudo.

## Progresso

Onda atual, projetos concluídos sobre o total e tempo decorrido. A granularidade de **onda** importa: é o que permite ao desenvolvedor estimar quanto falta, já que as ondas finais são bem menores que as iniciais (51 → 5 na DataPrev).

## Estado inicial

1. `ProfilesConfig.Carregar()`;
2. Sem perfis: abrir `ConfigForm` direto — não há nada a executar;
3. Com perfis: selecionar `PerfilAtivo` e verificar pré-condições (etapa 03);
4. Pré-condição falha: mostrar a mensagem e desabilitar as ações. Não deixar clicar em algo que vai falhar.

## Critérios de aceite

- [ ] A janela responde durante uma execução completa (arrastar, redimensionar, trocar de aba);
- [ ] Cancelar encerra todos os processos MSBuild em até 2s, sem órfãos no Gerenciador de Tarefas;
- [ ] Log colore corretamente sem nenhuma comparação de texto no código da UI;
- [ ] Linhas de build sempre prefixadas com o projeto;
- [ ] Primeira execução sem `configs.json` abre a tela de configuração, sem exceção;
- [ ] MSBuild ausente exibe mensagem acionável e desabilita as ações;
- [ ] Progresso avança monotonicamente e chega a 100% ao final.
