<!-- spectro-managed: true -->
# Especificação: Análise interprocedural V4 do C# Performance Analyzer

## Problema e objetivo

- Problema: as versões anteriores identificam padrões custosos principalmente no contexto local de uma instrução, método ou fluxo já observável. Isso não explica quando uma operação potencialmente cara é chamada por outro método, repetida por item em um loop, materializa dados ao longo de uma cadeia de chamadas ou participa de um caminho recursivo, assíncrono ou bloqueante.
- Objetivo: evoluir o analisador estático para analisar fluxos entre métodos e produzir evidências sobre a propagação de custo, repetição, alocações e bloqueios, mantendo a análise determinística, conservadora e explícita quanto ao que é desconhecido.

## Usuários e contexto

- Desenvolvedores e revisores de código C# que precisam entender por que uma operação local pode se tornar custosa quando executada em um fluxo maior.
- A análise deve explicar risco estrutural no código. Frequência real, volume de dados, custo efetivo e impacto em produção continuam dependentes de medição representativa.

## Escopo

### Incluído

- Análise de chamadas entre métodos quando a relação entre chamador e chamado for observável.
- Reconhecimento de chamadas indiretas quando os alvos puderem ser conhecidos com confiança suficiente.
- Propagação de operações potencialmente custosas através de chamadas e de loops, inclusive quando a operação estiver em método diferente daquele que contém a repetição.
- Rastreamento interprocedural de alocações e materializações ao longo do fluxo analisado.
- Identificação de chamadas recursivas e sinalização de risco de profundidade elevada quando houver evidência estrutural.
- Identificação de trabalho redundante entre métodos quando a repetição ou invariância puder ser demonstrada.
- Análise de caminhos síncronos, assíncronos e pontos de bloqueio observáveis.
- Diagnóstico interprocedural com evidência, impacto potencial, confiança, limitações e recomendação condicional.

### Excluído

- Estimativa de custo computacional exata, percentual de ganho, frequência real, throughput, CPU, GC ou memória em produção; isso pertence ao V5/V6 e a medições externas.
- Tratamento de corpo ausente, dependência externa ou alvo não resolvido como custo zero.
- Prova completa de comportamento em interfaces, virtual dispatch, delegates, reflection, geração dinâmica ou todos os caminhos possíveis de execução.
- Profiling, telemetria, coleta de código-fonte ou correlação com dados de runtime.
- Recomendações arquiteturais abrangentes ou refatorações automáticas cuja equivalência semântica não possa ser demonstrada.
- Alteração funcional das regras V1–V3, salvo integração indispensável e previamente aprovada.
- Definição antecipada de classes, serviços, algoritmo de resolução ou outra arquitetura interna.

## Requisitos

### Requisitos funcionais

- `FR-001`: O produto deve oferecer a etapa V4 de análise interprocedural integrada ao catálogo existente, preservando IDs, metadados e configuração das regras anteriores.
- `FR-002`: A análise deve construir evidência de relações entre métodos chamados e chamadores quando o alvo direto ou indireto for conhecido com confiança suficiente.
- `FR-003`: A análise deve propagar operações potencialmente custosas de um método chamado para o contexto do chamador e identificar quando elas ocorrem dentro de loops ou em repetição por item.
- `FR-004`: A análise deve rastrear alocações e materializações observáveis ao longo de um fluxo interprocedural, indicando o caminho de chamadas que sustenta a evidência.
- `FR-005`: A análise deve identificar chamadas recursivas e reportar risco de profundidade elevada somente quando houver evidência estrutural suficiente, sem afirmar profundidade ou falha efetiva não medidas.
- `FR-006`: A análise deve identificar trabalho redundante entre métodos quando operações equivalentes, invariantes ou repetidas puderem ser demonstradas no fluxo analisado.
- `FR-007`: A análise deve considerar caminhos síncronos e assíncronos e identificar pontos de bloqueio observáveis, distinguindo espera explícita, bloqueio e continuidade assíncrona quando essa distinção for possível.
- `FR-008`: Chamadas indiretas, interfaces, dispatch virtual, delegates e dependências externas devem ser tratadas conservadoramente: alvos conhecidos podem ser analisados; alvos não resolvidos devem produzir custo ou comportamento desconhecido, não custo zero.
- `FR-009`: O diagnóstico V4 deve explicar a cadeia de chamadas, a repetição ou amplificação observada, a operação custosa relacionada, o impacto potencial e uma orientação condicional de revisão.
- `FR-010`: O diagnóstico deve distinguir evidência estática de impacto dependente de frequência, tamanho da entrada, carga, ambiente ou comportamento de runtime, sem declarar complexidade efetiva ou ganho percentual sem medição.
- `FR-011`: A etapa V4 deve ser configurável e deduplicada em relação às regras existentes, emitindo diagnóstico adicional somente quando acrescentar contexto interprocedural relevante.
- `FR-012`: A especificação V4 deve manter rastreabilidade entre diagnóstico, documentação, exemplos positivos e negativos, exceções, testes e hipótese de validação reproduzível.

### Requisitos não funcionais

- `NFR-001`: A análise deve priorizar precisão e baixo ruído, reduzindo confiança ou omitindo o diagnóstico quando o alvo, a repetição, a invariância ou o bloqueio não forem demonstráveis.
- `NFR-002`: O custo do analyzer deve permanecer aceitável em IDE e build, evitando alocações desnecessárias, análise repetida de símbolos e travessia de métodos não relacionados ao diagnóstico.
- `NFR-003`: A integração Roslyn deve permanecer separada da lógica de análise, com um analisador por diagnóstico e serviços estáticos por padrão, conforme as convenções do projeto.
- `NFR-004`: As mensagens e recomendações devem preservar semântica, exceções, mutabilidade, propriedade, thread safety, ciclo de vida e contratos das APIs; hipóteses não demonstradas devem ser apresentadas para revisão manual.
- `NFR-005`: A solução deve permanecer compatível com as versões de C#, .NET, Target Framework e Roslyn aprovadas pelo projeto, incluindo os limites de análise estática dessas plataformas.
- `NFR-006`: A análise não deve enviar código-fonte, segredos, dados de produção ou informações sensíveis a serviços externos.

### Restrições

- `CON-001`: A V4 é uma evolução de análise estática baseada em Roslyn; profiling e dados reais de runtime estão fora desta etapa.
- `CON-002`: Corpo de método ausente, alvo externo não resolvido ou dispatch não determinável não pode ser interpretado como custo zero.
- `CON-003`: Severidade representa risco operacional plausível, não estimativa de ganho percentual; os valores permitidos permanecem `Critical`, `High`, `Medium` e `Low`.
- `CON-004`: Os IDs `PERF001`–`PERF030` permanecem estáveis e não são IDs oficiais do .NET. Por decisão humana registrada em `Q-V4-001`, a V4 pode introduzir o diagnóstico `PERF100` como extensão aprovada do catálogo; ele também não é um ID oficial do .NET.
- `CON-005`: Não deve haver Code Fix automático quando a alteração depender de comportamento não resolvido, contrato, mutabilidade, thread safety, ciclo de vida ou equivalência semântica não demonstrada.
- `CON-006`: Alterações de catálogo, severidade, plataforma, etapa do roadmap, dependências ou escopo exigem aprovação humana conforme a constituição do projeto.

## Critérios de aceite

- `AC-001` (FR-001): Dado o catálogo existente, quando a etapa V4 for carregada, então as regras V1–V3 permanecem únicas, configuráveis e com seus IDs e metadados preservados, e a capacidade interprocedural é identificável como V4.
- `AC-002` (FR-002): Dado um método que chama outro método com alvo resolvido, quando a análise for executada, então a relação chamador–chamado aparece na evidência; dada chamada indireta cujo alvo seja conhecido, então ela também pode ser incluída; dado alvo desconhecido, então a evidência é marcada como desconhecida.
- `AC-003` (FR-003): Dado um loop que chama um método contendo operação potencialmente custosa, quando a análise alcançar o corpo chamado, então o resultado relaciona a operação ao loop e à repetição por item, mesmo que estejam em métodos diferentes.
- `AC-004` (FR-004): Dado um fluxo de chamadas que aloca ou materializa dados, quando analisado, então o diagnóstico identifica a alocação/materialização e o caminho interprocedural que a sustenta; quando o corpo não estiver disponível, então não é tratado como ausência de alocação.
- `AC-005` (FR-005): Dado um ciclo de chamadas recursivas, quando a análise o detectar, então sinaliza risco de profundidade elevada somente com confiança compatível com a evidência e declara que a profundidade efetiva depende da entrada e da execução.
- `AC-006` (FR-006): Dado trabalho equivalente ou invariante repetido entre métodos no mesmo fluxo, quando essa repetição for demonstrável, então o diagnóstico a explica; dada dependência de estado ou entrada que impeça demonstrar equivalência, então o diagnóstico é omitido ou tem confiança reduzida.
- `AC-007` (FR-007): Dado caminho síncrono ou assíncrono com espera ou bloqueio observável, quando analisado, então o resultado identifica o ponto e seu contexto; dada fronteira cujo comportamento não seja resolvido, então a incerteza é explicitada.
- `AC-008` (FR-008): Dado dispatch virtual, interface, delegate ou dependência externa com múltiplos ou nenhum alvo resolvido, quando analisado, então somente os alvos conhecidos sustentam evidência e os demais permanecem desconhecidos, sem custo zero implícito.
- `AC-009` (FR-009, FR-010): Dado diagnóstico V4 emitido, quando seus detalhes forem consultados, então contém cadeia de chamadas, operação e repetição relevantes, impacto potencial, confiança, limitações e recomendação condicional, sem percentual de ganho ou frequência inventados.
- `AC-010` (FR-011): Dada configuração que desabilite ou altere a severidade de uma regra, quando a análise for executada, então somente a regra configurada é afetada; quando uma regra anterior já cobrir a mesma evidência sem contexto adicional, então não há diagnóstico duplicado.
- `AC-011` (FR-012): Dada a suíte V4, quando documentação e testes forem revisados, então cada regra possui casos positivos, negativos, dispatch desconhecido, recursão ou assíncrono quando aplicável, exceções, referência à fonte e hipótese mensurável.
- `AC-012` (NFR-001, NFR-002, NFR-003, NFR-004, NFR-005): Dado código nas plataformas aprovadas, quando os gates do projeto forem executados, então a análise compila, mantém custo aceitável, respeita a separação de responsabilidades e não recomenda alteração semanticamente não comprovada.
- `AC-013` (NFR-006): Dado código contendo identificadores, literais ou exemplos potencialmente sensíveis, quando testes e gates forem executados, então nenhum conteúdo é enviado a serviço externo e as mensagens expõem apenas o contexto necessário.

## Riscos, dependências e perguntas abertas

- Risco: interfaces, virtual dispatch, delegates, reflection e dependências externas podem gerar falsos positivos, caminhos incompletos ou explosão combinatória de alvos.
- Risco: recursão e chamadas indiretas podem exigir limites de análise; limites não devem converter incerteza em custo zero nem ocultar ciclos relevantes.
- Risco: a propagação interprocedural pode duplicar diagnósticos existentes ou tornar o custo do analyzer excessivo em soluções grandes.
- Risco: operações assíncronas podem ser confundidas com bloqueios, especialmente quando o corpo ou a continuação não estiver disponível.
- Dependência: catálogo e contratos das features `csharp-performance-analyzer`, `csharp-performance-analyzer-v2` e `csharp-performance-analyzer-v3`.
- Dependência: constituição, configuração e gates em `.spectro/config.yaml`.
- Dependência: Roslyn e versões aprovadas de C#/.NET.
- Decisão `Q-V4-001`: `PERF100` foi aprovado para o diagnóstico interprocedural V4, como extensão explícita do catálogo.
- Decisão `Q-V4-002`: não haverá limites numéricos específicos de profundidade, alvos ou grafo nesta fase; o custo continuará sujeito aos mecanismos gerais e aos gates do projeto.
- Decisão `Q-V4-003`: a primeira entrega incluirá todas as capacidades V4. A categoria será proposta no planejamento, pois não é definida pela documentação-fonte.
- Decisão `Q-V4-004`: `PERF100` usará `RiskSeverity.High`, mapeada para `DiagnosticSeverity.Warning` pelo contrato existente.
- Decisão `Q-V4-005`: `PERF100` usará a categoria fixa `Performance`; dimensões dinâmicas do custo serão apresentadas na evidência e na mensagem por ocorrência.

## Rastreabilidade

- Fonte funcional: `docs/csharp-performance-analyzer-2.md`, seção `2. V4 — Análise interprocedural`.
- Constituição: `.spectro/constitution.md`.
- Template: `.spectro/templates/specification.md`.
- Especificações relacionadas: `.spectro/features/csharp-performance-analyzer/spec.md`, `.spectro/features/csharp-performance-analyzer-v2/spec.md` e `.spectro/features/csharp-performance-analyzer-v3/spec.md`.

## Estado

- Estado da fase: `clarified`.
- Pronto para esclarecimento: não há ambiguidades bloqueantes remanescentes.
- Pronto para planejamento: sim; severidade e categoria de `PERF100` estão definidas.
