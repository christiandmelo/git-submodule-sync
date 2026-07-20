# 10 — Acabamento

**Entrega:** aba de erros navegável, aba de ordem de build, resumo, log em arquivo, `BuildAll.sln` opcional.
**Depende de:** 09.

---

São os itens que separam uma ferramenta que funciona de uma em que se confia. Nenhum é essencial para compilar; todos são essenciais para adoção.

## Aba "Erros"

Só as linhas `error CS\d+`, agrupadas por projeto, com contador no título da aba.

**Duplo clique abre o arquivo na linha:**

```
devenv /edit "<arquivo>" /command "edit.goto <linha>"
```

Se o Visual Studio não estiver aberto, `devenv /edit` reaproveita a instância existente ou abre uma nova. Sem VS instalado a ferramenta não funcionaria de todo modo (etapa 03), então não há caminho alternativo a manter.

Ordenar por onda e depois por projeto: os erros da onda mais baixa são normalmente a **causa**, e os das ondas seguintes, consequência. Mostrar o efeito antes da causa faz o desenvolvedor perseguir o erro errado.

## Aba "Ordem de build"

`TreeView` com as ondas e seus projetos, cada um com ícone de status (`Compilado`, `Pulado`, `Falhou`, `Bloqueado`). Selecionar um projeto mostra suas dependências diretas.

É o que torna o grafo **auditável**. Sem isso, "por que o Plugin não compilou?" não tem resposta na ferramenta — e a desconfiança no grafo automático é justamente o que a substituição do `.bat` precisa vencer.

## Aba "Resumo"

Relatório final por etapa, no espírito do que o `.bat` já faz bem:

```
====================================================================
   RESUMO DA EXECUÇÃO — perfil DataPrev
====================================================================
   Repositório pai .................. 3s
   Sincronização (16 submódulos) ..... 42s
   Restore NuGet (14 solutions) ...... 18s
   Grafo (158 projetos, 6 ondas) ..... 0,4s
   Build ............................. 120s
--------------------------------------------------------------------
   Compilados ........ 158     Pulados ....... 0
   Falharam .......... 0       Bloqueados .... 0
--------------------------------------------------------------------
   TEMPO TOTAL ....................... 3min 3s
====================================================================
```

Botão **"Copiar resumo"** — vai para chamado e para conversa com o time.

## Log em arquivo

Todas as execuções em `<PastaRaiz>\.gss-cache\logs\<timestamp>.log`, com cabeçalho contendo perfil, pasta raiz, `Bin\Custom` resolvido, branch base e versão da ferramenta.

O cabeçalho é o que permite responder "o que foi compilado aqui por último?" quando um ambiente ficar estranho — informação, não bloqueio (I9).

Reter os últimos 20 arquivos. Adicionar `.gss-cache/` ao `.gitignore` do repositório pai; sem isso o cache e os logs aparecem como working tree sujo e a etapa 07 passa a pular submódulos.

## `BuildAll.sln` — opcional

Deixou de ser necessário para compilar, mas segue útil para abrir tudo no Visual Studio. Manter como **ação separada** no menu, nunca automática.

Ao gerar, escrever as seções `ProjectDependencies` **a partir do grafo real**, o que também conserta a ordem de build dentro do VS:

```
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "RM.Cst.DataPrev.Plugin", "...", "{GUID}"
	ProjectSection(ProjectDependencies) = postProject
		{GUID-de-cada-uma-das-14-dependencias-reais} = {mesmo-GUID}
	EndProjectSection
EndProject
```

Diferença essencial em relação ao `.bat`: ele injeta **todos** os projetos como dependência de qualquer coisa com "Plugin" no nome. Aqui vão as 14 dependências reais do grafo, o que preserva o paralelismo do MSBuild dentro do Visual Studio.

Preservar os GUIDs de projetos já presentes ao regravar — trocá-los quebra as configurações de solução do desenvolvedor.

## Critérios de aceite

- [ ] Erro de compilação aparece na aba Erros com contador correto;
- [ ] Duplo clique abre o arquivo na linha certa no Visual Studio;
- [ ] Erros ordenados por onda, causa antes de consequência;
- [ ] `TreeView` mostra as 6 ondas com status por projeto;
- [ ] Selecionar `RM.Cst.DataPrev.Plugin` lista suas 14 dependências;
- [ ] Resumo bate com o observado; "Copiar resumo" produz texto legível;
- [ ] Log gravado com cabeçalho completo; retenção de 20 arquivos funciona;
- [ ] `.gss-cache/` no `.gitignore` e `git status` limpo após execução;
- [ ] `BuildAll.sln` gerado abre no VS 2022 e compila na ordem correta;
- [ ] Regravar o `BuildAll.sln` preserva os GUIDs existentes.
