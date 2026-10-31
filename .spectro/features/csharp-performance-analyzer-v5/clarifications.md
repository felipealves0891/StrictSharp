<!-- spectro-managed: true -->
# Clarificações: Modelo de custo computacional V5

Data da análise: 2026-10-31

| ID | Dúvida | Classificação | Impacto | Decisão | Responsável | Data |
|---|---|---|---|---|---|---|
| `Q-V5-001` | Quais versões de C#, .NET, Target Framework e Roslyn a V5 deve suportar formalmente? | Blocking | Define APIs disponíveis, testes, documentação e compatibilidade do modelo. | Resolvido: seguir a base já adotada pelo projeto: analyzer em `netstandard2.1`, testes em `net10.0`, C# `14.0` e Roslyn `5.0.0` (com `Microsoft.CodeAnalysis.Analyzers` `3.11.0`). | Responsável do projeto | 2026-10-31 |
| `Q-V5-002` | A V5 será publicada apenas como evidência compartilhada para regras existentes, como novos diagnósticos, ou nos dois formatos? Se houver novo diagnóstico, qual ID, severidade e categoria devem ser aprovados? | Blocking | Altera catálogo, configuração, contratos públicos, deduplicação, testes e rastreabilidade. `PERF100` já é o diagnóstico aprovado da V4 e não deve ser reutilizado implicitamente para um novo contrato V5. | Resolvido: publicar diagnósticos V5 `PERF101` a `PERF107`, todos com severidade `High`; as categorias seguirão as dimensões da documentação V5 (`Complexity`, `Allocation`, `Memory`, `Repetition`, `I/O` e `Concurrency`), com a divisão exata por regra definida no planejamento. | Responsável do projeto | 2026-10-31 |
| `Q-V5-003` | Quais formas de loop, entrada, I/O e concorrência entram na primeira entrega? | Major | Define o recorte implementável, os limites de inferência, os casos positivos/negativos e o custo do analyzer. | Resolvido: loops serão classificados por complexidade assintótica Big-O, sem limite numérico fixo de iterações; I/O e concorrência não terão limiar numérico pré-definido e permanecerão desconhecidos ou condicionais quando a quantidade estrutural não puder ser derivada. | Responsável do projeto | 2026-10-31 |
| `Q-V5-004` | A comparação entre alternativas terá formato público na primeira entrega ou ficará restrita ao núcleo de evidências? | Major | Define contrato de saída, documentação, testes e risco de expor uma capacidade sem semântica de comparação aprovada. | Resolvido: a comparação entre alternativas terá formato público na primeira entrega, limitada a diferenças estruturais demonstráveis, como Big-O e contagens conhecidas, sem percentual de ganho ou benchmark implícito. | Responsável do projeto | 2026-10-31 |

## Resultado

- Bloqueios restantes: 0
- Questões major restantes: 0
- Pronto para planejamento: sim
- Estado: `clarified`
