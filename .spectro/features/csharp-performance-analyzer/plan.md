<!-- spectro-managed: true -->
# Plano: C# Performance Analyzer — ajustes pós-revisão

## Contexto e objetivo técnico

A implementação V1 existente atende à estrutura principal do analyzer e possui gates recentes aprovados, mas a revisão `review-20261028T225900Z` solicitou ajustes antes da aprovação. Este plano trata exclusivamente os achados `FINDING-001` a `FINDING-004`; não amplia o catálogo, não implementa V2/V3 e não cria Code Fix.

Objetivos:

- fazer cada diagnóstico expor o contrato mínimo exigido pela especificação, sem prometer ganhos medidos;
- tornar a rastreabilidade reconhecível pelo validador automatizado;
- reconciliar tarefas, estado e evidências dos gates;
- separar ou obter aprovação explícita para alterações do tooling de gates fora do escopo do analyzer.

## Pré-condições e decisões confirmadas

- `clarifications.md` não possui bloqueios ou questões major abertas.
- Targets permanecem `netstandard2.1` para o analyzer e `net10.0` para testes, com C# 14.
- As 15 regras V1 e os IDs `PERF001`–`PERF030` permanecem inalterados.
- Nenhuma alteração automática de código será adicionada.
- Gates finais serão renovados após os ajustes.

## Solução proposta

### Contrato observável dos diagnósticos

Manter `RuleDefinition` como fonte única de metadados e centralizar em `RuleAnalysis` a construção da mensagem operacional. O diagnóstico deve tornar acessíveis o ID/título, evidência estrutural, severidade, categoria, impacto potencial, escalabilidade, orientação, exceções, confiança e referência. A mensagem deve declarar que custo efetivo, frequência e ganho percentual não são medidos nesta fase.

Descritores e textos constantes permanecem reutilizáveis; dados específicos da ocorrência devem ser mínimos. A orientação continua condicional quando a equivalência semântica não puder ser provada.

### Rastreabilidade validável

Usar seções `## TASK-...` em `tasks.md`, pois o validador atual extrai tarefas por esse formato. Cada seção conterá requisitos, critérios de aceite, dependências, componentes, risco, conclusão e uma linha explícita de validação associada aos `AC-*`. A tabela-resumo poderá permanecer, mas as seções serão a fonte canônica.

O resultado esperado de `validate_traceability.ps1` é `complete`, sem requisitos ou critérios descobertos.

### Estado, gates e escopo adicional

Depois dos ajustes, atualizar `TASK-010`, progresso e `state.json` de forma consistente. Executar build, testes, segurança e cobertura no revisionamento final, preservando tentativas históricas.

As alterações em `.codex/skills/spectro-gates/**` e `.codex/skills/shared/**` serão tratadas como mudança de infraestrutura separada ou receberão aprovação humana explícita. A feature do analyzer não ocultará essa diferença no diff.

## Componentes afetados

- Produção: `src/StrictSharp.Analyzers/Rules/RuleDefinition.cs` e `src/StrictSharp.Analyzers/Infrastructure/RuleAnalysis.cs`, somente para o contrato de mensagem.
- Testes: `tests/StrictSharp.Analyzers.Tests/InfrastructureTests.cs`, testes por regra e/ou novo teste de contrato de mensagem.
- Workflow: `.spectro/features/csharp-performance-analyzer/plan.md`, `tasks.md`, `state.json`, `gates.json` e pacote de review.
- Nenhuma alteração prevista no catálogo de IDs, nas regras V2/V3 ou em dependências externas.

## Fluxo, erros, persistência e concorrência

O analyzer continuará sem persistência, telemetria ou comunicação externa. A construção do diagnóstico será determinística e thread-safe para execução concorrente. Metadados inválidos ou IDs desconhecidos continuam sendo erro de programação detectável nos testes de contrato.

## Alternativas consideradas

- Manter somente o título foi rejeitado por violar `FR-003`/`AC-003`.
- Colocar campos somente em documentação não ligada ao diagnóstico foi rejeitado; precisam estar acessíveis ao consumidor.
- Alterar somente a tabela de tarefas foi rejeitado; o validador exige seções `## TASK-...`.
- Ignorar gaps do validador foi rejeitado; a rastreabilidade deve ser corrigida.
- Misturar tooling de gates com a feature requer aprovação humana; separar o escopo é a alternativa preferida.

## Riscos e compatibilidade

- Mensagens maiores podem aumentar ruído e custo; mitigar com textos concisos e metadados reutilizáveis.
- A cobertura de branches está próxima do limiar de 85%; qualquer mudança exige renovar a cobertura.
- Recomendações não podem tratar `ArrayPool<T>`, `HashSet<T>`, `await` ou logging estruturado como melhorias universais.
- O validador é sensível ao formato de tarefas; o plano torna esse contrato explícito.
- A decisão sobre tooling adicional pode exigir `human_review` antes dos gates finais.

## Estratégia de testes e validação

- Testar que uma ocorrência de cada regra V1 possui mensagem/contrato com os campos obrigatórios e sem ganho percentual.
- Preservar testes de ID, severidade, localização, referência e comportamento positivo/negativo.
- Executar `validate_traceability.ps1` e exigir `result: complete`.
- Executar build, testes, segurança e cobertura pelos comandos de `.spectro/config.yaml`.
- Conferir revisionamento dos gates, hashes dos artefatos e ausência de V2/V3, Code Fix, profiling ou telemetria.

## Matriz de rastreabilidade

| Requisito | Critério | Tarefa | Validação |
|---|---|---|---|
| `FR-001`, `CON-001` | `AC-001` | `TASK-001`, `TASK-003`, `TASK-013` | build/teste |
| `FR-002`, `CON-002` | `AC-002` | `TASK-002`, `TASK-008` | catálogo/IDs |
| `FR-003`, `FR-004`, `CON-004` | `AC-003` | `TASK-002`, `TASK-009`, `TASK-011` | contrato de mensagem |
| `FR-005`, `NFR-001`, `NFR-006` | `AC-004` | `TASK-003`, `TASK-005`–`TASK-008` | exceções |
| `FR-006` | `AC-005` | `TASK-005`–`TASK-008`, `TASK-013` | matriz V1/gates |
| `FR-007` | `AC-006` | `TASK-010`, `TASK-012` | roadmap/escopo |
| `FR-008` | `AC-007` | `TASK-004`, `TASK-008` | configuração |
| `FR-009`, `NFR-002` | `AC-008` | `TASK-009`, `TASK-011` | orientação condicional |
| `FR-010`, `CON-003` | `AC-009` | `TASK-002`, `TASK-009`, `TASK-011` | referências |
| `FR-011`, `NFR-008` | `AC-010`, `AC-013` | `TASK-008`, `TASK-010`, `TASK-013` | testes/trace/gates |
| `FR-012`, `NFR-005` | `AC-011` | `TASK-009`, `TASK-011` | ausência de medição |
| `NFR-003` | `AC-012` | `TASK-001`, `TASK-008`, `TASK-013` | targets/C# 14 |
| `NFR-004` | `AC-012` | `TASK-003`, `TASK-010`, `TASK-011`, `TASK-013` | gates |
| `NFR-007` | `AC-013` | `TASK-005`–`TASK-007`, `TASK-011` | evidência por categoria |
| `CON-005` | `AC-001`–`AC-013` | `TASK-001`–`TASK-013` | revisão/trace |

## Ordem de execução

1. `TASK-011` — corrigir o contrato observável e os testes.
2. `TASK-012` — normalizar rastreabilidade e validar ausência de gaps.
3. `TASK-013` — decidir/separar escopo adicional, reconciliar estado e renovar gates.
4. Repetir revisão independente; aprovar somente sem achados High/Critical e com traceabilidade completa.
