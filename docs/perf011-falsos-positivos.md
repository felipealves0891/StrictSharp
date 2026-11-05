# Casos de falso positivo na PERF011

A `PERF011` trata retenção potencial, não prova a existência de memory leak. A análise deve ser conservadora quando o ciclo de vida, o owner ou o limite da coleção forem demonstravelmente delimitados.

## Casos que não devem gerar suspeita

- A instância possui ciclo de vida curto e a coleção é limpa no descarte administrado pelo owner, por exemplo em `Dispose`.
- Um owner criado localmente é usado apenas durante a operação e não escapa para retorno, campo, closure ou chamada cujo contrato seja desconhecido.
- Um owner local é passado a uma API não resolvida: como a duração dessa transferência não pode ser demonstrada, a regra permanece silenciosa.
- A inserção está protegida por uma guarda explícita que preserva um limite efetivo, como `Count < limite` no caminho analisado.
- O receptor é uma propriedade ou uma abstração como `IEnumerable<T>` cuja implementação e ownership não podem ser resolvidos com segurança.
- A operação tem apenas nome homônimo (`Add` ou `Subscribe`), mas o tipo não é uma coleção/observável retentor reconhecido semanticamente.

## Limites e capacidade

`new(n)` em `List<T>` informa capacidade inicial e não impede crescimento posterior. Portanto, a capacidade fornecida ao construtor não é, sozinha, motivo para silenciar ou emitir a `PERF011`; o analyzer precisa de evidência do limite efetivo no uso.

Limites conhecidos apenas por convenção de uma API externa permanecem desconhecidos. Eles só podem ser usados para omitir o diagnóstico quando houver evidência no código analisado ou configuração/anotação explícita aprovada.

Casos ambíguos continuam silenciosos ou condicionais. Crescimento de coleção e custo de alocação pertencem a regras próprias quando houver evidência suficiente.

## Ownership e escape

Quando um owner local é transferido para um método estático, campo estático ou outro vínculo reconhecido como duradouro, a análise preserva a suspeita de retenção potencial. Isso não prova vazamento nem substitui a verificação do contrato de ownership; apenas evita classificar como transitório um objeto que deixou o escopo local.

A análise é local ao método. Caller, cache, evento, delegate ou API externa sem vínculo resolvido não são tratados como prova de segurança ou de vazamento: nesses casos, a `PERF011` omite a suspeita para reduzir ruído.
