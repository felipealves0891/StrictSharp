<!-- spectro-managed: true -->
# Review: Modelo de custo computacional V5

## Resumo

A implementação V5 compila, passa os gates obrigatórios e possui cobertura acima dos limites configurados. A revisão independente confirmou o catálogo, a separação básica entre analyzers e serviços, a documentação e a preservação dos contratos anteriores. Permanecem duas lacunas funcionais de alta severidade e uma inconsistência de rastreabilidade.

**Decisão final:** `changes_requested`

## Artefatos examinados

- `spec.md`, `clarifications.md`, `plan.md` e `tasks.md`
- `gates.json` da execução `gate-20261031T142145Z`
- Snapshot independente `reviews/review-20261031T142331Z/`
- Código V5, catálogo, integração interprocedural e testes
- Diff de `origin/main` e estado do repositório

## Consistência da evidência

- O gate aprovado e o snapshot referem-se ao commit `480651e63557c4c956ae7cb44597a19f74181379`.
- Build, testes, segurança e cobertura passaram; foram aprovados 249 testes.
- Cobertura registrada: 97,53% de linhas, 85,45% de branches, 97,24% de linhas novas e 85,26% de branches novos.
- O validador de gates aceitou o relatório.
- O validador de rastreabilidade retornou `gaps`: 26 requisitos ficaram sem tarefas vinculadas porque o script procura tarefas em headings, enquanto a matriz principal está em tabela. `TASK-010` também permanece `pending` apesar do gate aprovado.

## Requisitos verificados

- Catálogo público contém `PERF101`–`PERF107`, todos com severidade High.
- O núcleo V5 representa Big-O limitado, payload, dimensões, limitações e desconhecidos.
- A integração V5 utiliza o grafo interprocedural V4 para evidências de chamadas conhecidas.
- A comparação pública não apresenta percentual, custo monetário ou benchmark.
- A suíte e os gates configurados foram executados sem envio de código a serviços externos.

## Achados

### FINDING-004 — Multiplicadores conhecidos são descartados na propagação interprocedural

- **Severity:** High
- **Category:** Functional / consistency
- **Location:** `src/StrictSharp.Analyzers/V5/InterproceduralCostInference.cs:28-33`
- **Evidence:** Toda evidência interprocedural criada pelo serviço recebe `KnownCount.Unknown` para contagem e multiplicador, inclusive `RepeatedCall`, `Allocation` e `Materialization`. O caminho de chamada é preservado no texto, mas o multiplicador observável do loop não é transportado.
- **Impact:** Um loop conhecido que chama um método conhecido não informa a quantidade estrutural que amplifica a operação, contrariando `FR-004`, `AC-004` e o princípio de contagens estruturais do plano. `PERF104` pode ser emitido apenas pela dimensão `Repetition`, sem número conhecido quando ele é derivável.
- **Recommendation:** Propagar a contagem/multiplicador do caminho de chamada ou enriquecer a evidência do grafo com essa informação; manter `Unknown` somente quando limite, dispatch ou entrada não forem resolvidos.
- **Return stage:** `implement`

### FINDING-005 — PERF107 compara ramos sem demonstrar equivalência semântica

- **Severity:** High
- **Category:** Functional / scope
- **Location:** `src/StrictSharp.Analyzers/Analyzers/Perf107Analyzer.cs:23-43`
- **Evidence:** O analyzer compara o primeiro item de custo de qualquer ramo `if/else` que contenha loops. O teste `Perf107ReportsStructuralComparisonBetweenConditionalPaths` usa ramos estruturalmente diferentes (`foreach` sobre `items` versus `for` de 10 iterações) e espera diagnóstico, mas não há verificação de que os ramos sejam alternativas semanticamente comparáveis ou produzam o mesmo efeito.
- **Impact:** O diagnóstico pode sugerir uma alternativa melhor para dois caminhos que não são substituíveis, contrariando `CON-008`, `FR-009`, `AC-009` e a exigência de preservar semântica.
- **Recommendation:** Restringir a comparação a alternativas com contexto comparável demonstrável, ou expor a comparação somente pela API pública com responsabilidade explícita do chamador; adicionar testes negativos para ramos não equivalentes.
- **Return stage:** `implement`

### FINDING-003 — Rastreabilidade automatizada e estado de tarefas permanecem inconsistentes

- **Severity:** Medium
- **Category:** Consistency / scope
- **Location:** `.spectro/features/csharp-performance-analyzer-v5/tasks.md` e `.codex/skills/spectro-review/scripts/validate_traceability.ps1`
- **Evidence:** `validate_traceability.ps1` retornou `result: gaps` e listou 26 requisitos como `uncovered`, embora `plan.md` e a tabela de tarefas contenham as ligações. `TASK-010` está `pending` mesmo com `gate-20261031T142145Z` aprovado.
- **Impact:** A auditoria não consegue provar automaticamente a cobertura dos requisitos e o estado do fluxo não representa o resultado atual dos gates.
- **Recommendation:** Corrigir o parser/formato de rastreabilidade e sincronizar `TASK-010`; regenerar `traceability.json` e repetir a validação.
- **Return stage:** `plan`

## Riscos residuais

- A classificação Big-O continua limitada a formas sintáticas simples; entradas dinâmicas, recursão profunda e dispatch permanecem incompletos.
- I/O e concorrência não medem latência, throughput, disponibilidade ou capacidade efetiva do runtime.
- A margem de cobertura de branches novos é estreita (85,26% contra 85%); alterações no núcleo V5 devem preservar os testes de exceção.

## Tarefas de remediação

- `TASK-REV-003` cobre `FINDING-003`.
- `TASK-REV-004` cobre `FINDING-004`.
- `TASK-REV-005` cobre `FINDING-005`.

## Decisão

`changes_requested`. Os dois achados High impedem aprovação. O retorno recomendado é `spectro-implement`, seguido de `spectro-gates` e nova revisão.

## Atualização das remediações — 2026-10-31

- `FINDING-003`: resolvido. O validador agora lê tarefas em tabelas e headings; a validação regenerada em `reviews/review-20261031T142331Z/traceability.json` retorna `complete`. `TASK-010` reflete o bloqueio atual de cobertura.
- `FINDING-004`: resolvido. Evidências interprocedurais propagam contagem e multiplicador conhecidos e mantêm limites dinâmicos como desconhecidos. Testes cobrem multiplicador conhecido e desconhecido.
- `FINDING-005`: resolvido. `PERF107` compara corpos de loop equivalentes sintaticamente e com referências de símbolos iguais; testes cobrem caminhos comparáveis, incompatíveis e funções locais distintas.
- Testes direcionados: 13 aprovados. Gate `gate-20261031T143206Z`: build, suíte completa e segurança passaram; a cobertura não iniciou porque a árvore estava suja. Após commit, `gate-20261031T143403Z` executou os 253 testes e mediu 84,65% de branches novos, abaixo do mínimo de 85%; linhas novas ficaram em 96,84%. A recuperação classificou a falha como `implementation-defect` e moveu o fluxo para `waiting_input`, com retorno à implementação.
- Remediação de cobertura: adicionados casos positivos para `for`, `foreach`, `foreach` com desestruturação, `while` e `do/while`, e casos negativos para quantidades de loops diferentes, símbolos externos não resolvidos e complexidade desconhecida. A medição local com `-AllowDirtyTree` executou 259 testes e passou: 97,54% linhas, 85,74% branches, 97,28% linhas novas e 85,94% branches novos. O gate formal deve ser repetido em árvore limpa para gerar evidência final da revisão atual.
