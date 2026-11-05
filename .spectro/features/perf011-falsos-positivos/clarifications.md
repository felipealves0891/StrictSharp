<!-- spectro-managed: true -->
# Clarificações: Redução de falsos positivos na PERF011

| ID | Dúvida | Classificação | Impacto | Decisão | Fundamentação | Requisitos afetados | Data |
|---|---|---|---|---|---|---|---|
| `Q-PERF011-001` | Um limite conhecido apenas por convenção de uma API externa deve ser tratado como evidência suficiente para omitir a `PERF011`? | Minor | Pode alterar poucos casos de classificação quando o limite não aparece no código analisado. | Não. Convenção externa, sem evidência no código ou configuração/anotação explícita, não basta para silenciar o diagnóstico. | A constituição exige evidência suficiente, comportamento determinístico e tratamento explícito do que não puder ser resolvido. A documentação V5 também mantém limites de chamadas externas não resolvidas como desconhecidos ou condicionais. | `FR-003`, `FR-005`, `CON-003`, `AC-003`, `AC-005` | 2026-11-04 |

## Resultado

- Bloqueios restantes: 0
- Questões major restantes: 0
- Questões minor restantes: 0; a única questão foi consolidada como assunção conservadora.
- Estado: `completed`
