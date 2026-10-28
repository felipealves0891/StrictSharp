<!-- spectro-managed: true -->
# Tarefas: C# Performance Analyzer — V1 e ajustes pós-revisão

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Status |
|---|---|---|---|---|---|
| `TASK-001` | Bootstrap da solução e targets | `FR-001`, `NFR-003`, `CON-001` | `AC-001`, `AC-012` | — | completed |
| `TASK-002` | Catálogo e contrato de regras | `FR-002`, `FR-003`, `FR-010`, `CON-002`–`CON-004` | `AC-002`, `AC-003`, `AC-009` | `TASK-001` | completed |
| `TASK-003` | Infraestrutura Roslyn e execução concorrente | `FR-001`, `FR-004`, `FR-005`, `NFR-001`, `NFR-004`, `NFR-006`, `NFR-007`, `CON-005` | `AC-001`, `AC-004`, `AC-012`, `AC-013` | `TASK-001`, `TASK-002` | completed |
| `TASK-004` | Configuração `.editorconfig` | `FR-008` | `AC-007` | `TASK-002` | completed |
| `TASK-005` | Regras V1 de concorrência/I/O | `FR-001`, `FR-003`, `FR-005`, `FR-006`, `FR-009`, `NFR-007` | `AC-001`, `AC-003`–`AC-005`, `AC-008`, `AC-013` | `TASK-002`, `TASK-003` | completed |
| `TASK-006` | Regras V1 de memória e strings | `FR-001`, `FR-003`, `FR-004`, `FR-005`, `FR-006`, `FR-009`, `NFR-007` | `AC-001`, `AC-003`–`AC-005`, `AC-008`, `AC-013` | `TASK-002`, `TASK-003` | completed |
| `TASK-007` | Regras V1 de LINQ, coleções, HTTP e logging | `FR-001`, `FR-003`, `FR-004`, `FR-005`, `FR-006`, `FR-009`, `NFR-007` | `AC-001`, `AC-003`–`AC-005`, `AC-008`, `AC-013` | `TASK-002`, `TASK-003` | completed |
| `TASK-008` | Matriz de testes V1 e contratos | `FR-002`, `FR-005`, `FR-006`, `FR-008`, `FR-011`, `NFR-001`, `NFR-003`, `NFR-008` | `AC-002`, `AC-004`, `AC-005`, `AC-007`, `AC-010`, `AC-012`, `AC-013` | `TASK-004`–`TASK-007` | completed |
| `TASK-009` | Documentação, referências e orientação condicional | `FR-003`, `FR-004`, `FR-009`, `FR-010`, `FR-012`, `NFR-002`, `NFR-005`, `CON-003`, `CON-004` | `AC-003`, `AC-008`, `AC-009`, `AC-011` | `TASK-005`–`TASK-008` | completed |
| `TASK-010` | Revisão de cobertura, roadmap e gates iniciais | `FR-006`, `FR-007`, `FR-011`, `NFR-004`, `NFR-008`, `CON-005` | `AC-001`–`AC-013` | `TASK-008`, `TASK-009` | superseded |
| `TASK-011` | Corrigir contrato observável dos diagnósticos e cobrir mensagens | `FR-003`, `FR-004`, `FR-009`, `FR-010`, `FR-012`, `NFR-002`, `NFR-005`, `CON-004` | `AC-003`, `AC-008`, `AC-009`, `AC-011`: testes confirmam campos obrigatórios, orientação condicional e ausência de ganho percentual | `TASK-009` | completed |
| `TASK-012` | Normalizar rastreabilidade e validação automatizada | `FR-002`, `FR-006`, `FR-007`, `FR-011`, `NFR-008`, `CON-005` | `AC-002`, `AC-005`, `AC-006`, `AC-010`, `AC-013`: `validate_traceability.ps1` retorna `complete` e cada requisito/critério tem vínculo verificável | `TASK-010`, `TASK-011` | completed |
| `TASK-013` | Reconciliar escopo, estado, gates e revisão final | `FR-001`, `FR-006`, `FR-007`, `FR-011`, `NFR-003`, `NFR-004`, `NFR-008`, `CON-005` | `AC-001`–`AC-013`: escopo adicional tem aprovação ou separação, estado consistente, gates renovados e revisão repetida | `TASK-011`, `TASK-012` | completed |

## Detalhamento e rastreabilidade operacional

## TASK-001 — Bootstrap da solução e targets

- **Requisitos:** `FR-001`, `NFR-003`, `CON-001`.
- **Componentes:** `StrictSharp.sln`, projetos `src` e `tests`.
- **Validação:** build e teste de compatibilidade comprovam `AC-001` e `AC-012`.
- **Status:** completed.

## TASK-002 — Catálogo e contrato de regras

- **Requisitos:** `FR-002`, `FR-003`, `FR-010`, `CON-002`, `CON-003`, `CON-004`.
- **Componentes:** `RuleDefinition`, `RuleCatalog`, documentação.
- **Validação:** testes de IDs, descritores e referências comprovam `AC-002`, `AC-003` e `AC-009`.
- **Status:** completed.

## TASK-003 — Infraestrutura Roslyn e execução concorrente

- **Requisitos:** `FR-001`, `FR-004`, `FR-005`, `NFR-001`, `NFR-004`, `NFR-006`, `NFR-007`, `CON-005`.
- **Componentes:** `RuleAnalyzerBase`, `RuleAnalysis`.
- **Validação:** testes de helpers, localização, concorrência e filtros comprovam `AC-001`, `AC-004`, `AC-012` e `AC-013`.
- **Status:** completed.

## TASK-004 — Configuração `.editorconfig`

- **Requisitos:** `FR-008`.
- **Componentes:** `.editorconfig`, testes de opções Roslyn.
- **Validação:** testes de severidade e `none` comprovam `AC-007`.
- **Status:** completed.

## TASK-005 — Regras V1 de concorrência/I/O

- **Requisitos:** `FR-001`, `FR-003`, `FR-005`, `FR-006`, `FR-009`, `NFR-007`.
- **Componentes:** `Perf001Analyzer`, `Perf003Analyzer`, `Perf026Analyzer`.
- **Validação:** testes positivos, negativos e exceções comprovam `AC-001`, `AC-003`, `AC-004`, `AC-005`, `AC-008` e `AC-013`.
- **Status:** completed.

## TASK-006 — Regras V1 de memória e strings

- **Requisitos:** `FR-001`, `FR-003`, `FR-004`, `FR-005`, `FR-006`, `FR-009`, `NFR-007`.
- **Componentes:** `Perf007`, `Perf008`, `Perf012`, `Perf018`, `Perf019`, `Perf020`.
- **Validação:** testes positivos, negativos e exceções comprovam `AC-001`, `AC-003`, `AC-004`, `AC-005`, `AC-008` e `AC-013`.
- **Status:** completed.

## TASK-007 — Regras V1 de LINQ, coleções, HTTP e logging

- **Requisitos:** `FR-001`, `FR-003`, `FR-004`, `FR-005`, `FR-006`, `FR-009`, `NFR-007`.
- **Componentes:** `Perf013`, `Perf014`, `Perf015`, `Perf022`, `Perf025`, `Perf027`.
- **Validação:** testes positivos, negativos e exceções comprovam `AC-001`, `AC-003`, `AC-004`, `AC-005`, `AC-008` e `AC-013`.
- **Status:** completed.

## TASK-008 — Matriz de testes V1 e contratos

- **Requisitos:** `FR-002`, `FR-005`, `FR-006`, `FR-008`, `FR-011`, `NFR-001`, `NFR-003`, `NFR-008`.
- **Componentes:** testes por regra, matriz V1 e configuração.
- **Validação:** execução da suíte comprova `AC-002`, `AC-004`, `AC-005`, `AC-007`, `AC-010`, `AC-012` e `AC-013`.
- **Status:** completed; ampliar com o contrato de mensagem em `TASK-011`.

## TASK-009 — Documentação, referências e orientação condicional

- **Requisitos:** `FR-003`, `FR-004`, `FR-009`, `FR-010`, `FR-012`, `NFR-002`, `NFR-005`, `CON-003`, `CON-004`.
- **Componentes:** catálogo e `docs/csharp-performance-analyzer.md`.
- **Validação:** revisão textual e testes de mensagem comprovam `AC-003`, `AC-008`, `AC-009` e `AC-011`.
- **Status:** completed; a parte observável pendente está em `TASK-011`.

## TASK-010 — Revisão de cobertura, roadmap e gates iniciais

- **Requisitos:** `FR-006`, `FR-007`, `FR-011`, `NFR-004`, `NFR-008`, `CON-005`.
- **Componentes:** `.spectro/config.yaml`, gates e roadmap.
- **Validação:** gates iniciais foram executados, mas o estado foi superseded pelos ajustes desta revisão.
- **Status:** superseded por `TASK-012` e `TASK-013`.

## TASK-011 — Corrigir contrato observável dos diagnósticos e cobrir mensagens

- **Requisitos:** `FR-003`, `FR-004`, `FR-009`, `FR-010`, `FR-012`, `NFR-002`, `NFR-005`, `CON-004`.
- **Componentes:** `RuleDefinition`, `RuleAnalysis`, `InfrastructureTests` e testes representativos das regras.
- **Risco:** mensagens extensas podem aumentar ruído e custo; limitar textos e reutilizar metadados constantes.
- **Validação:** testes confirmam `AC-003`, `AC-008`, `AC-009` e `AC-011`, incluindo evidência, impacto, escalabilidade, correção, exceções, confiança, referência e ausência de percentual.
- **Status:** completed; retorno `implement`; originado de `FINDING-001`.

## TASK-012 — Normalizar rastreabilidade e validação automatizada

- **Requisitos:** `FR-002`, `FR-006`, `FR-007`, `FR-011`, `NFR-008`, `CON-005`.
- **Componentes:** `tasks.md`, `plan.md` e `validate_traceability.ps1` somente se necessário.
- **Risco:** alterar o parser pode afetar outras features; preferir o formato de seções compatível antes de mudar o script.
- **Validação:** executar `validate_traceability.ps1` e exigir `AC-002`, `AC-005`, `AC-006`, `AC-010` e `AC-013` sem itens em `uncovered_*`.
- **Status:** completed; retorno `plan`; originado de `FINDING-002`.

## TASK-013 — Reconciliar escopo, estado, gates e revisão final

- **Requisitos:** `FR-001`, `FR-006`, `FR-007`, `FR-011`, `NFR-003`, `NFR-004`, `NFR-008`, `CON-005`.
- **Componentes:** `state.json`, `gates.json`, evidências, diff e pacote de review.
- **Risco:** o tooling adicional pode exigir aprovação humana; não concluir a feature enquanto isso não estiver decidido.
- **Validação:** build, testes, segurança, cobertura e revisão final comprovam `AC-001`–`AC-013`; verificar revisionamento e hashes antes de concluir.
- **Status:** completed; retorno `gates`/`human_review`; originado de `FINDING-003` e `FINDING-004`.

## Regras de execução

- Não implementar V2/V3, Code Fix, profiling, benchmarks de runtime ou telemetria.
- Não alterar IDs, severidades ou targets sem nova clarificação/aprovação humana.
- Não aceitar mensagens que prometam ganho percentual, frequência real ou custo medido.
- Cada mudança de produção deve ter teste comportamental correspondente.
- Tooling de gates deve ser separado ou explicitamente aprovado.
- Gates finais somente após `validate_traceability.ps1` retornar `complete`.

## Progresso

- Concluídas: `TASK-001`–`TASK-009`.
- Superseded: `TASK-010`.
- Pendentes: nenhuma.
- Bloqueadas: nenhuma; `TASK-013` pode exigir decisão humana sobre tooling adicional.
