# Revisão independente — C# Performance Analyzer V4

## Resumo

A implementação V4 atende ao núcleo funcional observado: o build, os 204 testes, a análise de vulnerabilidades e a cobertura passaram. O catálogo mantém `PERF100` como regra V4, a lógica interprocedural está separada do adaptador Roslyn e há testes para alvos conhecidos/desconhecidos, ciclos, bloqueios, materializações, alocações e deduplicação.

Decisão: **changes_requested**.

A aprovação fica bloqueada porque a execução aprovada utilizou o teste `CoverageGapTests.cs` ainda não versionado, enquanto `gates.json` registra somente a revisão `eb2cef38c056006d2c112c33bb063d5086b24686`. Também há inconsistências documentais e uma lacuna na validação automática de rastreabilidade.

## Artefatos examinados

- `spec.md`, `clarifications.md`, `plan.md` e `tasks.md` da feature.
- `.spectro/constitution.md`.
- `gates.json` e a evidência da execução `gate-20261031T003900Z`.
- Packet independente `review-20261031T004829Z` e `traceability.json`.
- Código V4, núcleo interprocedural, catálogo, documentação e suíte de testes.
- Diff da implementação contra `origin/develop`.

## Requisitos verificados

- `FR-001`/`FR-011`: `PERF100` está no catálogo V4, com categoria `Performance`, severidade `High`/warning e catálogo Roslyn dedicado.
- `FR-002`/`FR-008`: o grafo diferencia alvos conhecidos e desconhecidos, preserva fronteiras externas e cobre dispatch virtual, interface e delegate nos testes.
- `FR-003`–`FR-007`: há propagação de operações custosas em loops, alocações, materializações, redundância, recursão e bloqueios/`await`.
- `FR-009`/`FR-010`: a mensagem inclui evidência, dimensão, cadeia, impacto potencial, limitações e não promete ganho medido.
- `NFR-003`/`NFR-005`/`NFR-006`: a lógica está separada do analyzer, compila em `netstandard2.1` e não há chamada de rede no núcleo.
- Gates atuais: build, testes, segurança e cobertura passaram; cobertura nova foi `96,88%` em linhas e `87,64%` em branches.

## Achados

### FINDING-001 — Evidência de gates não corresponde a uma revisão reproduzível

- Severidade: **High**
- Categoria: consistency / scope
- Localização: `gates.json` (`code_revision`), `tests/StrictSharp.Analyzers.Tests/CoverageGapTests.cs`
- Evidência: `gates.json` registra `eb2cef38c056006d2c112c33bb063d5086b24686`, mas `CoverageGapTests.cs` está untracked no working tree e não pertence a essa revisão. A execução `gate-20261031T003900Z` passou com 204 testes, enquanto a revisão registrada não contém o arquivo.
- Impacto: o resultado de cobertura não pode ser reproduzido a partir da revisão declarada; a regra do workflow invalida evidência de código diferente.
- Recomendação: versionar o novo teste, gerar a revisão correspondente e rerodar todos os gates após o commit. Não aprovar com a evidência atual.
- Return stage: `gates`

### FINDING-002 — Evidência documental V4 está desatualizada

- Severidade: **Medium**
- Categoria: consistency
- Localização: `.spectro/features/csharp-performance-analyzer-v4/evidence-v4-validation.md`
- Evidência: o documento registra 193 testes, cobertura antiga e execução com `-AllowDirtyTree`; a execução atual registra 204 testes e `New Branch Coverage 87,64%` em uma execução limpa.
- Impacto: auditoria e rastreabilidade apresentam números e condições diferentes da evidência aprovada atual.
- Recomendação: atualizar o documento com o execution ID `gate-20261031T003900Z`, métricas atuais e referência ao novo teste, depois invalidar qualquer evidência anterior que permaneça apresentada como final.
- Return stage: `implement`

### FINDING-003 — Validação automática de rastreabilidade reporta gaps apesar dos vínculos existentes

- Severidade: **Medium**
- Categoria: consistency / scope
- Localização: `reviews/review-20261031T004829Z/traceability.json`, `.codex/skills/spectro-review/scripts/validate_traceability.ps1`
- Evidência: o script retorna `result: gaps` e lista todos os requisitos sem tarefas vinculadas. Entretanto, `plan.md` contém a matriz requisito–aceite–tarefa e `tasks.md` contém os mesmos IDs na tabela e em seções `### TASK-*`; o parser procura somente seções `## TASK-*`.
- Impacto: a revisão não consegue usar o gate automático como evidência de rastreabilidade completa; isso é uma falha do tooling/formato, não prova de requisito implementado ausente.
- Recomendação: corrigir o parser para aceitar a tabela e níveis de heading usados, ou normalizar o formato dos artefatos, e rerodar a validação. Registrar a correção como tarefa de tooling antes da aprovação final.
- Return stage: `gates`

### FINDING-004 — Caminho de detecção de reentrada está estruturalmente inalcançável

- Severidade: **Low**
- Categoria: architecture / maintainability
- Localização: `src/StrictSharp.Analyzers/Interprocedural/InterproceduralGraphBuilder.cs:28-35`
- Evidência: `Visit` adiciona o método a `analyzed` antes de chamar `visiting.Add`. Uma chamada reentrante retorna em `if (!analyzed.Add(method))`, portanto o bloco que define `containsCycle` quando `!visiting.Add(method)` não é alcançado. Os ciclos atualmente são marcados pelo teste `visiting.Contains(target)` no chamador.
- Impacto: a implementação possui duas estratégias parcialmente sobrepostas; futuras alterações podem depender do ramo morto e deixar a detecção de ciclos inconsistente.
- Recomendação: remover o ramo inalcançável ou reorganizar os conjuntos para que a invariável seja explícita, mantendo teste de ciclo e cobertura correspondente.
- Return stage: `implement`

## Riscos residuais

- O benchmark é sintético e não estabelece custo aceitável universal do analyzer; o risco de explosão do grafo permanece dependente da compilação analisada.
- A cobertura mede branches do código modificado, mas não substitui validação de desempenho em soluções grandes nem profiling em runtime.
- A execução de segurança cobre vulnerabilidades de pacotes; não é uma análise completa de segurança semântica do código analisado.

## Decisão final

**changes_requested**. Os gates técnicos passaram, mas a aprovação deve aguardar a inclusão do teste na revisão versionada, a atualização da evidência V4 e a resolução da inconsistência de rastreabilidade. O achado de arquitetura de baixa severidade pode ser corrigido no mesmo ciclo de implementação.
