<!-- spectro-managed: true -->
# Clarificações: Implementação V2 do C# Performance Analyzer

| ID | Dúvida | Classificação | Impacto | Decisão | Responsável | Data |
|---|---|---|---|---|---|---|
| `Q-V2-001` | Como a V2 deve agir quando tamanho, duração, frequência ou capacidade não puderem ser provados estaticamente? | Blocking | Define o equilíbrio entre cobertura, falsos positivos e previsibilidade das 13 heurísticas contextuais. | Resolvido: usar somente evidência estrutural comprovável. Não usar limiares numéricos arbitrários; quando o contexto não for demonstrável, reduzir a confiança ou omitir o diagnóstico. | Usuário/projeto | 2026-10-28 |
| `Q-V2-002` | Quais regras devem compor a primeira entrega V2? | Major | Define o escopo de implementação e validação. | Resolvido pela especificação existente: implementar as 13 regras catalogadas como V2. | Documentação do projeto | 2026-10-28 |
| `Q-V2-003` | Quais APIs devem ser reconhecidas na primeira entrega? | Major | Define a cobertura mínima dos padrões de concorrência, regex, serialização, coleções e tratamento de erro. | Resolvido pela documentação existente: usar os exemplos e APIs já citados no catálogo, sem ampliar o contrato para APIs não documentadas durante o planejamento. | Documentação do projeto | 2026-10-28 |
| `Q-V2-004` | Qual limiar numérico deve definir custo aceitável do analyzer em IDE/build? | Major | Define eventual gate quantitativo para o desempenho do analyzer. | Resolvido pela clarificação anterior da feature-mãe: não fixar limiar numérico nesta fase; seguir as orientações oficiais de analyzers Roslyn e os gates existentes. | Clarificação da feature-mãe | 2026-10-28 |

## Resultado

- Bloqueios restantes: 0
- Questões major restantes: 0
- Pronto para planejamento: sim
- Estado: `completed`
