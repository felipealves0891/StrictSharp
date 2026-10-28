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
| V2 | 13 | Catalogados; implementação futura |
| V3 | 2 | Catalogados; análise mais profunda de ciclo de vida |

Os diagnósticos V1 usam C# 14 e são compilados como um analisador Roslyn para `netstandard2.1`. Cada ocorrência possui severidade, categoria, impacto, escalabilidade, correção sugerida, exceções e referência documental.

## Como usar

### Compilar e testar

```bash
dotnet build StrictSharp.sln
dotnet test StrictSharp.sln
```

### Adicionar a um projeto

O projeto consumidor deve referenciar o assembly do analisador como um analyzer. Em uma solução local, uma referência de projeto pode ser configurada assim:

```xml
<ProjectReference Include="..\path\to\StrictSharp.Analyzers\StrictSharp.Analyzers.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

Em testes, o projeto pode manter `ReferenceOutputAssembly="true"` quando também precisar acessar os tipos do catálogo. A distribuição como pacote NuGet ainda não está configurada neste repositório.

As regras V1 são habilitadas por padrão quando o analyzer é carregado. A severidade pode ser ajustada no projeto consumidor com `.editorconfig`:

```ini
dotnet_diagnostic.PERF001.severity = warning
dotnet_diagnostic.PERF027.severity = suggestion
```

## Diagnósticos

### V1 — implementados

| ID | Diagnóstico | O que identifica | Orientação principal |
| --- | --- | --- | --- |
| PERF001 | Bloqueio síncrono de operação assíncrona | `.Result`, `.Wait()` ou `GetAwaiter().GetResult()` em tarefa potencialmente incompleta | Propagar `await` pelo fluxo assíncrono |
| PERF003 | `Task.Run` desnecessário em I/O | Encapsulamento de operação assíncrona de I/O em `Task.Run` | Aguardar diretamente; manter `Task.Run` para CPU-bound ou isolamento justificado |
| PERF007 | Alocação recorrente dentro de loops | Criação repetida de objetos, coleções ou arrays dentro de loop | Reutilizar recursos ou usar pool quando for seguro |
| PERF008 | Alocações grandes recorrentes | Arrays grandes criados repetidamente, com possível pressão sobre LOH e Gen 2 | Usar streaming, segmentação ou `ArrayPool<T>` |
| PERF012 | Cópias desnecessárias de coleções e buffers | `Clone`, `ToArray` ou `ToList` quando a cópia pode ser evitada | Reutilizar a representação original quando a semântica permitir |
| PERF013 | Enumeração repetida de `IEnumerable` | A mesma sequência enumerada várias vezes no método | Materializar uma vez e reutilizar o resultado |
| PERF014 | Busca linear repetida em coleção | Busca em lista dentro de loop, potencialmente gerando custo O(n × m) | Avaliar `HashSet<T>` ou `Dictionary<TKey,TValue>` |
| PERF015 | Uso inadequado de `Count`, `Any` e propriedades | Enumeração ou chamada LINQ quando já existe contagem ou propriedade direta | Preferir `Count`, `Length`, `IsEmpty` ou API equivalente |
| PERF018 | Concatenação repetitiva de strings | Concatenação de strings em repetição, com cópias e alocações intermediárias | Usar `StringBuilder` ou `string.Join` conforme o caso |
| PERF019 | Substrings e conversões desnecessárias | Materialização intermediária por `Substring` ou `ToCharArray` | Preferir `ReadOnlySpan<char>` ou parsing sem cópia quando possível |
| PERF020 | Normalização de strings para comparação | `ToLower`/`ToUpper` usados antes de comparar texto | Usar `StringComparison` adequado |
| PERF022 | Consulta redundante em `Dictionary` | `ContainsKey` seguido de indexação ou remoção da mesma chave | Usar `TryGetValue` ou a operação direta equivalente |
| PERF025 | Uso inadequado de `HttpClient` | Criação de cliente HTTP por operação, prejudicando pooling e conexões | Reutilizar cliente/handler ou usar `IHttpClientFactory` |
| PERF026 | I/O síncrono em fluxo assíncrono | Operações síncronas de rede, arquivo ou stream em fluxo assíncrono | Usar API assíncrona quando houver benefício de escalabilidade |
| PERF027 | Logging com custo evitável | Interpolação, concatenação ou serialização executada antes de verificar o nível de log | Usar logging estruturado, `LoggerMessage` ou checagem condicional |

As regras V1 não aplicam Code Fix automático. Algumas correções dependem de mutabilidade, igualdade, propriedade dos dados, ciclo de vida ou thread safety.

### V2 — catalogados, ainda não implementados

| ID | Diagnóstico | O que pretende detectar |
| --- | --- | --- |
| PERF002 | Paralelismo sem limite | Criação de tarefas para uma coleção sem limite explícito de concorrência |
| PERF004 | `lock` durante operação longa ou bloqueante | Espera ou I/O dentro de região de exclusão mútua |
| PERF005 | Espera ativa ou polling excessivo | Repetição de consultas enquanto aguarda um evento ou recurso |
| PERF006 | Criação excessiva de threads ou tarefas | Criação de workers sem reutilização ou limite adequado |
| PERF009 | Boxing evitável | Conversão desnecessária de value types para `object` ou interfaces |
| PERF010 | Capturas e closures desnecessárias | Closures/delegates que geram alocação ou retêm referências sem necessidade |
| PERF016 | Ordenação repetida ou desnecessária | Ordenações recalculadas quando os dados ou a ordenação poderiam ser reutilizados |
| PERF017 | Consultas LINQ repetidas dentro de loops | Filtros, projeções ou agregações invariantes recalculados a cada iteração |
| PERF021 | Regex ou metadados recriados | Preparação repetida de regex, reflexão ou metadados reutilizáveis |
| PERF023 | Coleção inadequada para o padrão de acesso | Estrutura de dados incompatível com as operações dominantes |
| PERF024 | Crescimento repetido de coleções | Expansões frequentes por capacidade insuficiente conhecida |
| PERF028 | Recursos `IDisposable` não liberados | Recursos, handles, conexões ou buffers sem ciclo de vida controlado |
| PERF029 | Exceções como fluxo normal | Uso recorrente de exceções para representar falhas esperadas |

### V3 — catalogados, ainda não implementados

| ID | Diagnóstico | O que pretende detectar |
| --- | --- | --- |
| PERF011 | Retenção indevida de objetos | Estruturas que mantêm memória viva sem limite de tamanho ou expiração |
| PERF030 | Reconstrução de objetos pesados invariantes | Criação repetida de objetos caros cuja configuração não muda |

## Severidade e confiança

O catálogo separa risco operacional de confiança da detecção:

- `Critical`: risco potencial muito alto; exibido pelo Roslyn como erro;
- `High`: risco alto; exibido como warning;
- `Medium`: risco moderado; exibido como info;
- `Low`: risco baixo; exibido como hidden.

A confiança indica quão forte é a evidência estrutural, mas não significa que o impacto ocorrerá em toda execução. Por exemplo, uma busca linear pode ser irrelevante para uma coleção pequena e custosa para uma coleção grande dentro de um caminho quente.

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
