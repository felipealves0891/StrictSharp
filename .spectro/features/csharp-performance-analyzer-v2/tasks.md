<!-- spectro-managed: true -->
# Tarefas: Implementação V2 do C# Performance Analyzer

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Status |
|---|---|---|---|---|---|
| `TASK-001` | Criar a fundação V2: catálogo explícito de analyzers, seleção V2 no catálogo de regras e extensões mínimas reutilizáveis em `RuleAnalysis`, sem alterar o comportamento V1. | `FR-001`, `FR-015`, `FR-016`, `NFR-003`, `NFR-004`, `CON-001`, `CON-003` | `AC-001`, `AC-015`, `AC-016`, `AC-018` | — | completed |
| `TASK-002` | Implementar `PERF002`, `PERF004`, `PERF005` e `PERF006`, cobrindo paralelismo sem limite, operação bloqueante em `lock`, polling e criação repetida de threads/tarefas. | `FR-002`–`FR-005`, `NFR-001`, `NFR-002`, `NFR-004` | `AC-002`–`AC-005`, `AC-015`, `AC-018` | `TASK-001` | completed |
| `TASK-003` | Implementar `PERF009` e `PERF010`, distinguindo boxing evitável de boxing contratual e capturas repetidas de closures de casos sem alocação efetiva ou pouco frequentes. | `FR-006`, `FR-007`, `NFR-001`, `NFR-004` | `AC-006`, `AC-007`, `AC-015`, `AC-018` | `TASK-001` | completed |
| `TASK-004` | Implementar `PERF016` e `PERF017`, correlacionando ordenações e pipelines LINQ com repetição, invariância observável e mutação da fonte. | `FR-008`, `FR-009`, `NFR-001`, `NFR-004` | `AC-008`, `AC-009`, `AC-015`, `AC-018` | `TASK-001` | completed |
| `TASK-005` | Implementar `PERF021` para construções repetidas de regex, opções de serialização e metadados/configurações pesadas reutilizáveis conforme as APIs documentadas. | `FR-010`, `NFR-001`, `NFR-004`, `CON-002` | `AC-010`, `AC-015`, `AC-018` | `TASK-001` | completed |
| `TASK-006` | Implementar `PERF023` e `PERF024`, identificando padrão de acesso incompatível e crescimento repetido apenas quando capacidade relevante estiver explicitamente disponível. | `FR-011`, `FR-012`, `NFR-001`, `NFR-004` | `AC-011`, `AC-012`, `AC-015`, `AC-018` | `TASK-001` | completed |
| `TASK-007` | Implementar `PERF028` e `PERF029`, cobrindo descarte observável de `IDisposable`/`IAsyncDisposable` e exceções usadas para fluxo esperado, sem alegar prova completa de fluxo dinâmico. | `FR-013`, `FR-014`, `NFR-001`, `NFR-004`, `CON-004` | `AC-013`, `AC-014`, `AC-015`, `AC-018` | `TASK-001` | completed |
| `TASK-008` | Adicionar matriz V2, testes de catálogo, severidade/supressão e regressão V1/V3, verificando exatamente os 13 analyzers e a compatibilidade C# 14. | `FR-001`, `FR-016`, `FR-017`, `NFR-005`, `CON-001`, `CON-003` | `AC-001`, `AC-016`, `AC-017`, `AC-018` | `TASK-002`, `TASK-003`, `TASK-004`, `TASK-005`, `TASK-006`, `TASK-007` | completed |
| `TASK-009` | Atualizar documentação e rastreabilidade do catálogo para marcar V2 implementada, manter V3 catalogada e documentar evidência, exceções, confiança e ausência de limiares arbitrários. | `FR-015`, `FR-017`, `NFR-006`, `CON-002`, `CON-004`, `CON-005` | `AC-015`, `AC-017`, `AC-019` | `TASK-002`, `TASK-003`, `TASK-004`, `TASK-005`, `TASK-006`, `TASK-007` | completed |
| `TASK-010` | Executar build, testes, cobertura e segurança configurados; revisar diff, rastreabilidade, ausência de dependências novas e regressão das regras V1 antes de solicitar aprovação de conclusão. | `NFR-001`–`NFR-007`, `CON-005`, `CON-006` | `AC-018`, `AC-019` | `TASK-008`, `TASK-009` | pending |

## Detalhamento das tarefas

### `TASK-001` — fundação e catálogo

- Componentes afetados: `V2AnalyzerCatalog.cs`, `RuleCatalog.cs`, possivelmente `RuleAnalysis.cs`, testes de catálogo.
- Conclusão: os 13 analyzers são instanciáveis, cada um expõe um único descriptor V2, a V1 permanece com 15 entradas e V3 não é habilitada.
- Risco: alterar `RuleCatalog` pode quebrar a ordem/contagem existente; executar regressão do catálogo antes de prosseguir.

### `TASK-002` — concorrência

- Componentes afetados: quatro analyzers V2 e seus testes.
- Conclusão: cada regra possui detecção positiva, silêncio em neutralidade e exceções documentadas; consultas caras só ocorrem após filtros sintáticos.
- Risco: limites externos não são demonstráveis; omitir nesses casos é comportamento esperado pela clarificação.

### `TASK-003` — boxing e closures

- Componentes afetados: dois analyzers V2, utilitários de conversão/captura e testes.
- Conclusão: não reportar APIs tipadas, contratos que exigem `object`/interface ou cenários em que a captura não seja materializada de forma observável.
- Risco: detalhes de otimização do compilador/runtime podem não ser prováveis estaticamente; mensagens devem usar linguagem condicional.

### `TASK-004` — LINQ e ordenação

- Componentes afetados: dois analyzers V2 e testes de repetição, invariância e mutação.
- Conclusão: reportar somente repetição contextual clara; não reportar consulta dependente da iteração ou dados alterados.
- Risco: análise de equivalência de expressão pode ser limitada; preferir omissão a inferência insegura.

### `TASK-005` — regex e metadados

- Componentes afetados: `Perf021Analyzer.cs`, utilitários de identificação de APIs e testes.
- Conclusão: cobrir APIs documentadas e preservar construções dinâmicas/cacheadas; não adicionar dependência de serializador.
- Risco: caches internos de APIs podem variar; só omitir quando o cache for observável ou documentado no contrato reconhecido.

### `TASK-006` — coleções

- Componentes afetados: dois analyzers V2, predicados de tipos/operações e testes.
- Conclusão: recomendar alternativas apenas condicionalmente e detectar capacidade somente quando explicitamente conhecida.
- Risco: localidade de memória e tamanho real não são prováveis em geral; não emitir sem evidência estrutural.

### `TASK-007` — recursos e exceções

- Componentes afetados: dois analyzers V2 e testes de descarte, transferência de propriedade e `TryParse`.
- Conclusão: `PERF028` é suspeita fundamentada; `PERF029` cobre fluxo esperado observável sem classificar toda captura como problema.
- Risco: fluxo interprocedural/dinâmico incompleto; mensagens e testes devem deixar essa limitação explícita.

### `TASK-008` — matrizes e configuração

- Componentes afetados: `V2TestMatrixTests.cs`, `RuleCatalogTests.cs`, `RuleConfigurationTests.cs` e harness somente se necessário.
- Conclusão: 13 IDs implementados, 15 V1 preservadas, 2 V3 não ativadas, severidade/supressão configuráveis e código neutro silencioso.
- Risco: falsos positivos cruzados entre analyzers; usar analyzers isolados nos testes por regra e matriz conjunta para neutralidade.

### `TASK-009` — documentação

- Componentes afetados: `docs/csharp-performance-analyzer.md`, `README.md` se o status V2 for exposto, e referências Spectro.
- Conclusão: status, exemplos, limitações e rastreabilidade correspondem ao comportamento testado; não adicionar promessas de benchmark.
- Risco: documentação divergente do catálogo; conferir IDs e severidades automaticamente nos testes ou na revisão.

### `TASK-010` — gates e entrega

- Componentes afetados: nenhum componente funcional novo; evidências em `.spectro/features/csharp-performance-analyzer-v2/`.
- Conclusão: build, testes, segurança e cobertura executados conforme configuração; diff revisado e bloqueios registrados.
- Risco: cobertura pode exigir casos adicionais para branches contextuais; ajustar testes, não reduzir gates.

## Regras de execução

- Não implementar Code Fix, profiling, benchmark, telemetria ou dependência nova nesta etapa.
- Não usar limiares numéricos arbitrários para decidir diagnósticos.
- Não alterar IDs, severidades ou classificação V1/V2/V3 sem nova clarificação e aprovação.
- Preservar alterações existentes fora do escopo e evitar mudanças em arquivos gerados `bin/`/`obj/`.
- Cada tarefa deve deixar testes reproduzíveis e manter a separação entre analyzer e serviço de análise.

## Progresso

- Concluídas: `TASK-001`, `TASK-002`, `TASK-003`, `TASK-004`, `TASK-005`, `TASK-006`, `TASK-007`, `TASK-008`, `TASK-009`.
- Bloqueadas: nenhuma.
- Próxima tarefa: `TASK-010` (pendente: o gate de segurança retornou código 1 sem saída em duas tentativas; build e testes passaram).
