<!-- spectro-managed: true -->
# Plano: Correção de falso positivo do PERF013 em strings

## Contexto e objetivo técnico

- A especificação foi esclarecida; não há questões bloqueantes. A decisão `D-001` limita a correção às APIs próprias de busca de texto, preservando enumerações reais de caracteres.
- `Perf013Analyzer.IsEnumeration` atualmente aceita qualquer receptor para o qual `RuleAnalysis.IsEnumerable` retorne verdadeiro e compara o nome do método com uma lista fixa. `string` implementa `IEnumerable<char>` e sua busca de instância `Contains` coincide com um nome dessa lista, embora essa chamada use a API de texto de `System.String`.
- Objetivo técnico: distinguir a busca de texto resolvida para `System.String.Contains` de métodos que de fato enumeram a sequência, sem alterar o contrato `PERF013` ou a detecção das demais sequências.

## Solução proposta

- Usar o símbolo Roslyn resolvido da invocação para identificar chamadas `Contains` cuja API declarante é `System.String`; não classificá-las como enumeração do `PERF013`.
- Manter elegíveis os métodos LINQ e demais operações da lista atual que realmente enumeram o receptor, inclusive quando o receptor é uma string e o método é, por exemplo, `Enumerable.Count`.
- Manter `RuleAnalysis.IsEnumerable` inalterado: ele é um helper compartilhado e retirar `string` da definição genérica de enumerável poderia afetar outras regras e enumerações genuínas de caracteres.
- Manter o fallback atual para nome do método quando o símbolo não estiver resolvido, exceto quando a semântica de tipo e membro permitir reconhecer com segurança a busca de instância de `string`. Implementar a verificação localmente ao PERF013.
- Não adicionar dependências, opções, diagnósticos, configuração ou efeitos persistentes. O fluxo continua sendo análise sintática por método, consulta semântica e emissão do diagnóstico existente.

## Arquitetura e contratos

- Componente de produção afetado: `src/StrictSharp.Analyzers/Analyzers/Perf013Analyzer.cs`, especificamente a classificação de invocações em `IsEnumeration`.
- Helper consultado: `src/StrictSharp.Analyzers/Infrastructure/RuleAnalysis.cs`; `IsString` e `IsEnumerable` já existem. Não alterar o helper compartilhado nesta correção.
- Componente de validação: `tests/StrictSharp.Analyzers.Tests/Perf013AnalyzerTests.cs`, usando o `AnalyzerTestHarness` e o compilador Roslyn configurado para C# 14.
- Contrato externo: `PERF013`, severidade, localização e configuração permanecem como estão. Uma chamada de busca de texto em string não entra no conjunto de enumerações repetidas; operações LINQ que enumeram string continuam sujeitas à regra.
- Fluxo de dados: cada método analisado fornece suas invocações; o modelo semântico resolve tipo e método; a classificação separa a API de busca de texto do conjunto atual de enumeração; receptores repetidos ainda são comparados como atualmente e os diagnósticos são reportados na localização da invocação repetida.
- Erros e símbolos incompletos: não lançar exceções nem introduzir falha de análise. Manter o comportamento de fallback existente para invocações cujo símbolo não pode ser resolvido, sem classificar como enumeração uma chamada de busca de instância de string reconhecível pelo tipo do receptor e nome do membro.
- Persistência, I/O, concorrência e observabilidade externa: não aplicáveis. O estado `seen` permanece local à análise de cada método; nenhuma informação do código-fonte sai do processo.
- Compatibilidade: analyzer em `netstandard2.1`, C# 14 e Roslyn 5.0.0; testes em `net10.0`, C# 14 e xUnit. A solução não depende de API nova.

## Alternativas consideradas

- Excluir todo receptor `string` da verificação de `IEnumerable`: rejeitada porque suprimiria enumerações reais de caracteres, incluindo operadores LINQ que enumeram a string, contrariando `D-001` e `AC-004`.
- Alterar `RuleAnalysis.IsEnumerable` para retornar falso para `string`: rejeitada porque o helper é compartilhado e a mudança alteraria a semântica de outras regras fora do escopo.
- Ignorar todas as chamadas `Contains` por nome: rejeitada porque há métodos de enumeração `Contains` em outros receptores; isso poderia ocultar diagnósticos válidos.
- Distinguir a API de instância pelo símbolo Roslyn declarante: escolhida por separar a busca própria de string de operações de enumeração cobertas, mantendo a mudança restrita ao PERF013.

## Riscos e compatibilidade

- Risco de regressão: um filtro baseado apenas no tipo de receptor esconderia enumerações LINQ válidas; mitigação: teste de `string.Count()` repetido (ou operação equivalente resolvida para `Enumerable`) junto aos testes de busca em texto.
- Risco de símbolo ausente/ambíguo: classificação pode depender do fallback sintático existente; mitigação: reconhecer o caso de string sem tornar buscas próprias de texto diagnósticos e preservar o caminho existente para chamadas resolvidas normalmente.
- Risco de sobreajuste: cobrir apenas a forma com literal poderia deixar outras sobrecargas próprias de `Contains` como falso positivo; mitigação: testes com argumento string e char quando aplicável, além do exemplo de terminadores de linha.
- Sem mudanças em IDs, mensagens, severidade, configuração, dependências, documentação do produto ou outros analyzers.

## Estratégia de testes

- Adicionar regressão do exemplo `text.Contains("\r") || text.Contains("\n")`; esperado: nenhum `PERF013`.
- Cobrir chamadas repetidas de `string.Contains` nas sobrecargas de texto relevantes para garantir que a regra classifica o método resolvido, não apenas um literal específico.
- Cobrir enumeração repetida real em `IEnumerable<T>` não string; esperado: exatamente um `PERF013`, preservando o teste existente.
- Cobrir enumeração repetida real da string via operador LINQ reconhecido, como `text.Count()`; esperado: `PERF013`, demonstrando que `string` não foi excluída globalmente.
- Cobrir caso único de busca e caso único de enumeração; esperado: nenhum diagnóstico de enumeração repetida.
- Validar AC-001 a AC-005 pela suíte `Perf013AnalyzerTests` e verificar compatibilidade por build/testes da solução durante a implementação. Nenhum teste ou comando é executado nesta fase de planejamento.

## Rastreabilidade

| Requisito | Critérios de aceite | Tarefas |
|---|---|---|
| `FR-001` | `AC-001`, `AC-002`, `AC-004` | `TASK-001`, `TASK-002` |
| `FR-002` | `AC-003`, `AC-004` | `TASK-001`, `TASK-002` |
| `FR-003` | `AC-001`, `AC-002` | `TASK-001`, `TASK-002` |
| `NFR-001` | `AC-005` e ausência de regressões nos AC-001 a AC-004 | `TASK-001`, `TASK-002` |
| `CON-001` | `AC-003` | `TASK-001`, `TASK-002` |

## Ordem de execução

1. `TASK-001`: especificar os cenários de regressão nos testes do analyzer, incluindo o exemplo, operações enumeração reais e casos únicos.
2. `TASK-002`: alterar a classificação semântica local do PERF013 e validar a suíte direcionada e os gates definidos para a implementação.

Não há ciclos de dependência. A tarefa de implementação depende dos cenários de regressão.
