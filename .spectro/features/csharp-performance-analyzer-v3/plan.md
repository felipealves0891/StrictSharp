<!-- spectro-managed: true -->
# Plano: Implementação V3 do C# Performance Analyzer

## Contexto e objetivo técnico

- A especificação V3 foi clarificada e não possui bloqueios. A decisão `Q-V3-001` limita `PERF011` a padrões estruturais reconhecidos; `Q-V3-002` define construtores complexos, quantidade relevante de propriedades/dados e recriação com os mesmos valores como sinais para `PERF030`, sem limiar numérico fixo; `Q-V3-003` exige exemplos completos, benchmarks representativos e os gates existentes.
- O projeto já possui `RuleDefinition`, `RuleCatalog`, `RuleAnalyzerBase`, `RuleAnalysis`, catálogos explícitos V1/V2 e um harness Roslyn determinístico. Os IDs V3 já estão catalogados, mas ainda não há `V3AnalyzerCatalog`, analyzers V3 ou testes específicos.
- Objetivo técnico: habilitar exatamente `PERF011` e `PERF030` como analyzers executáveis, mantendo V1/V2 inalteradas, a análise concorrente segura e a distinção entre risco estrutural e impacto medido.

## Solução proposta

- Criar `Perf011Analyzer` e `Perf030Analyzer`, um por diagnóstico, derivados de `RuleAnalyzerBase`.
- Criar `V3AnalyzerCatalog` com os dois analyzers na mesma ordem de `RuleCatalog.V3`; adicionar `RuleCatalog.V3` sem incluir V3 nos catálogos V1/V2.
- `PERF011`: analisar somente padrões estruturais explicitamente reconhecidos no código e no modelo semântico, como referências de longa duração em campos/coleções/cache/eventos/delegates quando o vínculo e a ausência de limite, expiração, remoção ou descarte forem observáveis. O analyzer deve omitir a suspeita quando a propriedade, transferência, escopo delimitado ou retenção necessária forem plausíveis e não demonstráveis.
- `PERF030`: localizar criações repetidas em loops ou caminhos de repetição estrutural e avaliar sinais de custo/invariância: construtor com inicialização complexa observável, conjunto relevante de propriedades/dados inicializados e argumentos/valores repetidos. A regra não deve usar quantidade numérica fixa de propriedades nem afirmar que o tipo é universalmente pesado.
- Reutilizar `RuleAnalysis` somente para predicados compartilhados por ambas as regras ou por regras futuras; manter estado por compilação fora de serviços estáticos e preservar `EnableConcurrentExecution`.
- Manter metadados, severidade, confiança, limitações e referência no `RuleCatalog`; `PERF011` continua com severidade `Critical` e confiança baixa, e `PERF030` com severidade `High` e confiança baixa conforme o catálogo atual.
- Não criar Code Fix automático. As mensagens permanecem condicionais e o analyzer não executa profiling, telemetria, persistência ou chamadas externas.

## Arquitetura e contratos

### Componentes de produção

- `src/StrictSharp.Analyzers/Analyzers/Perf011Analyzer.cs`: detecção estrutural de retenção potencial e reporte no local que melhor representa a referência ou vínculo retentor.
- `src/StrictSharp.Analyzers/Analyzers/Perf030Analyzer.cs`: detecção de criação repetida com sinais observáveis de inicialização custosa/invariância.
- `src/StrictSharp.Analyzers/Analyzers/V3AnalyzerCatalog.cs`: catálogo explícito dos dois analyzers V3.
- `src/StrictSharp.Analyzers/Rules/RuleCatalog.cs`: exposição ordenada de `V3`, preservando os 30 IDs e as listas V1/V2 existentes.
- `src/StrictSharp.Analyzers/Infrastructure/RuleAnalysis.cs`: extensões estáticas somente quando a lógica for comum, pequena e semanticamente comprovável; não transformar o serviço em um grafo de análise global.

### Contrato de análise

- Entrada: árvore C#, `SemanticModel`, símbolos e operações Roslyn disponíveis nos callbacks registrados pelo analyzer.
- Saída: zero ou mais diagnósticos com exatamente um ID suportado pelo analyzer correspondente; cada analyzer expõe um único descriptor.
- Código gerado: permanece excluído por `RuleAnalyzerBase`.
- Configuração: severidade e supressão continuam delegadas ao Roslyn/`.editorconfig`; não haverá configuração proprietária de limiares.
- Concorrência: callbacks devem ser reentrantes e sem estado mutável compartilhado, pois `EnableConcurrentExecution` permanece habilitado.
- Segurança: nenhum código, literal, segredo ou dado de produção deixa o processo de análise.

### Fluxo de decisão

1. Aplicar filtro sintático barato para selecionar criações, atribuições, inscrições, adições a coleções, campos e caminhos de repetição candidatos.
2. Consultar o modelo semântico apenas após o filtro, resolver símbolos/tipos e confirmar que o padrão pertence a um caso documentado.
3. Procurar evidências de exceção: propriedade transferida, remoção/expiração, descarte, estado variável, configuração dependente da iteração, falta de thread safety ou isolamento por operação.
4. Reportar somente quando a evidência positiva superar a incerteza; caso contrário, omitir o diagnóstico e preservar baixo ruído.

## Alternativas consideradas

- **Análise geral de reachability ou memory leak:** rejeitada; excede o contrato estático aprovado, exigiria análise interprocedural/dinâmica e contradiz `CON-004`.
- **Lista de tipos “pesados” com limiar fixo de propriedades ou custo:** rejeitada; não há medição de runtime nem limiar numérico aprovado. Os sinais devem ser estruturais e conservadores.
- **Marcar toda criação em loop como `PERF030`:** rejeitada; duplicaria `PERF007`/`PERF021` e produziria ruído sem demonstrar invariância ou inicialização relevante.
- **Adicionar um analyzer único para as duas regras:** rejeitada; viola a convenção de um analyzer por diagnóstico e reduz a rastreabilidade do contrato.
- **Code Fix para mover instâncias, limpar referências ou alterar escopo:** rejeitada; propriedade, ciclo de vida, thread safety e semântica não podem ser demonstrados genericamente.
- **Dependência externa para profiling ou benchmark:** rejeitada na implementação; validações comparativas devem ser reproduzíveis sem alterar o runtime do analyzer.

## Riscos e compatibilidade

- **Falso positivo em retenção intencional:** mitigar com padrões reconhecidos, evidência de ausência de limite/expiração/remoção e testes para cache delimitado, singleton, transferência e descarte externo.
- **Falso positivo em objeto “pesado”:** mitigar exigindo repetição, sinais de inicialização e invariância observáveis; não usar contagem numérica fixa nem classificar toda classe com muitas propriedades.
- **Sobreposição com regras existentes:** `PERF011` deve acrescentar contexto de ciclo de vida além de `PERF007`, `PERF008`, `PERF010` e `PERF028`; `PERF030` deve acrescentar invariância/custo de reconstrução além de `PERF007` e `PERF021`.
- **Fluxo interprocedural incompleto:** omitir quando propriedade, mutabilidade, descarte ou configuração não forem demonstráveis, em vez de inferir contratos.
- **Custo do analyzer:** mitigar com filtros sintáticos, consultas semânticas condicionais, ausência de análise global e revisão de alocações; aplicar os gates existentes, sem novo limiar quantitativo.
- **Compatibilidade:** preservar `netstandard2.1`, C# 14, Roslyn 5.0 e o harness em `net10.0`; `BenchmarkDotNet 0.15.8` fica isolado exclusivamente no projeto de benchmark.

## Estratégia de testes

- Criar testes comportamentais isolados para cada analyzer, com caso positivo, negativo e exceções relevantes.
- `PERF011`: cobrir retenção estrutural reconhecida, retenção delimitada/expirada, transferência de propriedade, remoção explícita, descarte externo e código sem vínculo retentor.
- `PERF030`: cobrir criação repetida com construtor complexo, inicialização de múltiplas propriedades, mesmos argumentos/valores, configuração dependente da iteração, estado variável, tipo não thread-safe e instância criada fora da repetição.
- Criar `V3TestMatrixTests` para verificar exatamente os dois IDs, um descriptor por analyzer, ordem igual a `RuleCatalog.V3`, código neutro sem diagnósticos e compatibilidade com C# 14.
- Expandir `RuleCatalogTests` para confirmar listas 15/13/2, fases, metadados e severidades V3; reutilizar `RuleConfigurationTests` para supressão e severidade configurável.
- Manter regressão dos testes V1/V2 e executar a suíte sem habilitar V3 nos catálogos anteriores.
- Registrar benchmark comparativo representativo para cada regra, limitado a demonstrar o cenário de regressão da hipótese; o benchmark não deve ser usado para alterar a heurística por limiar de runtime.
- Executar `dotnet build StrictSharp.sln`, `dotnet test StrictSharp.sln` e os comandos de build, test, coverage e security definidos em `.spectro/config.yaml`.

## Rastreabilidade

| Requisito | Critério de aceite | Tarefa |
|---|---|---|
| `FR-001`, `CON-001`, `CON-003` | `AC-001` | `TASK-001`, `TASK-007` |
| `FR-002`, `FR-003` | `AC-002` | `TASK-002` |
| `FR-004`, `CON-004` | `AC-003` | `TASK-002`, `TASK-006` |
| `FR-005`, `FR-006` | `AC-004` | `TASK-003` |
| `FR-007` | `AC-005` | `TASK-003`, `TASK-006` |
| `FR-008`, `FR-009` | `AC-006` | `TASK-001`, `TASK-002`, `TASK-003`, `TASK-006` |
| `FR-010` | `AC-007` | `TASK-001`, `TASK-007` |
| `FR-011` | `AC-008` | `TASK-004`, `TASK-005`, `TASK-006`, `TASK-007` |
| `NFR-001`, `NFR-002`, `NFR-003`, `NFR-004`, `NFR-005` | `AC-009` | `TASK-001`–`TASK-007` |
| `NFR-006` | `AC-010` | `TASK-005`, `TASK-007` |
| `NFR-007` | `AC-011` | `TASK-006`, `TASK-007` |
| `CON-002`, `CON-005`, `CON-006`, `CON-007` | `AC-001`, `AC-005`, `AC-006`, `AC-009` | `TASK-001`, `TASK-003`, `TASK-006`, `TASK-007` |

## Ordem de execução

1. `TASK-001` — fundação, catálogo V3 e contratos compartilhados.
2. `TASK-002` — analyzer e testes de `PERF011`.
3. `TASK-003` — analyzer e testes de `PERF030`.
4. `TASK-004` — matriz V3, catálogo e configuração.
5. `TASK-005` — benchmarks e documentação funcional.
6. `TASK-006` — revisão de segurança, custo e não sobreposição.
7. `TASK-007` — build, testes e gates finais.

Não há dependências cíclicas. As tarefas 002 e 003 dependem da fundação e podem ser executadas em paralelo; as tarefas 004–007 dependem dos analyzers e seus testes.

## Estado

- Estado do plano: `ready_for_implementation`.
- Aprovação adicional necessária: aprovação humana para iniciar a implementação, conforme `require_human_approval_for_scope_changes` e os estágios configurados pelo projeto.

## Plano de remediação da revisão

### Escopo recebido

- A revisão `review-20261029T121815Z` decidiu `changes_requested` e não alterou a especificação nem as clarificações aprovadas.
- `FINDING-001` exige tornar a rastreabilidade consumível pelo validador, incluindo seções detalhadas para as tarefas principais e validações explícitas para todos os critérios de aceite.
- `FINDING-002` exige restringir `PERF011` a tipos e padrões retentores reconhecidos semanticamente; nomes genéricos de métodos (`Add`, `AddRange`, `Subscribe`) não são evidência suficiente.
- `FINDING-003` exige que `PERF030` tenha evidência observável de inicialização complexa ou invariância relevante; construtor parametrizado ou inicializador não vazio, isoladamente, não basta.
- `FINDING-004` exige benchmarks comparativos representativos para as duas regras, preservados como evidência de cenário e sem criar limiar de runtime para o analyzer.

### Ordem e dependências de remediação

1. `TASK-REV-001` — documentar a rastreabilidade detalhada e as validações; bloqueia o retorno à implementação até que o plano seja verificável.
2. `TASK-REV-002` e `TASK-REV-003` — corrigir `PERF011` e `PERF030`, respectivamente, após `TASK-REV-001`; podem ser executadas em paralelo.
3. `TASK-008` — criar o harness isolado após as correções dos analyzers e seus testes; depende de `TASK-REV-002` e `TASK-REV-003`.
4. `TASK-009` e `TASK-010` — executar os cenários comparativos de `PERF011` e `PERF030` em paralelo; dependem de `TASK-008`.
5. `TASK-011` / `TASK-REV-004` — preservar evidência, atualizar documentação e fechar a remediação; dependem de `TASK-009` e `TASK-010`.
6. Reexecutar os gates configurados e repetir a revisão independente após `TASK-REV-004`; essa etapa não altera a especificação e deve preservar as evidências anteriores.

### Contratos de remediação

- `PERF011` só pode reportar quando o receptor resolve para uma estrutura retentora reconhecida e houver evidência estrutural de retenção potencial; APIs desconhecidas, métodos homônimos e contratos não demonstráveis devem ser omitidos.
- `PERF030` deve exigir combinação conservadora de repetição, invariância observável e sinal estrutural de inicialização não trivial. Construtores simples, inicializadores triviais, estado variável, isolamento por operação e tipos sem thread safety demonstrável devem permanecer silenciosos.
- Benchmarks devem comparar cenário de repetição contra cenário limitado/reutilizado, medir memória viva para `PERF011` e alocações/CPU para `PERF030`, registrar ambiente/carga/comando e declarar que os resultados não configuram a heurística estática.
- Nenhuma remediação inclui Code Fix, dependência nova no analyzer, profiling no analyzer, telemetria, chamada externa ou mudança de IDs, severidades, fase e contrato V1/V2. Por decisão explícita `Q-V3-004`, o harness pode usar `BenchmarkDotNet 0.15.8`.

### Critério de saída

O plano estará pronto para implementação quando `TASK-REV-001` tiver todas as relações requisito-tarefa e critério-validação detectáveis, e as tarefas de implementação puderem demonstrar `AC-002`, `AC-004` e `AC-010` com testes/evidências correspondentes. A conclusão da remediação requer novo build, testes, segurança, cobertura, benchmarks preservados e revisão sem os achados High.

## Plano de criação dos benchmarks V3

### Objetivo e limites

- Atender `FINDING-004`, `AC-010` e `NFR-006` com evidência comparativa reproduzível para as hipóteses de `PERF011` e `PERF030`.
- Criar um executável isolado em `benchmarks/StrictSharp.Analyzers.V3.Benchmarks`, direcionado a `net10.0`, sem referência ao analyzer em runtime. O projeto usa somente `BenchmarkDotNet 0.15.8` para calibração, memória e exportação dos resultados.
- Medir o cenário de uso da regra, não o custo da análise Roslyn. O benchmark não altera heurísticas, severidades, mensagens ou critérios de diagnóstico.
- Produzir resultados brutos e metadados em `.spectro/features/csharp-performance-analyzer-v3/evidence/benchmark-<run-id>/`, sem enviar código, dados ou resultados para serviços externos.

### Cenários comparativos

#### `PERF011` — retenção potencial

- **Repetição:** adicionar repetidamente payloads de tamanho configurável a uma coleção mantida em campo, sem limpeza durante a janela de medição.
- **Controle limitado:** executar o mesmo número de operações mantendo a coleção limitada por remoção/limpeza observável e liberando referências temporárias ao fim da janela.
- **Métricas:** memória viva após coleta completa (`GC.GetTotalMemory(forceFullCollection: true)`), memória antes/depois, iterações, payload, duração e operações.
- **Interpretação:** comparar tendência e diferença entre cenários; não declarar que o analyzer prova memory leak nem converter bytes em limiar da regra.

#### `PERF030` — reconstrução versus reutilização

- **Reconstrução:** criar repetidamente uma instância com inicialização não trivial e argumentos invariantes, mantendo o resultado válido para impedir eliminação artificial do trabalho.
- **Controle reutilizado:** criar uma instância imutável equivalente uma vez e reutilizá-la no mesmo número de operações, preservando o contrato do cenário.
- **Métricas:** `Stopwatch` para tempo/CPU, `GC.GetAllocatedBytesForCurrentThread()` para alocações, iterações, dados e criações.
- **Interpretação:** registrar resultados por cenário e repetição; não afirmar ganho universal nem usar medição como limiar estático do analyzer.

### Contrato do harness

- Entrada: cenário (`perf011`, `perf030` ou `all`), iterações, payload, repetições, seed opcional e caminho absoluto de saída.
- Saída: JSON versionado com revisão do código, timestamp, runtime, sistema operacional, configuração `Release`, parâmetros, métricas por repetição, agregados e limitações; log textual deve reproduzir o comando.
- Repetibilidade: aquecer antes da medição, executar repetições configuráveis, impedir eliminação por efeitos observáveis e manter a mesma carga entre hipótese e controle.
- Isolamento: não executar como parte do analyzer, testes unitários ou cobertura; sem rede, telemetria, persistência de produção ou dados sensíveis.
- Higiene: coletar GC apenas nas fronteiras documentadas, evitar alocações de medição no trecho cronometrado e informar variação de GC/CPU por máquina e carga.

### Execução e evidência

1. Construir o projeto de benchmark em `Release`, restaurando apenas `BenchmarkDotNet 0.15.8` além do SDK aprovado.
2. Executar `perf011`, `perf030` e, quando necessário, `all`; registrar comando, revisão e parâmetros.
3. Preservar JSON, log, versão do `dotnet` e resumo de ambiente no diretório de evidência.
4. Atualizar `evidence-v3-validation.md` com caminhos, hipóteses, cenários, métricas, resultados brutos, limitações e conclusão restrita ao cenário medido.
5. Marcar `TASK-REV-004` como concluída somente quando ambas as regras tiverem evidência comparativa; repetir gates/review se artefatos rastreados ou configuração mudarem.

### Alternativas rejeitadas e riscos

- A implementação manual anterior foi substituída por `BenchmarkDotNet 0.15.8` após a decisão `Q-V3-004`; a dependência permanece fora do analyzer, testes, solução e gates do produto.
- Medir o analyzer dentro do benchmark é rejeitado por misturar custo Roslyn com custo do código analisado.
- Uma única execução ou limiar fixo é rejeitado por não fornecer reprodutibilidade e confundir evidência com contrato da regra.
- Ruído de GC/JIT/OS será mitigado com aquecimento, repetições, Release e ambiente registrado, sem eliminar a incerteza.
- Cenários artificiais terão payload, duração, distribuição e limites documentados; não haverá extrapolação automática para produção.

### Rastreabilidade dos benchmarks

| Requisito | Critério | Tarefa | Evidência |
|---|---|---|---|
| `FR-009`, `NFR-006` | `AC-010` | `TASK-008`–`TASK-011` | JSON/log de `PERF011` e `PERF030` |
| `FR-011` | `AC-008`, `AC-010` | `TASK-008`, `TASK-011` | versão, comando, ambiente, hipótese e limitação |
| `FR-008` | `AC-006` | `TASK-011` | resumo alinhado aos metadados e limitações |
