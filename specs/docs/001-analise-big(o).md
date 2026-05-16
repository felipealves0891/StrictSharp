# 001 - Documentacao - Analise Big O

## Resumo

Foi implementado o analisador `BigOAnalyzer`, registrado com o diagnostico `PERF009`, para inferir complexidade assintotica local em codigo C#.

O analisador roda sobre metodos e construtores com corpo sintatico disponivel. Quando encontra uma complexidade igual ou superior ao limiar configurado, emite diagnostico informativo no trecho que determina o custo.

Mensagem:

```text
This code has inferred complexity '{0}'
```

Categoria: `Performance`

Severidade: `Info`

## Arquivos implementados

- `src/StrictSharp.Analyzers/BigOAnalyzer.cs`
- `src/StrictSharp.Analyzers/BigOComplexity.cs`
- `src/StrictSharp.Analyzers/AnalyzerReleases.Unshipped.md`

## Testes implementados

- `tests/StrictSharp.Analyzers.Tests/BigOComplexityTests.cs`
- `tests/StrictSharp.Analyzers.Tests/BigOAnalyzerStage2Tests.cs`
- `tests/StrictSharp.Analyzers.Tests/BigOAnalyzerStage3Tests.cs`
- `tests/StrictSharp.Analyzers.Tests/BigOAnalyzerStage4Tests.cs`
- `tests/StrictSharp.Analyzers.Tests/BigOAnalyzerStage5Tests.cs`
- `tests/StrictSharp.Analyzers.Tests/BigOAnalyzerStage6Tests.cs`
- `tests/StrictSharp.Analyzers.Tests/BigOAnalyzerStage7Tests.cs`
- `tests/StrictSharp.Analyzers.Tests/BigOAnalyzerStage8Tests.cs`
- `tests/StrictSharp.Analyzers.Tests/BigOAnalyzerStage9Tests.cs`

Ultima execucao registrada:

```text
dotnet test tests\StrictSharp.Analyzers.Tests\StrictSharp.Analyzers.Tests.csproj -v:minimal
```

Resultado:

```text
53 testes passaram, 0 falharam
```

Observacao: o projeto de testes ainda emite aviso de `PackageReference` duplicado para `Microsoft.NET.Test.Sdk`, `xunit` e `xunit.runner.visualstudio`. Esse aviso ja existia e nao foi tratado nesta especificacao.

## Modelo de complexidade

O tipo `BigOComplexity` representa e compara as complexidades abaixo:

| Complexidade | Uso |
|---|---|
| `O(1)` | Instrucoes constantes, acesso a propriedades simples e blocos sem crescimento inferido |
| `O(log n)` | Loops ou recursao que dividem/multiplicam por constante maior que 1 |
| `O(n)` | Loops lineares, chamadas LINQ conhecidas e recursao direta com decremento |
| `O(n log n)` | Loop linear contendo loop logaritmico |
| `O(n*m)` | Dois loops aninhados com limites diferentes |
| `O(n^2)` | Dois loops aninhados com o mesmo limite |
| `O(n^c)` | Tres ou mais loops aninhados do mesmo limite reconhecido |
| `O(c^n)` | Recursao direta ramificada, como Fibonacci ingenuo |
| `O(?)` | Caso nao inferido com seguranca |

Sequencias usam o maior custo entre as instrucoes. Multiplicacao e usada para compor custo de loop com custo do corpo.

## Regras implementadas

### Blocos e condicionais

- Blocos sequenciais somam custo pelo maior termo.
- `if` e `else` usam o maior custo entre os ramos.
- `switch` usa o maior custo entre as secoes.
- Codigo constante nao gera diagnostico.

### Loops lineares

Padroes reconhecidos:

- `for` com condicao variavel, como `i < n`
- `while` com condicao variavel, como `i < n`
- `do/while` com condicao variavel
- `foreach` sobre identificador ou acesso de membro

Loops com limite literal, como `i < 4`, sao tratados como `O(1)`.

### Loops aninhados

- Mesmo limite: `O(n^2)`
- Limites diferentes: `O(n*m)`
- Tres niveis com mesmo limite: `O(n^c)`
- Loops sequenciais continuam no maior custo individual, por exemplo `O(n)`.

### Loops logaritmicos

Padroes reconhecidos:

- `for (var i = n; i > 0; i /= 2)`
- `for (var i = 1; i < n; i *= 2)`
- `while` ou `do/while` quando a variavel da condicao e multiplicada ou dividida por constante maior que 1 dentro do corpo

Loop logaritmico dentro de loop linear e reportado como `O(n log n)`.

### Chamadas locais

Chamadas para metodos do mesmo tipo com corpo disponivel entram na conta.

Exemplo:

- metodo `A` contem loop `O(n)`
- dentro do loop, chama metodo local `B`
- metodo `B` tambem e `O(n)`
- resultado de `A`: `O(n^2)`

Chamadas externas sao tratadas de forma conservadora como custo constante nesta etapa, exceto APIs conhecidas documentadas abaixo.

### Recursao direta

Padroes reconhecidos:

- Uma chamada recursiva com `n - constante positiva`: `O(n)`
- Uma chamada recursiva com `n / constante maior que 1`: `O(log n)`
- Mais de uma chamada recursiva direta no mesmo metodo: `O(c^n)`

Recursao mutua permanece sem diagnostico porque nao ha inferencia segura nesta implementacao.

### APIs conhecidas

As chamadas abaixo sao reconhecidas como `O(n)` quando resolvidas como `System.Linq.Enumerable`:

| API | Complexidade |
|---|---|
| `Where` | `O(n)` |
| `Select` | `O(n)` |
| `Any` | `O(n)` pior caso |
| `FirstOrDefault` | `O(n)` pior caso |
| `Count()` | `O(n)` sobre `IEnumerable<T>` |

Acesso a propriedade como `Length` em array permanece `O(1)` e nao gera diagnostico.

## Configuracao

O limiar minimo pode ser configurado por `.editorconfig`:

```ini
dotnet_diagnostic.PERF009.severity = suggestion
strictsharp_big_o_minimum_complexity = O(n^2)
```

Valores reconhecidos:

- `O(log n)`
- `O(n)`
- `O(n log n)`
- `O(n*m)`
- `O(n^2)`
- `O(n^c)`
- `O(c^n)`

Valor padrao atual: `O(log n)`.

Se o valor configurado nao for reconhecido, o analisador usa o padrao `O(log n)`.

## Localizacao do diagnostico

O diagnostico e reportado no no sintatico que determina o maior custo conhecido.

Exemplos:

- loop simples: diagnostico no `for`, `while`, `do` ou `foreach`
- chamada LINQ conhecida: diagnostico na chamada
- `switch`: diagnostico na instrucao que domina a secao de maior custo
- recursao direta: diagnostico no corpo do metodo

## Limitacoes conhecidas

- A analise nao prova complexidade formal; ela reconhece padroes sintaticos conservadores.
- Nao ha analise interprocedural profunda entre tipos, assemblies ou projetos.
- Chamadas externas desconhecidas nao entram na conta.
- Recursao mutua e tratada como desconhecida e nao gera diagnostico.
- Loops com condicoes complexas podem virar `O(?)`.
- O modelo nao diferencia todos os simbolos reais de entrada; ele normaliza a exibicao para `n`, `m`, `c` e `log n`.
- LINQ e reconhecido por uma lista pequena de APIs conhecidas.
- Consumo assintotico de memoria nao foi implementado.
- Benchmarks ainda nao foram criados nem executados para esta implementacao.

## Benchmark

Benchmark ainda pendente.

O planejamento exige medir o custo do analisador, nao o tempo do codigo analisado. Os cenarios previstos continuam:

- muitos metodos `O(1)`
- muitos loops lineares
- loops aninhados
- chamadas locais
- LINQ/API conhecida

Metricas obrigatorias:

- alocacao total
- coletas GC G0/G1/G2
- tempo medio por analise

## Status da especificacao

Implementado:

- Planejamento
- Testes das etapas 1 a 9
- Implementacao das etapas 1 a 9
- Documentacao

Pendente:

- Benchmark da implementacao
- Registro dos resultados de benchmark
