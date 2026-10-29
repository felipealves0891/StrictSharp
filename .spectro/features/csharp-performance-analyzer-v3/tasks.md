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

- Não implementar Code Fix, profiling, telemetria, persistência, chamadas de rede ou dependências novas no analyzer; `BenchmarkDotNet 0.15.8` é permitido somente no harness isolado por `Q-V3-004`.
- Não usar limiares numéricos fixos para “muitas propriedades”, frequência, memória retida ou custo do analyzer; usar sinais estruturais conservadores e documentados.
- Não alterar IDs, severidades, fases ou contrato V1/V2 sem nova clarificação e aprovação humana.
- `PERF011` deve permanecer uma suspeita fundamentada, nunca uma prova de memory leak.
- `PERF030` não deve reportar toda criação em loop nem afirmar que um tipo é universalmente pesado.
- Preservar o padrão de analyzer por diagnóstico, serviços estáticos por padrão, execução concorrente e mensagens condicionais.
- Cada tarefa deve adicionar ou atualizar testes reproduzíveis; alterações em `docs`, `README` e `.spectro` devem refletir o comportamento efetivamente planejado.

## Progresso

- Concluídas: `TASK-001`, `TASK-002`, `TASK-003`, `TASK-004`, `TASK-005`, `TASK-006`, `TASK-007`, `TASK-REV-001`, `TASK-REV-002`, `TASK-REV-003`.
- Concluídas: `TASK-008`, `TASK-009`, `TASK-010`, `TASK-011`, `TASK-REV-005`, `TASK-REV-006`.
- Bloqueadas: nenhuma.
- Próxima tarefa: `TASK-REV-004` (etapa `gates`).

## TASK-REV-002 - Remediate FINDING-002

- **Status:** completed
- **Finding:** FINDING-002
- **Severity:** High
- **Return stage:** implement
- **Description:** Restringir PERF011 a tipos e padrões de estruturas retentoras reconhecidos semanticamente, evitando diagnosticar qualquer método Add ou Subscribe em campo sem evidência de retenção.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Add or update behavior-focused coverage as required.

## TASK-REV-003 - Remediate FINDING-003

- **Status:** completed
- **Finding:** FINDING-003
- **Severity:** High
- **Return stage:** implement
- **Description:** Refinar PERF030 para exigir evidência observável de inicialização complexa ou invariância relevante, evitando classificar todo construtor parametrizado ou inicializador como objeto pesado.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Add or update behavior-focused coverage as required.

## TASK-REV-004 - Remediate FINDING-004

- **Status:** pending
- **Finding:** FINDING-004
- **Severity:** Medium
- **Return stage:** gates
- **Description:** Executar e preservar benchmarks comparativos representativos para PERF011 e PERF030, conforme AC-010 e NFR-006, sem transformar resultados em limiares do analyzer.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Add or update behavior-focused coverage as required.

## TASK-REV-001 - Remediate FINDING-001

- **Status:** completed
- **Finding:** FINDING-001
- **Severity:** High
- **Return stage:** plan
- **Description:** Adicionar seções detalhadas para TASK-001 a TASK-007 ou ajustar a rastreabilidade para que cada requisito e critério tenha tarefa e validação detectáveis pelo validador.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Add or update behavior-focused coverage as required.

## Detalhamento das tarefas principais

## TASK-001 - Fundação, catálogo e contratos V3

- **Requisitos:** `FR-001`, `FR-008`, `FR-010`, `NFR-003`, `NFR-004`, `CON-001`, `CON-003`.
- **Critérios:** `AC-001`, `AC-006`, `AC-007`, `AC-009`.
- **Componentes afetados:** `RuleCatalog`, `RuleDefinition`, `V3AnalyzerCatalog`, `RuleAnalyzerBase` e contratos compartilhados.
- **Dependências:** nenhuma.
- **Conclusão:** `PERF011` e `PERF030` aparecem uma vez, com metadados V3 consistentes; V1/V2 permanecem separados; configuração Roslyn continua individual por ID.
- **Validação:** testes de catálogo, matriz V3, configuração de supressão/severidade, build e gates confirmam `AC-001`, `AC-006`, `AC-007` e `AC-009`.
- **Risco:** alteração acidental da ordem ou do comportamento dos catálogos existentes; mitigar com regressão V1/V2.

## TASK-002 - Analyzer e testes de PERF011

- **Requisitos:** `FR-002`, `FR-003`, `FR-004`, `NFR-001`, `NFR-002`, `NFR-004`, `CON-004`.
- **Critérios:** `AC-002`, `AC-003`, `AC-006`, `AC-009`.
- **Componentes afetados:** `Perf011Analyzer`, serviço estático de análise se necessário e `Perf011AnalyzerTests`.
- **Dependências:** `TASK-001`.
- **Conclusão:** somente tipos/padrões retentores semanticamente reconhecidos geram suspeita; métodos genéricos, retenção delimitada, transferência, remoção, expiração e descarte externo ficam silenciosos.
- **Validação:** testes positivos, negativos e de exceção verificam `AC-002` e `AC-003`; benchmark de memória viva e gates verificam `AC-006` e `AC-009`.
- **Risco:** reconhecer poucos padrões reduz cobertura; reconhecer APIs por nome aumenta ruído. Preferir omissão quando o contrato não for demonstrável.

## TASK-003 - Analyzer e testes de PERF030

- **Requisitos:** `FR-005`, `FR-006`, `FR-007`, `NFR-001`, `NFR-002`, `NFR-004`.
- **Critérios:** `AC-004`, `AC-005`, `AC-006`, `AC-009`.
- **Componentes afetados:** `Perf030Analyzer` e `Perf030AnalyzerTests`.
- **Dependências:** `TASK-001`.
- **Conclusão:** o diagnóstico exige repetição, invariância observável e sinal estrutural de inicialização não trivial; construtor parametrizado ou inicializador trivial isolado não reporta.
- **Validação:** testes de construtor simples, inicializador trivial, estado variável, isolamento e falta de thread safety cobrem `AC-004`; testes de mensagem e benchmark de CPU/alocações cobrem `AC-005`, `AC-006` e `AC-009`.
- **Risco:** heurística de custo pode depender de semântica não observável; omitir o caso ambíguo preserva baixo ruído.

## TASK-004 - Matriz, catálogo e configuração

- **Requisitos:** `FR-001`, `FR-008`, `FR-010`, `FR-011`, `NFR-005`.
- **Critérios:** `AC-001`, `AC-007`, `AC-008`, `AC-009`.
- **Componentes afetados:** testes de matriz, testes de catálogo/configuração e documentação de versões suportadas.
- **Dependências:** `TASK-002`, `TASK-003`.
- **Conclusão:** os dois IDs, ordem, fase, metadados, exemplos positivos/negativos/exceções e configuração individual ficam cobertos.
- **Validação:** `V3TestMatrixTests`, `RuleCatalogTests`, `RuleConfigurationTests` e revisão dos exemplos validam `AC-001`, `AC-007`, `AC-008` e `AC-009`.
- **Risco:** cobertura de teste pode mascarar contrato incompleto; comparar a matriz com a especificação e a documentação.

## TASK-005 - Evidência comparativa e documentação

- **Requisitos:** `FR-008`, `FR-009`, `FR-011`, `NFR-006`, `CON-002`, `CON-004`.
- **Critérios:** `AC-003`, `AC-005`, `AC-006`, `AC-008`, `AC-010`.
- **Componentes afetados:** `evidence-v3-validation.md`, documentação pública e artefatos de benchmark.
- **Dependências:** `TASK-002`, `TASK-003`, `TASK-004`.
- **Conclusão:** hipóteses, limitações, referências, versões, exemplos e distinção entre evidência estática e impacto medido permanecem alinhados.
- **Validação:** revisão documental e evidência de benchmark reproduzível validam `AC-003`, `AC-005`, `AC-006`, `AC-008` e `AC-010`.
- **Risco:** transformar resultado do cenário em limiar do analyzer; registrar números apenas como evidência comparativa.

## TASK-006 - Segurança, custo e deduplicação

- **Requisitos:** `FR-004`, `FR-007`, `FR-009`, `NFR-001`, `NFR-002`, `NFR-004`, `NFR-007`, `CON-002`, `CON-005`, `CON-007`.
- **Critérios:** `AC-003`, `AC-005`, `AC-006`, `AC-009`, `AC-011`.
- **Componentes afetados:** analyzers V3, regras V1/V2, dependências e mensagens de diagnóstico.
- **Dependências:** `TASK-002`, `TASK-003`, `TASK-005`.
- **Conclusão:** não há chamadas externas/dependências novas/Code Fix; mensagens não expõem dados além do necessário; não há duplicação sem valor adicional.
- **Validação:** inspeção de diff, análise de pacotes, testes de segurança, regressão e gates verificam `AC-003`, `AC-005`, `AC-006`, `AC-009` e `AC-011`.
- **Risco:** otimização sugerida pode alterar semântica; manter orientação condicional e revisão manual.

## TASK-007 - Gates e rastreabilidade final

- **Requisitos:** `FR-001`–`FR-011`, `NFR-001`–`NFR-007`, `CON-001`–`CON-007` (incluindo aprovação humana para qualquer mudança de escopo, plataformas, severidade, fase ou contrato em `CON-006`).
- **Critérios:** `AC-001`–`AC-011`.
- **Componentes afetados:** solução, suíte de testes, `.spectro` e evidências finais.
- **Dependências:** `TASK-004`, `TASK-005`, `TASK-006`.
- **Conclusão:** build, testes, segurança, cobertura, rastreabilidade e revisão do diff passam com artefatos correspondentes.
- **Validação:** executar os gates configurados, validar a matriz de requisitos e repetir a revisão para confirmar `AC-001`–`AC-011`.
- **Risco:** artefatos podem ficar desalinhados após mudanças; invalidar e renovar evidências quando hashes divergirem.

## Tarefas de remediação da revisão

## TASK-REV-001 - Rastreabilidade consumível pelo validador

- **Finding:** `FINDING-001`; **etapa de retorno:** `plan`; **dependências:** nenhuma.
- **Requisitos:** todos os `FR`, `NFR` e `CON` aplicáveis; **critérios:** `AC-001`–`AC-011`.
- **Ação:** manter a matriz do plano e adicionar seções `TASK-###` com requisitos, critérios, componentes, dependências, conclusão, risco e validação explícitos.
- **Conclusão:** `validate_traceability.ps1` não retorna requisitos descobertos nem critérios sem `validation_hint`; a revisão pode localizar cada vínculo sem inferência da tabela.
- **Validação:** executar o validador de rastreabilidade e preservar `traceability.json` com resultado `complete` para `AC-001`–`AC-011`.
- **Risco:** regex depende de proximidade textual; repetir IDs e termos `test`, `validation`, `gate` ou `evidence` dentro de cada seção.

## TASK-REV-002 - Evidência semântica para PERF011

- **Finding:** `FINDING-002`; **etapa de retorno:** `implement`; **dependências:** `TASK-REV-001`.
- **Requisitos:** `FR-002`, `FR-003`, `FR-004`, `NFR-001`, `NFR-002`, `NFR-004`, `CON-004`; **critérios:** `AC-002`, `AC-003`, `AC-006`, `AC-009`.
- **Ação:** resolver semanticamente tipos de coleções/eventos/cache ou outros padrões explicitamente suportados e omitir receptores desconhecidos, preservando exceções de ciclo de vida.
- **Conclusão:** `Add`, `AddRange` ou `Subscribe` homônimos sem contrato retentor reconhecido não geram diagnóstico; testes cobrem falso positivo e positivo válido.
- **Validação:** atualizar `Perf011AnalyzerTests`, executar testes de comportamento e gates, e confirmar `AC-002`, `AC-003`, `AC-006` e `AC-009`.
- **Risco:** ampliar a lista de tipos por nome recria o problema; usar símbolos/interfaces e padrões documentados, não apenas texto sintático.

## TASK-REV-003 - Evidência conservadora para PERF030

- **Finding:** `FINDING-003`; **etapa de retorno:** `implement`; **dependências:** `TASK-REV-001`.
- **Requisitos:** `FR-005`, `FR-006`, `FR-007`, `NFR-001`, `NFR-002`, `NFR-004`; **critérios:** `AC-004`, `AC-005`, `AC-006`, `AC-009`.
- **Ação:** substituir os sinais amplos por evidências combinadas de inicialização não trivial e invariância observável; cobrir construtores simples, inicializadores triviais, estado variável e tipos não thread-safe.
- **Conclusão:** criação repetida trivial não é classificada; somente casos com sinais estruturais documentados e contrato de reutilização plausível são reportados.
- **Validação:** atualizar `Perf030AnalyzerTests`, executar regressão e gates, e confirmar `AC-004`, `AC-005`, `AC-006` e `AC-009`.
- **Risco:** introduzir limiar numérico arbitrário; usar sinais semânticos conservadores e omitir casos ambíguos.

## TASK-REV-004 - Benchmark comparativo V3

- **Finding:** `FINDING-004`; **etapa de retorno:** `gates`; **dependências:** `TASK-REV-002`, `TASK-REV-003`.
- **Requisitos:** `FR-009`, `FR-011`, `NFR-006`; **critérios:** `AC-006`, `AC-008`, `AC-010`.
- **Ação:** executar `TASK-008` a `TASK-011` para criar o harness isolado, medir os dois pares de cenários, preservar evidência e atualizar a documentação, sem alterar o analyzer.
- **Conclusão:** preservar comando, ambiente, carga, memória viva para `PERF011`, alocações/CPU para `PERF030`, resultados e limitações; declarar que números não são limiares estáticos.
- **Validação:** anexar evidência comparativa, atualizar `evidence-v3-validation.md`, executar gates e confirmar `AC-006`, `AC-008` e `AC-010`.
- **Risco:** benchmark não representar produção ou ser não reproduzível; fixar carga/comando e documentar a limitação, sem alegar ganho universal.

## TASK-008 - Criar harness isolado de benchmark

- **Status:** completed

- **Requisitos:** `FR-009`, `FR-011`, `NFR-006`, `NFR-007`, `CON-007`; **critérios:** `AC-006`, `AC-008`, `AC-010`.
- **Componentes afetados:** novo projeto `benchmarks/StrictSharp.Analyzers.V3.Benchmarks`, scripts de execução e contrato de saída; nenhum componente em `src/`.
- **Dependências:** `TASK-REV-002`, `TASK-REV-003`.
- **Conclusão:** executável `net10.0` em Release usa `BenchmarkDotNet 0.15.8`, aceita filtros/parâmetros do BDN, aquece, repete, registra ambiente e produz JSON/Markdown/CSV versionados sem rede durante a medição.
- **Validação:** build isolado, execução com parâmetros mínimos e inspeção do JSON/log confirmam `AC-006`, `AC-008` e `AC-010`.
- **Risco:** incluir benchmark na solução/gates pode ampliar escopo; manter execução explícita e fora dos gates do analyzer.

## TASK-009 - Cenário comparativo de PERF011

- **Status:** completed

- **Requisitos:** `FR-009`, `FR-011`, `NFR-006`, `NFR-007`; **critérios:** `AC-006`, `AC-010`.
- **Componentes afetados:** cenário `perf011` do harness e artefatos de evidência.
- **Dependências:** `TASK-008`.
- **Conclusão:** retenção repetida é comparada ao controle de retenção limitada com mesma carga; memória viva pós-GC, memória inicial/final, iterações, payload e duração são registrados.
- **Validação:** executar repetições configuráveis, comparar os cenários e preservar comando, parâmetros, resultados brutos e limitações para `AC-006` e `AC-010`.
- **Risco:** confundir crescimento do cenário com prova de leak; usar linguagem de suspeita e não criar limiar para `PERF011`.

## TASK-010 - Cenário comparativo de PERF030

- **Status:** completed

- **Requisitos:** `FR-009`, `FR-011`, `NFR-006`, `NFR-007`; **critérios:** `AC-006`, `AC-010`.
- **Componentes afetados:** cenário `perf030` do harness e artefatos de evidência.
- **Dependências:** `TASK-008`.
- **Conclusão:** reconstrução com inicialização não trivial é comparada à reutilização de instância imutável equivalente, com mesma carga e resultado observável; CPU/tempo e bytes alocados são registrados.
- **Validação:** executar aquecimento e repetições, verificar métricas por cenário e preservar resultados sem transformar diferença em ganho universal para `AC-006` e `AC-010`.
- **Risco:** otimização de JIT ou estado compartilhado distorcer comparação; manter operações equivalentes e documentar protocolo.

## TASK-011 - Preservar evidência e fechar a remediação

- **Status:** completed

- **Requisitos:** `FR-008`, `FR-009`, `FR-011`, `NFR-006`, `NFR-007`, `CON-002`, `CON-004`; **critérios:** `AC-003`, `AC-006`, `AC-008`, `AC-010`, `AC-011`.
- **Componentes afetados:** `.spectro/features/csharp-performance-analyzer-v3/evidence-v3-validation.md`, diretório de evidência, `tasks.md` e relatório de revisão.
- **Dependências:** `TASK-009`, `TASK-010`.
- **Conclusão:** JSON, log, comando, revisão, ambiente, carga, limitações e conclusão de cada regra são preservados; `TASK-REV-004` só passa a `completed` após ambos os cenários; nenhum dado sensível é exposto.
- **Validação:** revisar rastreabilidade, executar gates, confirmar `AC-003`, `AC-006`, `AC-008`, `AC-010` e `AC-011`, e repetir `spectro-review`.
- **Risco:** artefatos não corresponderem à revisão do código; incluir hash/revisão no resultado e invalidar evidência se código ou configuração mudar.

## TASK-REV-005 - Remediate FINDING-005

- **Status:** completed
- **Finding:** `FINDING-005`
- **Severity:** Medium
- **Return stage:** implement
- **Description:** Adicionar medição explícita de memória viva após coleta completa para o cenário `PERF011`, preservando o BenchmarkDotNet como mecanismo de timing/alocação e registrando a diferença entre retenção e controle limitado.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Executar o benchmark `PERF011` e preservar métricas de memória viva pós-GC para ambos os cenários.

## TASK-REV-006 - Remediate FINDING-006

- **Status:** completed
- **Finding:** `FINDING-006`
- **Severity:** Medium
- **Return stage:** implement
- **Description:** Preservar um log textual reproduzível da execução BenchmarkDotNet com comando, versão, ambiente, parâmetros, artefatos e limitações, alinhado ao contrato de `TASK-011`.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Executar o harness e verificar a presença e o conteúdo do log textual junto aos relatórios BDN.

