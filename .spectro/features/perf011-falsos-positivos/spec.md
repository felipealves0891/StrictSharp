<!-- spectro-managed: true -->
# Especificação: Redução de falsos positivos na PERF011

## Problema e objetivo

- Problema: a documentação `docs/perf011-falsos-positivos.md` aponta que a `PERF011` pode interpretar como retenção problemática situações em que a instância tem vida curta, a coleção possui limite observável ou a propriedade/coleção não escapa para um owner de longa duração. A implementação atual também depende de sinais amplos — campo, chamada em loop e método com nome `Add`, `AddRange` ou `Subscribe` — e a revisão da V3 registrou que isso não constitui evidência suficiente de retenção.
- Objetivo: reduzir diagnósticos incorretos da `PERF011`, fazendo com que ela reporte somente suspeitas de retenção estrutural compatíveis com o objetivo da regra, preservando a distinção entre risco de retenção, custo de alocação e ausência de prova de memory leak.

## Decisão da avaliação

Os três grupos de casos documentados são problemas reais de precisão e devem ser tratados. A correção deve ser conservadora: quando o código demonstrar ciclo de vida delimitado, limite efetivo ou ownership que não escapa, a `PERF011` não deve classificar o caso como retenção além do necessário. Isso não impede que outra regra trate crescimento, alocação ou custo de execução quando houver requisito próprio e evidência suficiente.

## Escopo

### Incluído

- Avaliar o ciclo de vida da instância que mantém a referência e distinguir owner transitório de owner potencialmente duradouro quando isso for demonstrável estaticamente.
- Considerar o tipo declarado, a implementação semanticamente conhecida e limites observáveis da coleção, inclusive capacidade ou construção limitada quando a conclusão for segura.
- Considerar escape e transferência de ownership para singleton, cache, closure/delegate, evento, campo externo ou outro vínculo de longa duração reconhecível.
- Adicionar testes positivos, negativos e de exceção para os três grupos documentados, mantendo a mensagem como suspeita fundamentada.

### Excluído

- Prova completa de memory leak, análise exaustiva de reachability, profiling ou medição de memória viva em produção.
- Inferir que toda instância não-singleton é segura ou que toda coleção com capacidade inicial permanecerá limitada; conclusões devem depender de evidência observável.
- Transformar este escopo em uma regra geral de alocação, crescimento de coleções ou custo de `IEnumerable`.
- Code Fix automático quando a propriedade, o escape, a mutabilidade, o descarte ou a semântica do owner não puderem ser demonstrados.
- Alterar IDs, severidade pública, etapa do roadmap ou regras diferentes da `PERF011`.

## Requisitos

### Requisitos funcionais

- `FR-001`: A `PERF011` deve distinguir retenção potencial além do ciclo de vida necessário de mera alocação ou crescimento temporário e não deve reportar um owner transitório quando a instância e seus elementos forem demonstravelmente limitados ao escopo de uma operação curta, sem escape relevante.
- `FR-002`: A `PERF011` deve considerar o tipo declarado e a implementação conhecida de uma coleção ou propriedade; uma abstração como `IEnumerable<T>` não deve ser tratada como evidência suficiente, por si só, para concluir que existe retenção problemática.
- `FR-003`: A `PERF011` deve considerar limites efetivos observáveis da coleção e só omitir a suspeita por causa de um limite quando o código demonstrar que o limite é preservado no cenário analisado; uma capacidade inicial isolada não deve ser confundida com limite máximo.
- `FR-004`: A `PERF011` deve reconhecer ownership e escape demonstráveis para singleton, cache, closure/delegate, evento, campo externo ou estrutura equivalente, distinguindo transferência legítima de ownership de retenção acidental.
- `FR-005`: Quando a análise não puder provar ciclo de vida, limite, implementação ou escape, a `PERF011` deve reduzir a confiança ou omitir o diagnóstico conforme a evidência disponível, sem afirmar que encontrou um memory leak.
- `FR-006`: A documentação e os testes da `PERF011` devem registrar exemplos positivos, negativos e de exceção para instância curta, coleção limitada, abstração de coleção e ausência de escape, mantendo rastreabilidade com este escopo.

### Requisitos não funcionais

- `NFR-001`: A evolução deve priorizar precisão e baixo ruído, sem substituir a evidência estrutural por nomes de métodos ou pela simples presença de um campo e de um loop.
- `NFR-002`: A análise deve preservar a semântica do código e evitar recomendações que dependam de propriedade, mutabilidade, thread safety, descarte ou ciclo de vida não demonstrados.
- `NFR-003`: O custo adicional da análise deve permanecer aceitável em IDE e build, evitando análise interprocedural ou de reachability geral quando ela não for necessária para uma decisão segura.

### Restrições

- `CON-001`: O ID e o contrato geral da regra permanecem `PERF011`; ela continua sendo uma suspeita de retenção potencial, não uma prova de vazamento.
- `CON-002`: A implementação deve manter a lógica de análise separada da integração Roslyn, com serviços estáticos por padrão e um analisador por diagnóstico.
- `CON-003`: Toda exceção que dependa de contrato externo, ciclo de vida dinâmico ou comportamento não demonstrável deve resultar em orientação condicional ou silêncio, não em certeza artificial.
- `CON-004`: A correção não deve duplicar diagnósticos oficiais nem ampliar este escopo para regras de alocação ou crescimento sem especificação própria.

## Critérios de aceite

- `AC-001` (FR-001): Dado um campo de uma instância criada e descartada dentro de uma operação, sem escape para fora dela, quando uma coleção for alimentada em loop, então a `PERF011` não é emitida; dado um campo de owner potencialmente duradouro sem limpeza ou limite demonstrável, então a suspeita continua possível.
- `AC-002` (FR-002): Dada uma propriedade ou campo declarado como `IEnumerable<T>` cuja implementação concreta não permita concluir retenção, quando o código for analisado, então a abstração não gera diagnóstico apenas por seu nome ou por uma operação semanticamente não comprovada.
- `AC-003` (FR-003): Dada uma coleção cuja construção e uso preservem um limite efetivo observável, quando ela for alimentada repetidamente, então a `PERF011` não é emitida; dada apenas uma capacidade inicial sem garantia de limite, então o analyzer não assume que o crescimento está limitado.
- `AC-004` (FR-004): Dado um objeto ou coleção transferido para owner de longa duração de forma reconhecível, quando a referência for adicionada ou registrada, então a análise distingue a transferência de ownership de uma retenção acidental e não reporta uma ausência de limpeza que não seja demonstrável.
- `AC-005` (FR-005, CON-003): Dado um caso em que o ciclo de vida, limite ou escape permaneça ambíguo, quando a análise for executada, então ela reduz a confiança ou permanece silenciosa e nunca declara que um memory leak foi comprovado.
- `AC-006` (FR-006): Dada a suíte da regra, quando os testes forem revisados, então existem cenários positivos e negativos para cada grupo documentado, incluindo pelo menos owner transitório, coleção limitada, capacidade apenas inicial, tipo abstrato e escape para owner duradouro.
- `AC-007` (NFR-001, NFR-002): Dado código com método arbitrário chamado `Add` ou `Subscribe`, quando não houver tipo retentor e vínculo semântico reconhecidos, então nenhum diagnóstico é emitido e nenhuma recomendação de alteração semanticamente não comprovada é apresentada.
- `AC-008` (NFR-003): Dado um projeto compatível com as plataformas aprovadas, quando a análise completa for executada nos gates do projeto, então compila, mantém os testes existentes e não introduz trabalho geral de profiling ou reachability dinâmica.

## Riscos, dependências e perguntas abertas

- Risco: a análise estática pode não determinar se um owner transitório é realmente curto ou se uma coleção respeita um limite em todos os caminhos; nesses casos, o silêncio pode perder suspeitas reais.
- Risco: uma regra excessivamente conservadora pode deixar de alertar crescimento relevante que não seja retenção além do ciclo de vida; esse risco deve ser tratado por regra ou escopo separado.
- Dependência: comportamento atual de `Perf011Analyzer`, `RuleAnalysis.IsRetainingCollection`, testes de `PERF011` e decisões da especificação V3.
- Dependência: aprovação humana para alterar o contrato observável da regra, caso a implementação mude severidade, confiança ou a etapa do roadmap.
- Assunção: um limite conhecido apenas por convenção de uma API externa não é evidência suficiente para silenciar a `PERF011`; será necessária evidência no código analisado ou configuração/anotação explícita, caso esse mecanismo seja aprovado em escopo futuro.
