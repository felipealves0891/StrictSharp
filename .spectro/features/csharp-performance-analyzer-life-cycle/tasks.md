<!-- spectro-managed: true -->
# Tarefas: Contextos de ciclo de vida no C# Performance Analyzer

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Status |
|---|---|---|---|---|---|
| `TASK-001` | Aprovar o contrato de `PERF108`–`PERF112`, incluindo títulos, categorias, severidades, referências, exceções, confiança e regra de deduplicação | `CON-005`, `NFR-004` | Metadados aprovados e compatíveis com a constituição, sem alterar `PERF100` ou `PERF101`–`PERF107` | — | completed |
| `TASK-002` | Definir e testar o modelo compartilhado de contexto, unidade de custo e classificação de espera para `BackgroundService.ExecuteAsync` | `FR-001`, `FR-004`, `FR-006`, `NFR-001`, `NFR-002`, `NFR-003`, `CON-002`, `CON-003`, `CON-004` | O núcleo reconhece somente a relação semântica com `BackgroundService`, classifica os casos conhecidos e preserva desconhecidos sem inferir frequência | `TASK-001` | completed |
| `TASK-003` | Registrar `PERF108`–`PERF112` no `RuleCatalog` e ampliar o `V4AnalyzerCatalog` | `FR-001`, `FR-002`, `FR-010`, `CON-005` | Catálogo V4 contém `PERF100`, `PERF108`–`PERF112`; cada regra tem um descriptor, fase V4, configuração e metadados aprovados | `TASK-001` | completed |
| `TASK-004` | Integrar o contexto à análise interprocedural e à inferência de loops, sem contar o loop de serviço como complexidade computacional externa | `FR-002`, `FR-003`, `FR-005`, `NFR-001`, `NFR-002`, `NFR-005` | Um `ExecuteAsync` com loop de serviço e loop computacional interno mantém a análise interna, não gera complexidade infinita/externa artificial e propaga contexto a alvos conhecidos | `TASK-002`, `TASK-003` | completed |
| `TASK-005` | Implementar os cinco serviços de evidência e os adaptadores `Perf108Analyzer`–`Perf112Analyzer` | `FR-003`, `FR-007`, `FR-008`, `FR-009`, `NFR-003`, `NFR-004`, `NFR-005` | Cada analyzer emite somente sua regra, respeita cancelamento, distingue evidência/hipótese e não trata fronteira desconhecida como custo zero | `TASK-002`, `TASK-003`, `TASK-004` | completed |
| `TASK-006` | Criar testes comportamentais, de incerteza, deduplicação e regressão V4/V5 para todos os critérios de aceite | `FR-001`, `FR-002`, `FR-003`, `FR-004`, `FR-005`, `FR-006`, `FR-007`, `FR-008`, `FR-009`, `FR-010`, `NFR-001`, `NFR-002`, `NFR-003`, `NFR-004`, `NFR-005`, `NFR-006` | `AC-001`–`AC-012` possuem cobertura positiva, negativa e de exceção; V1–V5 existentes permanecem estáveis | `TASK-004`, `TASK-005` | completed |
| `TASK-007` | Atualizar documentação, empacotamento e rastreabilidade dos cinco diagnósticos | `FR-010`, `NFR-004`, `NFR-006`, `CON-006` | Documento contém exemplos, limitações, IDs, categorias, severidades aprovadas e referência; pacote inclui a documentação | `TASK-001`, `TASK-003`, `TASK-005` | completed |
| `TASK-008` | Executar build, testes, segurança e cobertura; registrar evidências e resolver regressões | `NFR-001`, `NFR-002`, `NFR-003`, `NFR-004`, `NFR-005`, `NFR-006`, `CON-008` | Todos os gates obrigatórios passam ou têm falha documentada com causa e ação corretiva; `AC-013` permanece rastreável; nenhum segredo ou código é enviado externamente | `TASK-006`, `TASK-007`, `TASK-009` | completed |
| `TASK-009` | Reconciliar explicitamente na documentação-fonte e nos artefatos Spectro a numeração histórica `PERF101`–`PERF105` com o catálogo vigente `PERF108`–`PERF112` | `CON-008` | A documentação marca a tabela histórica como não normativa; `spec.md`, `clarifications.md`, `plan.md` e `tasks.md` mantêm o mesmo catálogo vigente e a rastreabilidade de `AC-013` | `TASK-007` | completed |

## Regras de execução

- Não implementar antes de concluir `TASK-001`; a aprovação humana de severidade e categoria é obrigatória.
- A primeira entrega reconhece somente `BackgroundService.ExecuteAsync`; outros entry points não devem entrar por conveniência.
- Não adicionar pacote de Hosting nem serviço externo; usar símbolos e fontes de teste controladas.
- Manter um analyzer por diagnóstico e serviços de análise estáticos/reutilizáveis.
- Preservar desconhecidos como desconhecidos, propagar cancelamento e evitar alocações/travessias repetidas.
- Não alterar a semântica dos contratos V1–V5 existentes nem reutilizar `PERF101`–`PERF107`.

## Progresso

- Concluídas: `TASK-001`–`TASK-009`.
- Pendentes: nenhuma.

## TASK-009 - Detalhamento

- **Componentes afetados:** `docs/csharp-performance-analyzer-life-cycle.md`, `.spectro/features/csharp-performance-analyzer-life-cycle/spec.md`, `clarifications.md`, `plan.md` e `tasks.md`.
- **Validação esperada:** confirmar que a tabela conceitual `PERF101`–`PERF105` está identificada como histórica e que o catálogo vigente é `PERF108`–`PERF112`, sem alterar o catálogo V5.
- **Risco:** uma alteração documental incompleta pode reintroduzir ambiguidade de contrato; mitigar com busca pelos IDs, revisão de rastreabilidade e execução posterior dos gates.
- Remediações concluídas: `TASK-REV-001`, `TASK-REV-002`, `TASK-REV-003`, `TASK-REV-004`, `TASK-REV-005`.

## TASK-REV-001 - Remediate FINDING-001

- **Status:** completed
- **Finding:** FINDING-001
- **Severity:** High
- **Return stage:** implement
- **Description:** LifecycleContext expõe apenas CostPerIteration e não representa CostPerWorkItem ou CostPerTime, embora FR-006/AC-006 exijam unidades separadas e custo temporal apenas como hipótese.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Add or update behavior-focused coverage as required.

## TASK-REV-002 - Remediate FINDING-002

- **Status:** completed
- **Finding:** FINDING-002
- **Severity:** Medium
- **Return stage:** implement
- **Description:** PERF109 reporta qualquer criação de objeto ou array no loop de serviço sem demonstrar invariância, redundância ou independência do item; isso viola FR-008, AC-008 e a restrição de baixo ruído.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Add or update behavior-focused coverage as required.


## TASK-REV-003 - Remediate FINDING-003

- **Status:** completed
- **Finding:** FINDING-003
- **Severity:** Medium
- **Return stage:** plan
- **Description:** A validação de rastreabilidade retorna gaps porque tasks.md usa intervalos para FR-008 e CON-001..CON-007; expandir os IDs nas linhas de requisito ou ajustar o artefato para que cada requisito tenha vínculo explícito.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Add or update behavior-focused coverage as required.


## TASK-REV-004 - Remediate FINDING-004

- **Status:** completed
- **Finding:** FINDING-004
- **Severity:** Medium
- **Return stage:** plan
- **Description:** Atualizar o estado de TASK-008 para completed quando o relatório gates.json validado estiver aprovado, mantendo o progresso Spectro consistente com a execução dos gates.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Add or update behavior-focused coverage as required.


## TASK-REV-005 - Remediate FINDING-005

- **Status:** completed
- **Finding:** FINDING-005
- **Severity:** Medium
- **Return stage:** implement
- **Description:** A execução conjunta de V4 pode emitir PERF110 junto com PERF001 para a mesma operação bloqueante (por exemplo, Task.Wait ou Task.Result), contrariando FR-010/AC-010 e o plano de deduplicação; implementar deduplicação por evidência ou suprimir o diagnóstico contextual quando a regra existente já explica a mesma causa, com teste de catálogo combinado.
- **Dependencies:** none
- **Affected components:** `src/StrictSharp.Analyzers/Analyzers/Perf110Analyzer.cs`, `src/StrictSharp.Analyzers/Analyzers/Perf001Analyzer.cs`, shared analysis helper if required, and V4 analyzer tests.
- **Risk:** suppressing too broadly may hide lifecycle-specific context; suppressing too narrowly may leave duplicate diagnostics for the same blocking evidence.
- **Completion criteria:** Finding is resolved in code and behavior-focused tests pass; gates and review must be renewed before approval.
- **Expected tests:** Run `V4AnalyzerCatalog.All` against `Task.Wait`, `Task.Result`, `GetResult`, and a synchronous blocking operation not covered by `PERF001`; assert deduplication and preservation of contextual diagnostics.

## Expansão solicitada

### Remediações da revisão `review-20261103T-final`

- `FINDING-001`: estado reaberto para `implement`; `TASK-017` permanece pendente até nova execução de gates e revisão final.
- `FINDING-002`: concluído; o contrato `PerformanceContext` agora é identificado pelo nome de metadados totalmente qualificado, com teste para atributo homônimo.
- `FINDING-003`: concluído; a origem da frequência distingue hipótese declarada de atraso solicitado, com mensagens e testes específicos.
- Validação local: 329 testes aprovados.

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Status |
|---|---|---|---|---|---|
| `TASK-010` | Projetar o modelo compartilhado de contexto expandido, origem da evidência e unidades de custo sem quebrar `BackgroundService.ExecuteAsync` | `FR-006`, `FR-013`, `NFR-001`, `NFR-002`, `NFR-003`, `NFR-005`, `NFR-007`, `CON-001`, `CON-006`, `CON-009`, `CON-010` | O modelo representa os cinco contextos aprovados, mantém desconhecidos, propaga cancelamento e preserva os diagnósticos da primeira entrega | `TASK-001`–`TASK-009` | completed |
| `TASK-011` | Implementar detecção semântica dos entry points adicionais e o mapeamento para `Service Lifecycle`, `Event Driven`, `Polling`, `Retry` e `Batch` | `FR-011`, `FR-013`, `NFR-001`, `NFR-002`, `NFR-003`, `NFR-007`, `CON-009` | Testes demonstram o mapeamento de `IHostedService`, brokers, `Channel<T>`, schedulers, TCP, game loops, eventos, retry e batch; nome isolado não classifica | `TASK-010` | completed |
| `TASK-012` | Criar e validar o contrato público opcional `PerformanceContext`/`PerformanceContextKind` com `Kind` e `ExpectedIterationsPerSecond` | `FR-012`, `FR-014`, `NFR-004`, `NFR-005`, `NFR-007`, `CON-010` | Declarações válidas são lidas como hipótese; frequência ausente, não positiva, desconhecida ou conflitante permanece desconhecida sem exceção | `TASK-010` | completed |
| `TASK-013` | Integrar propagação interprocedural, custo por item/lote/frame/tempo condicional e mensagens de hipótese aos analyzers `PERF108`–`PERF112` | `FR-003`, `FR-005`, `FR-006`, `FR-010`, `FR-013`, `FR-014`, `NFR-004`, `NFR-005`, `CON-002`, `CON-003`, `CON-004`, `CON-007`, `CON-010` | Operações resolvidas preservam a hierarquia de contexto; fronteiras desconhecidas não viram custo zero; nenhuma frequência é apresentada como fato | `TASK-010`, `TASK-011`, `TASK-012` | completed |
| `TASK-014` | Revalidar `RuleCatalog`, `V4AnalyzerCatalog`, configuração e deduplicação mantendo exclusivamente `PERF108`–`PERF112` | `FR-010`, `NFR-007`, `CON-005`, `CON-008` | Catálogo, IDs, categorias, severidades, fase e configuração permanecem compatíveis; não há novos IDs nem duplicação de evidência | `TASK-011`, `TASK-012`, `TASK-013` | completed |
| `TASK-015` | Criar matriz comportamental, negativa, de incerteza e de regressão para todos os entry points e o contrato declarativo | `FR-001`, `FR-002`, `FR-003`, `FR-004`, `FR-005`, `FR-006`, `FR-007`, `FR-008`, `FR-009`, `FR-010`, `FR-011`, `FR-012`, `FR-013`, `FR-014`, `NFR-001`, `NFR-002`, `NFR-003`, `NFR-004`, `NFR-005`, `NFR-006`, `NFR-007`, `CON-001`, `CON-002`, `CON-003`, `CON-004`, `CON-005`, `CON-006`, `CON-007`, `CON-008`, `CON-009`, `CON-010` | `AC-001`, `AC-002`, `AC-003`, `AC-004`, `AC-005`, `AC-006`, `AC-007`, `AC-008`, `AC-009`, `AC-010`, `AC-011`, `AC-012`, `AC-013`, `AC-014`, `AC-015`, `AC-016`, `AC-017`, `AC-018` têm cobertura rastreável, incluindo primeira entrega, mapeamentos expandidos, contrato inválido, propagação e deduplicação | `TASK-011`, `TASK-012`, `TASK-013`, `TASK-014` | completed |
| `TASK-016` | Atualizar documentação empacotada e artefatos de rastreabilidade para a expansão implementada | `NFR-004`, `NFR-006`, `NFR-007`, `CON-006`, `CON-008`, `CON-009`, `CON-010` | A documentação descreve o mapeamento, o contrato, as limitações e o catálogo reutilizado; o pacote inclui a versão atual | `TASK-014`, `TASK-015` | completed |
| `TASK-017` | Executar build, testes, segurança, cobertura, empacotamento e revisão final da expansão | `NFR-001`–`NFR-007`, `CON-006`, `CON-007` | Todos os gates obrigatórios passam, ou falhas ficam documentadas com ação corretiva; a revisão confirma rastreabilidade completa e compatibilidade | `TASK-015`, `TASK-016` | completed |

### Regras de execução da expansão

- Não adicionar dependências de Hosting, brokers, TCP ou runtime ao analyzer.
- Não classificar por nome isolado; exigir evidência semântica e preservar desconhecidos.
- Não transformar `ExpectedIterationsPerSecond` em frequência garantida ou medição de produção.
- Reutilizar `PERF108`–`PERF112`; não alterar `PERF101`–`PERF107` nem contratos V1–V5.
- Manter um analyzer por diagnóstico e serviços estáticos/reutilizáveis, com cancelamento e baixa alocação.
- `TASK-017` é bloqueada se houver regressão nos testes da primeira entrega ou duplicação de diagnósticos.

### Progresso da expansão

- Concluídas: `TASK-010`, `TASK-011`, `TASK-012`, `TASK-013`, `TASK-014`, `TASK-015`, `TASK-016`, `TASK-017`.
- Pendentes: nenhuma.

