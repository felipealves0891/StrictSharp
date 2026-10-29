<!-- spectro-managed: true -->
# Especificação: Implementação V2 do C# Performance Analyzer

## Problema e objetivo

- Problema: a V1 cobre padrões relativamente objetivos, mas ainda não identifica 13 anti-padrões em que o risco depende da relação entre operações, repetição, concorrência, estrutura de dados e ciclo de vida. Sem essa etapa, o catálogo V2 permanece documentado, porém não oferece feedback acionável para padrões compostos de complexidade e escalabilidade.
- Objetivo: implementar a etapa V2 do analisador estático, mantendo os diagnósticos `PERF002`, `PERF004`, `PERF005`, `PERF006`, `PERF009`, `PERF010`, `PERF016`, `PERF017`, `PERF021`, `PERF023`, `PERF024`, `PERF028` e `PERF029` rastreáveis, configuráveis, de baixo ruído e explícitos quanto à confiança e às limitações da análise estática.

## Usuários e contexto

- Desenvolvedores e revisores de código C# que precisam avaliar riscos de concorrência, alocação, complexidade algorítmica, estruturas de dados e gerenciamento de recursos.
- Caminhos de execução que processam coleções, fazem operações repetidas, acessam I/O ou coordenam trabalho concorrente são contextos prioritários; a especificação não presume que todo padrão analisado seja um gargalo em produção.

## Escopo

### Incluído

- Implementação dos 13 diagnósticos V2 catalogados:
  - concorrência e sincronização: `PERF002`, `PERF004`, `PERF005`, `PERF006`;
  - alocação e delegates: `PERF009`, `PERF010`;
  - LINQ e ordenação: `PERF016`, `PERF017`;
  - regex e metadados reutilizáveis: `PERF021`;
  - coleções: `PERF023`, `PERF024`;
  - ciclo de vida e exceções: `PERF028`, `PERF029`.
- Integração de cada regra ao catálogo estável `PERF001`–`PERF030`, à documentação e à configuração de severidade.
- Mensagens que expliquem evidência estrutural, impacto potencial, recomendação condicional, exceções, confiança e limitações.
- Testes positivos, negativos e de exceção para cada regra, incluindo casos de fronteira relevantes ao contexto.
- Validação de que a análise continua compatível com as plataformas e versões já aprovadas para o projeto.

### Excluído

- Implementação das regras V1 ou V3, salvo ajustes de integração indispensáveis para manter o catálogo consistente.
- Profiling, benchmarks obrigatórios, telemetria, análise de carga real ou correlação com dados de produção.
- Promessa de que uma coleção, API, estratégia de concorrência ou mecanismo de sincronização alternativo será sempre mais rápido.
- Detecção completa de vazamentos ou prova de que um recurso não será liberado em todos os caminhos dinâmicos.
- Code Fix automático para mudanças que dependam de contrato, mutabilidade, igualdade, propriedade, thread safety, ciclo de vida ou semântica não demonstrada.
- Definição de arquitetura interna, nomes de classes, divisão de serviços ou mecanismo específico de implementação; essas decisões pertencem ao planejamento técnico.

## Requisitos

### Requisitos funcionais

- `FR-001`: O produto deve disponibilizar os 13 diagnósticos V2 com os IDs estáveis, títulos, categorias e severidades definidas no catálogo existente, sem apresentá-los como regras oficiais do .NET.
- `FR-002`: `PERF002` deve identificar criação de trabalho concorrente sem limite explícito ou demonstrável, especialmente quando `Task.WhenAll` recebe uma coleção de tamanho desconhecido, e deve reconhecer como exceção limites comprovados ou coleções comprovadamente pequenas.
- `FR-003`: `PERF004` deve identificar região de exclusão mútua que contenha I/O, espera de tarefa, `Sleep`, consulta remota ou processamento potencialmente longo, sem reportar operação curta com sincronização necessária quando essa exceção for demonstrável.
- `FR-004`: `PERF005` deve identificar polling ou espera ativa com consultas repetitivas e espera ausente ou excessivamente curta, preservando casos de spin-wait deliberado, extremamente curto e justificado.
- `FR-005`: `PERF006` deve identificar criação repetida de `Thread` ou `Task.Run` em loops, por item ou em execuções de alta frequência, preservando threads dedicadas de longa duração e tarefas pontuais justificadas.
- `FR-006`: `PERF009` deve identificar boxing explícito ou implícito evitável de value types para `object` ou interfaces quando houver alternativa genérica ou tipada observável, preservando boxing exigido pelo contrato ou eliminado por especialização comprovada.
- `FR-007`: `PERF010` deve identificar delegates, lambdas ou closures criados repetidamente com captura de estado externo quando a captura puder ser evitada ou o delegate puder ser reutilizado, preservando casos em que a alocação não ocorre no cenário efetivo ou em que a execução é pouco frequente.
- `FR-008`: `PERF016` deve identificar ordenações repetidas sem alteração dos dados ou ordenações completas quando o padrão observado exige somente poucos elementos, preservando ordenações necessárias após mutação.
- `FR-009`: `PERF017` deve identificar pipelines LINQ com filtros, projeções ou agregações invariantes recalculados dentro de loops, preservando consultas dependentes da iteração ou sequências com efeitos colaterais relevantes.
- `FR-010`: `PERF021` deve identificar criação repetida de `Regex`, opções de serialização ou metadados/configurações pesadas imutáveis quando a reutilização for aplicável, preservando configurações dinâmicas e APIs que já forneçam cache suficiente.
- `FR-011`: `PERF023` deve identificar estruturas de coleção incompatíveis com o padrão de acesso dominante, como remoção recorrente no início de `List<T>`, buscas lineares massivas ou inserções que deslocam muitos elementos, preservando coleções pequenas e casos em que a localidade de memória favoreça a estrutura atual.
- `FR-012`: `PERF024` deve identificar construção de coleções com múltiplas inserções quando uma capacidade inicial relevante for conhecida, preservando casos em que a capacidade não seja conhecida ou em que já exista estratégia adequada de crescimento.
- `FR-013`: `PERF028` deve identificar recursos com propriedade e ciclo de vida observáveis que não tenham liberação adequada, incluindo quando aplicável `using`, `await using` ou descarte controlado, preservando transferência de propriedade e gerenciamento externo demonstráveis.
- `FR-014`: `PERF029` deve identificar uso recorrente de exceções para representar fluxo esperado, como `Parse` com captura repetida de `FormatException`, preservando falhas verdadeiramente excepcionais, raras ou exigidas pelo contrato da API.
- `FR-015`: Cada diagnóstico V2 deve informar a evidência observada, o recurso ou custo potencial, a recomendação condicional, as exceções conhecidas, o nível de confiança e a limitação de que o custo real depende de execução e carga.
- `FR-016`: Cada diagnóstico V2 deve ser configurável individualmente para habilitação, desabilitação e severidade, mantendo o ID estável e respeitando a configuração padrão do ecossistema Roslyn.
- `FR-017`: Cada diagnóstico V2 deve possuir documentação rastreável, exemplos positivos e negativos, testes de exceção e justificativa reproduzível, sem exigir dados de produção para a validação estática.

### Requisitos não funcionais

- `NFR-001`: A análise deve priorizar precisão e baixo ruído; não deve emitir diagnóstico apenas porque uma construção é possível, sem evidência contextual suficiente.
- `NFR-002`: O custo do analyzer deve permanecer aceitável em IDE e build, evitando trabalho semântico repetido, alocações desnecessárias e análise de símbolos não relacionados ao diagnóstico.
- `NFR-003`: A implementação deve manter separadas a integração Roslyn e a lógica específica de análise, com um analyzer por diagnóstico e serviços estáticos por padrão, conforme as convenções do projeto.
- `NFR-004`: As recomendações devem preservar a semântica observável e ser condicionais quando dependerem de ordem, mutabilidade, igualdade, thread safety, cancelamento, propriedade de recurso ou contrato de API.
- `NFR-005`: Os diagnósticos devem continuar compatíveis com .NET 10, .NET Standard 2.1 e C# 14, conforme a base aprovada da especificação-mãe.
- `NFR-006`: A validação deve distinguir evidência estrutural de impacto medido e não deve apresentar ganhos percentuais, complexidade efetiva em produção ou frequência de execução sem benchmark correspondente.
- `NFR-007`: A análise não deve enviar código-fonte, segredos, dados de produção ou informações sensíveis para serviços externos.

### Restrições

- `CON-001`: Os IDs V2 são exatamente `PERF002`, `PERF004`, `PERF005`, `PERF006`, `PERF009`, `PERF010`, `PERF016`, `PERF017`, `PERF021`, `PERF023`, `PERF024`, `PERF028` e `PERF029`.
- `CON-002`: A V2 deve complementar os analisadores oficiais e evitar duplicação sem valor explicativo adicional, especialmente quando houver regra `CA18xx` aplicável.
- `CON-003`: Severidade representa risco operacional plausível, não estimativa de ganho. Os valores devem permanecer entre `Critical`, `High`, `Medium` e `Low`.
- `CON-004`: Suspeitas de ciclo de vida ou descarte devem ser apresentadas como evidência fundamentada, nunca como prova completa de vazamento ou falha em caminhos dinâmicos.
- `CON-005`: Alterações de severidade, plataforma, etapa do roadmap, escopo, dependências ou contrato público exigem aprovação humana conforme a constituição do projeto.
- `CON-006`: A implementação não deve adicionar dependências externas sem justificativa funcional, de compatibilidade, performance e manutenção.

## Critérios de aceite

- `AC-001` (FR-001): Dado o catálogo carregado, quando os diagnósticos V2 forem consultados, então os 13 IDs aparecem uma única vez, com metadados consistentes e etapa V2.
- `AC-002` (FR-002): Dado `Task.WhenAll` sobre fonte não limitada, quando a análise não conseguir demonstrar limite ou tamanho pequeno, então `PERF002` é emitido; dado limite explícito ou coleção pequena demonstrável, então não é emitido.
- `AC-003` (FR-003): Dado `lock` contendo I/O, espera ou operação longa observável, quando o método for analisado, então `PERF004` é emitido; dado bloco curto e justificado, então não é emitido.
- `AC-004` (FR-004): Dado loop que consulta repetidamente sem espera adequada ou com intervalo excessivamente curto, quando analisado, então `PERF005` é emitido; dado spin-wait deliberado e justificado, então não é emitido.
- `AC-005` (FR-005): Dado `new Thread` ou `Task.Run` criado repetidamente em loop ou por item, quando analisado, então `PERF006` é emitido; dada thread dedicada longa ou tarefa pontual justificada, então não é emitido.
- `AC-006` (FR-006): Dado value type enviado a API `object`/interface sem necessidade contratual, quando analisado, então `PERF009` é emitido; dada exigência contratual ou especialização comprovada, então não é emitido.
- `AC-007` (FR-007): Dado delegate/lambda com captura recriado em repetição e substituível por forma sem captura, quando analisado, então `PERF010` é emitido; dado cenário sem alocação efetiva ou pouco frequente documentado, então não é emitido.
- `AC-008` (FR-008): Dado `OrderBy`/`Sort` repetido sobre dados não alterados ou ordenação completa para poucos elementos, quando analisado, então `PERF016` é emitido; dada mutação entre ordenações, então não é emitido.
- `AC-009` (FR-009): Dado pipeline LINQ invariável dentro de loop, quando analisado, então `PERF017` é emitido com indicação da repetição; dada dependência real da iteração, então não é emitido.
- `AC-010` (FR-010): Dada criação repetida de regex ou configuração pesada imutável reutilizável, quando analisada, então `PERF021` é emitido; dada configuração dinâmica ou cache garantido pela API, então não é emitido.
- `AC-011` (FR-011): Dado padrão de acesso dominante incompatível com `List<T>` ou outra coleção observável, quando analisado, então `PERF023` é emitido com alternativa condicional; dada coleção pequena ou localidade favorável demonstrável, então não é emitido.
- `AC-012` (FR-012): Dada capacidade relevante conhecida antes da construção e múltiplas inserções, quando analisada, então `PERF024` é emitido; dada capacidade desconhecida, então não é emitido.
- `AC-013` (FR-013): Dado recurso descartável criado e sem caminho de liberação observável, quando analisado, então `PERF028` é emitido como suspeita fundamentada; dada transferência de propriedade ou gerenciamento externo documentável, então não é emitido.
- `AC-014` (FR-014): Dado `Parse` em caminho recorrente com captura de exceção esperada, quando analisado, então `PERF029` é emitido; dado erro raro e excepcional ou contrato que exige exceção, então não é emitido.
- `AC-015` (FR-015, NFR-006): Dado qualquer diagnóstico V2 emitido, quando seus detalhes forem consultados, então a mensagem diferencia evidência estática de impacto potencial, inclui confiança, exceções e não promete ganho percentual ou medição de produção.
- `AC-016` (FR-016): Dada configuração `.editorconfig` que desabilita ou altera a severidade de uma regra V2, quando a análise for executada, então somente essa configuração é respeitada sem alterar o ID ou os metadados do catálogo.
- `AC-017` (FR-017): Dada a suíte V2, quando os testes forem executados nas plataformas suportadas, então cada regra possui caso positivo, negativo, exceção relevante e justificativa rastreável à documentação.
- `AC-018` (NFR-001, NFR-002, NFR-003, NFR-004, NFR-005): Dado código compatível com as plataformas aprovadas, quando a análise completa for executada, então compila, preserva a semântica das recomendações condicionais e permanece dentro dos gates de qualidade definidos pelo projeto.
- `AC-019` (NFR-007): Dado código analisado contendo literais ou identificadores sensíveis, quando a suíte e os gates forem executados, então nenhum conteúdo é enviado a serviço externo e as mensagens expõem apenas o contexto necessário.

## Riscos, dependências e perguntas abertas

- Risco: a análise estática pode não provar tamanho de coleção, frequência, duração de operação, limite de concorrência, efeitos colaterais ou propriedade de recurso; nesses casos, a confiança deve ser reduzida ou o diagnóstico omitido.
- Risco: recomendações como `Parallel.ForEachAsync`, `SemaphoreSlim`, `Channel<T>`, `HashSet<T>`, `Dictionary<TKey,TValue>`, lambdas `static`, caches e `TryParse` podem alterar ordem, memória, throughput, tratamento de erro ou concorrência.
- Risco: `PERF028` pode se sobrepor a regras oficiais e a padrões válidos de transferência de propriedade; a deduplicação e as exceções precisam ser verificadas nos testes.
- Risco: heurísticas de “operação longa”, “coleção pequena”, “intervalo excessivamente curto” e “capacidade relevante” podem gerar ruído se não houver evidência suficiente.
- Dependência: catálogo, documentação e contratos existentes da feature `csharp-performance-analyzer`.
- Dependência: Roslyn e versões de C#/.NET aprovadas na especificação-mãe.
- Dependência: gates de build, testes, segurança e cobertura configurados em `.spectro/config.yaml`.
- Decisão `Q-V2-001`: quando tamanho, duração, frequência ou capacidade não forem demonstráveis estaticamente, a V2 deve reduzir a confiança ou omitir o diagnóstico; não deve usar limiares numéricos arbitrários.
- Decisão `Q-V2-003`: a primeira entrega deve reconhecer os exemplos e APIs já citados no catálogo e não ampliar esse contrato durante o planejamento sem nova clarificação.
- Decisão `Q-V2-004`: não haverá limiar numérico adicional para o custo do analyzer nesta fase; aplicam-se as orientações oficiais de analyzers Roslyn e os gates existentes.

## Rastreabilidade

- Especificação-mãe: `.spectro/features/csharp-performance-analyzer/spec.md`.
- Fonte funcional: `docs/csharp-performance-analyzer.md`, seções do catálogo V2 e estratégia de evolução.
- Constituição: `.spectro/constitution.md`.
- Configuração de gates: `.spectro/config.yaml`.

## Estado

- Estado da fase: `completed`.
- Pronto para planejamento: sim.
