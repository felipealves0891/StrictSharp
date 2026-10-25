<!-- spectro-managed: true -->
# Especificação: C# Performance Analyzer

## Problema e objetivo

- Problema: aplicações .NET podem conter decisões de código que degradam CPU, memória, concorrência, I/O, escalabilidade ou latência, mas diagnósticos isolados frequentemente não explicam o custo, as condições de ocorrência e as alternativas de correção. Roslyn identifica riscos estruturais, porém não determina sozinho frequência de execução, volume de dados ou perda percentual em produção.
- Objetivo: definir um analisador estático de C# baseado em Roslyn que complemente os analisadores oficiais, detecte anti-padrões de performance e produza diagnósticos acionáveis, transparentes e rastreáveis, distinguindo custo evidenciado pelo código de impacto potencial dependente da carga.

## Usuários e contexto

- Desenvolvedores e revisores de código que precisam decidir se um padrão apresenta risco de performance.
- Aplicações de longa duração, APIs, Workers, processamento de filas e bibliotecas usadas em caminhos críticos são contextos prioritários, sem presumir que todo código analisado esteja nesses contextos.

## Escopo

### Incluído

- Análise de código-fonte C# com Roslyn nas dimensões CPU, memória, concorrência, I/O, escalabilidade e latência.
- Catálogo inicial de 30 diagnósticos estáveis `PERF001` a `PERF030`, que não são IDs oficiais do .NET:
  - Concorrência e assíncrono: `PERF001`–`PERF006`.
  - Memória, alocação e GC: `PERF007`–`PERF012`.
  - LINQ e complexidade algorítmica: `PERF013`–`PERF017`.
  - Strings, parsing e serialização: `PERF018`–`PERF021`.
  - Coleções e estruturas de acesso: `PERF022`–`PERF024`.
  - I/O, recursos e observabilidade: `PERF025`–`PERF030`.
- Entrega incremental: V1 com 15 regras objetivas; V2 com 13 regras dependentes de contexto; V3 com 2 regras de ciclo de vida e análise profunda.
- Priorização inicial para demonstrar valor: `PERF001`, `PERF008`, `PERF014`, `PERF025` e `PERF027`.
- Mensagens que expliquem evidência, recurso consumido, evolução do custo, correção recomendada, exceções, confiança e referências.
- Orientação de refatoração quando uma correção automática não for segura ou não puder preservar a semântica.

### Excluído

- Medir frequência real de execução, carga de produção ou ganho percentual somente por análise estática.
- Prometer detecção completa de memory leaks ou substituir profiling e benchmarks.
- Reproduzir regras oficiais sem acrescentar contexto, correlação ou valor; diagnósticos duplicados de `CA18xx` devem ser evitados quando não houver valor adicional.
- Escolher arquitetura de implementação, APIs internas, estratégia de classes ou mecanismo específico de Code Fix nesta especificação.
- Alterar o comportamento do programa em nome de uma otimização cuja equivalência semântica não esteja demonstrada.

## Requisitos

### Requisitos funcionais

- `FR-001`: O produto deve analisar código-fonte C# com Roslyn e identificar riscos estruturais relacionados às seis dimensões de performance definidas no documento de origem.
- `FR-002`: O produto deve manter os identificadores `PERF001` a `PERF030` estáveis, únicos e associados ao catálogo de regras definido na documentação.
- `FR-003`: Cada ocorrência reportada deve conter identificador, título, severidade (`Critical`, `High`, `Medium` ou `Low`), categoria, evidência, impacto, escalabilidade, correção, exemplo, exceções, confiança e referência técnica.
- `FR-004`: O analisador deve distinguir o custo comprovável pela estrutura do código do impacto potencial que depende de frequência, tamanho da entrada, concorrência ou ambiente de execução.
- `FR-005`: O analisador deve evitar emitir diagnóstico quando houver evidência insuficiente ou quando a documentação da regra definir uma exceção aplicável, incluindo tarefas já concluídas, coleções comprovadamente pequenas, fronteiras síncronas intencionais, APIs sem alternativa assíncrona e propriedade de recursos transferida.
- `FR-006`: A primeira entrega deve implementar as 15 regras classificadas como V1. `PERF001` (sync-over-async), `PERF008` (alocações grandes recorrentes), `PERF014` (busca linear repetida), `PERF025` (uso inadequado de `HttpClient`) e `PERF027` (logging com custo evitável) permanecem como prioridade de demonstração de valor.
- `FR-007`: A evolução do catálogo deve respeitar as etapas V1, V2 e V3 documentadas, sem tratar regras de maior dependência contextual como equivalentes às regras objetivas da V1.
- `FR-008`: O produto deve permitir habilitar, desabilitar e ajustar a severidade de cada regra.
- `FR-009`: O produto deve fornecer recomendação de correção ou refatoração condicional, incluindo alternativas como `await`, limitação de concorrência, pooling, indexação, APIs de comparação adequadas, logging estruturado e descarte controlado quando aplicáveis.
- `FR-010`: O produto deve indicar a documentação ou regra oficial relacionada a cada diagnóstico, quando existir, e identificar quando a regra é complementar em vez de uma reprodução.
- `FR-011`: Cada regra deve ser validada com exemplos positivos e negativos, versões suportadas de runtime e uma justificativa reproduzível. Nesta fase, a validação é exclusivamente estática; benchmarks e evidências de runtime ficam fora do escopo atual.
- `FR-012`: O produto deve separar a identificação estática do risco da medição de seu custo real em execução, deixando profiling e evidências de runtime como capacidade futura.

### Requisitos não funcionais

- `NFR-001`: Os diagnósticos devem ter baixo ruído e precisão suficiente para não transformar micro-otimizações sem evidência em erros.
- `NFR-002`: As recomendações não devem alterar a semântica do programa; alternativas dependentes de contrato, mutabilidade, ciclo de vida, thread safety ou propriedade de dados devem ser apresentadas condicionalmente.
- `NFR-003`: O analisador deve respeitar .NET 10 e .NET Standard 2.1 como plataformas definidas para a V1 e C# 14 como versão de linguagem.
- `NFR-004`: O custo do próprio analisador não deve tornar a IDE ou o build demasiadamente lentos; a avaliação seguirá as orientações oficiais da Microsoft para analyzers Roslyn, incluindo evitar trabalho semântico desnecessário e habilitar execução concorrente quando seguro.
- `NFR-005`: O analisador deve representar severidade como classificação de risco operacional, nunca como estimativa de ganho percentual.
- `NFR-006`: As regras devem considerar contexto suficiente para diferenciar loops, caminhos críticos, inicialização e código frio quando esse contexto estiver disponível.
- `NFR-007`: A análise deve considerar performance e segurança operacional de recursos, incluindo pressão de GC/LOH, ThreadPool, conexões, handles, contenção e retenção.
- `NFR-008`: Nesta fase, a validação deve usar evidências estáticas adequadas à categoria e registrar limitações sobre CPU, bytes alocados, GC/LOH, throughput, threads, contenção, latência, conexões, memória viva e escalabilidade, que só poderão ser quantificados por profiling ou benchmark futuro.

### Restrições

- `CON-001`: A implementação deve usar Roslyn e permanecer no ecossistema C#/.NET definido pelo projeto, com .NET 10, .NET Standard 2.1 e C# 14 como base da V1.
- `CON-002`: Os IDs `PERF001`–`PERF030` são convenções do produto e não devem ser apresentados como IDs oficiais do .NET.
- `CON-003`: A ferramenta deve complementar os analisadores oficiais e não duplicar seus diagnósticos sem valor adicional.
- `CON-004`: Cada regra deve explicitar exceções e nível de confiança; suspeitas de ciclo de vida não podem ser apresentadas como prova de vazamento.
- `CON-005`: A solução deve seguir Specification First e as convenções do projeto, separando a lógica de análise do analisador e mantendo um analisador por diagnóstico.

## Critérios de aceite

- `AC-001` (FR-001): Dado um projeto C# compatível, quando a análise for executada, então os padrões suportados são avaliados sem exigir dados de produção para produzir um risco estrutural.
- `AC-002` (FR-002): Dado qualquer diagnóstico do catálogo, quando ele for emitido, então seu ID pertence a `PERF001`–`PERF030` e não muda entre execuções equivalentes.
- `AC-003` (FR-003, FR-004): Dado um exemplo positivo, quando o diagnóstico for exibido, então a mensagem informa os campos obrigatórios e diferencia custo observado no código de impacto potencial dependente da carga.
- `AC-004` (FR-005): Dado um exemplo que satisfaz uma exceção documentada, quando a análise puder demonstrar essa condição, então o diagnóstico não é emitido.
- `AC-005` (FR-006): Dado o escopo da V1, quando a primeira entrega for avaliada, então as 15 regras V1 possuem casos positivos, negativos, explicação e orientação rastreável; `PERF001`, `PERF008`, `PERF014`, `PERF025` e `PERF027` são verificadas também como prioridade de demonstração de valor.
- `AC-006` (FR-007): Dado o catálogo completo, quando o roadmap for consultado, então cada regra está classificada em exatamente uma etapa V1, V2 ou V3 conforme a documentação.
- `AC-007` (FR-008): Dada uma configuração de projeto, quando uma regra for desabilitada ou tiver sua severidade ajustada, então a saída respeita essa configuração sem alterar os IDs.
- `AC-008` (FR-009): Dado um padrão detectado, quando a correção depender de semântica ou contexto não comprovado, então o produto apresenta orientação condicional em vez de aplicar automaticamente uma alteração insegura.
- `AC-009` (FR-010): Dado um diagnóstico com referência oficial disponível, quando seus detalhes forem consultados, então a referência é apresentada e o diagnóstico é identificado como complementar quando aplicável.
- `AC-010` (FR-011): Dado cada regra aceita, quando sua suíte de validação for executada, então existem exemplos positivos e negativos, plataformas suportadas e justificativa reproduzível; nenhum benchmark ou dado de runtime é exigido nesta fase estática.
- `AC-011` (FR-012, NFR-005): Dado um risco estático sem medição de execução, quando o diagnóstico for apresentado, então não há promessa de ganho percentual nem afirmação sobre frequência real.
- `AC-012` (NFR-001, NFR-002, NFR-003, NFR-004): Dado código válido nos runtimes suportados, quando a análise for executada, então ela preserva a compilação e a semântica das recomendações e permanece dentro do custo de execução aceitável definido pelos gates do projeto.
- `AC-013` (NFR-006, NFR-007, NFR-008): Dado um caso de código analisado, quando a regra for avaliada, então a evidência estática corresponde à categoria do risco e a mensagem explicita que o custo efetivo sob carga não foi medido.

## Riscos, dependências e perguntas abertas

- Risco: uma regra estrutural pode gerar falso positivo quando o analisador não consegue observar carga, frequência, ciclo de vida, contrato ou propriedade dos dados.
- Risco: recomendações como `Span<T>`, `ArrayPool<T>`, `ValueTask`, `Task.Run` ou estruturas concorrentes podem piorar o resultado em cenários diferentes; devem ser medidas e condicionadas ao contexto.
- Risco: o catálogo pode sobrepor regras oficiais ou entre si; a deduplicação deve preservar valor explicativo.
- Dependência: Roslyn compatível com .NET 10, .NET Standard 2.1 e C# 14.
- Dependência: configuração padrão de analyzers por `.editorconfig`, usando as convenções `dotnet_diagnostic.<ID>.severity` e categorias documentadas pela Microsoft.
- Dependência: orientações oficiais da Microsoft para desempenho de analyzers Roslyn, execução concorrente e análise em tempo de edição/build.
- Dependência: documentação oficial da Microsoft para regras de performance, Roslyn, ThreadPool starvation, Large Object Heap e high-performance logging.
- Dependência: gates de qualidade e testes estáticos do projeto ainda precisam ser configurados; benchmarks e profiling não fazem parte da fase atual.

## Rastreabilidade

- Fonte principal: `docs/csharp-performance-analyzer.md`.
- Referências técnicas citadas na fonte: Microsoft Performance Rules, Roslyn Analyzer e Code Fix, ThreadPool Starvation, Large Object Heap e High-performance Logging.
- Referências oficiais usadas na clarificação: [configuração de regras de análise .NET](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options), [customização de regras Roslyn](https://learn.microsoft.com/en-us/visualstudio/code-quality/use-roslyn-analyzers) e [execução concorrente de analyzers](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.diagnostics.analysiscontext.enableconcurrentexecution).

## Estado

- Estado da fase: `completed`.
- Pronto para planejamento: sim.
- As decisões registradas em `clarifications.md` definiram as plataformas, C# 14, a configuração compatível com as convenções Roslyn/Microsoft e a avaliação qualitativa de custo do analyzer nesta fase estática.
