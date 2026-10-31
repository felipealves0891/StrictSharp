<!-- spectro-managed: true -->
# Especificação: Modelo de custo computacional V5 do C# Performance Analyzer

## Problema e objetivo

- Problema: as versões anteriores identificam padrões potencialmente custosos e, com a análise interprocedural da V4, conseguem relacionar parte do custo a chamadas e repetições. Ainda falta classificar a magnitude estrutural desse trabalho em dimensões compreensíveis, sem confundir uma estimativa estática com uma medição do comportamento real da aplicação.
- Objetivo: disponibilizar a etapa V5 como um modelo determinístico e conservador de custo computacional, capaz de reunir evidências de complexidade, alocação, memória, repetição, I/O e concorrência e de explicar sua amplificação potencial no código analisado.

## Usuários e contexto

- Desenvolvedores e revisores de código C# que precisam priorizar riscos de performance pela magnitude potencial do trabalho, e não somente pela presença isolada de uma API ou construção.
- A análise deve explicar custo estrutural observável no código. Frequência real, tamanho efetivo das entradas, comportamento do runtime, carga e impacto em produção continuam dependentes de medição representativa.

## Escopo

### Incluído

- Modelo de evidências estáticas para as dimensões de complexidade, alocação, memória, repetição, I/O e concorrência.
- Classificação assintótica quando puder ser inferida com evidência suficiente, incluindo `O(1)`, `O(n)`, `O(n log n)` e `O(n²)`; quando não puder, classificação desconhecida ou limitada.
- Estimativa de contagens e payloads conhecidos, como iterações, alocações por iteração, bytes de payload de arrays e tamanho mínimo ou estimável de buffers.
- Propagação de multiplicadores observáveis de loops e chamadas, aproveitando relações interprocedurais conhecidas pela V4 quando disponíveis.
- Identificação de operações externas e tarefas concorrentes conhecidas, com incerteza explícita nas fronteiras não resolvidas.
- Diagnóstico ou evidência V5 com custo, dimensões, confiança, limitações, impacto potencial e orientação de revisão.
- Comparação qualitativa de alternativas quando a diferença estrutural puder ser demonstrada, sem converter a comparação em previsão percentual.
- Documentação, testes positivos, negativos e de exceção, além de hipótese mensurável para validação do modelo.

### Excluído

- Profiling, EventPipe, `dotnet-trace`, `dotnet-counters`, `dotnet-gcdump`, benchmarks executados ou qualquer correlação com dados de runtime; isso pertence às etapas posteriores.
- Previsão exata de pausas de GC, sobrevivência de objetos, memória viva, CPU, latência, throughput, frequência de execução ou impacto em produção.
- Percentuais de ganho, custo monetário ou afirmações de complexidade efetiva em produção sem medição correspondente.
- Inferência de comportamento de métodos externos, corpos ausentes, reflection, geração dinâmica ou dispatch não resolvido como se fossem custo zero.
- Recomendação arquitetural ampla, implementação de refatorações automáticas ou Code Fix quando a equivalência semântica não puder ser demonstrada.
- Detecção de regressões entre versões, recomendação inteligente, validação experimental e uso de dados reais de workload; são capacidades previstas para V7–V10.
- Definição antecipada de classes, serviços, algoritmo específico de inferência, formato interno do grafo ou número de novos diagnósticos.
- Alteração funcional das regras V1–V4, salvo integração indispensável e aprovada para apresentar o contexto de custo V5.

## Requisitos

### Requisitos funcionais

- `FR-001`: O produto deve disponibilizar a etapa V5 como um modelo de custo estático integrado ao catálogo e às capacidades anteriores, preservando a configuração e os contratos existentes e publicando os diagnósticos `PERF101` a `PERF107`.
- `FR-002`: O modelo deve representar a complexidade algorítmica observável e classificar o trabalho como `O(1)`, `O(n)`, `O(n log n)`, `O(n²)` ou desconhecido quando a forma não puder ser inferida com confiança suficiente.
- `FR-003`: O modelo deve identificar alocações conhecidas por iteração e estimar payloads de memória quando tamanho e quantidade forem deriváveis estaticamente, distinguindo payload de custos adicionais de objeto, cabeçalho e alinhamento.
- `FR-004`: O modelo deve rastrear multiplicadores de repetição associados a loops e chamadas conhecidas, explicando quando uma operação é amplificada por iteração, aninhamento ou cadeia interprocedural.
- `FR-005`: O modelo deve representar o tamanho mínimo ou estimável de buffers e outras estruturas de memória quando houver evidência suficiente, marcando como desconhecida qualquer parcela dependente de entrada, runtime ou estado não resolvido.
- `FR-006`: O modelo deve identificar operações de I/O ou chamadas externas potencialmente repetidas e expressar a quantidade estrutural conhecida, sem afirmar duração, latência, disponibilidade ou volume real transferido.
- `FR-007`: O modelo deve representar a quantidade máxima conhecida de tarefas ou operações concorrentes quando derivável, explicitando limites, filas, bloqueios ou alvos desconhecidos sem inferir capacidade efetiva do runtime.
- `FR-008`: O modelo deve combinar as dimensões aplicáveis em uma evidência de custo por caminho analisado, mantendo separadas as dimensões que não puderem ser correlacionadas ou determinadas.
- `FR-009`: O produto deve permitir comparar duas alternativas quando suas diferenças estruturais forem demonstráveis, informando a mudança assintótica ou de trabalho conhecido sem inventar ganho percentual ou resultado de benchmark.
- `FR-010`: O diagnóstico ou resultado V5 deve explicar a evidência, as premissas, os multiplicadores, as dimensões afetadas, a confiança, o impacto potencial, as limitações e uma orientação condicional de revisão.
- `FR-011`: O modelo deve tratar fronteiras desconhecidas de forma conservadora: ausência de corpo, alvo externo, entrada não conhecida, dispatch não resolvido ou comportamento dependente de runtime não podem ser convertidos em custo zero nem em precisão artificial.
- `FR-012`: A etapa V5 deve ser configurável e deduplicada em relação às regras existentes, emitindo contexto adicional somente quando o modelo acrescentar valor explicativo ao diagnóstico já disponível.
- `FR-013`: A implementação da V5 deve possuir documentação rastreável, exemplos positivos, negativos e de exceção, versões suportadas e hipótese mensurável de validação para cada regra ou capacidade publicada.

### Requisitos não funcionais

- `NFR-001`: A análise deve ser determinística para o mesmo código, configuração e conjunto de símbolos, produzindo a mesma classificação e evidência sem depender de execução da aplicação ou serviço externo.
- `NFR-002`: O modelo deve priorizar precisão e baixo ruído; classificações e números devem ser omitidos, limitados ou marcados como desconhecidos quando a evidência não sustentar a conclusão.
- `NFR-003`: O custo do analyzer deve permanecer aceitável em IDE e build, evitando alocações desnecessárias, travessias repetidas e cálculo de dimensões não relacionadas ao diagnóstico solicitado.
- `NFR-004`: A lógica de análise deve permanecer separada da integração Roslyn, com um analisador por diagnóstico e serviços estáticos por padrão, conforme as convenções do projeto.
- `NFR-005`: A solução deve permanecer compatível com as versões de C#, .NET, Target Framework e Roslyn aprovadas pelo projeto.
- `NFR-006`: As recomendações devem preservar semântica, mutabilidade, propriedade, thread safety, ciclo de vida, contrato de I/O e comportamento de concorrência; hipóteses não demonstradas devem ser apresentadas para revisão manual.
- `NFR-007`: A análise não deve enviar código-fonte, segredos, dados de produção ou informações sensíveis a serviços externos.

### Restrições

- `CON-001`: A V5 é uma estimativa estática de custo e não uma medição de performance; a severidade continua representando risco operacional plausível, não ganho esperado.
- `CON-002`: Payload conhecido não inclui automaticamente cabeçalhos, alinhamento, metadados, sobrevivência de objetos ou pausas de GC; esses componentes devem ser explicitamente desconhecidos ou qualificados.
- `CON-003`: Valores apresentados como exatos devem ser limitados ao que é derivável do código. No exemplo documentado, 1.000 alocações e 100.000.000 bytes de payload podem ser reportados, aproximadamente 95,4 MiB, sem prometer custo total de memória ou GC.
- `CON-004`: Potencial de LOH, pressão de GC ou outro efeito de runtime só pode ser apresentado como possibilidade condicional sustentada pelo tamanho ou padrão estrutural; não pode ser apresentado como ocorrência ou pausa medida.
- `CON-005`: Corpo ausente, alvo externo não resolvido, dispatch indeterminado e dependência de entrada ou ambiente não podem ser tratados como custo zero.
- `CON-006`: IDs, severidades, categorias, novas dependências e mudanças de etapa do roadmap exigem aprovação humana; a documentação V5 não autoriza inventar um novo ID de diagnóstico.
- `CON-007`: A solução deve complementar os analisadores oficiais do .NET e deduplicar regras equivalentes quando o contexto de custo não acrescentar valor.
- `CON-008`: Não deve haver Code Fix automático quando a correção depender de contrato, mutabilidade, propriedade, ciclo de vida, thread safety ou equivalência semântica não demonstrada.

## Critérios de aceite

- `AC-001` (FR-001): Dado o catálogo existente, quando a etapa V5 for carregada, então ela aparece como capacidade de modelo de custo, preserva regras e configurações V1–V4 e não altera IDs ou contratos sem decisão aprovada.
- `AC-002` (FR-002): Dado um algoritmo cuja relação entre entrada e operações possa ser demonstrada, quando analisado, então a classificação assintótica aplicável é apresentada; dado um caso ambíguo ou não resolvido, então a classificação é desconhecida ou limitada, sem escolha arbitrária.
- `AC-003` (FR-003, CON-003): Dado um loop de 1.000 iterações que aloca um array de `byte[100_000]`, quando analisado, então o resultado informa 1.000 alocações e 100.000.000 bytes de payload, aproximadamente 95,4 MiB, distinguindo cabeçalhos e alinhamento não calculados.
- `AC-004` (FR-004): Dado um loop que chama uma operação custosa em outro método, quando a relação interprocedural for conhecida, então a evidência relaciona operação, caminho de chamadas e multiplicador de repetição; dada fronteira desconhecida, então a amplificação não é tratada como zero.
- `AC-005` (FR-005): Dado buffer com tamanho derivável, quando analisado, então seu tamanho mínimo ou estimado é informado; dada dependência de entrada ou runtime, então a parcela é marcada como desconhecida ou condicional.
- `AC-006` (FR-006): Dado I/O executado em loop, quando o número estrutural de execuções for observável, então ele é reportado como repetição potencial; duração, latência, disponibilidade e volume real não são declarados sem medição.
- `AC-007` (FR-007): Dado código que cria ou agenda tarefas em uma repetição com limite conhecido, quando analisado, então a quantidade máxima estrutural é informada; quando fila, execução ou limite dependerem do runtime, essa dependência aparece na evidência.
- `AC-008` (FR-008): Dado caminho com múltiplas dimensões conhecidas, quando o resultado for produzido, então complexidade, alocação, memória, repetição, I/O e concorrência permanecem identificáveis separadamente, sem somar unidades incompatíveis em um número único sem significado.
- `AC-009` (FR-009): Dadas duas alternativas com formas de trabalho demonstráveis, quando comparadas, então o resultado explica a diferença assintótica ou de contagem conhecida; não apresenta percentual de ganho, custo monetário ou benchmark inexistente.
- `AC-010` (FR-010, FR-011): Dado resultado V5 emitido, quando seus detalhes forem consultados, então ele contém evidência, premissas, confiança, impacto potencial, limitações e orientação condicional, tratando chamadas externas e corpos ausentes como desconhecidos.
- `AC-011` (FR-012): Dada configuração que desabilite ou altere a severidade de uma regra, quando a análise for executada, então somente a regra configurada é afetada; quando outra regra já cobrir o mesmo padrão sem contexto adicional, então não há diagnóstico duplicado.
- `AC-012` (FR-013): Dada a suíte V5, quando documentação e testes forem revisados, então cada capacidade publicada possui casos positivos, negativos e de exceção, referência rastreável, versões suportadas e hipótese de validação reproduzível.
- `AC-013` (NFR-001, NFR-002): Dado o mesmo código, configuração e símbolos, quando a análise for executada repetidamente sem alterar o ambiente, então a classificação e a evidência são determinísticas; diante de evidência insuficiente, o resultado reduz confiança ou marca desconhecido.
- `AC-014` (NFR-003, NFR-004, NFR-005, NFR-006): Dado código nas plataformas aprovadas, quando os gates do projeto forem executados, então a solução compila, mantém custo aceitável, respeita a separação de responsabilidades e não recomenda alteração semanticamente não comprovada.
- `AC-015` (NFR-007): Dado código contendo identificadores, literais ou exemplos potencialmente sensíveis, quando testes e gates forem executados, então nenhum conteúdo é enviado a serviço externo e as mensagens expõem apenas o contexto necessário.

## Riscos, dependências e perguntas abertas

- Risco: limites de inferência, loops com limites dinâmicos, recursão, dispatch e chamadas externas podem gerar classificação incompleta, explosão combinatória ou falsa sensação de precisão.
- Risco: somar dimensões heterogêneas pode produzir um score enganoso; o modelo deve preservar evidências por dimensão e não criar um número agregado sem semântica definida.
- Risco: uma indicação de LOH, GC, contenção ou concorrência pode ser interpretada como medição de runtime se a mensagem não declarar suas condições e limitações.
- Risco: a V5 pode duplicar regras V1–V4 ou analisadores oficiais se o contexto de custo não for usado como critério de valor adicional.
- Dependência: evidências e relações interprocedurais da especificação `.spectro/features/csharp-performance-analyzer-v4/spec.md`, quando a V4 estiver disponível no fluxo analisado.
- Dependência: catálogo, documentação e contratos das features `csharp-performance-analyzer`, `csharp-performance-analyzer-v2`, `csharp-performance-analyzer-v3` e `csharp-performance-analyzer-v4`.
- Dependência: constituição, configuração e gates em `.spectro/config.yaml`.
- Dependência: Roslyn e versões aprovadas de C#/.NET.
- Decisão `Q-V5-001`: seguir a base já adotada pelo projeto: analyzer em `netstandard2.1`, testes em `net10.0`, C# `14.0` e Roslyn `5.0.0`, com `Microsoft.CodeAnalysis.Analyzers` `3.11.0`.
- Decisão `Q-V5-002`: publicar `PERF101` a `PERF107`, todos com severidade `High`; as categorias seguirão as dimensões V5 documentadas, e a divisão exata por regra será definida no planejamento. `PERF100` permanece exclusivo da V4.
- Decisão `Q-V5-003`: classificar loops por Big-O, sem limite numérico fixo de iterações; I/O e concorrência não terão limiar numérico pré-definido e serão desconhecidos ou condicionais quando não houver quantidade estrutural derivável.
- Decisão `Q-V5-004`: disponibilizar comparação entre alternativas publicamente na primeira entrega, restrita a diferenças estruturais demonstráveis, sem percentual de ganho ou benchmark implícito.

## Rastreabilidade

- Fonte funcional: `docs/csharp-performance-analyzer-2.md`, seção `3. V5 — Modelo de custo computacional`.
- Constituição: `.spectro/constitution.md`.
- Template: `.spectro/templates/specification.md`.
- Especificações relacionadas: `.spectro/features/csharp-performance-analyzer/spec.md`, `.spectro/features/csharp-performance-analyzer-v2/spec.md`, `.spectro/features/csharp-performance-analyzer-v3/spec.md` e `.spectro/features/csharp-performance-analyzer-v4/spec.md`.

## Estado

- Estado da fase: `clarified`.
- Pronto para esclarecimento: não há ambiguidades bloqueantes ou major remanescentes.
- Pronto para planejamento: sim; as decisões de versões, catálogo, recorte inicial e comparação pública estão consolidadas.
