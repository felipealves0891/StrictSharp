<!-- spectro-managed: true -->
# Tarefas: Implementação V3 do C# Performance Analyzer

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Status |
|---|---|---|---|---|---|
| `TASK-001` | Adicionar `RuleCatalog.V3`, criar `V3AnalyzerCatalog` e preparar extensões compartilhadas mínimas, preservando as listas e o comportamento V1/V2. | `FR-001`, `FR-008`, `FR-010`, `NFR-003`, `NFR-004`, `CON-001`, `CON-003` | `AC-001`, `AC-006`, `AC-007`, `AC-009` | — | completed |
| `TASK-002` | Implementar `Perf011Analyzer` para padrões estruturais reconhecidos de retenção potencial, com omissão em retenção delimitada, transferência, remoção, descarte ou propriedade não demonstrável; adicionar testes comportamentais. | `FR-002`, `FR-003`, `FR-004`, `NFR-001`, `NFR-002`, `NFR-004`, `CON-004` | `AC-002`, `AC-003`, `AC-006`, `AC-009` | `TASK-001` | completed |
| `TASK-003` | Implementar `Perf030Analyzer` para criação repetida com sinais de construtor complexo, inicialização relevante ou valores invariantes, preservando estado variável, isolamento e falta de thread safety; adicionar testes comportamentais. | `FR-005`, `FR-006`, `FR-007`, `NFR-001`, `NFR-002`, `NFR-004` | `AC-004`, `AC-005`, `AC-006`, `AC-009` | `TASK-001` | completed |
| `TASK-004` | Criar matriz V3 e expandir testes de catálogo/configuração para verificar exatamente os dois IDs, metadados, fases, ordem, neutralidade, supressão e severidade configurável. | `FR-001`, `FR-008`, `FR-010`, `FR-011`, `NFR-005` | `AC-001`, `AC-007`, `AC-008`, `AC-009` | `TASK-002`, `TASK-003` | completed |
| `TASK-005` | Produzir validação comparativa representativa para retenção e reconstrução, atualizar documentação e manter hipóteses, limitações, exemplos e rastreabilidade alinhados ao comportamento implementado. | `FR-008`, `FR-009`, `FR-011`, `NFR-006`, `CON-002`, `CON-004` | `AC-003`, `AC-005`, `AC-006`, `AC-008`, `AC-010` | `TASK-002`, `TASK-003`, `TASK-004` | completed |
| `TASK-006` | Revisar segurança, ausência de chamadas externas, custo do analyzer, deduplicação com V1/V2/CA18xx e preservação semântica das recomendações. | `FR-004`, `FR-007`, `FR-009`, `NFR-001`, `NFR-002`, `NFR-004`, `NFR-007`, `CON-002`, `CON-005`, `CON-007` | `AC-003`, `AC-005`, `AC-006`, `AC-009`, `AC-011` | `TASK-002`, `TASK-003`, `TASK-005` | completed |
| `TASK-007` | Executar build, testes, cobertura e segurança, revisar diff e confirmar a rastreabilidade integral antes da aprovação da implementação. | `FR-001`–`FR-011`, `NFR-001`–`NFR-007`, `CON-001`–`CON-007` | `AC-001`–`AC-011` | `TASK-004`, `TASK-005`, `TASK-006` | completed |

## Regras de execução

- Não implementar Code Fix, profiling, telemetria, persistência, chamadas de rede ou dependências novas.
- Não usar limiares numéricos fixos para “muitas propriedades”, frequência, memória retida ou custo do analyzer; usar sinais estruturais conservadores e documentados.
- Não alterar IDs, severidades, fases ou contrato V1/V2 sem nova clarificação e aprovação humana.
- `PERF011` deve permanecer uma suspeita fundamentada, nunca uma prova de memory leak.
- `PERF030` não deve reportar toda criação em loop nem afirmar que um tipo é universalmente pesado.
- Preservar o padrão de analyzer por diagnóstico, serviços estáticos por padrão, execução concorrente e mensagens condicionais.
- Cada tarefa deve adicionar ou atualizar testes reproduzíveis; alterações em `docs`, `README` e `.spectro` devem refletir o comportamento efetivamente planejado.

## Progresso

- Concluídas: `TASK-001`, `TASK-002`, `TASK-003`, `TASK-004`, `TASK-005`, `TASK-006`, `TASK-007`.
- Bloqueadas: nenhuma.
- Próxima tarefa: nenhuma.
