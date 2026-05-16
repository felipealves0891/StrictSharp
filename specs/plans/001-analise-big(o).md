# 001 - Planejamento - Analise Big O

## Especificacao

Arquivo base: `specs/feats/001-analise-big(o).md`

## Objetivo do planejamento

Planejar a criacao incremental de um analisador Roslyn para identificar custo assintotico em codigo C# e reportar pontos com crescimento relevante conforme a entrada aumenta.

O analisador deve evoluir por etapas pequenas, cada uma com testes, implementacao, benchmark e documentacao propria antes da proxima etapa.

## Fora do escopo deste plano

- Medir tempo real de execucao do codigo analisado.
- Provar a complexidade exata de qualquer algoritmo arbitrario.
- Fazer analise interprocedural profunda entre projetos ou assemblies.
- Resolver limites formais de funcoes matematicas.
- Classificar consumo de memoria assintotico nesta primeira especificacao.
- Criar testes, implementacao, benchmark ou documentacao antes de aprovacao deste planejamento.

## Decisoes iniciais

- Nome sugerido do analisador: `BigOAnalyzer`.
- ID sugerido do diagnostico principal: `PERF009`, para evitar conflito com os IDs existentes.
- Categoria: `Performance`.
- Severidade inicial: `Info`, pois a complexidade pode ser aceitavel dependendo do dominio.
- Escopo inicial: metodos, construtores e lambdas com corpo analisavel por sintaxe local.
- Saida inicial: diagnostico no bloco ou instrucao que determina o maior custo.
- Modelo interno: classificar custo em uma escala ordenavel.

## Modelo de complexidade

Ordem proposta para comparacao:

1. `O(1)`
2. `O(log n)`
3. `O(n)`
4. `O(n log n)`
5. `O(n*m)`
6. `O(n^2)`
7. `O(n^c)`
8. `O(c^n)`
9. `O(?)`

`O(?)` deve representar casos que o analisador nao consegue inferir com seguranca. O analisador nao deve emitir diagnostico agressivo quando a inferencia for incerta.

## Etapas incrementais

### Etapa 1 - Infraestrutura de classificacao

Objetivo:
Criar a base interna para representar e comparar complexidades sem ainda tentar cobrir todo C#.

Testes planejados:
- Comparar `O(1)` menor que `O(n)`.
- Comparar `O(n)` menor que `O(n^2)`.
- Somar sequencias mantendo o maior termo.
- Multiplicar custos em loops aninhados.
- Tratar custo desconhecido como `O(?)`.

Implementacao planejada:
- Criar tipo interno para representar complexidade.
- Criar operacoes de soma sequencial e multiplicacao por iteracao.
- Criar formatacao estavel para mensagens do diagnostico.

Benchmark planejado:
- Medir custo de classificar uma lista pequena de complexidades.
- Meta: nenhuma coleta G1/G2 e alocacao minima.

Documentacao planejada:
- Documentar a escala usada pelo analisador e as limitacoes do modelo.

### Etapa 2 - Sequencia, if e blocos constantes

Objetivo:
Detectar que instrucoes simples sao `O(1)` e que condicionais usam o maior custo entre os blocos.

Testes planejados:
- Metodo com atribuicoes, comparacoes e retornos deve ser `O(1)`.
- `if` com dois lados constantes deve ser `O(1)`.
- `if` com um lado constante e outro linear deve escolher o lado linear quando a Etapa 3 existir.
- `else if` deve ser tratado como cadeia de alternativas.

Implementacao planejada:
- Registrar analise em declaracoes de metodos e construtores.
- Avaliar blocos sequenciais.
- Avaliar `if` pelo maior custo entre `then` e `else`.
- Evitar diagnostico para `O(1)`.

Benchmark planejado:
- Analisar metodo com muitos blocos constantes.
- Medir alocacao por metodo analisado.

Documentacao planejada:
- Documentar regra de soma de sequencias e regra de maximo em condicionais.

### Etapa 3 - Loops lineares

Objetivo:
Detectar loops simples que crescem com uma entrada.

Testes planejados:
- `for (var i = 0; i < n; i++)` deve ser `O(n)`.
- `while (i < n)` com incremento simples deve ser `O(n)`.
- `foreach` sobre parametro `IEnumerable<T>`, array ou collection deve ser `O(n)`.
- Loop com limite constante deve permanecer `O(1)`.
- Loop sem limite inferivel deve ser `O(?)` e nao deve gerar falso positivo forte.

Implementacao planejada:
- Identificar padroes comuns de `for`, `while`, `do` e `foreach`.
- Distinguir limite constante de limite dependente de parametro/local.
- Reportar diagnostico informativo para loops `O(n)` quando a regra estiver habilitada.

Benchmark planejado:
- Analisar arquivos com muitos loops simples.
- Meta: evitar LINQ em caminhos quentes da analise.

Documentacao planejada:
- Documentar padroes reconhecidos e padroes ignorados.

### Etapa 4 - Loops aninhados e custo quadratico

Objetivo:
Detectar multiplicacao de custo em loops aninhados.

Testes planejados:
- Dois loops aninhados dependentes do mesmo limite devem ser `O(n^2)`.
- Dois loops aninhados com limites diferentes devem ser `O(n*m)`.
- Tres loops aninhados do mesmo limite devem ser `O(n^c)`.
- Loop aninhado dentro de `if` deve preservar a maior complexidade do bloco.
- Loops sequenciais nao devem virar `O(n^2)`.

Implementacao planejada:
- Multiplicar custo do corpo pelo custo de iteracao do loop.
- Preservar simbolo do limite quando possivel.
- Diferenciar repeticao do mesmo limite de limites distintos.

Benchmark planejado:
- Analisar metodos com profundidade de loop variando de 1 a 4.
- Medir alocacao e tempo de classificacao por profundidade.

Documentacao planejada:
- Documentar diferenca entre loops aninhados e loops sequenciais.

### Etapa 5 - Crescimento logaritmico

Objetivo:
Detectar loops que reduzem ou multiplicam o indice a cada iteracao.

Testes planejados:
- `for (var i = n; i > 0; i /= 2)` deve ser `O(log n)`.
- `while (n > 1) n /= 2` deve ser `O(log n)`.
- `for (var i = 1; i < n; i *= 2)` deve ser `O(log n)`.
- Loop logaritmico dentro de loop linear deve ser `O(n log n)`.

Implementacao planejada:
- Reconhecer incremento multiplicativo e divisao por constante maior que 1.
- Combinar `O(n)` com `O(log n)` quando houver aninhamento.

Benchmark planejado:
- Analisar variacoes de loops logaritmicos sem alocar colecoes temporarias.

Documentacao planejada:
- Documentar os padroes aceitos para `O(log n)` e `O(n log n)`.

### Etapa 6 - Chamadas locais de funcao

Objetivo:
Incluir custo de chamadas quando a funcao chamada esta no mesmo tipo e tem corpo analisavel.

Testes planejados:
- Metodo `A` que chama metodo constante `B` deve continuar `O(1)`.
- Loop linear que chama metodo linear deve resultar em `O(n^2)`.
- Chamada para metodo externo deve ser `O(?)` ou custo configurado.
- Recursao direta deve ser deixada para a Etapa 7.

Implementacao planejada:
- Criar cache por simbolo de metodo analisado.
- Evitar ciclos de analise.
- Limitar a profundidade de chamadas para nao degradar o analisador.

Benchmark planejado:
- Medir analise de classe com muitos metodos pequenos chamando uns aos outros.
- Verificar que cache reduz reanalise.

Documentacao planejada:
- Documentar limites da analise interprocedural local.

### Etapa 7 - Recursao e custo exponencial

Objetivo:
Detectar padroes simples de recursao linear e ramificada.

Testes planejados:
- Recursao direta com uma chamada e decremento simples deve ser `O(n)`.
- Recursao binaria como Fibonacci ingenuo deve ser `O(c^n)`.
- Recursao com divisao do problema em metade deve ser classificada como `O(log n)` ou `O(n log n)` apenas em padroes seguros.
- Recursao mutua deve ser `O(?)`.

Implementacao planejada:
- Detectar chamadas ao proprio metodo.
- Contar chamadas recursivas no mesmo caminho.
- Identificar reducao simples do argumento.
- Usar `O(?)` quando nao houver reducao clara.

Benchmark planejado:
- Medir metodos recursivos pequenos e casos com muitas chamadas nao recursivas.

Documentacao planejada:
- Documentar criterios para recursao linear, ramificada e desconhecida.

### Etapa 8 - Configuracao e politica de diagnostico

Objetivo:
Permitir que o usuario escolha quais custos devem gerar diagnostico.

Testes planejados:
- Padrao nao deve alertar para `O(1)`.
- Configuracao deve permitir alertar apenas a partir de `O(n^2)`.
- Configuracao deve permitir alertar para `O(n)` quando o usuario quiser.
- Mensagem deve indicar a complexidade inferida e o ponto do codigo.

Implementacao planejada:
- Ler `.editorconfig` quando aplicavel.
- Definir limiar padrao conservador, sugerido: `O(n^2)`.
- Criar mensagem clara e estavel.

Benchmark planejado:
- Medir custo de leitura de opcoes por arvore/compilacao.

Documentacao planejada:
- Documentar opcoes de configuracao e exemplos de uso.

### Etapa 9 - Cobertura de linguagem C# e reducao de falsos positivos

Objetivo:
Ampliar cobertura sem comprometer confiabilidade.

Testes planejados:
- `switch` deve usar maior custo entre secoes.
- Expressoes LINQ conhecidas, como `Select` e `Where`, devem ser tratadas como lineares apenas em padroes seguros.
- `Any` e `FirstOrDefault` devem ser tratadas como pior caso `O(n)`.
- `Count` em array ou `ICollection<T>` deve ser `O(1)`.
- `Count()` extension method sobre `IEnumerable<T>` deve ser `O(n)`.

Implementacao planejada:
- Mapear APIs comuns com custo conhecido.
- Usar simbolos Roslyn para diferenciar propriedade `Count` de metodo LINQ `Count()`.
- Manter fallback conservador para chamadas desconhecidas.

Benchmark planejado:
- Medir analise de arquivo com muitas chamadas LINQ e acesso a propriedades.

Documentacao planejada:
- Documentar tabela de APIs conhecidas e seus custos no pior caso.

## Arquivos previstos

Testes:
- `tests/StrictSharp.Analyzers.Tests/BigOAnalyzerTests.cs`
- Possivel arquivo auxiliar para testar o modelo interno se ele for exposto a `InternalsVisibleTo`.

Implementacao:
- `src/StrictSharp.Analyzers/BigOAnalyzer.cs`
- `src/StrictSharp.Analyzers/BigOComplexity.cs`
- `src/StrictSharp.Analyzers/BigOComplexityWalker.cs`
- `src/StrictSharp.Analyzers/BigOKnownApiCosts.cs`
- `src/StrictSharp.Analyzers/AnalyzerReleases.Unshipped.md`

Benchmark:
- `tests/StrictSharp.Analyzers.Benchmark/BigOAnalyzerBenchmark.cs`
- `tests/StrictSharp.Analyzers.Benchmark/Program.cs`

Documentacao:
- `specs/docs/001-analise-big(o).md`

## Estrategia de testes

Os testes devem ser escritos antes de cada etapa de implementacao.

Cada etapa deve ter:
- Casos positivos com marcador `[| |]` no trecho diagnosticado.
- Casos negativos para evitar falsos positivos.
- Pelo menos um caso de composicao com etapa anterior.
- Mensagens e severidade validadas quando o diagnostico for emitido.

## Estrategia de benchmark

Os benchmarks devem medir o custo do analisador, nao a performance do codigo analisado.

Cenarios minimos:
- Arquivo com muitos metodos `O(1)`.
- Arquivo com muitos loops lineares.
- Arquivo com loops aninhados.
- Arquivo com chamadas locais.
- Arquivo com LINQ/API conhecida.

Metricas obrigatorias:
- Alocacao total.
- Coletas GC G0/G1/G2.
- Tempo medio por analise.

Criterio:
- G1 e G2 devem permanecer em zero.
- G0 deve ser o menor possivel.
- Alocacao deve ser revisada quando crescer entre etapas.

## Criterios de conclusao da especificacao

A especificacao so deve ser considerada concluida quando todas as etapas aprovadas tiverem:

- Testes escritos antes da implementacao.
- Implementacao passando nos testes.
- Benchmark executado e registrado.
- Documentacao em `specs/docs/001-analise-big(o).md`.
- Limitacoes conhecidas documentadas.

## Questoes para aprovacao

1. O diagnostico deve alertar por padrao a partir de `O(n^2)` ou tambem para `O(n)`?
2. `PERF009` pode ser reservado para o analisador Big O?
3. A primeira implementacao deve parar na Etapa 4, cobrindo ate loops aninhados, ou devemos aprovar o plano completo de nove etapas?
