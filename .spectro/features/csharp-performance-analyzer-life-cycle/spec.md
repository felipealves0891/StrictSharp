<!-- spectro-managed: true -->
# Especificação: Contextos de ciclo de vida no C# Performance Analyzer

## Problema e objetivo

- Problema: loops de execução contínua, como o `while` de `BackgroundService.ExecuteAsync`, podem ser confundidos com loops computacionais de número desconhecido de iterações. Isso pode produzir diagnósticos de complexidade infinita ou custo excessivo mesmo quando o loop é a estrutura esperada de um serviço.
- Objetivo: reconhecer loops de ciclo de vida e distinguir sua semântica de loops computacionais, mantendo a análise das operações internas e permitindo que o custo seja explicado por iteração, item de trabalho e tempo somente quando houver evidência suficiente.

## Usuários e contexto

- Desenvolvedores e revisores de código C# que analisam Workers, consumidores, schedulers, servidores e outros pontos de entrada de longa duração.
- A análise deve explicar risco estrutural no código. Frequência real, disponibilidade de trabalho, custo efetivo e impacto em produção continuam dependentes de execução e medição representativa.

## Escopo

### Incluído

- Reconhecimento de `BackgroundService.ExecuteAsync` como entry point formal da primeira entrega; outros entry points de longa duração permanecem no roadmap.
- Classificação de contextos `Computational`, `Service Lifecycle`, `Polling`, `Event Driven`, `Retry` e `Batch` quando houver evidência suficiente.
- Tratamento diferenciado de espera temporal, espera por evento, sincronização, espera desconhecida, ausência de suspensão efetiva e trabalho custoso repetido.
- Propagação do contexto para métodos chamados e análise das operações executadas dentro do ciclo.
- Evidência de custo por iteração, item de trabalho e, condicionalmente, por unidade de tempo.
- Integração com as capacidades interprocedurais V4 e o modelo de custo V5 sem transformar o contexto de serviço em isenção de performance.
- Análise de `BackgroundService.ExecuteAsync` como entry point formal da primeira entrega.

### Excluído

- Assumir que todo `while` é um antipadrão, uma complexidade infinita ou um loop computacional.
- Inferir frequência fixa a partir de `Task.Delay`, disponibilidade de mensagens ou presença de `await`.
- Provar que um método assíncrono sempre suspende, sempre retorna sincronamente ou sempre encontra trabalho quando seu corpo ou comportamento não forem resolvidos.
- Profiling, telemetria, correlação com dados de runtime e medição de CPU, memória, throughput, latência ou ciclos por segundo.
- Criar o atributo ou contrato público `PerformanceContext` proposto no documento-fonte; essa possibilidade permanece futura e depende de decisão própria.
- Suporte formal a consumers de brokers, servidores TCP, processadores de `Channel<T>`, schedulers, game loops e serviços baseados em `IHostedService` na primeira entrega; esses entry points ficam para evolução posterior.
- Code Fix ou refatoração automática cuja equivalência semântica dependa de contrato, cancelamento, ciclo de vida, concorrência ou comportamento não demonstrado.

### Evolução não incluída nesta entrega

- A generalização da classificação para `IHostedService`, consumers de brokers, processadores de `Channel<T>`, schedulers, servidores TCP, game loops e outros entry points continua sendo uma evolução futura; a documentação-fonte a apresenta como recomendação, não como requisito da entrega atual.
- O contrato opcional `PerformanceContext` e qualquer frequência declarada pelo usuário continuam fora do contrato atual.

### Expansão solicitada

- Reconhecimento semântico de entry points de longa duração em serviços baseados em `IHostedService`, consumers de brokers, processadores de `Channel<T>`, schedulers, servidores TCP, game loops e loops de processamento de eventos, sem depender apenas do nome do método.
- Contrato opcional `PerformanceContext` para declarar a finalidade do contexto e uma frequência esperada como hipótese de análise, incluindo a explicitação de que a declaração não representa garantia de runtime.
- Propagação dos contextos expandidos para as análises de ciclo, mensagem, item, tentativa, lote e unidade de tempo, preservando os diagnósticos existentes e a distinção entre evidência estática e hipótese declarada.

### Mapeamento semântico da expansão

- `IHostedService` com execução contínua: `Service Lifecycle`.
- Consumers de brokers, processadores de `Channel<T>`, servidores TCP orientados a eventos e loops de processamento de eventos: `Event Driven`.
- Schedulers baseados em timer, atraso ou consulta periódica: `Polling`.
- Loops com tentativas, limite de tentativas e backoff: `Retry`.
- Processamento finito de itens agrupados: `Batch`, mantendo o ciclo externo como `Service Lifecycle` ou `Polling` quando aplicável.
- Game loops: `Service Lifecycle` para o ciclo de frames, com o trabalho de cada frame analisado como unidade computacional ou por item.
- A classificação exige evidência estrutural de APIs, controle de fluxo, espera, notificação, timer, backoff ou agrupamento; nomes isolados não são suficientes.

### Contrato declarativo

- `PerformanceContext` é um atributo público opcional com os campos `Kind` e `ExpectedIterationsPerSecond`.
- `Kind` usa `PerformanceContextKind` para declarar `BackgroundWorker`, `EventDriven`, `Polling`, `Retry`, `Batch` ou `GameLoop`; `BackgroundWorker` corresponde a `Service Lifecycle`.
- `ExpectedIterationsPerSecond` deve ser positivo quando informado e representa hipótese declarada para estimativa, não frequência efetiva garantida.
- Declaração ausente, inválida ou conflitante não deve impedir a análise estrutural; a frequência deve permanecer desconhecida nesses casos.

## Requisitos

### Requisitos funcionais

- `FR-001`: O produto deve reconhecer `BackgroundService.ExecuteAsync` e classificar seus loops de controle como `Service Lifecycle` quando houver evidência suficiente; outros entry points não fazem parte da primeira entrega.
- `FR-002`: A análise não deve emitir diagnóstico de complexidade infinita ou custo computacional indefinido apenas pela existência de um loop de ciclo de vida.
- `FR-003`: A análise deve continuar examinando as operações dentro do loop de ciclo de vida e reportar trabalho custoso, alocação, materialização, bloqueio ou repetição quando houver evidência suficiente.
- `FR-004`: O produto deve distinguir espera temporal explícita, espera por disponibilidade de trabalho, espera de sincronização, espera desconhecida, continuidade sem suspensão efetiva e simples cessão de execução.
- `FR-005`: O contexto de execução deve ser propagado para métodos chamados de forma observável, relacionando o ciclo, o lote, o item ou a mensagem às operações internas analisadas.
- `FR-006`: O modelo deve representar custo por iteração, por item de trabalho e, quando a frequência puder ser determinada ou declarada como hipótese, por unidade de tempo, mantendo as unidades separadas.
- `FR-007`: A análise deve identificar espera ativa ou busy polling quando o ciclo consulta trabalho repetidamente sem pausa, bloqueio ou mecanismo de notificação observável.
- `FR-008`: A análise deve identificar trabalho invariável ou materialização repetida entre ciclos quando a redundância puder ser demonstrada, sem acusar o loop como defeito por si só.
- `FR-009`: A análise deve considerar cancelamento e encerramento do serviço, sinalizando espera ou operação que possa impedir o término eficiente somente quando houver evidência estrutural suficiente.
- `FR-010`: A integração deve complementar as regras V4/V5 e evitar diagnóstico duplicado quando uma regra existente já explicar a mesma causa sem contexto adicional.
- `FR-011`: O produto deve reconhecer, por semântica estrutural e símbolos disponíveis, os entry points de longa duração incluídos na expansão — `IHostedService`, consumers de brokers, processadores de `Channel<T>`, schedulers, servidores TCP, game loops e loops de processamento de eventos — e classificá-los conforme o mapeamento `Service Lifecycle`, `Event Driven`, `Polling`, `Retry` ou `Batch` quando houver evidência suficiente.
- `FR-012`: O produto deve aceitar o atributo público opcional `PerformanceContext`, com `Kind` e `ExpectedIterationsPerSecond`, permitindo declarar a finalidade do processamento e uma frequência esperada como hipótese explícita, sem tratar a declaração como medição ou garantia de execução.
- `FR-013`: O produto deve propagar os contextos reconhecidos e declarados para métodos chamados, relacionando operações a ciclo, mensagem, item, tentativa, lote e unidade de tempo conforme a evidência disponível, mantendo desconhecido o que não puder ser resolvido.
- `FR-014`: A análise deve usar a frequência declarada em `PerformanceContext` somente para apresentar estimativas ou limites condicionais de custo por tempo, identificando a hipótese utilizada e sem substituir evidência estrutural ou medição de runtime.

### Requisitos não funcionais

- `NFR-001`: A análise deve ser conservadora e determinística para o mesmo código, símbolos e configuração, marcando como desconhecido o comportamento que não puder ser resolvido.
- `NFR-002`: O custo do analyzer deve permanecer aceitável em IDE e build, evitando travessias repetidas, alocações desnecessárias e inferência de frequência sem relação com o diagnóstico solicitado.
- `NFR-003`: A lógica de classificação e inferência deve permanecer separada da integração Roslyn, com um analisador por diagnóstico e serviços estáticos por padrão, conforme a constituição.
- `NFR-004`: Mensagens e recomendações devem distinguir evidência estática, hipótese, impacto potencial e limitação, sem prometer frequência, throughput ou ganho de performance não medidos.
- `NFR-005`: A solução deve preservar cancelamento, semântica assíncrona, thread safety, ciclo de vida e contratos das APIs analisadas; nenhuma sugestão deve depender de equivalência não demonstrada.
- `NFR-006`: A análise não deve enviar código-fonte, segredos, dados de produção ou informações sensíveis para serviços externos.
- `NFR-007`: A expansão deve permanecer compatível com o catálogo e os contratos V1–V5 existentes, sem reclassificar loops já suportados de forma incompatível nem reutilizar IDs reservados para outra etapa.

### Restrições

- `CON-001`: O loop de serviço define o ciclo de vida da aplicação; sua existência não deve ser usada como substituto da complexidade do trabalho executado em cada ciclo.
- `CON-002`: `await ProcessAsync(...)` não prova que existe uma pausa útil; o comportamento do método chamado deve ser examinado quando disponível e marcado como desconhecido quando não estiver resolvido.
- `CON-003`: `Task.Delay(...)` representa uma espera solicitada entre ciclos, mas não garante uma frequência efetiva fixa; qualquer custo por tempo deve declarar a hipótese e suas limitações.
- `CON-004`: `Task.CompletedTask` não introduz suspensão efetiva; `Task.Yield()` cede execução, mas não controla a frequência do loop por si só.
- `CON-005`: Os diagnósticos de ciclo de vida usarão os IDs `PERF108`–`PERF112`, sem reutilizar `PERF101`–`PERF107` do catálogo V5. O mapeamento de severidade e categoria deve respeitar a aprovação do planejamento.
- `CON-006`: A capacidade deve permanecer baseada em análise estática Roslyn e compatível com as versões de C#, .NET, Target Framework e Roslyn aprovadas pelo projeto.
- `CON-007`: Suspeita de busy polling, retenção, bloqueio ou encerramento ineficiente deve ser apresentada como risco fundamentado, não como comportamento de runtime comprovado.
- `CON-008`: A numeração histórica `PERF101`–`PERF105` apresentada na seção conceitual da documentação não deve ser tratada como catálogo vigente; a primeira entrega usa exclusivamente o mapeamento final `PERF108`–`PERF112` aprovado para o ciclo de vida.
- `CON-009`: A identificação dos novos entry points deve ser baseada em evidência semântica disponível; nome de método, tipo ou framework isoladamente não é prova suficiente de contexto de ciclo de vida.
- `CON-010`: `PerformanceContext` é um contrato opcional de hipótese para análise estática; sua frequência esperada não representa frequência efetiva, throughput, latência ou comportamento garantido em runtime.

## Critérios de aceite

- `AC-001` (FR-001): Dado um `BackgroundService.ExecuteAsync` com `while (!stoppingToken.IsCancellationRequested)`, quando analisado, então o loop é classificado como `Service Lifecycle` e não como loop computacional genérico apenas por ser contínuo.
- `AC-002` (FR-002): Dado um loop de ciclo de vida com `Task.Delay` ou espera por evento, quando analisado, então nenhum diagnóstico de complexidade infinita é emitido somente pela quantidade potencial de ciclos.
- `AC-003` (FR-003): Dado um ciclo que chama `ProcessBatchAsync` e materializa dados ou aloca por item, quando analisado, então a evidência relaciona a operação ao ciclo e mantém o diagnóstico da operação aplicável.
- `AC-004` (FR-004): Dado `Task.Delay`, `ReadAsync`, `WaitAsync`, `Task.CompletedTask`, `Task.Yield()` ou um método assíncrono arbitrário, quando analisado, então cada caso é classificado conforme a semântica conhecida e o caso não resolvido é marcado como desconhecido.
- `AC-005` (FR-005): Dado `ExecuteAsync` chamando `ProcessBatchAsync`, que chama `ProcessItemAsync`, quando os alvos forem resolvidos, então a evidência preserva a hierarquia de serviço, lote e item até a operação analisada.
- `AC-006` (FR-006): Dado um custo conhecido por item e quantidade conhecida por ciclo, quando analisado, então o resultado pode apresentar custo por item e por ciclo separadamente; se houver apenas `Task.Delay(500)`, então não afirma dois ciclos por segundo como fato.
- `AC-007` (FR-007): Dado um loop que consulta uma fila e retorna imediatamente quando vazia, sem espera ou notificação observável, quando analisado, então é sinalizado possível busy polling; dado intervalo, bloqueio ou espera por evento observável, então o loop não é sinalizado por essa causa.
- `AC-008` (FR-008): Dado trabalho invariável ou materialização repetida entre ciclos que possa ser demonstrado, quando analisado, então a evidência explica a repetição; quando a invariância depender de estado desconhecido, então o diagnóstico é omitido ou qualificado.
- `AC-009` (FR-009): Dado um ciclo cujo caminho de espera ignore o token de cancelamento ou possa impedir o encerramento, quando a relação for demonstrável, então o resultado sinaliza risco de encerramento e identifica a operação responsável.
- `AC-010` (FR-010): Dado um padrão já coberto por uma regra existente, quando o contexto de ciclo de vida não acrescentar informação relevante, então não são emitidos diagnósticos duplicados; quando acrescentar contexto, a relação é explicitada.
- `AC-011` (NFR-001, NFR-002, NFR-003): Dado o mesmo código, símbolos e configuração, quando a análise for repetida nos gates do projeto, então a classificação e a evidência permanecem determinísticas, com custo aceitável e separação de responsabilidades preservada.
- `AC-012` (NFR-004, NFR-005, NFR-006): Dado um diagnóstico de ciclo de vida, quando seus detalhes forem consultados, então ele distingue evidência e hipótese, preserva contratos de cancelamento e async, não afirma medição inexistente e não envia conteúdo para serviço externo.
- `AC-013` (CON-005, CON-008): Dado o catálogo de diagnósticos de ciclo de vida, quando seus metadados forem consultados, então somente `PERF108`–`PERF112` são usados para esta capacidade, com categorias e severidades aprovadas, e nenhuma regra nova reutiliza `PERF101`–`PERF107`.
- `AC-014` (FR-011): Dado um entry point suportado de `IHostedService`, consumer, `Channel<T>`, scheduler, servidor TCP, game loop ou processamento de eventos, quando houver evidência estrutural suficiente, então ele é classificado conforme o mapeamento definido — ciclo de vida, orientado a eventos, polling, retry ou batch —; quando a semântica não puder ser determinada, então permanece desconhecido e não gera diagnóstico baseado apenas em convenção nominal.
- `AC-015` (FR-012): Dado um `PerformanceContext` válido associado ao processamento, com `Kind` e opcionalmente `ExpectedIterationsPerSecond` positivo, quando seus campos forem analisados, então a finalidade e a frequência esperada ficam disponíveis como hipótese rastreável; na ausência ou invalidez do contrato, a análise continua funcionando e mantém a frequência desconhecida.
- `AC-016` (FR-013): Dado um entry point expandido que chama métodos de processamento de mensagem, item, tentativa ou lote, quando os alvos forem resolvidos, então a evidência preserva a hierarquia do contexto até a operação analisada; fronteiras não resolvidas permanecem qualificadas como desconhecidas.
- `AC-017` (FR-014, CON-010): Dado um `PerformanceContext` com frequência esperada, quando o custo por tempo for apresentado, então o resultado identifica a frequência como hipótese e não afirma ciclos efetivos, throughput ou impacto de produção como fatos medidos.
- `AC-018` (NFR-007, CON-009): Dado código que usa um entry point já suportado ou um padrão semelhante sem evidência semântica suficiente, quando a análise for executada, então os diagnósticos V1–V5 e `PERF108`–`PERF112` permanecem compatíveis e não são emitidos novos diagnósticos apenas por correspondência nominal.

## Riscos, dependências e perguntas abertas

- Risco: reconhecer semântica de serviço apenas por nomes pode gerar falsos positivos ou deixar entry points válidos sem classificação.
- Risco: dispatch virtual, interfaces, delegates, métodos externos e retorno síncrono de métodos assíncronos podem impedir a determinação da suspensão real.
- Risco: a propagação interprocedural pode aumentar o custo do analyzer ou duplicar evidências V4/V5 em soluções grandes.
- Risco: usuários podem interpretar custo por tempo como medição; mensagens devem declarar que a frequência é hipótese ou limite estrutural quando aplicável.
- Dependência: `.spectro/features/csharp-performance-analyzer-v4/spec.md` para relações interprocedurais e `PERF100`.
- Dependência: `.spectro/features/csharp-performance-analyzer-v5/spec.md` para unidades e modelo de custo, incluindo o catálogo já reservado `PERF101`–`PERF107`.
- Dependência: constituição, configuração e gates em `.spectro/constitution.md` e `.spectro/config.yaml`.
- Decisão `Q-LIFE-007`: a expansão solicitada inclui tanto o reconhecimento dos entry points adicionais quanto o contrato opcional `PerformanceContext`; ambos devem ser planejados como evolução da feature, sem alterar o escopo já concluído da primeira entrega.
- Decisão `Q-LIFE-008`: o mapeamento semântico e as evidências mínimas da expansão estão definidos na seção “Mapeamento semântico da expansão”.
- Decisão `Q-LIFE-009`: `PerformanceContext` será um atributo público opcional com `Kind` e `ExpectedIterationsPerSecond`, usando os valores de `PerformanceContextKind` definidos na seção “Contrato declarativo”; frequência inválida ou ausente permanece desconhecida.
- Decisão `Q-LIFE-010`: a expansão reutilizará `PERF108`–`PERF112`; não serão criados novos IDs para os entry points adicionais ou para o contrato declarativo.
- Decisão `Q-LIFE-001`: criar `PERF108`–`PERF112` para os cinco padrões de ciclo de vida descritos na fonte, mantendo `PERF101`–`PERF107` exclusivos da V5.
- Decisão `Q-LIFE-002`: limitar a primeira entrega a `BackgroundService.ExecuteAsync`; os demais entry points permanecem no roadmap futuro.
- Decisão `Q-LIFE-005`: `PERF108` e `PERF109` usam severidade `High`, `PERF110` e `PERF111` usam `Critical`, `PERF112` usa `Medium`; as categorias são `Concurrency` para `PERF108`, `PERF110`, `PERF111` e `PERF112`, e `Performance` para `PERF109`.
- Decisão `Q-LIFE-006`: a tabela de `PERF101`–`PERF105` na seção conceitual da documentação é tratada como proposta histórica; a seção final da documentação, as clarificações e o catálogo implementado prevalecem com `PERF108`–`PERF112`.

## Rastreabilidade

- Fonte funcional: `docs/csharp-performance-analyzer-life-cycle.md`.
- Constituição: `.spectro/constitution.md`.
- Template: `.spectro/templates/specification.md`.
- Especificações relacionadas: `.spectro/features/csharp-performance-analyzer-v4/spec.md` e `.spectro/features/csharp-performance-analyzer-v5/spec.md`.
- Conflito reconciliado: a documentação contém uma tabela conceitual com IDs `PERF101`–`PERF105` e uma definição final com `PERF108`–`PERF112`; somente a definição final é normativa para esta feature.

## Estado

- Estado da fase: `clarified` para a expansão solicitada; a primeira entrega permanece `completed`.
- Pronto para esclarecimento: não há ambiguidades bloqueantes ou major remanescentes.
- Pronto para planejamento: sim; o mapeamento semântico, o contrato público e o catálogo foram definidos.
- Implementação da primeira entrega: concluída, com gates aprovados e revisão final `approved`.
- Implementação da expansão: pendente de planejamento e execução.
