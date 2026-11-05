<!-- spectro-managed: true -->
# Tarefas: Redução de falsos positivos na PERF101

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Componentes afetados | Risco | Status |
|---|---|---|---|---|---|---|---|
| `TASK-001` | Definir o contrato interno da oportunidade acionável, incluindo dados mínimos de padrão repetido, chave estável, compatibilidade semântica, vantagem estrutural e estado de desconhecido. | `FR-001`, `FR-003`, `NFR-003`, `CON-003`, `CON-004` | `AC-003`, `AC-004` | Nenhuma | `src/StrictSharp.Analyzers/V5/`, contratos de `CostEvidence` | Médio: contrato insuficiente pode forçar heurísticas frágeis. | completed |
| `TASK-002` | Implementar serviço estático de inferência para o padrão conservador aprovado: busca linear repetida na mesma coleção, chave estável e alternativa compatível; silenciar padrões incompletos ou ambíguos. | `FR-002`, `FR-003`, `NFR-001`, `NFR-003`, `CON-004` | `AC-002`, `AC-003`, `AC-004`, `AC-006` | `TASK-001` | Novo serviço em `src/StrictSharp.Analyzers/V5/` e testes unitários | Alto: equivalência semântica e custo de construção podem não ser resolvíveis estaticamente. | completed |
| `TASK-003` | Integrar a oportunidade ao `Perf101Analyzer`, preservando `LoopCostInference`, `InterproceduralCostInference`, `V5DiagnosticAnalysis` para as demais regras e o contrato `PERF101`. | `FR-001`, `FR-004`, `NFR-002`, `CON-001`, `CON-002` | `AC-001`, `AC-005`, `AC-008` | `TASK-001`, `TASK-002` | `src/StrictSharp.Analyzers/Analyzers/Perf101Analyzer.cs` | Médio: filtro no ponto errado pode alterar o comportamento de outros diagnósticos. | completed |
| `TASK-004` | Adicionar e ajustar testes positivos, negativos e de exceção para serviço e analyzer, incluindo percurso linear necessário, ausência de oportunidade, busca repetida compatível, semântica ambígua, desconhecidos e mensagem sem alegações de medição. | `FR-002`, `FR-003`, `FR-004`, `FR-005`, `NFR-001`, `NFR-003` | `AC-001`–`AC-007` | `TASK-002`, `TASK-003` | `tests/StrictSharp.Analyzers.Tests/V5*Tests.cs`, `V5AnalyzerTests.cs` | Médio: testes podem cristalizar um padrão mais amplo que o escopo aprovado. | completed |
| `TASK-005` | Atualizar documentação da `PERF101`, executar matriz V5 e gates configurados, revisar diff e registrar evidências de build, testes, cobertura e segurança. | `FR-005`, `NFR-002`, `CON-001`, `CON-002` | `AC-007`, `AC-008` | `TASK-004` | `docs/perf101-falsos-positivos.md`, documentação V5, evidências Spectro | Baixo: cobertura ou gates podem revelar caminhos não cobertos. | pending |

## Regras de execução

- Não alterar IDs, severidade, etapa V5, outras regras ou dependências externas.
- Não emitir diagnóstico somente por `O(n)`; sem oportunidade comprovada, o resultado público deve ser vazio.
- Não usar nomes de métodos/tipos isolados como prova de equivalência ou melhoria.
- Não adicionar Code Fix, profiling, benchmark, execução externa ou cache global.
- Manter serviços de análise separados da integração Roslyn e estáticos por padrão.
- Preservar cancelamento, execução concorrente e fronteiras desconhecidas como silêncio conservador.

## Progresso

- Concluídas: `TASK-001`, `TASK-002`, `TASK-003`, `TASK-004`
- Pendentes: `TASK-005` — renovar os gates após a correção da revisão.

## TASK-REV-001 - Remediate FINDING-001

- **Status:** in_progress
- **Finding:** FINDING-001
- **Severity:** High
- **Return stage:** implement
- **Description:** Restringir a inferência de oportunidade da PERF101 para provar compatibilidade semântica e escopo da oportunidade: silenciar ordem/duplicidade/propriedade/mutação indireta/aliases, usos incompatíveis de IndexOf e custo de construção não demonstrável; adicionar testes de regressão e renovar gates.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Add or update behavior-focused coverage as required.
