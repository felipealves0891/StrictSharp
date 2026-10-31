<!-- spectro-managed: true -->
# Tarefas: Correção de falso positivo do PERF013 em strings

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Status |
|---|---|---|---|---|---|
| `TASK-001` | Adicionar cobertura de regressão para buscas em string, enumeração LINQ real, sequência `IEnumerable<T>` e casos únicos | `FR-001`, `FR-002`, `FR-003`, `NFR-001`, `CON-001` | `AC-001` a `AC-005` | Nenhuma | completed |
| `TASK-002` | Ajustar a classificação de invocações do PERF013 para excluir buscas próprias de `System.String` sem excluir operações que realmente enumeram string | `FR-001`, `FR-002`, `FR-003`, `NFR-001`, `CON-001` | `AC-001` a `AC-005` | `TASK-001` | completed |

## Detalhes das tarefas

### `TASK-001` — Cobertura de regressão

- Componentes: `tests/StrictSharp.Analyzers.Tests/Perf013AnalyzerTests.cs`.
- Critérios de conclusão: testes demonstram que o exemplo com `Contains("\r")` e `Contains("\n")` e buscas próprias de texto repetidas não geram diagnóstico; enumeração repetida em `IEnumerable<T>` e via LINQ sobre string continuam gerando `PERF013`; usos únicos continuam sem diagnóstico.
- Testes esperados: cenários de analyzer usando `AnalyzerTestHarness`; verificar IDs e quantidade de diagnósticos.
- Risco: baixa cobertura de uma sobrecarga ou regressão do caso válido; incluir pelo menos busca de texto e operação LINQ resolvida em símbolos distintos.

### `TASK-002` — Classificação semântica do PERF013

- Componentes: `src/StrictSharp.Analyzers/Analyzers/Perf013Analyzer.cs`; helper existente `RuleAnalysis.IsString` se necessário. `RuleAnalysis.IsEnumerable` não deve ser alterado.
- Critérios de conclusão: chamadas de busca de instância de `System.String` não são classificadas como enumeração; chamadas cobertas que realmente enumeram continuam elegíveis; o diagnóstico mantém ID, mensagem, localização e configuração; custo não acrescenta travessia sintática nem consulta semântica redundante além da necessária à classificação já existente.
- Testes esperados: executar `Perf013AnalyzerTests`, depois a suíte/build configurados na fase de implementação.
- Risco: filtro amplo baseado em tipo ou apenas no nome `Contains` suprimir enumeração legítima; restringir a decisão pela API resolvida e conferir os testes de regressão.

## Regras de execução

- Seguir a decisão `D-001` em `clarifications.md` e a especificação consolidada.
- Não alterar outros diagnósticos, o helper compartilhado `IsEnumerable`, catálogo, documentação de produto, configuração ou dependências.
- Resolver a classificação pela semântica disponível no Roslyn, preservando o fallback atual para demais invocações e evitando falsos positivos quando uma busca de instância de string puder ser reconhecida.
- Executar os testes definidos nos critérios e gates apenas durante a implementação, não durante o planejamento.

## Progresso

- Concluídas: `TASK-001`, `TASK-002`.
- Bloqueadas: nenhuma.

## Evidências

- `dotnet test tests/StrictSharp.Analyzers.Tests/StrictSharp.Analyzers.Tests.csproj --no-restore --filter FullyQualifiedName~Perf013AnalyzerTests`: aprovado, 4 testes.
- `dotnet test StrictSharp.sln --no-restore`: aprovado, 294 testes.
