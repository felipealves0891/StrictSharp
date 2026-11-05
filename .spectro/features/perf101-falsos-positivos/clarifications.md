<!-- spectro-managed: true -->
# Clarificações: Redução de falsos positivos na PERF101

| ID | Dúvida | Classificação | Impacto | Decisão | Fundamentação | Requisitos afetados | Data |
|---|---|---|---|---|---|---|---|
| `Q-PERF101-001` | Qual forma mínima de evidência deve ser considerada “possibilidade de melhoria comprovada” para permitir um warning? | Blocking | Define quais padrões podem gerar diagnóstico e pode alterar a lógica e os testes da regra. | Exigir padrão repetido, chave estável, alternativa estrutural semanticamente compatível e vantagem demonstrável, como busca linear repetida com índice compatível. | A fonte exige melhoria comprovada; a decisão adota o critério conservador sugerido para separar oportunidade demonstrável de hipótese de otimização. | `FR-002`, `FR-003`, `NFR-001`, `NFR-003`, `CON-004`, `AC-003`, `AC-004` | 2026-11-05 |
| `Q-PERF101-002` | Quando houver classificação `O(n)` sem oportunidade de melhoria demonstrável, a regra deve expor a classificação como informação não acionável ou permanecer completamente silenciosa? | Blocking | Define o contrato observável da `PERF101`, a severidade emitida e os testes de ausência/presença de diagnóstico. | Permanecer completamente silenciosa. A classificação sem oportunidade demonstrável não deve produzir diagnóstico público. | A decisão reduz ruído e mantém o diagnóstico reservado a casos acionáveis, sem transformar complexidade necessária em warning. | `FR-001`, `FR-004`, `NFR-001`, `AC-001`, `AC-005` | 2026-11-05 |

## Resultado

- Bloqueios restantes: 0
- Questões major restantes: 0
- Questões minor restantes: 0
- Estado: `completed`
