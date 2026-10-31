# StrictSharp

Analisador estático de C# para identificar riscos de performance e antipatrones em aplicações .NET. O projeto usa Roslyn e segue uma abordagem *Specification First*: cada regra descreve o padrão observado, o impacto potencial, a orientação de correção e as limitações da análise estática.

## Objetivos

- encontrar custos estruturais no código antes da execução;
- explicar por que um padrão pode prejudicar CPU, memória, concorrência, I/O ou escalabilidade;
- complementar, e não simplesmente repetir, os analisadores oficiais do .NET;
- manter diagnósticos rápidos, rastreáveis e com baixo ruído;
- deixar explícito que o analisador identifica risco potencial, mas não mede o custo real em produção.

O StrictSharp não substitui profiling ou benchmarks. Frequência de execução, volume de dados, latência externa, contenção e ganho percentual precisam ser validados com medições representativas.

## Estado atual

O catálogo contém 30 diagnósticos, organizados em três fases:

| Fase | Quantidade | Situação |
| --- | ---: | --- |
| V1 | 15 | Implementados e registrados em `V1AnalyzerCatalog` |
| V2 | 13 | Implementados e registrados em `V2AnalyzerCatalog` |
| V3 | 2 | Implementados e registrados em `V3AnalyzerCatalog` |

Os diagnósticos V1 usam C# 14 e são compilados como um analisador Roslyn para `netstandard2.1`. Cada ocorrência possui severidade, categoria, impacto, escalabilidade, correção sugerida, exceções e referência documental.

## Como usar

### Compilar e testar

```bash
dotnet build StrictSharp.sln
dotnet test StrictSharp.sln
```

### Adicionar a um projeto

O StrictSharp é distribuído como um analyzer Roslyn no pacote NuGet `StrictSharp`. No diretório do projeto consumidor, execute:

```bash
dotnet add package StrictSharp
```

Para uma instalação reproduzível, fixe a versão publicada:

```bash
dotnet add package StrictSharp --version 1.0.28
```

O pacote é executado pelo IDE e pelo build do projeto para analisar o código C#. Ele não adiciona uma dependência de runtime nem uma biblioteca que precise ser chamada pela aplicação.

Em uma solução local do repositório, uma referência de projeto continua disponível para desenvolvimento do próprio analyzer:

```xml
<ProjectReference Include="..\path\to\StrictSharp.Analyzers\StrictSharp.Analyzers.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

Em testes do repositório, o projeto pode manter `ReferenceOutputAssembly="true"` quando também precisar acessar os tipos do catálogo. Para consumidores, prefira o pacote NuGet.

As regras são habilitadas quando o analyzer é carregado. A severidade pode ser ajustada ou uma regra pode ser desabilitada no projeto consumidor com `.editorconfig`:

```ini
dotnet_diagnostic.PERF001.severity = warning
dotnet_diagnostic.PERF027.severity = suggestion
dotnet_diagnostic.PERF014.severity = none
```

Os valores usuais são `none`, `silent`, `suggestion`, `warning` e `error`. A severidade controla como o diagnóstico aparece no fluxo de desenvolvimento; não é uma estimativa de ganho percentual.

## Diagnósticos

Cada regra descreve um padrão observado no código e o risco potencial associado. A severidade representa o risco operacional sob condições plausíveis; a confiança indica a força da evidência estrutural. Nenhuma delas mede, sozinha, o impacto real em produção.

### V1 — implementados

| ID | Diagnóstico | O que significa | Severidade | Confiança | Orientação principal |
| --- | --- | --- | --- | --- | --- |
| PERF001 | Bloqueio síncrono de operação assíncrona | Bloqueia threads e pode reduzir throughput durante esperas assíncronas. | Critical | Alta | Usar `await` e propagar o fluxo assíncrono. |
| PERF003 | `Task.Run` desnecessário em I/O | Adiciona agendamento e filas no ThreadPool sem benefício para I/O. | High | Alta | Aguardar diretamente; manter `Task.Run` para CPU-bound ou isolamento justificado. |
| PERF007 | Alocação recorrente dentro de loops | Eleva alocações e pressão sobre o GC a cada iteração. | High | Média | Reutilizar recursos ou usar pool quando for seguro. |
| PERF008 | Alocações grandes recorrentes | Pode pressionar LOH e aumentar coletas Gen 2. | Critical | Alta | Usar streaming, segmentação ou `ArrayPool<T>` quando apropriado. |
| PERF012 | Cópias desnecessárias de coleções e buffers | Adiciona custo linear de CPU, memória temporária e cópia. | High | Alta | Reutilizar a representação original quando compatível. |
| PERF013 | Enumeração repetida de `IEnumerable` | Repete processamento e pode repetir consultas ou I/O. | High | Alta | Materializar uma vez ou reutilizar a coleção materializada. |
| PERF014 | Busca linear repetida em coleção | Pode gerar complexidade `O(n × m)` em loops aninhados. | Critical | Alta | Avaliar `HashSet<T>` ou `Dictionary<TKey,TValue>`. |
| PERF015 | Uso inadequado de `Count`, `Any` e propriedades | Pode adicionar enumeração e overhead quando existe acesso direto. | Medium | Alta | Preferir `Count`, `Length`, `IsEmpty` ou API equivalente. |
| PERF018 | Concatenação repetitiva de strings | Pode gerar cópias e alocações repetidas, aproximando custo `O(n²)`. | High | Alta | Usar `StringBuilder` ou `string.Join` conforme o caso. |
| PERF019 | Substrings e conversões desnecessárias | Cria strings e cópias intermediárias evitáveis. | Medium | Média | Usar `ReadOnlySpan<char>` ou parsing sem materialização quando possível. |
| PERF020 | Normalização de strings para comparação | Adiciona processamento e alocações antes da comparação. | Medium | Alta | Usar `StringComparison` adequado. |
| PERF022 | Consulta redundante em `Dictionary` | Repete hash e comparação em consultas sequenciais. | Medium | Alta | Usar `TryGetValue` ou operação direta equivalente. |
| PERF025 | Uso inadequado de `HttpClient` | Pode perder pooling e exaurir portas ou conexões. | Critical | Alta | Reutilizar cliente/handler ou usar `IHttpClientFactory`. |
| PERF026 | I/O síncrono em fluxo assíncrono | Mantém threads bloqueadas aguardando rede, arquivo ou stream. | High | Média | Usar API assíncrona quando houver benefício de escalabilidade. |
| PERF027 | Logging com custo evitável | Pode formatar ou serializar mesmo quando o nível está desabilitado. | High | Alta | Usar logging estruturado, `LoggerMessage` e checagem condicional para trabalho caro. |

### V2 — implementados

| ID | Diagnóstico | O que significa | Severidade | Confiança | Orientação principal |
| --- | --- | --- | --- | --- | --- |
| PERF002 | Paralelismo sem limite | Retém tarefas, memória e recursos concorrentes conforme a coleção cresce. | Critical | Média | Limitar concorrência com lotes ou mecanismo apropriado. |
| PERF004 | `lock` durante operação longa ou bloqueante | Mantém exclusão mútua durante espera e aumenta contenção. | Critical | Média | Reduzir a região crítica e separar I/O do estado compartilhado. |
| PERF005 | Espera ativa ou polling excessivo | Consome CPU e repete chamadas enquanto aguarda um evento ou recurso. | High | Média | Usar sinalização, filas, `Channel<T>` ou intervalos adequados. |
| PERF006 | Criação excessiva de threads ou tarefas | Aumenta agendamento, troca de contexto e coordenação. | High | Média | Reutilizar workers e limitar concorrência. |
| PERF009 | Boxing evitável | Adiciona conversão e possível alocação de value types. | Medium | Média | Preferir APIs genéricas e tipadas. |
| PERF010 | Capturas e closures desnecessárias | Pode alocar closures/delegates e reter referências. | Medium | Média | Preferir lambda `static`, delegate reutilizado ou estado explícito. |
| PERF016 | Ordenação repetida ou desnecessária | Repete ordenação e buffers temporários, tipicamente em `O(n log n)`. | High | Média | Ordenar uma vez ou usar seleção quando apropriado. |
| PERF017 | Consultas LINQ repetidas dentro de loops | Recalcula filtros, projeções e agregações invariantes. | High | Média | Antecipar cálculo ou criar índice reutilizável. |
| PERF021 | Regex ou metadados recriados | Repete parsing, compilação ou preparação de metadados. | High | Média | Reutilizar instâncias, cache ou `GeneratedRegex` quando suportado. |
| PERF023 | Coleção inadequada para o padrão de acesso | Pode exigir deslocamentos, cópias e buscas lineares. | High | Média | Avaliar a estrutura adequada às operações dominantes. |
| PERF024 | Crescimento repetido de coleções | Pode realocar e copiar buffers internos durante expansões. | Medium | Média | Informar capacidade quando conhecida. |
| PERF028 | Recursos `IDisposable` não liberados | Pode reter handles, conexões e buffers. | High | Média | Usar `using`, `await using` ou ciclo de vida controlado. |
| PERF029 | Exceções como fluxo normal | Cria exceções e stack traces para falhas esperadas. | High | Média | Usar `TryParse`, `TryGetValue` ou resultado explícito. |

### V3 — implementados

| ID | Diagnóstico | O que significa | Severidade | Confiança | Orientação principal |
| --- | --- | --- | --- | --- | --- |
| PERF011 | Retenção indevida de objetos | Pode aumentar memória viva, Gen 2 e risco de esgotamento. | Critical | Baixa | Limitar tamanho/tempo, expirar entradas e liberar inscrições. |
| PERF030 | Reconstrução de objetos pesados invariantes | Repete inicialização, alocações e pressão sobre o GC. | High | Baixa | Compartilhar instâncias imutáveis ou cachear no escopo apropriado. |

As recomendações são condicionais: mutabilidade, igualdade, propriedade dos dados, ciclo de vida, thread safety, tamanho da entrada e frequência de execução precisam ser considerados. As regras reportam suspeitas estruturais; não provam memory leaks, não medem memória viva ou custo de execução e não classificam automaticamente todo objeto com muitas propriedades como pesado.

## Severidade e confiança

O catálogo separa risco operacional de confiança da detecção:

- `Critical`: risco potencial muito alto; exibido pelo Roslyn como erro;
- `High`: risco alto; exibido como warning;
- `Medium`: risco moderado; exibido como info;
- `Low`: risco baixo; exibido como hidden.

A confiança indica quão forte é a evidência estrutural, mas não significa que o impacto ocorrerá em toda execução. Por exemplo, uma busca linear pode ser irrelevante para uma coleção pequena e custosa para uma coleção grande dentro de um caminho quente. `PERF001`–`PERF030` são IDs do StrictSharp, não IDs oficiais do .NET; quando houver uma regra oficial relacionada, a documentação detalhada indica a relação.

## Organização do código

```text
src/StrictSharp.Analyzers/
├── Analyzers/       Implementação de um analyzer por diagnóstico
├── Infrastructure/  Base e serviços compartilhados de análise
└── Rules/           Catálogo e metadados das regras
tests/               Testes de comportamento, catálogo e infraestrutura
docs/                Especificação completa e exemplos das regras
```

Os analyzers delegam metadados ao `RuleCatalog` e habilitam execução concorrente do Roslyn. A lógica específica de cada regra permanece separada da infraestrutura comum.

## Documentação e contribuição

A especificação detalhada, incluindo exemplos positivos e negativos, limitações e critérios de aceitação, está em [docs/csharp-performance-analyzer.md](docs/csharp-performance-analyzer.md).

Ao adicionar ou alterar uma regra:

1. atualize o catálogo e a documentação;
2. mantenha um analyzer por diagnóstico;
3. adicione testes positivos, negativos e de casos-limite;
4. evite afirmar ganhos percentuais sem benchmark;
5. execute `dotnet build` e `dotnet test` antes de enviar a alteração.
