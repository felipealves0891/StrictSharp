<!-- spectro-managed: true -->
# Review: Modelo de custo computacional V5

## Resumo

A implementação V5 compila, passa os gates obrigatórios e possui cobertura nova acima dos limites configurados. A revisão independente, porém, encontrou duas lacunas funcionais de alta severidade entre o contrato aprovado e o comportamento entregue: a propagação interprocedural prevista não está conectada aos analyzers V5, e `PERF107` permanece como analyzer sem análise registrada.

**Decisão final:** `changes_requested`

## Artefatos examinados

- `spec.md`, `clarifications.md`, `plan.md`, `tasks.md`
- `gates.json` da execução `gate-20261031T140814Z`
- Snapshot independente `reviews/review-20261031T140934Z/`
- Constituição e configuração Spectro
- Catálogo, analyzers e serviços V5
- Testes V5 e suíte completa reportada pelos gates

## Consistência da evidência

- O snapshot e o gate aprovado referem-se ao mesmo código: `699a47a3c97bd045d0e8a020aa80541a6d620736`.
- Build, testes, segurança e cobertura passaram; o gate registrou 242 testes e branch coverage nova de 85,04%.
- O validador automático de rastreabilidade retornou gaps porque procura tarefas em headings `TASK-###`, enquanto `tasks.md` usa tabela. Isso é uma limitação do validador, mas impede considerar a rastreabilidade automatizada completa sem correção do formato ou da ferramenta.

## Requisitos verificados

- Catálogo V5 contém `PERF101`–`PERF107`, todos `High`, com matriz e testes.
- O modelo representa Big-O limitado, payload de arrays, contagens, I/O, concorrência e desconhecidos.
- A comparação pública evita percentuais e benchmarks.
- A suíte completa e os gates configurados passaram.
- Não foram identificadas dependências externas novas nem envio de código a serviços.

## Achados

### FINDING-001 — Propagação interprocedural V5 não implementada

- **Severity:** High
- **Category:** Functional / consistency
- **Location:** `src/StrictSharp.Analyzers/Analyzers/Perf101Analyzer.cs:21-24`, `src/StrictSharp.Analyzers/V5/LoopCostInference.cs:9-61`, analyzers `Perf102`/`Perf104`/`Perf105`/`Perf106`
- **Evidence:** Os analyzers chamam somente `LoopCostInference`, `AllocationPayloadInference` ou `IoConcurrencyInference` sobre `context.Node`. Nenhum deles constrói ou consulta `InterproceduralGraphBuilder`, `InterproceduralEvidenceAnalyzer` ou `CallSite`.
- **Impact:** Chamadas custosas em outro método, multiplicadores por cadeia de chamadas e fronteiras conhecidas da V4 não são propagados para os diagnósticos V5. Isso viola `FR-004`, `FR-008`, `AC-004` e a tarefa `TASK-006`.
- **Recommendation:** Criar um serviço V5 que consuma o grafo V4 e combine evidências locais/interprocedurais, mantendo alvos ausentes e dispatch desconhecido explícitos. Adicionar testes de chamada conhecida dentro de loop e alvo externo.
- **Return stage:** `implement`

### FINDING-002 — `PERF107Analyzer` é no-op

- **Severity:** High
- **Category:** Functional / scope
- **Location:** `src/StrictSharp.Analyzers/Analyzers/Perf107Analyzer.cs:16-18`
- **Evidence:** `RegisterAnalysis` possui corpo vazio. O serviço `CostComparison.Compare` existe e tem testes unitários, mas nenhum analyzer ou resultado Roslyn o utiliza.
- **Impact:** O ID público `PERF107` é anunciado na especificação e documentação, mas não produz diagnóstico/evidência por meio do analyzer. A capacidade de comparação não está integrada ao contrato público V5, contrariando `FR-009`, `FR-010`, `AC-009` e `TASK-REV-002`.
- **Recommendation:** Definir e implementar o ponto de entrada público para comparar alternativas, ou ajustar o contrato antes da implementação. Cobrir o fluxo completo com teste comportamental do analyzer, sem inventar equivalência semântica.
- **Return stage:** `implement`

### FINDING-003 — Tarefas e rastreabilidade automatizada inconsistentes

- **Severity:** Medium
- **Category:** Consistency / scope
- **Location:** `.spectro/features/csharp-performance-analyzer-v5/tasks.md` e `validate_traceability.ps1`
- **Evidence:** O validador retornou todos os requisitos como `uncovered` e todos os critérios como sem validação, embora `plan.md` tenha uma matriz de rastreabilidade e `tasks.md` tenha tabela com requisitos/AC por tarefa. `TASK-010` também permanece `blocked`, enquanto o último `gates.json` está `passed`.
- **Impact:** A auditoria automática não consegue provar cobertura de requisitos e o estado da tarefa contradiz o gate atual.
- **Recommendation:** Atualizar o formato esperado pelo validador ou migrar as tarefas para headings reconhecidos; depois sincronizar `TASK-010` e regenerar a rastreabilidade.
- **Return stage:** `plan`

## Riscos residuais

- A classificação Big-O continua deliberadamente limitada a formas sintáticas simples; entradas dinâmicas, recursão profunda e dispatch permanecem incompletos.
- I/O e concorrência não medem runtime, latência, throughput ou capacidade efetiva.
- A cobertura passou por margem estreita no branch novo (85,04% contra 85%), portanto mudanças no núcleo V5 devem preservar os testes de exceção.

## Tarefas de remediação

- `TASK-REV-001` cobre `FINDING-001`.
- `TASK-REV-002` cobre `FINDING-002`.

## Decisão

`changes_requested`. Os gates são aprovados, mas os dois achados High impedem aprovação da implementação. O retorno recomendado é `spectro-implement`, seguido de `spectro-gates` e nova revisão.
