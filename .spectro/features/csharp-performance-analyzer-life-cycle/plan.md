<!-- spectro-managed: true -->
# Plano: Contextos de ciclo de vida no C# Performance Analyzer

## Contexto e objetivo técnico

- A especificação está `clarified` e define a primeira entrega exclusivamente para `BackgroundService.ExecuteAsync`.
- A implementação atual trata loops por estrutura sintática. Um `while` de serviço pode envolver loops computacionais, chamadas interprocedurais e operações assíncronas, mas não deve ser usado como multiplicador computacional por si só.
- A capacidade será entregue na V4, antes da aplicação do modelo de custo V5, com os novos IDs `PERF108`–`PERF112`.
- O objetivo técnico é introduzir contexto semântico compartilhado para o ciclo de serviço, preservar a análise do trabalho interno e emitir diagnósticos específicos somente com evidência estática suficiente.
- A decisão `Q-LIFE-006` resolve a inconsistência documental: `PERF101`–`PERF105` são históricos/conceituais e o catálogo normativo do ciclo de vida é `PERF108`–`PERF112`.

## Solução proposta

- Adicionar um núcleo estático de análise de ciclo de vida que:
  - reconheça `BackgroundService.ExecuteAsync` por símbolos Roslyn, sem adicionar dependência de `Microsoft.Extensions.Hosting` ao analyzer;
  - identifique o loop de controle associado ao entry point e classifique espera temporal, espera por evento, sincronização, ausência de suspensão efetiva e comportamento desconhecido;
  - propague um contexto imutável para a análise de chamadas e operações internas;
  - mantenha separadas as unidades `CostPerIteration`, `CostPerWorkItem` e `CostPerTime`, sem derivar frequência real de `Task.Delay`.
- Criar cinco analisadores Roslyn, um por diagnóstico:
  - `PERF108`: espera ativa/busy polling;
  - `PERF109`: trabalho invariável ou materialização repetida entre ciclos;
  - `PERF110`: operação bloqueante em ciclo de serviço;
  - `PERF111`: paralelismo crescente entre ciclos;
  - `PERF112`: cancelamento ou espera que impede encerramento eficiente.
- Registrar as cinco regras como `RulePhase.V4`, com metadados e categorias aprovados, e incluí-las no `V4AnalyzerCatalog` junto de `PERF100`.
- Ajustar a inferência compartilhada de loops para que o loop de ciclo de vida seja uma unidade de contexto, não um multiplicador de complexidade; loops computacionais internos continuam analisáveis.
- Reutilizar a infraestrutura existente de `RuleAnalyzerBase`, `RuleCatalog`, análise interprocedural e `V5DiagnosticAnalysis` apenas quando o contrato de evidência for compatível; não criar diagnósticos agregados nem alterar os contratos V1–V5 existentes.

## Arquitetura e contratos

### Componentes a criar ou modificar

- `src/StrictSharp.Analyzers/Lifecycle/`: tipos de contexto, classificação de entry point, tipo de espera, unidades de custo e serviço estático compartilhado.
- `src/StrictSharp.Analyzers/Analyzers/Perf108Analyzer.cs` a `Perf112Analyzer.cs`: adaptadores Roslyn finos, um por diagnóstico, delegando a análise ao núcleo compartilhado.
- `src/StrictSharp.Analyzers/Rules/RuleCatalog.cs`: cinco definições de regra V4, com referências à documentação de ciclo de vida.
- `src/StrictSharp.Analyzers/Analyzers/V4AnalyzerCatalog.cs`: catálogo V4 com `PERF100` e `PERF108`–`PERF112`.
- `src/StrictSharp.Analyzers/V5/LoopCostInference.cs` ou serviço compartilhado equivalente: excluir o loop de serviço do cálculo de complexidade externa e preservar loops computacionais internos.
- `tests/StrictSharp.Analyzers.Tests/`: testes de catálogo, classificação, cada diagnóstico, regressão V4/V5, deduplicação e cancelamento.
- `docs/csharp-performance-analyzer-life-cycle.md`: contrato documental dos cinco diagnósticos, exemplos positivos/negativos, limitações e referências.
- `src/StrictSharp.Analyzers/StrictSharp.Analyzers.csproj`: empacotamento da documentação de ciclo de vida.
- `.spectro/features/csharp-performance-analyzer-life-cycle/clarifications.md`: histórico da decisão que reconcilia os IDs históricos e vigentes.

### Fluxo de dados

1. O adaptador de cada regra recebe um `MethodDeclaration` e obtém seu `IMethodSymbol`.
2. O detector verifica se o método é o `ExecuteAsync` de uma classe derivada de `Microsoft.Extensions.Hosting.BackgroundService`; se o símbolo não puder ser demonstrado, não classifica como ciclo de serviço.
3. O núcleo encontra o loop de controle, constrói o contexto e classifica operações de espera, chamadas, cancelamento e trabalho repetido.
4. A análise interprocedural existente é consultada somente para alvos conhecidos e recebe o contexto do ciclo; fronteiras desconhecidas permanecem desconhecidas.
5. O analisador específico aplica sua predicado de evidência e emite no máximo o diagnóstico previsto pelo seu contrato, com localização e mensagem contendo evidência, impacto potencial, confiança e limitação.

### Contratos e metadados aprovados

Os IDs, categorias e severidades abaixo estão aprovados pela especificação e pelas clarificações:

| ID | Título | Categoria proposta | Severidade proposta |
|---|---|---|---|
| `PERF108` | Espera ativa em ciclo de serviço | `Concurrency` | `High` |
| `PERF109` | Trabalho repetido entre ciclos de serviço | `Performance` | `High` |
| `PERF110` | Operação bloqueante em ciclo de serviço | `Concurrency` | `Critical` |
| `PERF111` | Paralelismo crescente entre ciclos | `Concurrency` | `Critical` |
| `PERF112` | Encerramento ineficiente de ciclo de serviço | `Concurrency` | `Medium` |

A emissão deve ser configurável e deduplicada quando `PERF005`, `PERF001`, `PERF004`, `PERF006`, `PERF011`, `PERF028` ou `PERF100` já explicarem a mesma evidência sem contexto adicional. Nenhuma regra de ciclo de vida pode reutilizar `PERF101`–`PERF107`.

### Erros, concorrência e observabilidade

- Cancelamento do Roslyn deve ser propagado por toda travessia e toda análise interprocedural.
- A análise deve ser somente leitura, determinística e segura para execução concorrente do analyzer; estado mutável compartilhado não deve ser usado.
- Falha de resolução de símbolo, corpo ausente, dispatch desconhecido ou método externo deve produzir estado desconhecido, não custo zero.
- Não haverá persistência, I/O ou serviço externo.
- Mensagens usarão os metadados já existentes de `RuleDefinition`; não devem exibir frequência, throughput ou ganho percentual não medidos.

## Alternativas consideradas

- **Adicionar a lógica diretamente aos cinco analyzers:** rejeitada. Duplicaria reconhecimento de entry point, classificação de espera e regras de incerteza, contrariando a separação entre integração Roslyn e serviços reutilizáveis.
- **Reutilizar `PERF101`–`PERF105`:** rejeitada. Esses IDs já são contratos V5 e a decisão de clarificação aprovou `PERF108`–`PERF112`.
- **Criar um único diagnóstico transversal:** rejeitada. A constituição exige um analyzer por diagnóstico e os cinco padrões possuem evidências e correções diferentes.
- **Adicionar referência de pacote de Hosting ao analyzer:** rejeitada. O contrato pode ser identificado por símbolos e metadados Roslyn nos testes, sem ampliar dependências ou o custo de distribuição.
- **Tratar todo `while` em `ExecuteAsync` como isento:** rejeitada. O contexto impede o falso diagnóstico do loop externo, mas mantém análise de busy polling, bloqueios, alocações, repetição e paralelismo internos.
- **Inferir ciclos por nome de método apenas:** rejeitada. O primeiro recorte exige relação semântica demonstrável com `BackgroundService`.

## Riscos e compatibilidade

- **Risco de contrato:** a documentação-fonte ainda contém a tabela histórica `PERF101`–`PERF105`; a mitigação é marcá-la como histórica e manter `PERF108`–`PERF112` como único catálogo normativo.
- **Risco de resolução:** testes precisam representar `BackgroundService` sem introduzir dependência de Hosting no projeto do analyzer.
- **Risco de falso positivo:** `await` pode completar sincronamente, e um método chamado pode retornar imediatamente; o resultado deve ser desconhecido quando o corpo não for resolvido.
- **Risco de duplicação:** `PERF108` pode se sobrepor a `PERF005`, e `PERF110` a `PERF001`/`PERF004`; a deduplicação precisa ser testada por evidência, não somente por localização. A remediação aprovada deve preservar a regra contextual somente quando ela acrescentar informação, sem depender de coordenação mutável entre analyzers.
- **Risco de custo:** a propagação para chamadas dentro de um serviço pode aumentar o trabalho em IDE/build; usar memoização, cancelamento e limites já adotados pela V4.
- **Compatibilidade:** não alterar os IDs, severidades, categorias ou resultados esperados das regras V1–V5; a única extensão de catálogo é a fase V4 aprovada para `PERF108`–`PERF112`.

## Estratégia de testes

- Testes unitários do detector de `BackgroundService.ExecuteAsync`, identificação do loop, classificação de espera e unidades de custo.
- Matriz de catálogo garantindo exatamente `PERF100`, `PERF108`–`PERF112` na V4, um analyzer por diagnóstico, metadados aprovados e severidade Roslyn correspondente.
- Casos positivos e negativos para cada regra: espera ativa versus `Task.Delay`, `ReadAsync` e `WaitAsync`; trabalho repetido versus trabalho dependente do item; bloqueio versus async real; tarefas acumuladas versus paralelismo delimitado; cancelamento respeitado versus espera que ignora o token.
- Casos de incerteza: método externo, dispatch não resolvido, corpo ausente, `await ProcessAsync` sem corpo disponível e símbolo que apenas possui nome semelhante.
- Regressão V4/V5: `ExecuteAsync` com loop externo e loop computacional interno não deve gerar complexidade externa artificial, mas deve preservar evidência dos loops e chamadas internas.
- Testes de deduplicação com `PERF005`, `PERF001`, `PERF004`, `PERF006`, `PERF011`, `PERF028` e `PERF100`; para `PERF001`/`PERF110`, executar o catálogo V4 combinado e confirmar no máximo uma evidência para o mesmo `Task.Wait`, `Task.Result` ou `GetResult`.
- Verificação documental de `AC-013`: a documentação, o `RuleCatalog`, o catálogo V4, os testes e as clarificações devem apontar para `PERF108`–`PERF112`; a tabela `PERF101`–`PERF105` deve estar explicitamente identificada como histórica.
- Execução dos gates configurados: build, testes, segurança e cobertura; benchmark somente se a aprovação de impacto exigir evidência de custo do analyzer.

## Rastreabilidade

| Requisito | Critério de aceite | Tarefa |
|---|---|---|
| `FR-001` | `AC-001` | `TASK-003`, `TASK-006` |
| `FR-002` | `AC-002` | `TASK-003`, `TASK-004`, `TASK-006` |
| `FR-003` | `AC-003` | `TASK-005`, `TASK-006` |
| `FR-004` | `AC-004` | `TASK-002`, `TASK-006` |
| `FR-005` | `AC-005` | `TASK-004`, `TASK-006` |
| `FR-006` | `AC-006` | `TASK-002`, `TASK-005`, `TASK-006` |
| `FR-007` | `AC-007` | `TASK-005`, `TASK-006` |
| `FR-008` | `AC-008` | `TASK-005`, `TASK-006` |
| `FR-009` | `AC-009` | `TASK-005`, `TASK-006` |
| `FR-010` | `AC-010` | `TASK-003`, `TASK-006`, `TASK-REV-005` |
| `NFR-001` | `AC-011` | `TASK-002`, `TASK-004`, `TASK-006`, `TASK-008` |
| `NFR-002` | `AC-011` | `TASK-002`, `TASK-004`, `TASK-006`, `TASK-008` |
| `NFR-003` | `AC-011` | `TASK-002`, `TASK-005`, `TASK-006`, `TASK-008` |
| `NFR-004` | `AC-012` | `TASK-001`, `TASK-005`, `TASK-006`, `TASK-007`, `TASK-008` |
| `NFR-005` | `AC-012` | `TASK-004`, `TASK-005`, `TASK-006`, `TASK-008` |
| `NFR-006` | `AC-012` | `TASK-006`, `TASK-007`, `TASK-008` |
| `CON-001` | `AC-001` | `TASK-004`, `TASK-006` |
| `CON-002` | `AC-004` | `TASK-002`, `TASK-006` |
| `CON-003` | `AC-006` | `TASK-002`, `TASK-006` |
| `CON-004` | `AC-004` | `TASK-002`, `TASK-006` |
| `CON-005` | `AC-010` | `TASK-001`, `TASK-003`, `TASK-006` |
| `CON-006` | `AC-011` | `TASK-007`, `TASK-008` |
| `CON-007` | `AC-007`, `AC-008`, `AC-009` | `TASK-005`, `TASK-006`, `TASK-REV-002` |
| `CON-008` | `AC-013` | `TASK-009` |

## Plano incremental da expansão solicitada

### Contexto e solução

- A primeira entrega permanece concluída e não será reaberta; `TASK-010`–`TASK-017` cobrem somente a expansão clarificada.
- `LifecycleContext` receberá o contexto semântico (`Service Lifecycle`, `Event Driven`, `Polling`, `Retry` ou `Batch`), a origem estrutural/declarada e a hipótese de frequência, mantendo representação por valor.
- `LifecycleAnalysis` será decomposta em detecção semântica, validação de `PerformanceContext`, classificação e propagação. A detecção exigirá símbolos/APIs e controle de fluxo observáveis; nomes isolados ficam desconhecidos.
- `PerformanceContext` e `PerformanceContextKind` serão contratos públicos opcionais com `Kind` e `ExpectedIterationsPerSecond`. A frequência positiva será consumida somente como hipótese de `CostPerTime`.
- `PERF108`–`PERF112` serão reutilizados para todos os contextos expandidos; não haverá novo diagnóstico nem dependência de Hosting, brokers ou runtime.

### Componentes afetados

- `src/StrictSharp.Analyzers/Lifecycle/LifecycleContext.cs` e `LifecycleAnalysis.cs`.
- Novo serviço estático de detecção/classificação e contrato público de `PerformanceContext`, se a separação exigir novos arquivos em `Lifecycle/`.
- `src/StrictSharp.Analyzers/Lifecycle/LifecycleAnalyzerBase.cs` e `Perf108Analyzer.cs`–`Perf112Analyzer.cs`.
- `src/StrictSharp.Analyzers/Rules/RuleCatalog.cs` somente para preservar/revalidar o catálogo existente.
- `tests/StrictSharp.Analyzers.Tests/LifecycleAnalyzerTests.cs`, testes de catálogo, regressão V4/V5 e empacotamento.
- `docs/csharp-performance-analyzer-life-cycle.md` e artefatos Spectro de rastreabilidade.

### Fluxo e validação da expansão

1. Resolver o símbolo do entry point e suas operações de espera, notificação, timer, backoff ou agrupamento.
2. Classificar conforme o mapeamento aprovado: `IHostedService`/game loop como `Service Lifecycle`; brokers, `Channel<T>`, TCP orientado a eventos e eventos como `Event Driven`; scheduler como `Polling`; retry com backoff como `Retry`; agrupamento finito como `Batch`.
3. Ler `PerformanceContext` quando presente, validar `Kind` e frequência positiva e manter desconhecido o que for inválido/conflitante.
4. Propagar contexto aos métodos resolvidos e preservar custo por ciclo, item, lote, frame e tempo condicional sem afirmar frequência efetiva.
5. Executar testes positivos, negativos e de incerteza para cada família, regressão completa e gates.

### Riscos específicos

- A semântica de brokers e frameworks não é uniforme; mitigar por evidência estrutural mínima e fallback conservador.
- Um atributo público inválido pode alterar estimativas; validar sem interromper a análise e marcar a hipótese como desconhecida.
- Reutilização do catálogo pode ocultar a origem do contexto; mensagens e evidências devem explicitar o contexto sem alterar o ID.
- A propagação interprocedural pode elevar o custo da IDE; limitar travessias, reutilizar resultados e validar com os gates de performance.

## Estado e aprovação

- Estado do plano: `implementation_ready` para a expansão; `TASK-010`–`TASK-017` estão pendentes.
- A implementação e os cinco diagnósticos já foram validados; a atualização da especificação acrescentou a reconciliação documental `Q-LIFE-006`/`CON-008`/`AC-013`.
- Os gates e a revisão anteriores são evidência do código anterior à atualização dos artefatos Spectro; após `TASK-009`, devem ser renovados para o conjunto documental atual.
- Próxima etapa autorizada: executar `TASK-010`–`TASK-016`, então `TASK-017` com `spectro-gates` e, em seguida, `spectro-review`.

## Plano de remediação pós-revisão

### Objetivo

- Completar o contrato de unidades de custo de `FR-006` sem inventar frequência de runtime.
- Reduzir falsos positivos de `PERF109`, exigindo evidência de invariância ou materialização redundante antes da emissão.
- Tornar a rastreabilidade verificável pelo script, com cada requisito listado individualmente.
- Alinhar o estado de `TASK-008` com o relatório de gates aprovado.
- Eliminar a duplicação de evidência entre `PERF110` e `PERF001` sem alterar IDs, severidades ou o escopo de `BackgroundService.ExecuteAsync`.

### Abordagem técnica

- Evoluir `LifecycleContext` para carregar a unidade de custo inferida por evidência: iteração como padrão, item de trabalho quando o ciclo representar processamento de item/mensagem e tempo somente como hipótese explícita, nunca como frequência garantida por `Task.Delay`.
- Expor a unidade nas evidências/mensagens relevantes e adicionar casos positivos, negativos e desconhecidos para cada unidade.
- Separar alocação local de trabalho invariável: `PERF109` deve reportar materialização repetida ou trabalho demonstravelmente independente do item, e permanecer silencioso para buffers/alocações dependentes do item.
- Não alterar IDs, severidades, catálogo ou escopo de entry points; preservar o comportamento V1–V5 existente.
- Substituir intervalos nas colunas de requisitos das tarefas por listas explícitas (`FR-007`, `FR-008`, `FR-009` etc.) e rerodar `validate_traceability.ps1`.
- Marcar `TASK-008` como concluída somente com `gates.json` válido e todos os quatro gates obrigatórios aprovados.
- Para `FINDING-005`, centralizar a decisão de sobreposição em uma função estática/reutilizável baseada no símbolo e na operação: quando `PERF001` já explicar `Task.Wait`, `Task.Result` ou `GetResult`, `PERF110` não deve emitir a mesma evidência; bloqueios síncronos não cobertos por `PERF001` continuam elegíveis ao diagnóstico contextual. Não compartilhar estado mutável entre analyzers.

### Validação da remediação

- Testes do modelo de unidade para `CostPerIteration`, `CostPerWorkItem`, `CostPerTime` como hipótese e estado desconhecido.
- Testes negativos de `PERF109` para alocação por item e testes positivos de invariância/materialização repetida.
- Teste comportamental com `V4AnalyzerCatalog.All` para `Task.Wait`, `Task.Result` e um bloqueio síncrono não coberto, verificando ausência de duplicação e preservação do diagnóstico contextual quando houver valor adicional.
- `validate_traceability.ps1` deve retornar `complete`, sem requisitos ou critérios de aceite descobertos.
- Reexecutar build, testes, segurança e cobertura; evidência deve apontar para a mesma revisão de código remediada.
- Reexecutar a revisão independente e obter decisão `approved` ou registrar novos achados.

### Ordem e bloqueios

1. `spectro-gates` após a implementação de `TASK-REV-005`.
2. `spectro-review` final.

Bloqueador atual: gates e revisão final ainda precisam ser renovados para confirmar a remediação de `FINDING-005`.
