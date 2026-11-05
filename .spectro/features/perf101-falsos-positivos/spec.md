<!-- spectro-managed: true -->
# Especificação: Redução de falsos positivos na PERF101

## Problema e objetivo

- Problema: a documentação `docs/perf101-falsos-positivos.md` registra falsos positivos na `PERF101`, especialmente quando uma classificação `O(n)` é tratada como warning embora o percurso linear seja necessário ou não exista uma alternativa de melhoria demonstrável. A complexidade observável e a oportunidade de otimização são conceitos distintos.
- Objetivo: reduzir diagnósticos indevidos da `PERF101`, mantendo a classificação estrutural quando sustentada pelo código, mas emitindo alerta somente quando houver evidência suficiente de uma possibilidade concreta de melhoria que preserve a semântica.

## Usuários e contexto

- Desenvolvedores e revisores de código C# que usam a `PERF101` para priorizar riscos de complexidade, sem transformar um custo linear necessário em uma recomendação de otimização sem fundamento.
- A regra continua sendo estática e local: não mede frequência real, tamanho efetivo das entradas, CPU ou ganho em produção.

## Decisão da avaliação

A classificação `O(n)` não é suficiente isoladamente para justificar um warning. O diagnóstico deve exigir um padrão repetido, uma chave estável, uma alternativa estrutural semanticamente compatível e uma vantagem demonstrável, como busca linear repetida com índice compatível. Quando essa oportunidade não puder ser demonstrada, a `PERF101` deve permanecer completamente silenciosa.

## Escopo

### Incluído

- Separar a classificação de complexidade observável da decisão de emitir um alerta acionável.
- Evitar warning para `O(n)` quando o percurso linear for necessário, quando não houver alternativa estrutural demonstrável ou quando a melhoria depender de premissas não resolvidas.
- Reconhecer oportunidades de melhoria somente quando a alternativa e sua vantagem estrutural puderem ser sustentadas pelo código analisado, sem prometer ganho percentual.
- Usar a classificação de `O(1)`, `O(n)`, `O(n log n)`, `O(n²)` ou desconhecida como evidência para um alerta somente quando houver oportunidade acionável; sem essa oportunidade, não publicar diagnóstico.
- Adicionar testes positivos, negativos e de exceção para classificação sem alerta, `O(n)` necessário, melhoria demonstrável e casos ambíguos.

### Excluído

- Profiling, benchmark, execução da aplicação, medição de CPU, frequência, tamanho real de entrada ou ganho percentual.
- Considerar todo `O(n)` um problema ou exigir que toda operação linear seja substituída por uma estrutura de acesso diferente.
- Inferir que `HashSet<T>`, `Dictionary<TKey,TValue>`, ordenação, cache ou qualquer alternativa é mais adequada sem evidência de equivalência semântica, mutabilidade, propriedade e custo de construção.
- Code Fix automático ou recomendação arquitetural ampla quando a equivalência da alternativa não puder ser demonstrada.
- Alterar IDs, severidade pública, etapa V5 ou outras regras `PERF101`–`PERF107`.

## Requisitos

### Requisitos funcionais

- `FR-001`: A `PERF101` deve distinguir a classificação de complexidade observável da decisão de emitir um warning acionável; conhecer `O(n)` não deve ser condição suficiente para alertar.
- `FR-002`: A `PERF101` deve omitir o warning quando o percurso linear for necessário para produzir o resultado, quando não houver alternativa estrutural demonstrável ou quando a suposta melhoria depender de comportamento não resolvido.
- `FR-003`: A `PERF101` deve emitir alerta somente quando identificar uma possibilidade de melhoria estrutural sustentada pelo código analisado, com vantagem de complexidade ou de trabalho estrutural explicável e sem equivalência semântica não demonstrada.
- `FR-004`: Quando emitir um alerta, a `PERF101` deve informar a classificação estrutural aplicável — incluindo `O(1)`, `O(n)`, `O(n log n)`, `O(n²)` ou desconhecida — sem convertê-la em medição ou promessa de resultado em produção; sem oportunidade demonstrável, não deve publicar a classificação como diagnóstico informativo.
- `FR-005`: A documentação e os testes devem manter rastreabilidade entre classificação, decisão de alerta e evidência da melhoria, cobrindo casos positivos, negativos e de exceção relacionados a falsos positivos.

### Requisitos não funcionais

- `NFR-001`: A regra deve priorizar precisão e baixo ruído; ausência de evidência suficiente deve resultar em silêncio, classificação desconhecida ou orientação explicitamente condicional, conforme o contrato aprovado.
- `NFR-002`: A análise deve ser determinística, local e compatível com IDE e build, sem profiling, serviços externos ou processamento desnecessário para casos sem oportunidade de melhoria.
- `NFR-003`: Recomendações devem preservar semântica, mutabilidade, propriedade, thread safety e custo de construção; hipóteses não demonstradas não podem ser apresentadas como correção certa.

### Restrições

- `CON-001`: O ID permanece `PERF101` e a regra continua pertencendo ao modelo de custo computacional V5; esta especificação trata de precisão do alerta, não da criação de um novo diagnóstico.
- `CON-002`: A complexidade apresentada é evidência estática, não prova de gargalo, frequência de execução, custo efetivo ou ganho percentual.
- `CON-003`: A implementação deve manter a lógica de análise separada da integração Roslyn, com um analisador por diagnóstico e serviços estáticos por padrão.
- `CON-004`: Não se deve recomendar uma alternativa quando a equivalência semântica, o contrato da API, a mutabilidade, a propriedade ou o custo adicional não forem demonstráveis no escopo analisado.

## Critérios de aceite

- `AC-001` (FR-001, FR-004): Dado um método que percorre uma coleção uma vez e cuja análise classifica o trabalho como `O(n)`, quando não houver alternativa de melhoria estrutural demonstrável, então a `PERF101` permanece completamente silenciosa e não publica nem warning nem classificação informativa.
- `AC-002` (FR-002): Dado um percurso linear necessário para inspecionar todos os elementos ou produzir o resultado, quando o método for analisado, então a `PERF101` não recomenda substituir o percurso apenas por ele ser `O(n)`.
- `AC-003` (FR-003, CON-004): Dado um padrão de busca linear repetida em uma coleção, com chave estável e alternativa estrutural semanticamente compatível cuja vantagem seja demonstrável no código analisado, quando a análise for executada, então o warning explica a oportunidade e a mudança estrutural esperada, sem afirmar percentual de ganho.
- `AC-004` (FR-003, NFR-003): Dado um caso em que uma alternativa como índice, conjunto, cache ou ordenação possa alterar semântica, propriedade, mutabilidade ou custo de construção, quando essa condição não puder ser resolvida, então nenhum alerta prescritivo é emitido como se a alternativa fosse comprovadamente melhor.
- `AC-005` (FR-004, CON-002): Dado código com complexidade classificável como `O(1)`, `O(n)`, `O(n log n)` ou `O(n²)`, quando também houver oportunidade de melhoria demonstrável, então o alerta informa a classificação e distingue evidência estrutural de medição de CPU, frequência, impacto em produção ou percentual de ganho; sem essa oportunidade, a saída permanece silenciosa.
- `AC-006` (FR-004, NFR-001): Dado limite dinâmico, dispatch, entrada ou comportamento não resolvido, quando a forma de crescimento não puder ser sustentada, então a classificação permanece desconhecida ou condicional e não é convertida em warning preciso por inferência arbitrária.
- `AC-007` (FR-005): Dada a suíte da `PERF101`, quando os testes forem revisados, então existem cenários para `O(n)` necessário sem warning, classificação sem melhoria demonstrável, melhoria estrutural demonstrável, semântica ambígua e complexidade desconhecida, todos rastreáveis a este escopo.
- `AC-008` (NFR-002, CON-003): Dado o mesmo código, configuração e conjunto de símbolos, quando a análise for executada repetidamente nos gates aprovados, então o resultado é determinístico, compila nas plataformas suportadas e não depende de profiling ou serviço externo.

## Riscos, dependências e perguntas abertas

- Risco: uma política excessivamente conservadora pode deixar de alertar oportunidades reais de melhoria; a perda de recall deve ser aceita enquanto a evidência necessária para reduzir o ruído não estiver definida.
- Risco: uma recomendação de estrutura de dados pode ignorar custo de construção, mutabilidade, ordem, duplicidade ou thread safety; esses fatores devem impedir certeza artificial.
- Dependência: contrato, catálogo e infraestrutura de diagnóstico da V5, incluindo a classificação atual da `PERF101` e seus testes.
- Dependência: `.spectro/constitution.md` e as versões aprovadas de C#, .NET, Target Framework e Roslyn.

## Rastreabilidade

- Fonte funcional: `docs/perf101-falsos-positivos.md`.
- Contexto relacionado: `docs/csharp-performance-analyzer-v5.md` e `.spectro/features/csharp-performance-analyzer-v5/spec.md`.
- Constituição: `.spectro/constitution.md`.
- Template: `.spectro/templates/specification.md`.

## Estado

- Estado da fase: `clarified`.
- Pronto para esclarecimento: não há ambiguidades bloqueantes ou major remanescentes.
- Pronto para planejamento: sim; o critério mínimo de evidência e o comportamento silencioso sem oportunidade demonstrável estão consolidados.
