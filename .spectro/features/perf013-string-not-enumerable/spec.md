<!-- spectro-managed: true -->
# Especificação: Correção de falso positivo do PERF013 em strings

## Problema e objetivo

- Problema: o `PERF013` identifica enumeração repetida a partir do tipo do receptor. Como `string` implementa `IEnumerable<char>`, operações próprias de texto, como `text.Contains("\n")`, podem ser confundidas com enumeração de uma sequência e gerar um diagnóstico indevido.
- Objetivo: evitar que operações de texto em `string` sejam interpretadas como enumerações repetidas pelo `PERF013`, preservando a detecção de enumerações repetidas em sequências aplicáveis.

## Escopo

### Incluído

- Corrigir a classificação de buscas de texto em que o receptor tem tipo `string` na análise do `PERF013`.
- Cobrir chamadas de busca em texto como `string.Contains(string)`, incluindo o exemplo `text.Contains("\n")`.
- Preservar a detecção de chamadas repetidas sobre sequências `IEnumerable` que não sejam strings.
- Preservar a possibilidade de diagnosticar operações de enumeração explícita de caracteres de uma string quando a operação coberta pelo `PERF013` realmente enumera seus elementos.

### Excluído

- Alterar o propósito, identificador, severidade, mensagem ou configuração do `PERF013`.
- Alterar outros diagnósticos, regras de enumeração ou recomendações gerais sobre operações de string.
- Definir otimizações ou recomendar substituições de API para buscas em texto.

## Requisitos

### Requisitos funcionais

- `FR-001`: A análise do `PERF013` não deve classificar buscas de texto próprias de `string` como enumeração repetida apenas porque `string` implementa `IEnumerable<char>`.
- `FR-002`: A análise deve continuar identificando enumeração repetida para sequências `IEnumerable` aplicáveis que não sejam strings.
- `FR-003`: O comportamento corrigido deve contemplar chamadas de busca de texto sobre strings, incluindo as sobrecargas de `Contains` e buscas por terminadores de linha.

### Requisitos não funcionais

- `NFR-001`: A correção deve reduzir falso positivos sem degradar a precisão da detecção de enumeração repetida em sequências.

### Restrições

- `CON-001`: O diagnóstico continua sendo `PERF013` e conserva seu contrato e configuração existentes.

## Critérios de aceite

- `AC-001` (`FR-001`, `FR-003`): Dado código que chama `text.Contains("\r")` e `text.Contains("\n")` na mesma expressão, em que `text` é `string`, quando analisado pelo `PERF013`, então as buscas por terminadores de linha não são tratadas como enumeração de `IEnumerable` e não geram diagnóstico de enumeração repetida.
- `AC-002` (`FR-001`, `FR-003`): Dado código que chama repetidamente APIs próprias de busca de texto em uma variável `string`, quando analisado pelo `PERF013`, então essas chamadas não geram diagnóstico por serem confundidas com enumeração.
- `AC-003` (`FR-002`, `CON-001`): Dado código que enumera repetidamente a mesma sequência `IEnumerable<T>` não string por operações cobertas pelo `PERF013`, quando analisado, então o diagnóstico `PERF013` continua sendo emitido conforme o comportamento existente.
- `AC-004` (`FR-001`, `FR-002`): Dado código que enumera repetidamente caracteres de uma string por uma operação de enumeração coberta pelo `PERF013`, quando analisado, então a correção das buscas de texto não suprime esse caso de enumeração.
- `AC-005` (`NFR-001`): Dado código com uma única operação sobre string ou uma única enumeração de sequência, quando analisado, então não é emitido diagnóstico de enumeração repetida.

## Riscos, dependências e perguntas abertas

- Risco: uma exclusão excessivamente ampla de operações de `string` pode ocultar uma enumeração de caracteres intencional que a regra pretenda identificar; os casos de teste devem distinguir buscas próprias de texto de operações que realmente enumeram caracteres.
- Dependência: implementação e testes existentes do `PERF013`.
- Pergunta: nenhuma pergunta bloqueante remanescente.

## Estado

- Estado da fase: `clarified`.
- Pronto para esclarecimento: não; as ambiguidades identificadas foram resolvidas.
- Pronto para planejamento: sim.
