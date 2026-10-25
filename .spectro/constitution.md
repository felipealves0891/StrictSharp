<!-- spectro-managed: true -->
# Constituição do projeto Spectro

Esta constituição consolida as decisões normativas de `docs/csharp-performance-analyzer.md`. O projeto é um analisador estático de C#/.NET, baseado em Roslyn, que complementa os analisadores oficiais e explica riscos de performance sem prometer resultados que dependam de medição em produção.

## Princípios arquiteturais

- A análise deve ser baseada em código-fonte e organizada por diagnóstico, mantendo um analisador por regra.
- A lógica de análise deve permanecer separada da integração com o analisador Roslyn; serviços de análise devem ser estáticos por padrão e usar instâncias somente quando necessário.
- O custo deve ser considerado em CPU, memória, concorrência, I/O, escalabilidade e latência.
- O analisador deve correlacionar operações, repetição, estruturas de dados e tempo de vida sempre que houver evidência estrutural suficiente.
- A solução deve complementar os analisadores oficiais do .NET, especialmente as regras `CA18xx`, sem duplicar diagnósticos quando não houver valor adicional.
- Os identificadores `PERF001` a `PERF030` são IDs estáveis do produto e não devem ser apresentados como IDs oficiais do .NET.
- A evolução deve ser incremental: V1 para diagnósticos objetivos, V2 para padrões dependentes de contexto e V3 para riscos de ciclo de vida e análise profunda.

## Padrões de código

- O código deve seguir C#/.NET e as convenções do projeto, evitando `else`, mantendo o fluxo próximo à margem e reutilizando lógica comum.
- Performance do próprio analisador é requisito: evitar alocações desnecessárias, preferir tipos por valor quando apropriado e não introduzir trabalho repetido na IDE ou no build.
- Recomendações devem preservar a semântica do código analisado e considerar mutabilidade, igualdade, propriedade, thread safety, ciclo de vida e contrato das APIs.
- Alternativas como `Span<T>`, `ArrayPool<T>`, `ValueTask`, `Task.Run`, tipos concretos e coleções concorrentes não devem ser tratadas como universalmente mais rápidas; seu uso exige contexto e medição representativa.
- Code Fix automático deve ser limitado a alterações locais cuja equivalência semântica possa ser demonstrada. Nos demais casos, deve ser fornecida orientação de refatoração condicional.

## Tratamento de erros e diagnósticos

- Cada diagnóstico deve apresentar identificador, título, severidade (`Critical`, `High`, `Medium` ou `Low`), categoria, evidência, impacto, escalabilidade, correção, exemplo, exceções, confiança e referência técnica.
- A severidade representa risco operacional sob condições plausíveis, não estimativa de ganho percentual.
- O diagnóstico deve distinguir custo comprovável no código de impacto potencial dependente de frequência, volume de dados, carga ou ambiente.
- Não emitir diagnóstico sem evidência suficiente nem quando uma exceção documentada puder ser demonstrada.
- Suspeitas de retenção ou vazamento devem ser apresentadas como suspeitas fundamentadas; o analisador não deve prometer detecção completa de memory leaks.
- Mensagens devem explicar o problema e a decisão recomendada sem exigir pesquisa externa para entendimento básico.
- Diagnósticos equivalentes aos oficiais devem ser deduplicados quando a regra do produto não acrescentar contexto ou valor.

## Convenções de testes

- Cada regra deve possuir exemplos positivos e negativos, incluindo as exceções relevantes.
- Os testes devem cobrir as versões de C# e Target Framework suportadas.
- Cada regra deve ter uma hipótese mensurável e uma justificativa reproduzível.
- Diagnósticos de maior impacto devem possuir benchmark comparativo com pelo menos um cenário representativo de regressão.
- A evidência deve corresponder à categoria: bytes alocados e GC/LOH para memória; CPU para computação; throughput, threads, filas e contenção para concorrência; latência e conexões para I/O; variação por tamanho de entrada para algoritmos; memória viva para retenção; TPS e p95/p99 para escalabilidade.
- A validação deve verificar baixo ruído, precisão, contexto, preservação semântica, compatibilidade, configurabilidade, deduplicação, rastreabilidade, transparência e custo do analyzer.

## Requisitos de segurança

- A análise não deve exigir nem enviar código-fonte, segredos, dados de produção ou informações sensíveis para serviços externos.
- O analisador deve tratar código, literais, logs e exemplos de teste como potencialmente sensíveis, evitando expô-los além do necessário na mensagem do diagnóstico.
- Recomendações de pooling, buffers e reutilização devem considerar conteúdo residual e limpeza quando houver possibilidade de dados sensíveis.
- O analisador deve considerar riscos operacionais de recursos, incluindo conexões, handles, buffers, retenção de objetos, ThreadPool starvation e saturação de serviços externos.
- Nenhuma otimização deve ser recomendada se puder alterar incorretamente controle de acesso, isolamento, propriedade de dados ou ciclo de vida de recursos.

## Regras de dependências

- Roslyn e C#/.NET são dependências fundamentais e devem permanecer compatíveis com as versões formalmente suportadas pelo projeto.
- Referências técnicas da Microsoft devem ser usadas para fundamentar regras relacionadas a performance, Roslyn, ThreadPool starvation, Large Object Heap e high-performance logging.
- Uma dependência nova deve ser justificada por necessidade funcional ou de qualidade, impacto de performance, compatibilidade e custo de manutenção.
- Não adicionar dependência para reproduzir uma regra já coberta pelos analisadores oficiais sem valor complementar demonstrável.
- APIs e estratégias sugeridas devem ser validadas no Target Framework aplicável antes de aparecerem como correção.

## Requisitos mínimos de qualidade

- O catálogo deve manter rastreabilidade entre cada regra, sua documentação, exemplos, testes e etapa de evolução.
- As regras devem permitir habilitação, desabilitação e ajuste de severidade por configuração.
- O analyzer não deve transformar micro-otimizações em erros sem evidência suficiente.
- Não apresentar ganhos percentuais, complexidade efetiva em produção ou frequência de execução sem medição correspondente.
- O custo da análise deve permanecer aceitável para IDE e build, evitando alocações e processamento desnecessários.
- V1 deve priorizar `PERF001`, `PERF008`, `PERF014`, `PERF025` e `PERF027`, sem impedir o rastreamento das demais regras `PERF001`–`PERF030` no roadmap.
- Alterações que ampliem o catálogo, mudem o contrato dos diagnósticos ou alterem a etapa de uma regra devem atualizar especificação, testes e evidências.

## Aprovação humana

Exigem decisão ou aprovação humana:

- versões suportadas de C#, .NET, Target Framework e Roslyn;
- inclusão, remoção, alteração de severidade ou mudança de etapa de uma regra;
- definição de critérios de baixo ruído, custo aceitável do analyzer e gates de benchmark;
- criação de Code Fix quando a alteração puder depender de contrato, mutabilidade, ciclo de vida, thread safety ou semântica não comprovada;
- mudanças de escopo que introduzam profiling, correlação com dados de runtime ou coleta de informações fora da análise estática;
- alterações que adicionem dependências externas ou mudem requisitos de segurança e privacidade.
