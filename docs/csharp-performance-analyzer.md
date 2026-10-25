# Especificação — C# Performance Analyzer

> DOCUMENTO DE REQUISITOS · VERSÃO 0.1

Análise estática de performance e anti-padões em C# 

- Roslyn
- C# / .NET
- Specification First

## 1. Visão do produto

O objetivo é desenvolver um analisador estático que identifique decisões de código capazes de causar degradação de performance em aplicações .NET.

O diferencial não deve ser simplesmente detectar práticas consideradas ruins, mas explicar o custo real de cada decisão, em quais condições esse custo se manifesta e como eliminá-lo.

A análise deve considerar seis dimensões:
- **CPU**: computação redundante, complexidade algorítmica e uso ineficiente de APIs.
- **Memória**: alocações evitáveis, retenção de objetos, pressão sobre GC, Gen 2 e LOH.
- **Concorrência**: contenção, bloqueios, paralelismo excessivo e ThreadPool starvation.
- **I/O**: operações bloqueantes, conexões e uso inadequado de recursos.
- **Escalabilidade**: padrões cujo custo cresce desproporcionalmente com o volume de trabalho.
- **Latência**: decisões que aumentam o tempo de resposta ou provocam pausas e picos de processamento.

A análise será baseada em código-fonte. Isso impõe uma restrição importante: **Roslyn consegue identificar riscos estruturais, mas não consegue determinar sozinho a frequência real de execução, a quantidade de dados ou o percentual de performance perdido em produção.**

*Por isso, cada diagnóstico deverá distinguir custo comprovável pelo código de impacto potencial dependente da carga de trabalho.*

## 2. Referência na documentação oficial
A documentação da Microsoft confirma a existência de analisadores Roslyn integrados ao SDK .NET, inclusive regras de performance `CA18xx`, com avisos, sugestões e correções de código. 

Isso determina uma decisão de escopo: o novo produto deve complementar os analisadores oficiais, não apenas reproduzi-los. O maior valor estará nas regras que relacionam operações, estruturas de dados, tempo de vida e repetição, algo que verificações isoladas de APIs nem sempre capturam.

A especificação seguinte utiliza identificadores propostos `PERF001` a `PERF030`. Eles não são IDs oficiais do .NET.

## 3. Contrato dos diagnósticos
Cada ocorrência deve gerar um diagnóstico com informação suficiente para que o desenvolvedor tome uma decisão sem precisar pesquisar o problema.

--- 

PERF001 — Bloqueio síncrono de operação assíncrona
`Critical`

<small>Categoria: Concorrência · Confiança: Alta</small>

> Problema: Task.Result pode bloquear uma thread enquanto a operação assíncrona está pendente.

- **Impacto**: consumo desnecessário de threads, redução de throughput e risco de ThreadPool starvation sob carga.

- **Correção**: utilizar await ao longo do fluxo assíncrono, preservando APIs genuinamente síncronas quando necessário.

- **Condição**: não reportar como bloqueio quando for possível demonstrar que a tarefa já foi concluída.

---

Os campos obrigatórios de qualquer diagnóstico serão:

| Campo | Requisito |
| --- | --- |
|Identificador |	Código estável e único |
|Título |	Descrição clara do problema |
|Severidade |	Critical, High, Medium ou Low |
|Categoria |	Dimensão de performance afetada |
|Evidência |	Expressão ou padrão encontrado |
|Impacto |	Qual recurso é consumido e por quê |
|Escalabilidade |	Como o custo evolui com a carga |
|Correção |	Alternativa recomendada |
|Exemplo |	Código problemático e código corrigido |
|Exceções |	Quando não emitir o diagnóstico |
|Confiança |	Alta, média ou baixa |
|Referência |	Documentação e regra oficial relacionada |

A severidade será uma classificação de risco, não uma estimativa de ganho percentual. Critical não significa que uma linha isolada necessariamente destrói a performance; significa que o padrão pode causar um problema operacional grave sob condições plausíveis.

## 4. Catálogo de regras

A primeira versão da especificação contempla 30 diagnósticos, divididos em seis categorias.

<small>Os exemplos são ilustrativos. A classificação de impacto considera especialmente aplicações de longa duração, processamento de filas, Workers, APIs e bibliotecas usadas em caminhos críticos.</small>

### A. Concorrência e operações assíncronas

Esses padrões têm potencial para afetar a aplicação inteira, mesmo quando aparecem em poucos pontos do código. A documentação oficial associa bloqueios síncronos sobre operações assíncronas à redução de escalabilidade e ao esgotamento de threads disponíveis. 

#### PERF001 — Sync-over-async
---
> **Critical**

Identificar .Result, .Wait() e .GetAwaiter().GetResult() em tarefas potencialmente incompletas.

**Impacto**: bloqueia threads que poderiam executar outros trabalhos; sob concorrência elevada, pode reduzir drasticamente throughput e aumentar latência.

**Correção**: usar await e propagar assincronismo. Não recomendar Task.Run(...).Result como solução genérica.

**Exceção**: tarefa comprovadamente concluída ou fronteira síncrona intencional.

#### PERF002 — Paralelismo sem limite
---
> **Critical**

Identificar criação não limitada de tarefas concorrentes, especialmente Task.WhenAll aplicado sobre coleções de tamanho desconhecido.

**Impacto**: aumento simultâneo de operações, memória retida, contenção por recursos e possível saturação de serviços externos.

**Correção**: concorrência limitada com Parallel.ForEachAsync, SemaphoreSlim ou processamento em lotes, conforme a carga.

**Exceção**: coleções comprovadamente pequenas ou limites garantidos por outro componente.


#### PERF003 — Task.Run desnecessário em operações de I/O
---
> **High**

Identificar métodos que apenas encapsulam chamadas assíncronas de I/O em Task.Run.

**Impacto**: agendamento extra no ThreadPool, overhead e possíveis filas de trabalho sem benefício de paralelismo.

**Correção**: aguardar diretamente a operação assíncrona.

**Exceção**: isolamento explícito de API síncrona bloqueante ou processamento CPU-bound.

#### PERF004 — Lock durante operação longa ou bloqueante
---
> **Critical**

Identificar exclusão mútua envolvendo I/O, Sleep, esperas de tarefas, consultas remotas ou processamento potencialmente demorado.

**Impacto**: serialização de operações, contenção e crescimento das filas de espera.

**Correção**: reduzir a região crítica, separar I/O do estado compartilhado e usar mecanismos assíncronos quando aplicáveis.

**Exceção**: operação curta com sincronização necessária e custo delimitado.

#### PERF005 — Espera ativa ou polling excessivo
---
> **High**

Identificar loops de consulta sem espera adequada, ou intervalos muito curtos associados a operações repetitivas.

**Impacto**: consumo contínuo de CPU, chamadas redundantes e pior eficiência por unidade de trabalho.

**Correção**: adotar sinalização, filas, Channel<T>, mecanismos orientados a eventos ou intervalos ajustados à necessidade de latência.

**Exceção**: spin-wait deliberado, extremamente curto e justificado por baixa latência.


#### PERF006 — Criação excessiva de threads ou tarefas
---
> **High**

Identificar new Thread ou Task.Run em loops, processamento por item ou execuções de alta frequência.

**Impacto**: custo de agendamento, crescimento do número de threads, troca de contexto e sobrecarga de coordenação.

**Correção**: reutilizar workers, limitar concorrência e escolher entre operações assíncronas e execução paralela conforme a natureza da carga.

**Exceção**: threads dedicadas de longa duração e tarefas pontuais justificadas.


### B. Memória, alocação e Garbage Collector

O foco não deve ser condenar toda alocação, mas identificar alocações desnecessárias, repetidas ou retidas por tempo excessivo.

No .NET, objetos grandes podem ser direcionados ao LOH, e a atividade nessa área pode contribuir para coletas de Gen 2. Isso torna particularmente relevantes grandes arrays temporários e recriações frequentes de buffers. 

#### PERF007 — Alocação recorrente dentro de loops
---
> **High**

Detectar arrays, listas, buffers e outros objetos temporários criados repetidamente em loops.

**Impacto**: taxa elevada de alocação, maior frequência de GC e possível queda de throughput.

**Correção**: remover alocações desnecessárias, reutilizar recursos quando seguro ou empregar pools apropriados.

**Exceção**: objetos pequenos, necessários e de vida curta cujo reuso introduziria complexidade desproporcional.


#### PERF008 — Alocações grandes recorrentes
---
> **Critical**

Identificar buffers e arrays de tamanho estático grande, ou potencialmente grande, alocados em caminhos repetitivos.

**Impacto**: custo de inicialização de memória, pressão sobre LOH e potencial aumento de coletas Gen 2.

**Correção**: processamento segmentado, streaming, dimensionamento real de buffers ou ArrayPool<T> quando apropriado.

**Exceção**: alocação única, controlada ou sem recorrência.


#### PERF009 — Boxing evitável
---

> **Medium**
Identificar conversões implícitas ou explícitas de value types em referências quando evitáveis, como em APIs que recebem object.

**Impacto**: alocações adicionais e custo de conversão; relevante em caminhos com muitas operações pequenas.

**Correção**: utilizar APIs genéricas e tipadas, evitando boxing desnecessário.

**Exceção**: boxing exigido pelo contrato ou eliminado por especializações internas comprovadas.


#### PERF010 — Capturas de variáveis e closures desnecessárias
---

> **Medium**

Detectar delegates e lambdas repetidamente criados que capturam estado externo.

**Impacto**: alocações de closures/delegates e possível retenção de referências.

**Correção**: preferir lambdas static, reutilização de delegates ou passagem explícita de estado quando a API permitir.

**Exceção**: closure não alocada no cenário efetivo ou execução pouco frequente.


#### PERF011 — Retenção indevida de objetos
---

> **Critical**

Identificar padrões de retenção potencialmente ilimitada em coleções estáticas, caches sem política de remoção, assinaturas de eventos e timers.

**Impacto**: crescimento de memória viva, maior custo de marcação do GC, Gen 2 e possível esgotamento de memória.

**Correção**: limitar tamanho e tempo de retenção, implementar expiração e liberar assinaturas ou recursos.

**Exceção**: retenção explicitamente delimitada ou necessária durante toda a vida da aplicação.

Observação: muitos vazamentos dependem do fluxo de vida real e só podem ser reportados como suspeitas.


#### PERF012 — Cópias desnecessárias de coleções e buffers
---
> **High**

Detectar materializações redundantes com ToArray, ToList, Clone e operações equivalentes.

**Impacto**: custo linear em CPU, memória temporária e cópia de dados. Para coleções grandes, pode pressionar o LOH.

**Correção**: utilizar diretamente a coleção original, ReadOnlySpan<T>, Memory<T> ou outra representação não proprietária, quando compatível.

**Exceção**: snapshot, isolamento de mutabilidade ou transferência de propriedade necessária.

### C. LINQ e complexidade algorítmica

Essa categoria tem um dos melhores potenciais para produzir diagnósticos objetivos e correções úteis.

#### PERF013 — Enumeração repetida de IEnumerable
---
> **High**

Identificar sequências enumeradas múltiplas vezes, especialmente quando possuem execução adiada.

**Impacto**: processamento repetido, possíveis consultas repetidas e multiplicação do custo de CPU/I/O.

**Correção**: materializar uma vez quando necessário, reutilizar o resultado ou utilizar uma coleção já materializada.

**Exceção**: sequência reutilizável, barata e comprovadamente intencional.

Referência: CA1851.


#### PERF014 — Busca linear repetida em coleção
---
> **Critical**

Identificar List.Contains, FirstOrDefault ou buscas equivalentes dentro de loops sobre outras coleções.

**Impacto**: complexidade potencialmente quadrática O(n × m) em vez de buscas próximas de O(1) médio com estruturas baseadas em hash.

**Correção**: utilizar HashSet<T> ou Dictionary<TKey,TValue> para múltiplas consultas, quando semanticamente adequado.

**Exceção**: conjuntos pequenos, busca única ou custo de construção do índice superior à economia.


#### PERF015 — Uso inadequado de Count, Any e propriedades de coleção
---

> **Medium**
Identificar chamadas LINQ desnecessárias quando uma propriedade ou operação direta equivalente está disponível.

**Impacto**: overhead de invocação, abstração e eventual enumeração desnecessária.

**Correção**: preferir Count, Length, IsEmpty ou APIs diretas adequadas ao tipo.

**Exceção**: coleções sem acesso eficiente à contagem ou casos nos quais Any(predicate) realmente procura uma condição.

Referências: CA1827, CA1829 e CA1860.


#### PERF016 — Ordenação repetida ou desnecessária
---
> **High**

Identificar OrderBy, Sort e materializações ordenadas repetidas sem alteração dos dados, ou ordenações completas quando somente poucos elementos são necessários.

**Impacto**: custo típico O(n log n) por ordenação, além de possíveis buffers temporários.

**Correção**: ordenar uma vez, reutilizar o resultado ou aplicar algoritmos de seleção quando apropriado.

**Exceção**: ordenação obrigatória após mutação dos dados.


#### PERF017 — Consultas LINQ repetidas dentro de loops
---

> **High**

Detectar pipelines LINQ com filtros, projeções ou agregações repetidos em cada iteração quando os dados relevantes não mudam.

**Impacto**: recalcula operações redundantes, pode criar iteradores/delegates e ampliar a complexidade total.

**Correção**: antecipar o cálculo invariável ou criar um índice reutilizável.

**Exceção**: consulta dependente da iteração ou sequência com efeitos colaterais relevantes.

### D. Strings, parsing e serialização

#### PERF018 — Concatenação repetitiva de strings
---
> **High**

Identificar acumulação com += ou concatenação equivalente dentro de loops.

**Impacto**: alocações e cópias repetidas; para concatenações crescentes, o volume total de caracteres copiados pode se aproximar de O(n²).

**Correção**: utilizar StringBuilder quando houver construção incremental significativa, ou juntar fragmentos já disponíveis com string.Join.

**Exceção**: concatenação pequena, delimitada e otimizada pelo compilador.


#### PERF019 — Substrings e conversões de texto desnecessárias
---

> **Medium**

Detectar recortes de strings e conversões intermediárias utilizadas apenas para inspeção ou parsing.

**Impacto**: criação de novas strings, cópias e pressão de GC.

**Correção**: utilizar APIs compatíveis com ReadOnlySpan<char>, AsSpan ou parsing sem materialização quando possível.

**Exceção**: necessidade de armazenar, transferir ou manter o resultado independentemente da origem.


#### PERF020 — Normalização de strings para comparação
---

> **Medium**

Identificar ToLower() e ToUpper() utilizados exclusivamente para realizar comparações.

**Impacto**: alocações evitáveis e processamento extra de caracteres.

**Correção**: usar overloads que aceitam StringComparison, respeitando a semântica cultural desejada.

**Exceção**: normalização exigida para persistência ou utilização posterior.

Referência: CA1862.


#### PERF021 — Regex ou metadados de serialização recriados
---

> **High**

Detectar construção repetitiva de Regex, opções de serialização ou configurações pesadas imutáveis.

**Impacto**: custo repetido de inicialização, parsing, compilação ou preparação de metadados.

**Correção**: reutilizar instâncias adequadas, caches ou [GeneratedRegex] quando suportado.

**Exceção**: configuração dinâmica que exige instâncias distintas, ou APIs que já realizam cache suficiente.


A Microsoft recomenda regex gerado em código quando aplicável, devido às vantagens potenciais em inicialização e execução. 

### E. Coleções, acesso a dados e estruturas

#### PERF022 — Consulta redundante em Dictionary
---

> **Medium**

Identificar ContainsKey seguido de leitura pelo indexador, ou ContainsKey antes de Remove.

**Impacto**: pesquisas duplicadas, cálculo de hash e comparações evitáveis.

**Correção**: TryGetValue ou Remove diretamente, quando a semântica for equivalente.

**Exceção**: operações com contratos diferentes ou necessidade explícita de verificar condições independentes.

Referências: CA1853 e CA1854.


#### PERF023 — Coleção inadequada para padrão de acesso
---

> **High**

Identificar padrões repetitivos de remoção no início de List<T>, buscas lineares massivas, inserções deslocando muitos elementos ou estruturas que exigem reorganizações frequentes.

**Impacto**: cópias de elementos, deslocamentos de memória e complexidade desnecessariamente elevada.

**Correção**: avaliar Queue<T>, LinkedList<T>, HashSet<T>, Dictionary<TKey,TValue> ou estruturas especializadas segundo a operação dominante.

**Exceção**: coleções pequenas ou operações cuja localidade de memória torne a alternativa original mais eficiente.


#### PERF024 — Crescimento repetido de coleções
---

> **Medium**

Identificar coleções montadas com múltiplas inserções quando uma capacidade inicial relevante é conhecida.

**Impacto**: realocações e cópias de buffers internos durante o crescimento.

**Correção**: informar capacidade antecipadamente ou usar construtores com tamanho conhecido.

**Exceção**: quando a capacidade não é conhecida ou já existe estratégia adequada de crescimento.


### F. I/O, recursos e observabilidade

#### PERF025 — Uso inadequado de HttpClient
---

> **Critical**

Detectar criação e descarte frequentes de HttpClient e seus handlers por operação.

**Impacto**: criação de conexões desnecessárias, perda de pooling, overhead de estabelecimento de conexões e eventual exaustão de portas.

**Correção**: reutilizar clientes com política apropriada de conexões, ou utilizar IHttpClientFactory.

**Exceção**: uso de factory ou reutilização comprovada do handler subjacente.


#### PERF026 — I/O síncrono em fluxo assíncrono
---

> **High**

Identificar operações síncronas bloqueantes de rede, arquivos e streams em caminhos assíncronos.

**Impacto**: threads ocupadas aguardando I/O, reduzindo a capacidade de atender operações concorrentes.

**Correção**: utilizar APIs assíncronas quando houver benefícios de escalabilidade.

**Exceção**: operações muito curtas, APIs sem alternativas assíncronas ou código genuinamente síncrono.


#### PERF027 — Logging com custo evitável
---

> **High**

Identificar interpolação, concatenação e serialização de objetos para mensagens de log, especialmente quando não há verificação do nível habilitado.

**Impacto**: alocação, formatação e serialização executadas mesmo quando a mensagem é descartada.

**Correção**: logging estruturado, geração de código com LoggerMessage e avaliação condicional para operações caras.

**Exceção**: mensagem pré-construída, valor já disponível ou cenário de baixa frequência.

Referência: CA1848.


#### PERF028 — Recursos IDisposable não liberados
---

> **High**

Identificar recursos com propriedade e ciclo de vida conhecidos que não possuem liberação adequada.

**Impacto**: retenção de handles, conexões, buffers e outros recursos gerenciados ou nativos.

**Correção**: utilizar using, await using ou descarte controlado pelo ciclo de vida do componente.

**Exceção**: recurso cuja propriedade foi transferida ou cujo ciclo de vida é administrado externamente.


#### PERF029 — Tratamento de exceções como fluxo normal
---

> **High**

Detectar uso recorrente de exceções para controle esperado de execução, como conversões com Parse e captura repetida de FormatException.

**Impacto**: criação de exceções, construção de stack traces e custo significativo quando falhas são frequentes.

**Correção**: usar TryParse, TryGetValue e APIs que representam falhas esperadas por resultados.

**Exceção**: falha verdadeiramente excepcional, rara ou dependente do contrato da API.


#### PERF030 — Reconstrução de objetos pesados invariantes
---

> **High**

Identificar criação repetida de recursos cuja configuração não muda, como metadados, descritores, estruturas de lookup ou objetos de inicialização custosa.

**Impacto**: CPU de inicialização, alocações e pressão sobre GC.

**Correção**: criar uma vez no escopo apropriado, aplicar cache limitado ou compartilhar instâncias imutáveis.

**Exceção**: instância não thread-safe, estado variável ou isolamento obrigatório por operação.

O diagnóstico `PERF025` segue as recomendações oficiais de gerenciamento de conexões HTTP. O `PERF027` é apoiado pela recomendação de source-generated logging, que reduz boxing, parsing de templates e alocações. 

##  5. Exemplos de comportamento esperado

Os exemplos abaixo fazem parte dos critérios de aceitação das regras. São exemplos de código da aplicação inspecionada, não de implementação do analisador.

### Exemplo 1 — Processamento assíncrono bloqueante
<small>PERF001 · Critical</small>

#### Código problemático

```csharp
public Workflow Load(int id)
{
    return repository.GetAsync(id).Result;
}
```

#### Código recomendado

```csharp
public async Task<Workflow> LoadAsync(int id)
{
    return await repository.GetAsync(id);
}
```

**Explicação esperada**: uma thread fica bloqueada até a conclusão da operação. Sob alta concorrência, o número de threads bloqueadas pode aumentar, diminuindo a capacidade do processo de executar outros trabalhos.

### Exemplo 2 — Complexidade quadrática

<small>PERF014 · Critical</small>

#### Código problemático

```csharp
foreach (var item in items)
{
    if (processedIds.Contains(item.Id))
        Process(item);
}
```

Considerando processedIds como List<int>, o custo potencial de comparação cresce com o produto dos tamanhos das coleções.

#### Código recomendado

```csharp
var processedSet = processedIds.ToHashSet();

foreach (var item in items)
{
    if (processedSet.Contains(item.Id))
        Process(item);
}
```

**Explicação esperada**: em uma lista, a busca exige percorrer elementos até encontrar o valor. Com HashSet<T>, a consulta normalmente possui complexidade média constante, após o custo inicial de construção do conjunto.

A recomendação deve destacar que a melhoria depende do número de consultas, do tamanho das coleções e da semântica de igualdade.

### Exemplo 3 — Pressão sobre GC e LOH

<small>PERF008 · Critical</small>

#### Código problemático

```csharp
foreach (var message in messages)
{
    byte[] buffer = new byte[100_000];
    Process(message, buffer);
}
```

#### Código recomendado — quando o buffer realmente precisa desse tamanho

```csharp
byte[] buffer = ArrayPool<byte>.Shared.Rent(100_000);

try
{
    foreach (var message in messages)
        Process(message, buffer);
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}
```

**Explicação esperada**: cada iteração cria um array de aproximadamente 100 KB, normalmente grande o suficiente para o LOH. O aluguel e a reutilização podem reduzir o volume de alocações.

Condições da correção: o processamento deve ser síncrono, não pode reter o buffer e não pode executar simultaneamente usando a mesma instância. Deve-se tratar também conteúdo residual e limpeza quando houver dados sensíveis. Caso a função retenha o buffer, a correção apresentada não é segura.

### Exemplo 4 — Logging com custo mesmo quando desabilitado

<small>PERF027 · High</small>

#### Código problemático

```csharp
logger.LogDebug(
    $"Workflow: {JsonSerializer.Serialize(workflow)}");


#### Código recomendado
if (logger.IsEnabled(LogLevel.Debug))
{
    logger.LogDebug(
        "Workflow: {Workflow}",
        JsonSerializer.Serialize(workflow));
}
```

A correção evita serializar quando o nível está desabilitado. Ainda assim, a serialização é executada quando Debug está ativo. Para logging frequente, o diagnóstico pode recomendar templates estruturados e LoggerMessage, conforme o conteúdo necessário.

### Exemplo 5 — Paralelismo não controlado

<small>PERF002 · Critical</small>

#### Código problemático

```csharp
await Task.WhenAll(
    messages.Select(ProcessAsync));
```

#### Código recomendado

```csharp
await Parallel.ForEachAsync(
    messages,
    new ParallelOptions
    {
        MaxDegreeOfParallelism = 8
    },
    async (message, ct) =>
    {
        await ProcessAsync(message, ct);
    });
```

**Explicação esperada**: a versão original inicia uma tarefa de processamento para cada mensagem enumerada, sem impor um limite explícito de concorrência.

A segunda versão limita o número de operações simultâneas. O valor `8` é ilustrativo: deverá ser definido por benchmark e pela capacidade dos recursos internos e externos. Se a fonte também precisar de backpressure, será necessário controlar a entrada de mensagens.

## 6. Requisitos de qualidade dos diagnósticos

O produto não deve transformar qualquer micro-otimização em um erro.

| Requisito |	Comportamento esperado |
| --- | --- |
| Q01 — Baixo ruído |	Não reportar um problema sem evidência suficiente|
| Q02 — Precisão |	Identificar custo real ou risco fundamentado|
| Q03 — Contexto |	Distinguir loops, caminhos críticos, inicialização e código frio|
| Q04 — Semântica |	Não propor uma correção que altere o resultado do programa|
| Q05 — Compatibilidade |	Respeitar a versão de C# e o Target Framework|
| Q06 — Configurabilidade |	Permitir habilitar, desabilitar e ajustar severidade por regra| 
| Q07 — Deduplicação |	Não repetir diagnósticos de CA18xx sem acrescentar valor |
| Q08 — Rastreabilidade |	Cada regra deve apontar sua documentação de referência |
| Q09 — Transparência |	Não apresentar ganhos percentuais sem medição |
| Q10 — Custo do analyzer |	Evitar que o próprio analisador torne a IDE ou o build demasiadamente lentos |

Uma distinção importante: nem toda correção será automatizável. Trocar `ContainsKey` por `TryGetValue` geralmente é uma alteração localizada; mudar a estratégia de concorrência de um Worker exige conhecimento do contrato de execução da aplicação.

Para essas situações, o produto poderá apresentar uma orientação de refatoração em vez de um Code Fix automático.

## 7. Priorização por impacto e dificuldade

A classificação abaixo é uma estimativa de engenharia, não um benchmark. O impacto representa o potencial de degradação quando o padrão ocorre em um caminho executado frequentemente. A dificuldade representa a complexidade esperada para produzir um diagnóstico confiável, incluindo controle de falsos positivos, e não apenas reconhecer uma expressão.

Escalas:
- Impacto: 1 (muito baixo) a 5 (crítico).
- Dificuldade: 1 (trivial) a 5 (muito complexa).
- Prioridade: relação entre impacto, dificuldade e confiança esperada.

| Regra	| Impacto | Dific. | Etapa
| --- | --- | --- | --- |
| PERF001 Sync-over-async	| 5/5	| 2/5	| V1 |
| PERF008 Alocações grandes recorrentes	| 5/5	| 3/5	| V1 |
| PERF014 Busca linear repetitiva	| 5/5	| 3/5	| V1 |
| PERF025 Uso inadequado de HttpClient	| 5/5	| 3/5	| V1 |
| PERF002 Paralelismo sem limite	| 5/5	| 4/5	| V2 |
| PERF004 Lock com operação bloqueante	| 5/5	| 4/5	| V2 |
| PERF011 Retenção indevida de objetos	| 5/5	| 5/5	| V3 |
| PERF012 Cópias redundantes de coleções	| 4/5	| 2/5	| V1 |
| PERF018 Concatenação de strings em loops	| 4/5	| 2/5	| V1 |
| PERF027 Logging custoso	| 4/5	| 2/5	| V1 |
| PERF005 Polling excessivo	| 4/5	| 3/5	| V2 |
| PERF007 Alocação recorrente em loops	| 4/5	| 3/5	| V1 |
| PERF013 Enumeração repetida	| 4/5	| 3/5	| V1 |
| PERF016 Ordenação repetida	| 4/5	| 3/5	| V2 |
| PERF021 Regex ou metadados recriados	| 4/5	| 3/5	| V2 |
| PERF026 I/O síncrono em fluxo assíncrono	| 4/5	| 3/5	| V1 |
| PERF029 Exceções como fluxo normal	| 4/5	| 3/5	| V2 |
| PERF006 Criação excessiva de tarefas/threads	| 4/5	| 4/5	| V2 |
| PERF017 LINQ repetitivo em loops	| 4/5	| 4/5	| V2 |
| PERF023 Coleção inadequada	| 4/5	| 4/5	| V2 |
| PERF028 Recursos não liberados	| 4/5	| 4/5	| V2 |
| PERF030 Objetos pesados recriados	| 4/5	| 4/5	| V3 |
| PERF020 Normalização para comparação	| 3/5	| 1/5	| V1 |
| PERF022 Buscas duplicadas em Dictionary	| 3/5	| 1/5	| V1 |
| PERF003 Task.Run desnecessário	| 3/5	| 2/5	| V1 |
| PERF019 Substrings desnecessárias	| 3/5	| 2/5	| V1 |
| PERF009 Boxing evitável	| 3/5	| 3/5	| V2 |
| PERF010 Closures desnecessárias	| 3/5	| 3/5	| V2 |
| PERF024 Crescimento de coleções	| 3/5	| 3/5	| V2 |
| PERF015 Count/Any inadequados	| 2/5	| 1/5	| V1 |


## 8. Estratégia de evolução

Eu dividiria a entrega em três versões, evitando iniciar pelas regras mais complexas.

- V1 — Diagnósticos objetivos

    <small>15 regras · Foco em precisão e valor imediato</small>

    Inclui bloqueio assíncrono, alocações em loops, buscas ineficientes, concatenação, logging, LINQ básico, HttpClient e conversões desnecessárias.

    **Meta de aceitação**: erros bem explicados, poucos falsos positivos e correções locais seguras onde possível.

- V2 — Antipatrones dependentes de contexto

    <small>13 regras · Foco em complexidade e escalabilidade</small>

    Inclui concorrência excessiva, contenção de locks, estratégias de coleções, cálculos repetidos e problemas de gerenciamento de recursos.

    **Meta de aceitação**: reconhecer padrões compostos e apresentar recomendações condicionais sem assumir que toda substituição melhora a performance.

- V3 — Riscos de ciclo de vida e análise profunda

    <small>2 regras · Foco em retenção e reconstrução de estado</small>

    Inclui retenção indevida de objetos e recriação de recursos pesados invariantes.

    **Meta de aceitação**: reportar suspeitas fundamentadas, com evidências e nível de confiança explícitos. Não prometer detecção completa de memory leaks.

## 9. O principal diferencial

Os analisadores oficiais já cobrem muitas otimizações pontuais. Criar outra regra dizendo que `ToLower()` seguido de comparação aloca memória tem algum valor, mas não seria o motivo principal para usar uma nova ferramenta.

O maior diferencial seria correlacionar custo, frequência e escala.

Considere:

```csharp
foreach (var workflow in workflows)
{
    var steps = allSteps
        .Where(x => x.WorkflowId == workflow.Id)
        .ToList();

    Execute(workflow, steps);
}
```

O código não tem necessariamente uma única operação absurda. O problema é o conjunto:
1. A cada workflow, percorre potencialmente todos os steps.
2. Executa filtragem repetitiva.
3. Cria uma nova lista a cada iteração.
4. O custo cresce com a quantidade de workflows e steps.

Uma recomendação estrutural seria avaliar a indexação prévia dos steps:

```csharp
var stepsByWorkflow = allSteps
    .ToLookup(x => x.WorkflowId);

foreach (var workflow in workflows)
{
    Execute(
        workflow,
        stepsByWorkflow[workflow.Id]);
}
```

Essa versão elimina buscas repetidas, em troca do custo inicial e da memória do índice. Preserva a ordem relativa dos elementos em cada grupo, mas não entrega uma `List<T>`: a correção só é adequada se `Execute` aceitar a sequência sem exigir uma lista mutável, e se `allSteps` representar um snapshot compatível com o processamento.

Esse tipo de recomendação tem mais valor do que uma micro-otimização isolada, porque ataca a complexidade global do algoritmo.

## 10. Critérios de validação de performance

Para que as regras não se tornem apenas opiniões, cada uma deverá ter uma hipótese mensurável.

| Categoria	| Evidência que interessa |
| --- | --- |
| Alocação/GC |	Bytes alocados por operação, Gen 0/1/2, tamanho do LOH |
| CPU |	Tempo de CPU por operação, instruções e tempo total |
| Concorrência |	Throughput, threads ativas, filas e contenção |
| Algoritmos |	Tempo para diferentes tamanhos de entrada |
| I/O | Latência, conexões, operações simultâneas |
| Retenção | Memória viva após períodos prolongados |
| Escalabilidade | Variação de TPS e latências p95/p99 com carga crescente |

O aceite de cada regra deve exigir exemplos positivos e negativos, versões suportadas de runtime e uma justificativa reproduzível. Para diagnósticos de maior impacto, benchmarks comparativos devem demonstrar pelo menos um cenário representativo de regressão.

Isso é especialmente importante para `Span<T>`, `ArrayPool<T>`, `ValueTask`, `Task.Run`, tipos concretos e coleções concorrentes: essas alternativas não são universalmente mais rápidas. A documentação da Microsoft recomenda medir o comportamento com cargas representativas ao escolher estruturas de concorrência. 

## 11. Fontes técnicas fundamentais

### Microsoft — Performance Rules
Inventário das regras de performance já existentes.

### Microsoft — Roslyn Analyzer e Code Fix
Contrato oficial de diagnósticos e correções.

### Microsoft — ThreadPool Starvation
Comportamento de bloqueios e efeitos sobre throughput.

### Microsoft — Large Object Heap
Custos de alocação, GC e retenção de grandes objetos.

### Microsoft — High-performance Logging
Boxing, formatação e logging gerado em compilação.

## 12. Conclusão e escopo recomendado

Eu manteria as 30 regras como backlog inicial, mas priorizaria cinco para demonstrar o valor do produto:

- PERF001: bloqueio síncrono de operações assíncronas.
- PERF008: grandes alocações repetitivas, com impacto no GC.
- PERF014: buscas repetidas e complexidade algorítmica.
- PERF025: gerenciamento inadequado de conexões HTTP.
- PERF027: logging com processamento e alocações evitáveis.

Essas cinco oferecem boa cobertura de problemas operacionais significativos, com dificuldade moderada ou baixa de detecção inicial.

Há ainda uma capacidade que considero essencial para versões futuras: priorização baseada em evidências de runtime, correlacionando diagnósticos estáticos com dados de profiling. Isso permitiria distinguir uma alocação suspeita executada uma vez ao iniciar a aplicação de uma alocação repetida milhões de vezes por minuto.

A especificação estabelece, portanto, duas responsabilidades diferentes: o Roslyn identifica o risco; a medição determina quanto esse risco custa na execução real. Essa separação permitirá construir uma ferramenta tecnicamente rigorosa, útil para revisão de código e capaz de evoluir para diagnósticos de performance orientados por dados.