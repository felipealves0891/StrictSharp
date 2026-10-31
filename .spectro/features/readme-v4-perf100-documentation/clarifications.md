<!-- spectro-managed: true -->
# Esclarecimentos: Documentação da V4 e da regra PERF100 no README

## CL-001 — Contagem atual do catálogo

- **Severidade:** Major
- **Pergunta:** A inclusão da V4 deve atualizar a contagem geral do catálogo?
- **Decisão:** Sim. O README deve informar 31 diagnósticos: 15 em V1, 13 em V2, 2 em V3 e 1 em V4 (`PERF100`).
- **Racional:** `RuleCatalog` contém `PERF001`–`PERF030` e `PERF100`; os catálogos de fase confirmam a distribuição 15 + 13 + 2 + 1.
- **Requisitos afetados:** `FR-001`, `AC-001`.
- **Data:** 2026-10-30.

## CL-002 — Contrato documental de PERF100

- **Severidade:** Major
- **Pergunta:** Quais metadados e limites devem ser apresentados no README?
- **Decisão:** Documentar `PERF100` como `Custo interprocedural propagado`, categoria `Performance`, severidade `High`, confiança `Média`, fase V4 e orientação condicional. Explicar dimensões dinâmicas da evidência e preservar incerteza de alvos não resolvidos.
- **Racional:** Os valores estão definidos em `RuleCatalog.cs` e detalhados em `docs/csharp-performance-analyzer-v4.md`; a categoria fixa não deve ser confundida com as dimensões por ocorrência.
- **Requisitos afetados:** `FR-002` a `FR-006`, `CON-001`, `CON-004`.
- **Data:** 2026-10-30.

## CL-003 — Escopo de validação

- **Severidade:** Minor
- **Pergunta:** A feature deve executar os gates Spectro?
- **Decisão:** Não. A feature altera somente documentação; a implementação poderá usar validação proporcional de texto, links e regressão, sem executar os gates configurados como requisito da feature.
- **Racional:** A decisão segue o escopo explicitamente documental e não altera código, testes ou empacotamento.
- **Requisitos afetados:** seção de escopo excluído, `CON-002`.
- **Data:** 2026-10-30.

## Resultado

- Não há ambiguidades bloqueantes remanescentes.
- A especificação está pronta para planejamento.

