<!-- spectro-managed: true -->
# Tarefas: C# Performance Analyzer — V1

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Status |
|---|---|---|---|---|---|
| `TASK-001` | Criar `StrictSharp.sln`, projeto de analyzer `netstandard2.1` e projeto de testes `net10.0`, fixando C# 14 e referências Roslyn compatíveis. | `FR-001`, `NFR-003`, `CON-001` | A solução compila com os targets definidos e o analyzer pode ser referenciado por um projeto de teste. | — | completed |
| `TASK-002` | Criar o catálogo de regras e o contrato compartilhado de descritores/metadados para `PERF001`–`PERF030`, com implementação habilitada somente para V1. | `FR-002`, `FR-003`, `FR-010`, `CON-002`, `CON-003`, `CON-004` | IDs são únicos/estáveis; descritores V1 têm campos obrigatórios, severidade documentada e referência rastreável. | `TASK-001` | completed |
| `TASK-003` | Criar infraestrutura estática de análise: registro por regra, helpers Roslyn, filtros de contexto, execução concorrente segura e utilitários de localização/mensagem. | `FR-001`, `FR-004`, `FR-005`, `NFR-001`, `NFR-004`, `NFR-006`, `NFR-007`, `CON-005` | Serviços não mantêm estado mutável compartilhado; análise evita trabalho semântico desnecessário e cada analyzer pode ser testado isoladamente. | `TASK-001`, `TASK-002` | completed |
| `TASK-004` | Adicionar `.editorconfig` de referência e testes de configuração para `dotnet_diagnostic.PERFxxx.severity`, incluindo desabilitação com `none`. | `FR-008` | Uma regra pode ser habilitada, desabilitada e ajustada sem alterar seu ID ou descritor. | `TASK-002` | completed |
| `TASK-005` | Implementar as regras V1 de concorrência/I/O: `PERF001`, `PERF003` e `PERF026`, com exceções e orientações documentadas. | `FR-001`, `FR-003`, `FR-005`, `FR-006`, `FR-009` | Cada regra possui positivos, negativos, exceções, localização e mensagem coerentes; não há Code Fix. | `TASK-002`, `TASK-003` | completed |
| `TASK-006` | Implementar as regras V1 de memória e strings: `PERF007`, `PERF008`, `PERF012`, `PERF018`, `PERF019` e `PERF020`. | `FR-001`, `FR-003`, `FR-004`, `FR-005`, `FR-006`, `FR-009` | Casos de alocação/cópia/concatenação/normalização são distinguidos de exceções semânticas e de baixa evidência. | `TASK-002`, `TASK-003` | completed |
| `TASK-007` | Implementar as regras V1 de LINQ, coleções, HTTP e logging: `PERF013`, `PERF014`, `PERF015`, `PERF022`, `PERF025` e `PERF027`. | `FR-001`, `FR-003`, `FR-004`, `FR-005`, `FR-006`, `FR-009`, `NFR-007` | Cada regra cobre o padrão documentado, evita duplicação sem valor e apresenta correção condicional quando depender de contrato/carga. | `TASK-002`, `TASK-003` | completed |
| `TASK-008` | Criar a matriz de testes V1 para 15 regras, plataformas, C# 14, configuração e contratos de diagnóstico. | `FR-011`, `NFR-001`, `NFR-003`, `NFR-008` | Todas as 15 regras têm casos positivos/negativos/exceções e a suíte roda em `net10.0`; nenhum benchmark de runtime é exigido. | `TASK-004`, `TASK-005`, `TASK-006`, `TASK-007` | completed |
| `TASK-009` | Consolidar mensagens, orientação de refatoração, limitações estáticas, referências oficiais e ausência de Code Fix. | `FR-003`, `FR-004`, `FR-009`, `FR-010`, `FR-012`, `NFR-002`, `NFR-005` | Mensagens não prometem ganho percentual nem frequência real e informam evidência, impacto, escalabilidade, exceções, confiança e referência. | `TASK-005`, `TASK-006`, `TASK-007` | completed |
| `TASK-010` | Executar revisão de cobertura de requisitos, build/testes/gates estáticos e atualizar o roadmap V1/V2/V3; corrigir previamente os caminhos inválidos do `config.yaml` mediante aprovação. | `FR-006`, `FR-007`, `FR-011`, `NFR-004`, `NFR-008`, `CON-005` | `AC-001`–`AC-013` têm evidência; 15 regras V1 estão implementadas/testadas; V2/V3 permanecem sem implementação acidental; gates reportam resultado reproduzível. | `TASK-008`, `TASK-009` | pending |

## Regras de execução

- Não implementar V2/V3 nesta entrega.
- Não criar Code Fix.
- Não adicionar profiling, benchmarks de runtime, telemetria ou dependências externas não justificadas.
- Cada regra deve ser implementada e testada isoladamente antes da próxima regra do grupo.
- Mudanças de escopo, targets, contrato de diagnóstico ou dependências exigem aprovação humana.
- `TASK-010` não deve executar gates enquanto os caminhos inválidos de `.spectro/config.yaml` não forem corrigidos e aprovados.

## Progresso

- Concluídas: `TASK-001`, `TASK-002`, `TASK-003`, `TASK-004`, `TASK-005`, `TASK-006`, `TASK-007`, `TASK-008`, `TASK-009`.
- Bloqueadas: nenhuma; `TASK-010` possui pré-condição de configuração dos gates.
