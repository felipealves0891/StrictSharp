<!-- spectro-managed: true -->
# Revisão: Implementação V3 do C# Performance Analyzer

## Resumo

- Revisão independente da implementação V3 na revisão `75fd8e7871474f6f118310ab41a32324f2c7f1c6`.
- `gates.json` corresponde à mesma revisão e registra build, testes, segurança e cobertura aprovados.
- A suíte possui 168 testes aprovados.
- Decisão: `changes_requested`.

## Artefatos examinados

- `.spectro/features/csharp-performance-analyzer-v3/spec.md`
- `.spectro/features/csharp-performance-analyzer-v3/clarifications.md`
- `.spectro/features/csharp-performance-analyzer-v3/plan.md`
- `.spectro/features/csharp-performance-analyzer-v3/tasks.md`
- `.spectro/features/csharp-performance-analyzer-v3/gates.json` e evidências da execução `gate-20261029T121528Z`
- `src/StrictSharp.Analyzers/Analyzers/Perf011Analyzer.cs`
- `src/StrictSharp.Analyzers/Analyzers/Perf030Analyzer.cs`
- `src/StrictSharp.Analyzers/Analyzers/V3AnalyzerCatalog.cs`
- `src/StrictSharp.Analyzers/Rules/RuleCatalog.cs` e `RuleDefinition.cs`
- testes V3, testes de catálogo/configuração e documentação pública

## Rastreabilidade e gates

- Os gates obrigatórios estão aprovados na revisão correspondente.
- `validate_traceability.ps1` retornou `gaps`: todos os requisitos foram marcados sem tarefa detalhada porque `tasks.md` registra as tarefas principais apenas em tabela; `AC-001`, `AC-007`, `AC-008`, `AC-010` e `AC-011` também não receberam uma validação detectável pelo padrão do script.
- As remediações `TASK-REV-001` a `TASK-REV-004` foram registradas em `tasks.md`.

## Requisitos verificados

- Os IDs `PERF011` e `PERF030` estão no catálogo V3 e no `V3AnalyzerCatalog`.
- V1 e V2 permanecem em catálogos separados.
- Os analyzers usam `RuleAnalyzerBase`, execução concorrente e um descriptor por regra.
- Existem testes positivos, negativos, de exceção, matriz V3 e configuração de severidade/supressão.
- Não foram observadas chamadas externas, dependências novas ou Code Fix automático.
- A documentação distingue suspeita estática de impacto medido e não promete detecção completa de memory leaks.

## Achados

### `FINDING-001` — Rastreabilidade não consumível pelo validador

- **Severidade:** High
- **Categoria:** Consistency / scope
- **Localização:** `.spectro/features/csharp-performance-analyzer-v3/tasks.md`, tabela principal; `.spectro/.../reviews/review-20261029T121815Z/traceability.json`
- **Evidência:** `validate_traceability.ps1` retornou `result: gaps`, `uncovered_requirements` com todos os `FR`, `NFR` e `CON`, e cinco critérios sem `validation_hint`.
- **Impacto:** a revisão não consegue comprovar automaticamente que cada requisito e critério possui tarefa e validação, apesar da matriz de rastreabilidade existir no plano.
- **Recomendação:** adicionar seções detalhadas `## TASK-###` para as tarefas principais ou ajustar o formato/validador para interpretar a tabela como fonte válida.
- **Return stage:** `plan`
- **Remediação:** `TASK-REV-001`

### `FINDING-002` — `PERF011` aceita operações sem evidência suficiente de retenção

- **Severidade:** High
- **Categoria:** Functional / performance
- **Localização:** `src/StrictSharp.Analyzers/Analyzers/Perf011Analyzer.cs:20-27`
- **Evidência:** a regra reporta qualquer chamada `Add`, `AddRange` ou `Subscribe` cujo receptor seja um campo, esteja em loop e não tenha `Clear`/`Remove` observável; não confirma o tipo de coleção, cache, evento ou outro mecanismo retentor.
- **Impacto:** métodos arbitrários com nome `Add` em campos podem gerar falsos positivos, contrariando `NFR-001`, `FR-003` e a exigência de evidência estrutural suficiente.
- **Recomendação:** restringir o diagnóstico a tipos e padrões retentores reconhecidos semanticamente, preservando exceções de propriedade, transferência, expiração e descarte.
- **Return stage:** `implement`
- **Remediação:** `TASK-REV-002`

### `FINDING-003` — `PERF030` classifica inicialização simples como objeto pesado

- **Severidade:** High
- **Categoria:** Functional / performance
- **Localização:** `src/StrictSharp.Analyzers/Analyzers/Perf030Analyzer.cs:77-98`
- **Evidência:** qualquer construtor parametrizado é aceito por `constructor.Parameters.Length > 0`, e qualquer inicializador não vazio é aceito por `creation.Initializer.Expressions.Count > 0`; não há prova de construtor complexo, quantidade relevante de propriedades ou custo observável.
- **Impacto:** criações triviais com argumentos invariantes em loops podem ser diagnosticadas como `PERF030`, contrariando a decisão `Q-V3-002`, `NFR-001` e `FR-006`.
- **Recomendação:** exigir sinais estruturais mais fortes e documentados de inicialização custosa/invariância relevante, além de testes negativos para construtores simples, inicializadores triviais, estado variável e tipos não thread-safe.
- **Return stage:** `implement`
- **Remediação:** `TASK-REV-003`

### `FINDING-004` — Benchmark comparativo ainda não foi executado

- **Severidade:** Medium
- **Categoria:** Performance / validation
- **Localização:** `.spectro/features/csharp-performance-analyzer-v3/evidence-v3-validation.md`
- **Evidência:** o arquivo registra hipóteses e cenários, mas declara que a medição comparativa deve ser executada; os gates configurados não incluem benchmark.
- **Impacto:** `AC-010` e `NFR-006` permanecem parcialmente não demonstrados para impacto de retenção e reconstrução.
- **Recomendação:** executar e preservar benchmarks representativos de memória viva para `PERF011` e alocações/CPU para `PERF030`, sem converter resultados em limiares do analyzer.
- **Return stage:** `gates`
- **Remediação:** `TASK-REV-004`

## Riscos residuais

- Mesmo após restringir os padrões, a análise V3 continuará sendo uma aproximação estática; não provará memory leaks, frequência real, custo de runtime ou thread safety em todos os caminhos.
- A cobertura global e de código novo passou, mas cobertura não demonstra por si só a precisão semântica das heurísticas.
- Os gates aprovados não incluem benchmark de runtime.

## Decisão final

`changes_requested`. Os achados High `FINDING-002` e `FINDING-003` impedem aprovação porque podem gerar diagnósticos falsos positivos em padrões fora do contrato V3. `FINDING-001` também precisa ser resolvido para que a rastreabilidade automática seja verificável. Após as remediações, retornar a `spectro-implement`, depois executar `spectro-gates` e repetir a revisão.
