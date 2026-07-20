# 09 — Interface de configuração

**Entrega:** `ConfigForm` — perfis e branch por projeto.
**Depende de:** 08.

---

## Layout

```
┌──────────────────────────────────────────────────────────────┐
│ Perfil: [DataPrev ▾]   [Novo]  [Duplicar]  [Excluir]         │
│                                                              │
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
│ ☑ Atualizar repositório pai   ☑ Ignorar projetos de teste    │
│ ☑ Build incremental                                          │
│ Paralelismo git:   [4 ]      Paralelismo build: [0 = auto]   │
│                                                              │
│                                    [ Salvar ]  [ Cancelar ]  │
└──────────────────────────────────────────────────────────────┘
```

Não há seletor de configuração de build: `Debug|AnyCPU` é fixo (I5).

## "Ler projetos"

1. Lê `.gitmodules` e `git submodule status` da pasta raiz e popula a grade;
2. Preenche a coluna Branch com a **branch base**, exibida com o sufixo `(base)` em cinza — indicando herança, não valor fixado. Ao gravar, essas linhas persistem como `Branch: null`;
3. A célula é `ComboBox` editável, carregada **sob demanda** com `git ls-remote --heads origin` daquele submódulo. Digitar é permitido (branch ainda não publicada); escolher da lista evita erro de digitação;
4. Alterar uma célula remove o `(base)` e marca a linha em negrito — dá para ver de relance quais projetos fogem do padrão;
5. **"Restaurar todos para a base"** volta tudo para `null`.

### Regras

- **Ler projetos é opcional.** Sem nunca clicar, a ferramenta roda com a branch base em todos os submódulos. O botão existe para quem precisa do override, não como etapa obrigatória de configuração;
- **Reler não descarta overrides.** Segundo clique reconcilia por nome de submódulo: acrescenta os novos, remove os que sumiram do `.gitmodules`, **preserva** as branches customizadas. Se um submódulo com override for removido do `.gitmodules`, avisar antes de descartar;
- **Abrir a tela não pode disparar rede.** A leitura é local e instantânea; só o `ls-remote` de cada combo vai ao servidor, e apenas quando o combo é aberto. Carregar 16 `ls-remote` ao abrir a tela a tornaria inutilizável em rede lenta.

## Perfis

Um perfil por cliente. `Duplicar` é o caminho prático: clientes compartilham quase toda a configuração e diferem na pasta raiz.

- `Excluir` pede confirmação;
- Nome de perfil é único; nome duplicado bloqueia o salvamento com mensagem;
- `Salvar` grava o `configs.json` inteiro de forma atômica (etapa 02).

## Validação

| Campo | Regra | Momento |
|---|---|---|
| Nome | obrigatório, único | ao salvar |
| Pasta raiz | existe e contém `.gitmodules` | ao sair do campo |
| Branch base | obrigatória, sem espaços | ao salvar |
| Paralelismo git | 1 a 16 | ao digitar |
| Paralelismo build | 0 a 64 (`0` = auto) | ao digitar |

Pasta raiz sem `.gitmodules` é **aviso**, não bloqueio: o desenvolvedor pode estar configurando um perfil antes de clonar o repositório.

## Critérios de aceite

- [ ] "Ler projetos" na DataPrev popula 16 linhas, todas marcadas `(base)`;
- [ ] Salvar sem alterar nenhuma branch grava `Branch: null` em todas — nunca a string `"develop"`;
- [ ] Alterar uma branch, salvar, reabrir e "Ler projetos" de novo **preserva** o override;
- [ ] Trocar a branch base altera todas as linhas `(base)` e **nenhum** override;
- [ ] Abrir a tela não dispara nenhuma chamada de rede;
- [ ] Abrir o combo de um submódulo lista as branches remotas reais;
- [ ] Nome de perfil duplicado bloqueia o salvamento com mensagem clara;
- [ ] Cancelar descarta as alterações sem tocar no `configs.json`.
