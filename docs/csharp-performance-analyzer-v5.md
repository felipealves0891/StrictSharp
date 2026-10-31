# Custo computacional V5

A V5 do StrictSharp classifica evidências estruturais de custo no código C#. Ela não executa a aplicação, não faz profiling e não promete ganho percentual ou impacto medido em produção.

## Diagnósticos

| ID | Dimensão | Severidade | Escopo |
|---|---|---|---|
| `PERF101` | Complexidade | High | Classifica loops e ordenações observáveis como `O(1)`, `O(n)`, `O(n log n)`, `O(n²)` ou desconhecido. |
| `PERF102` | Alocação | High | Identifica alocações que podem ser multiplicadas por loops constantes. |
| `PERF103` | Memória | High | Estima payload de arrays e buffers quando elemento e tamanho são conhecidos. |
| `PERF104` | Repetição | High | Expõe multiplicadores estruturais de loops e chamadas locais conhecidas. |
| `PERF105` | I/O | High | Identifica operações externas potencialmente repetidas. |
| `PERF106` | Concorrência | High | Identifica criação/agendamento estrutural de tarefas. |
| `PERF107` | Performance transversal | High | Compara alternativas por Big-O ou contagens estruturais demonstráveis. |

## Exemplo de payload

```csharp
for (var i = 0; i < 1_000; i++)
{
    var buffer = new byte[100_000];
    Process(buffer);
}
```

Quando os limites forem resolvidos estaticamente, a evidência pode informar 1.000 alocações e 100.000.000 bytes de payload, aproximadamente 95,4 MiB. Cabeçalhos, alinhamento, metadados, sobrevivência, memória viva, GC e pausas não estão incluídos.

## Limites

- Limites dinâmicos de loops, entradas desconhecidas, dispatch, corpos ausentes e chamadas externas não resolvidas permanecem desconhecidos ou condicionais.
- I/O não informa duração, latência, disponibilidade ou volume real transferido.
- Concorrência não informa capacidade do ThreadPool, simultaneidade efetiva, throughput ou tamanho de fila em runtime.
- `O(n²)` só é emitido para dois níveis estruturais suportados; formas mais profundas ou ambíguas ficam desconhecidas.
- A comparação pública informa diferenças estruturais, sem percentual de ganho, custo monetário ou benchmark implícito.
- A análise é determinística e local: nenhum código-fonte, segredo ou dado de produção é enviado a serviço externo.

## Validação

Os testes V5 cobrem casos positivos, negativos e de exceção para catálogo, complexidade, multiplicadores, payload, I/O, concorrência, desconhecidos e comparação. A base aprovada é analyzer `netstandard2.1`, testes `net10.0`, C# 14 e Roslyn 5.0.0.
