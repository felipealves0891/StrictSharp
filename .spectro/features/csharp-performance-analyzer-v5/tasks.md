<!-- spectro-managed: true -->
# Tarefas: Modelo de custo computacional V5 do C# Performance Analyzer

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Status |
|---|---|---|---|---|---|
| `TASK-001` | Evoluir o modelo de fases e o catálogo para V5, registrar `PERF101`–`PERF107` com severidade `High`, mapeando `PERF101`–`PERF106` às seis dimensões e `PERF107` à comparação transversal, além de referências/documentação. | `FR-001`, `FR-012`, `CON-006` | `AC-001`, `AC-011`; matriz do catálogo preserva V1–V4 e contém exatamente os sete IDs V5 com categorias definidas. | — | completed |
| `TASK-002` | Criar `V5AnalyzerCatalog` e os sete analyzers finos, um por diagnóstico, seguindo `RuleAnalyzerBase` e deixando inferência no núcleo V5. | `NFR-004`, `NFR-005` | `AC-001`, `AC-014`; cada analyzer expõe um único descriptor e não duplica lógica de análise. | `TASK-001` | completed |
| `TASK-003` | Definir os contratos imutáveis de evidência V5, dimensões, confiança, desconhecido, premissas e limitações, integrando chamadas/ciclos/fronteiras do grafo V4. | `FR-002`, `FR-004`, `FR-008`, `FR-010`, `FR-011`, `NFR-001`, `NFR-002` | `AC-002`, `AC-004`, `AC-008`, `AC-010`, `AC-013`; desconhecidos não são convertidos em zero e dimensões permanecem separadas. | `TASK-001` | completed |
| `TASK-004` | Implementar inferência de loops, multiplicadores e classificação Big-O (`O(1)`, `O(n)`, `O(n log n)`, `O(n²)` ou desconhecida), com cancelamento e memoização. | `FR-002`, `FR-004`, `FR-011`, `NFR-003` | `AC-002`, `AC-004`, `AC-013`, `AC-014`; não há limite numérico fixo de iterações e casos dinâmicos ficam limitados/desconhecidos. | `TASK-003` | completed |
| `TASK-005` | Implementar contagem de alocações e estimativa de payload de arrays/buffers, separando bytes conhecidos de cabeçalho/alinhamento e efeitos de GC/LOH. | `FR-003`, `FR-005`, `CON-002`, `CON-003`, `CON-004` | `AC-003`, `AC-005`; o exemplo reporta 1.000 alocações e 100.000.000 bytes, aprox. 95,4 MiB, sem declarar custo total. | `TASK-003`, `TASK-004` | completed |
| `TASK-006` | Implementar evidências de I/O, tarefas e concorrência, propagação por chamadas conhecidas e tratamento explícito de alvo externo, fila, limite ou runtime desconhecido. | `FR-006`, `FR-007`, `FR-008`, `FR-011`, `NFR-006` | `AC-006`, `AC-007`, `AC-008`, `AC-010`; não há limiar numérico inventado para I/O/concorrência e quantidades não deriváveis permanecem condicionais. | `TASK-003`, `TASK-004` | completed |
| `TASK-007` | Implementar comparação pública entre alternativas baseada em estrutura, Big-O e contagens conhecidas, sem percentual, custo monetário ou benchmark. | `FR-009`, `FR-010`, `CON-001` | `AC-009`, `AC-010`; saída documentada e determinística, com limitações explícitas. | `TASK-003`, `TASK-004`, `TASK-005` | completed |
| `TASK-008` | Integrar cada analyzer `PERF101`–`PERF107` ao núcleo, definir deduplicação, mensagens públicas, orientação condicional e mapeamento das sete regras às dimensões documentadas. | `FR-008`, `FR-010`, `FR-012`, `FR-013`, `NFR-004`, `NFR-006` | `AC-008`, `AC-010`, `AC-011`, `AC-012`, `AC-014`; um diagnóstico só é emitido quando acrescenta contexto e não recomenda Code Fix sem equivalência demonstrada. | `TASK-002`, `TASK-004`, `TASK-005`, `TASK-006`, `TASK-007` | completed |
| `TASK-009` | Criar documentação V5 e suíte de testes positivos, negativos, exceções, catálogo, matriz, configuração, deduplicação e regressão V1–V4. | `FR-013`, `CON-007`, `CON-008`, `NFR-001`, `NFR-002` | `AC-011`, `AC-012`, `AC-013`; documentação cobre as seis dimensões, sete IDs, desconhecidos e comparação pública. | `TASK-001`, `TASK-008` | completed |
| `TASK-010` | Executar build, testes, segurança e cobertura; revisar custo do analyzer, determinismo, privacidade e rastreabilidade dos critérios de aceite. | `NFR-003`, `NFR-007` | `AC-014`, `AC-015`; gates reproduzíveis sem envio de código, segredos ou dados de produção. | `TASK-009` | blocked |

## Regras de execução

- Não implementar código durante a fase de planejamento.
- Manter um analyzer por diagnóstico (`PERF101`–`PERF107`) e lógica de inferência em serviços V5 estáticos separados.
- Preservar V1–V4, especialmente `PERF100`; nenhuma mudança de severidade, ID ou contrato anterior é permitida sem nova aprovação.
- Usar Big-O e contagens estruturais, não limiares fixos de iteração; I/O e concorrência sem limiar numérico pré-aprovado devem permanecer desconhecidos/condicionais quando necessário.
- Não agregar dimensões em score único nem prometer GC, LOH, CPU, latência, throughput ou ganho percentual medido.
- Propagar cancelamento e evitar alocações/travessias repetidas; qualquer cache deve ser seguro para execução concorrente do Roslyn.
- `TASK-008` depende da definição dos contratos e serviços, mas pode dividir a implementação em commits menores por diagnóstico sem quebrar a rastreabilidade.

## Progresso

- Concluídas: `TASK-001`, `TASK-002`, `TASK-003`, `TASK-004`, `TASK-005`, `TASK-006`, `TASK-007`, `TASK-008`, `TASK-009`
- Bloqueadas: `TASK-010` — `dotnet list package --vulnerable` falhou sem saída/diagnóstico em duas invocações; build, testes e cobertura passaram.
