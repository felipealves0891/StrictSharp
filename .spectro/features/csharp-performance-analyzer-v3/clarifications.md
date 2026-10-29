<!-- spectro-managed: true -->
# Clarificações: Implementação V3 do C# Performance Analyzer

| ID | Dúvida | Classificação | Impacto | Decisão | Responsável | Data |
|---|---|---|---|---|---|---|
| `Q-V3-001` | Que evidência deve ser suportada na primeira entrega de `PERF011`? | Blocking | Define a cobertura e o risco de falsos positivos da análise de retenção. | Resolvido: limitar a primeira entrega a padrões estruturais explicitamente reconhecidos, com evidência comprovável; não realizar análise geral de todos os caminhos de alcance. | Usuário/projeto | 2026-10-29 |
| `Q-V3-002` | Como definir “objeto pesado” e invariância para `PERF030`? | Blocking | Define quando a recriação repetida pode ser reportada sem medição de produção. | Resolvido: usar como sinais construtores complexos, quantidade relevante de propriedades/dados inicializados e recriação com os mesmos valores. Não fixar limiar numérico; o planejamento deve definir heurísticas conservadoras e observáveis. | Usuário/projeto | 2026-10-29 |
| `Q-V3-003` | Quais critérios de validação e limite de custo do analyzer aplicar à V3? | Major | Define a evidência necessária para aceitar as duas regras e evitar um gate de performance arbitrário. | Resolvido: exigir exemplos positivos, negativos e de exceção, benchmark comparativo representativo para cada regra e uso dos gates existentes para o custo do analyzer, sem novo limiar quantitativo. | Usuário/projeto | 2026-10-29 |
| `Q-V3-004` | Qual biblioteca deve executar o benchmark comparativo? | Major | Define o mecanismo de medição e a dependência do harness isolado. | Resolvido por solicitação explícita do usuário: refazer o benchmark com `BenchmarkDotNet`, fixando a versão estável `0.15.8`; a dependência fica restrita ao projeto de benchmark e não entra no analyzer nem na solução principal. | Usuário | 2026-10-29 |

## Resultado

- Bloqueios restantes: 0
- Questões major restantes: 0
- Pronto para planejamento: sim
- Estado: `completed`
