# PERF100 — custo interprocedural propagado

`PERF100` é o diagnóstico V4 do StrictSharp para evidências de custo que se propagam entre métodos. Sua categoria fixa é `Performance` e sua severidade padrão é `High`, exibida pelo Roslyn como warning.

## O que é analisado

- chamadas conhecidas entre métodos, incluindo implementações virtuais concretas;
- chamadas dentro de loops cujo método chamado aloca ou materializa dados;
- alocações de objetos/arrays e materializações como `ToList`, `ToArray`, `ToDictionary` e ordenações;
- chamadas redundantes em contexto repetido;
- ciclos recursivos e chamadas bloqueantes em contexto repetido;
- caminhos assíncronos com `await` e distinção de bloqueio síncrono.

Exemplo:

```csharp
void ProcessOrders(List<int> orders)
{
    foreach (var orderId in orders)
        GetItems(orderId);
}

List<Item> GetItems(int orderId)
{
    var buffer = new byte[1024];
    return allItems.Where(item => item.OrderId == orderId).ToList();
}
```

O diagnóstico relaciona o loop em `ProcessOrders` à alocação/materialização em `GetItems` e apresenta as dimensões identificadas na evidência.

## Limitações

`PERF100` não mede frequência real, CPU, memória, GC, I/O, complexidade efetiva ou percentual de ganho. Corpos ausentes, dependências externas, interfaces e delegates sem alvo resolvido permanecem desconhecidos e não são tratados como custo zero.

Não há limites numéricos próprios de profundidade, quantidade de alvos ou tamanho do grafo nesta fase. A análise usa memoização, detecção de ciclos, cancelamento do Roslyn e os gates do projeto.

As dimensões dinâmicas da evidência — por exemplo `CPU`, `Memory`, `I/O`, `Concurrency`, `Recursion` e `Async` — aparecem na mensagem do diagnóstico; a categoria Roslyn permanece `Performance`.
