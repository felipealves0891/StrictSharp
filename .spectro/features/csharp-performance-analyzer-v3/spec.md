<!-- spectro-managed: true -->
# Especificação: Implementação V3 do C# Performance Analyzer

## Problema e objetivo

- Problema: as versões V1 e V2 cobrem padrões objetivos e antipatrones dependentes de contexto, mas ainda não identificam dois riscos relevantes de aplicações de longa duração: objetos que permanecem retidos além do necessário e recursos pesados invariantes reconstruídos repetidamente. Esses riscos dependem de relações de propriedade, ciclo de vida, alcance e repetição que não podem ser reduzidas a uma única chamada de API.
- Objetivo: implementar a etapa V3 do analisador estático, disponibilizando `PERF011` e `PERF030` com suspeitas fundamentadas, evidências observáveis, nível de confiança explícito e recomendações condicionais, sem prometer detecção completa de memory leaks nem ganho de performance medido.

## Usuários e contexto

- Desenvolvedores e revisores de código C# que trabalham com serviços de longa duração, Workers, APIs, filas e caminhos de inicialização ou processamento repetitivo.
- A análise deve indicar risco estrutural no código; a confirmação do impacto em memória viva, CPU, GC ou throughput depende de profiling ou benchmark representativo.

## Escopo

### Incluído

- Implementação dos dois diagnósticos V3 catalogados: `PERF011` e `PERF030`.
- Identificação de evidências de retenção indevida e de reconstrução repetida de objetos ou recursos pesados invariantes.
- Mensagens com impacto potencial, escalabilidade, correção condicional, exceções, confiança e limitações da análise estática.
- Integração com o catálogo estável `PERF001`–`PERF030`, documentação e configuração de severidade.
- Testes positivos, negativos e de exceção, incluindo propriedade, transferência de ciclo de vida, mutabilidade, thread safety e isolamento por operação quando relevantes.
- Justificativa reproduzível e, para diagnósticos de maior impacto, benchmark comparativo com cenário representativo de regressão.

### Excluído

- Implementação ou alteração funcional das regras V1 e V2, salvo ajustes de integração indispensáveis para manter o catálogo consistente.
- Prova completa de memory leak, análise de todos os caminhos dinâmicos, determinação de memória viva em produção ou garantia de ausência de retenção.
- Medição automática de bytes alocados, GC/LOH, CPU, throughput, latência, frequência de execução ou custo real em produção.
- Code Fix automático quando a correção depender de contrato, propriedade, mutabilidade, thread safety, ciclo de vida, estado variável ou isolamento.
- Definição antecipada de arquitetura interna, nomes de classes, divisão de serviços ou mecanismo específico de implementação.
- Profiling, telemetria, coleta de código-fonte ou correlação com dados externos de runtime.

## Requisitos

### Requisitos funcionais

- `FR-001`: O produto deve disponibilizar `PERF011` e `PERF030` como diagnósticos V3, com IDs estáveis, títulos, categorias e severidades do catálogo, sem apresentá-los como regras oficiais do .NET.
- `FR-002`: `PERF011` deve identificar situações em que um objeto ou recurso pode permanecer alcançável além do ciclo de vida necessário por uma referência, coleção, cache, delegate, evento ou outro vínculo estrutural observável.
- `FR-003`: `PERF011` deve emitir a suspeita somente quando houver evidência suficiente de retenção potencial e deve reduzir a confiança ou omitir o diagnóstico quando propriedade, transferência de ciclo de vida, escopo de cache ou descarte externo forem plausíveis e não demonstráveis.
- `FR-004`: `PERF011` deve orientar a revisão do ciclo de vida, remoção de referências, escopo limitado ou descarte apropriado conforme o caso, sem afirmar que encontrou um memory leak comprovado.
- `FR-005`: `PERF030` deve identificar criação repetida de objetos, recursos ou estruturas de inicialização custosa cuja configuração seja observavelmente invariável no contexto analisado, usando como evidência construtores complexos, quantidade relevante de propriedades/dados inicializados ou recriação com os mesmos valores.
- `FR-006`: `PERF030` deve considerar como exceções relevantes instâncias não thread-safe, estado variável, isolamento obrigatório por operação, propriedade transferida e casos em que a reutilização não seja demonstrável ou possa alterar a semântica.
- `FR-007`: `PERF030` deve recomendar criação única no escopo apropriado, cache limitado ou compartilhamento de instâncias imutáveis somente como orientação condicional, preservando o contrato do código analisado.
- `FR-008`: Cada diagnóstico V3 deve apresentar identificador, título, severidade, categoria, evidência, impacto, escalabilidade, correção, exemplo, exceções, confiança e referência técnica.
- `FR-009`: Cada diagnóstico V3 deve distinguir custo ou risco estrutural observável de impacto dependente de frequência, duração, volume, carga ou ambiente, sem informar percentual de ganho sem medição.
- `FR-010`: Cada diagnóstico V3 deve ser configurável individualmente para habilitação, desabilitação e severidade, mantendo o ID estável e evitando duplicação de regras oficiais sem valor adicional.
- `FR-011`: Cada diagnóstico V3 deve possuir documentação rastreável, exemplos positivos e negativos, casos de exceção, versões suportadas e uma hipótese mensurável de validação.

### Requisitos não funcionais

- `NFR-001`: A análise deve priorizar precisão e baixo ruído; não deve reportar retenção ou invariância apenas porque uma construção é possível.
- `NFR-002`: O custo do analyzer deve permanecer aceitável em IDE e build, evitando alocações desnecessárias, trabalho semântico repetido e análise de símbolos não relacionados ao diagnóstico.
- `NFR-003`: A implementação deve manter separadas a integração Roslyn e a lógica específica de análise, com um analisador por diagnóstico e serviços estáticos por padrão, conforme as convenções do projeto.
- `NFR-004`: As recomendações devem preservar semântica, propriedade, mutabilidade, thread safety, descarte, igualdade, estado e contrato das APIs; quando isso não puder ser provado, devem ser apresentadas como revisão manual.
- `NFR-005`: A V3 deve permanecer compatível com as versões de C#, .NET, Target Framework e Roslyn aprovadas pelo projeto.
- `NFR-006`: A validação deve usar evidência adequada à categoria: memória viva após períodos prolongados para retenção e alocações, GC/LOH e CPU quando aplicáveis à reconstrução; benchmarks comparativos devem ser representativos e reproduzíveis, sem criar limiar numérico adicional para o custo do analyzer além dos gates existentes.
- `NFR-007`: A análise não deve enviar código-fonte, segredos, dados de produção ou informações sensíveis para serviços externos.

### Restrições

- `CON-001`: Os IDs V3 são exatamente `PERF011` e `PERF030`.
- `CON-002`: A V3 deve complementar os analisadores oficiais e não duplicar diagnósticos `CA18xx` sem contexto ou valor explicativo adicional.
- `CON-003`: Severidade representa risco operacional plausível, não estimativa de ganho percentual; os valores permitidos são `Critical`, `High`, `Medium` e `Low`.
- `CON-004`: `PERF011` deve ser descrito como suspeita fundamentada e nunca como detecção completa ou prova de memory leak.
- `CON-005`: Code Fix automático não é obrigatório e não deve ser oferecido quando a equivalência semântica, propriedade ou ciclo de vida não puder ser demonstrada.
- `CON-006`: Alterações de severidade, plataformas, etapa do roadmap, escopo, dependências ou contrato público exigem aprovação humana conforme a constituição do projeto.
- `CON-007`: Dependências externas novas só podem ser adicionadas com justificativa funcional, de compatibilidade, performance e manutenção.

## Critérios de aceite

- `AC-001` (FR-001): Dado o catálogo carregado, quando os diagnósticos V3 forem consultados, então `PERF011` e `PERF030` aparecem uma única vez, com metadados consistentes e etapa V3, sem IDs oficiais do .NET.
- `AC-002` (FR-002, FR-003): Dado um objeto alcançável por vínculo estrutural que excede o escopo de vida esperado, quando a análise tiver evidência suficiente e nenhuma transferência ou administração externa demonstrável, então `PERF011` é emitido como suspeita; quando a propriedade externa ou o ciclo de vida válido forem demonstráveis, então não é emitido.
- `AC-003` (FR-004, CON-004): Dado `PERF011` emitido, quando seus detalhes forem consultados, então a mensagem identifica a referência e o risco de retenção, orienta revisão do ciclo de vida e declara que não prova um memory leak nem mede memória viva em produção.
- `AC-004` (FR-005, FR-006): Dado objeto ou recurso criado repetidamente, com construtor complexo, quantidade relevante de propriedades/dados inicializados ou os mesmos valores em cada criação, quando não houver estado variável, isolamento ou falta de thread safety que justifique a recriação, então `PERF030` é emitido; diante de qualquer exceção demonstrável, então não é emitido ou tem confiança reduzida.
- `AC-005` (FR-007): Dado `PERF030` emitido, quando a recomendação for apresentada, então ela sugere reutilização, cache limitado ou compartilhamento apenas de forma condicional e explicita os contratos que precisam ser verificados.
- `AC-006` (FR-008, FR-009): Dado qualquer diagnóstico V3 emitido, quando o desenvolvedor visualizar seus detalhes, então todos os campos obrigatórios estão presentes e a mensagem separa evidência estática, impacto potencial, escalabilidade, confiança, exceções e limitações de medição.
- `AC-007` (FR-010): Dada configuração que desabilite ou altere a severidade de apenas `PERF011` ou `PERF030`, quando a análise for executada, então somente a regra configurada é afetada e seus IDs permanecem estáveis.
- `AC-008` (FR-011): Dada a suíte V3, quando os testes e a documentação forem revisados, então cada regra possui exemplos positivos, negativos e de exceção, referência rastreável, versões suportadas e hipótese mensurável.
- `AC-009` (NFR-001, NFR-002, NFR-003, NFR-004, NFR-005): Dado código compatível com as plataformas aprovadas, quando a análise completa for executada nos gates do projeto, então ela compila, mantém o custo aceitável, respeita a separação de responsabilidades e não recomenda alteração semanticamente não comprovada.
- `AC-010` (NFR-006): Dado cenário representativo para cada regra, quando a validação for executada, então a evidência escolhida corresponde à categoria do risco e, para diagnósticos de maior impacto, existe benchmark comparativo com pelo menos uma regressão reproduzível.
- `AC-011` (NFR-007): Dado código analisado contendo literais, identificadores ou exemplos potencialmente sensíveis, quando testes e gates forem executados, então nenhum conteúdo é enviado a serviço externo e as mensagens expõem apenas o contexto necessário.

## Riscos, dependências e perguntas abertas

- Risco: a análise estática pode não provar o tempo de vida esperado, a propriedade de uma referência, a frequência real, o peso de uma inicialização ou a segurança de compartilhar uma instância.
- Risco: `PERF011` pode gerar falso positivo em caches intencionais, eventos, delegates, singletons, pools e componentes cuja propriedade foi transferida.
- Risco: `PERF030` pode recomendar compartilhamento incorreto quando houver estado oculto, falta de thread safety, mutabilidade, isolamento, configuração dependente da operação ou custo de sincronização; “construtor complexo” e “quantidade relevante de propriedades” devem ser usados como sinais conservadores, sem limiar numérico arbitrário.
- Risco: sobreposição com regras oficiais ou com `PERF007`, `PERF008`, `PERF021` e `PERF028` pode gerar diagnósticos duplicados; a regra deve acrescentar contexto de ciclo de vida ou invariância para permanecer válida.
- Dependência: catálogo, documentação e contratos existentes da feature `csharp-performance-analyzer`.
- Dependência: constituição e gates configurados em `.spectro/config.yaml`, incluindo cobertura, segurança, compatibilidade e custo do analyzer.
- Dependência: Roslyn e versões de C#/.NET aprovadas pelo projeto.
- Decisão aplicada: `PERF011` fica limitado a padrões estruturais explicitamente reconhecidos, com evidência comprovável, sem análise geral de todos os caminhos de alcance.
- Decisão aplicada: `PERF030` usa construtores complexos, quantidade relevante de propriedades/dados e recriação com os mesmos valores como sinais de custo/invariância, sem limiar numérico fixo; a seleção conservadora dos sinais será detalhada no planejamento.
- Decisão aplicada: a V3 exige exemplos positivos, negativos e de exceção, além de benchmark comparativo representativo para cada regra; o custo do analyzer segue os gates existentes.

## Rastreabilidade

- Especificação-mãe: `.spectro/features/csharp-performance-analyzer/spec.md`.
- Fonte funcional: `docs/csharp-performance-analyzer.md`, seções 3, 4, 6, 7, 8 e 10.
- Constituição: `.spectro/constitution.md`.
- Configuração de gates: `.spectro/config.yaml`.

## Estado

- Estado da fase: `clarified`.
- Pronto para esclarecimento: não.
- Pronto para planejamento: sim.
