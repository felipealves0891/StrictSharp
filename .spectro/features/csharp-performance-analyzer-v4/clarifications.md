<!-- spectro-managed: true -->
# Clarificações: Análise interprocedural V4

Data da análise: 2026-10-30

## Q-V4-001 — ID e contrato do diagnóstico

- Severidade: `Blocking`
- Pergunta: o exemplo da documentação usa `PERF100`, mas a constituição limita os IDs estáveis a `PERF001`–`PERF030`. Qual contrato deve ser adotado?
- Alternativas:
  - A: reutilizar um ID `PERF001`–`PERF030` reservado ou ampliar explicitamente o catálogo;
  - B: aprovar uma exceção e introduzir `PERF100`.
- Impacto: altera catálogo, documentação, configuração, testes, severidade e rastreabilidade pública.
- Decisão: aprovado o uso de `PERF100` para o diagnóstico interprocedural V4.
- Rationale: a documentação usa esse identificador para o diagnóstico esperado e a decisão humana autoriza uma extensão explícita do catálogo para esta etapa.
- Requisitos afetados: `FR-001`, `FR-011`, `CON-004`, `AC-001`, `AC-010`.

## Q-V4-002 — Limites operacionais da análise

- Severidade: `Blocking`
- Pergunta: quais limites devem ser adotados para profundidade de chamadas, número de alvos indiretos e tamanho do grafo para manter IDE e build aceitáveis?
- Alternativas:
  - A: definir limites numéricos agora;
  - B: deixar os limites para o planejamento, condicionados aos gates de custo do analyzer;
  - C: não impor limites além dos mecanismos normais de cancelamento/timeout.
- Impacto: altera comportamento sob explosão combinatória, custo de análise, confiança e critérios de aceite.
- Decisão: a V4 não terá limites numéricos próprios de profundidade, alvos indiretos ou tamanho do grafo nesta fase. O comportamento continuará sujeito aos mecanismos gerais de cancelamento/timeout e aos gates de custo do analyzer.
- Rationale: evitar fixar limites arbitrários antes de observar o custo real da análise; a ausência de limite específico não autoriza custo ilimitado em execução.
- Requisitos afetados: `FR-002`, `FR-005`, `FR-008`, `NFR-001`, `NFR-002`, `AC-002`, `AC-005`, `AC-008`, `AC-012`.

## Q-V4-003 — Recorte da primeira entrega

- Severidade: `Major`
- Pergunta: quais capacidades V4 devem compor a primeira entrega e quais severidades/categorias terão?
- Alternativas:
  - A: chamadas em loops e propagação de alocações/materializações;
  - B: todas as capacidades listadas na especificação;
  - C: outro recorte definido pelo responsável do produto.
- Impacto: altera escopo implementável, catálogo de diagnósticos, testes e planejamento.
- Decisão: a primeira entrega incluirá todas as capacidades V4 especificadas: chamadas diretas e indiretas conhecidas, propagação em loops, alocações/materializações, recursão, redundância e caminhos síncronos/assíncronos com bloqueios.
- Rationale: resposta humana escolheu o escopo completo da V4. A severidade e a categoria de cada diagnóstico ainda serão propostas no planejamento, pois não estão definidas na documentação-fonte.
- Requisitos afetados: `FR-003`–`FR-009`, `FR-012`, `AC-003`–`AC-011`.

## Resultado

- Estado: `clarified`.
- As decisões bloqueantes foram resolvidas. A definição de severidade/categoria permanece uma decisão de planejamento, não uma ambiguidade de escopo.

## Q-V4-004 — Severidade do PERF100

- Severidade: `Major`
- Pergunta: qual severidade deve ser usada para `PERF100`?
- Decisão: `RiskSeverity.High`, exposta pelo Roslyn como `DiagnosticSeverity.Warning`.
- Rationale: “warning” corresponde ao mapeamento existente de severidade alta no `RuleDefinition`, preservando o contrato de risco do projeto.
- Requisitos afetados: `FR-001`, `FR-009`, `CON-003`, `AC-001`, `AC-009`.

## Q-V4-005 — Categoria do PERF100

- Severidade: `Major`
- Pergunta: a categoria deve variar conforme a ocorrência ou permanecer estável no descritor Roslyn?
- Decisão: usar a categoria fixa `Performance` para `PERF100`. As dimensões identificadas em cada ocorrência — como `CPU`, `Memory`, `I/O`, `Concurrency`, `Recursion` ou `Async` — serão expressas na evidência e na mensagem, não como categoria Roslyn dinâmica.
- Rationale: `DiagnosticDescriptor.Category` é estático no contrato atual; uma categoria fixa preserva o ID e permite classificação detalhada por ocorrência sem fragmentar o catálogo.
- Requisitos afetados: `FR-009`, `FR-010`, `NFR-004`, `AC-009`.
