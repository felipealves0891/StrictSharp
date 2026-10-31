<!-- spectro-managed: true -->
# Esclarecimentos: Correção de falso positivo do PERF013 em strings

## Decisões

- `D-001` — Pergunta: a correção deve excluir todo receptor `string` da análise do `PERF013` ou somente evitar falso positivo para buscas de texto, como `string.Contains`, preservando enumerações reais de caracteres? **Decisão:** excluir da classificação de enumeração as APIs próprias de busca de texto em `string`; preservar a análise de operações cobertas pelo `PERF013` que realmente enumerem caracteres. **Racional:** o exemplo fornecido usa buscas por terminadores de linha, que são operações de texto; uma exclusão total de strings poderia esconder uma enumeração real, contrariando a preservação de precisão e baixo ruído prevista na constituição e na especificação existente de `PERF013`. **Requisitos afetados:** `FR-001`, `FR-002`, `FR-003`, `AC-001` a `AC-004`. **Data:** 2026-10-31.

## Questões remanescentes

- Nenhuma questão bloqueante, major ou minor identificada.
