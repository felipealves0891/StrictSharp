# Revisão: Modelo de custo computacional V5

## Resumo

**Decisão:** `changes_requested`.

A revisão independente confirmou que o HEAD `d1c43178b4fc814079c51606bf9bf45b2c02ff8d` corresponde à revisão de código registrada pelos gates `gate-20261031T151134Z`. Os quatro gates obrigatórios passaram, e a validação de rastreabilidade não encontrou requisitos ou critérios de aceite sem vínculo. Ainda há uma falha alta que pode lançar exceção durante a análise e uma forma comum de comparação que `PERF107` não cobre. O status de `TASK-010` e das tarefas de revisão também continua inconsistente com a evidência atual.

## Artefatos examinados

- `spec.md`, `clarifications.md`, `plan.md`, `tasks.md`, `.spectro/constitution.md` e `.spectro/config.yaml`.
- `gates.json` e evidências da execução `gate-20261031T151134Z`.
- Pacote independente `reviews/review-v5-post-gates-20261031T151500Z/`, diff contra `origin/main` e manifesto.
- Implementação V5, grafo interprocedural, analyzers, testes e documentação.

## Consistência da evidência

- O gate e o pacote de revisão identificam o mesmo HEAD: `d1c43178b4fc814079c51606bf9bf45b2c02ff8d`.
- Build, testes (290), segurança e coverage passaram. Coverage: 96,80% de linhas, 85,47% de branches, 95,99% de linhas novas e 85,35% de branches novos.
- `validate_traceability.ps1` retornou `complete`: nenhum requisito ou critério de aceite descoberto.
- Durante esta revisão, foram adicionadas tarefas de remediação a `tasks.md`; o hash desse artefato mudou depois do pacote e dos gates. O pacote preserva o estado examinado; gates devem ser renovados após implementar as correções.

## Requisitos verificados

- O catálogo V5 publica `PERF101`–`PERF107` e mantém a separação entre os analyzers e os serviços de inferência.
- A contagem exata de loops agora restringe comparador e incremento a formas demonstráveis; há testes negativos para limites inclusivos e passos não unitários.
- A comparação `PERF107` cobre diferenças de cabeçalho de loops e evita percentual ou benchmark.
- A evidência desconhecida e as limitações de I/O, concorrência e payload permanecem explícitas.
- A suíte e os gates foram executados localmente, sem envio de código ou dados a serviços externos.

## Achados

### FINDING-009 — `PERF107` ignora loops usados diretamente como ramos

- **Severity:** Medium
- **Category:** Functional
- **Location:** `src/StrictSharp.Analyzers/Analyzers/Perf107Analyzer.cs:32-35`; `src/StrictSharp.Analyzers/V5/LoopCostInference.cs:16-17`
- **Evidence:** `HaveComparableLoopEffects` inclui o próprio nó com `DescendantNodesAndSelf`, mas `LoopCostInference.Analyze` procura loops somente em `DescendantNodes()`. Quando `if (flag) for (...) ... else for (...) ...` usa os loops diretamente como statements, ambos os resultados ficam vazios e o analyzer retorna sem comparar. A cobertura positiva existente usa blocos em torno dos loops.
- **Impact:** Uma forma válida e comum de alternativas com contagens diferentes não recebe o diagnóstico público `PERF107`, apesar de os limites e efeitos serem comparáveis.
- **Recommendation:** Incluir o nó raiz quando ele próprio for um loop e adicionar teste comportamental com os dois loops diretamente nos ramos.
- **Return stage:** `implement`
- **Remediation task:** `TASK-REV-009`.

### FINDING-010 — Overflow no payload total escapa como exceção do analyzer

- **Severity:** High
- **Category:** Functional
- **Location:** `src/StrictSharp.Analyzers/V5/AllocationPayloadInference.cs:25-27`
- **Evidence:** A multiplicação `checked(payload.Bytes * allocationCount.Value)` não trata `OverflowException`. Por exemplo, um array `decimal[1000000000]` alocado em um loop conhecido de 1.000.000.000 iterações produz valores individualmente representáveis, mas o produto excede `long.MaxValue`.
- **Impact:** Ao analisar esse código, o serviço pode lançar uma exceção em vez de preservar o valor como desconhecido; um analyzer que falha pode gerar `AD0001` e interromper a análise do arquivo/projeto.
- **Recommendation:** Detectar overflow antes da multiplicação ou capturar `OverflowException` e usar `PayloadEstimate.Unknown`; adicionar teste de regressão com produto total fora do intervalo.
- **Return stage:** `implement`
- **Remediation task:** `TASK-REV-010`.

### FINDING-008 — Status das tarefas não acompanha os gates aprovados

- **Severity:** Medium
- **Category:** Consistency
- **Location:** `tasks.md:15, 29-31, 89-122`
- **Evidence:** `TASK-010` ainda está `blocked` pelo gate antigo `gate-20261031T150004Z`, embora `gate-20261031T151134Z` tenha passado. `TASK-REV-006` e `TASK-REV-007` permanecem `running` após as correções e a aprovação dos gates; `TASK-REV-008` continua `pending`.
- **Impact:** O registro de conclusão da implementação diverge da evidência de gates, prejudicando a trilha de auditoria e mantendo trabalho marcado como bloqueado sem refletir o estado atual.
- **Recommendation:** Sincronizar `TASK-010` e as tarefas REV-006/007 com o resultado atual e renovar os gates após atualizar `tasks.md` e concluir as correções pendentes.
- **Return stage:** `plan`
- **Remediation task:** `TASK-REV-008` (já existente).

## Riscos residuais

- A especificação exige custo aceitável em IDE/build, mas não há gate de benchmark ou medição de desempenho V5 configurado. O custo repetido de construir o grafo por método/analyzer permanece sem medição; isso é um risco, não uma regressão medida.
- A estimativa de payload dá suporte a um conjunto limitado de arrays e tipos primitivos; buffers e formas multidimensionais não reconhecidos devem permanecer explicitamente desconhecidos.
- Limites de entrada, comportamento do runtime e chamadas externas continuam fora do que a análise estática pode medir.

## Decisão final

`changes_requested`. `FINDING-010` é High e bloqueia aprovação. Corrigir os achados `FINDING-009` e `FINDING-010` na implementação, sincronizar as tarefas e executar novamente os gates antes de uma nova revisão final.
