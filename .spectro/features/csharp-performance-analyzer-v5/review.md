<!-- spectro-managed: true -->
# Review: Modelo de custo computacional V5

## Resumo

A revisão confirmou que as remediações anteriores estão presentes, que a rastreabilidade não tem lacunas e que os gates passaram na revisão de código `1517c175ec2592d29f13186cbf1fca630269a337`. Ainda há dois problemas funcionais de severidade alta na inferência de contagens e na comparação de caminhos do `PERF107`. O status de `TASK-010` também não acompanha o gate aprovado.

**Decisão:** `changes_requested`.

## Artefatos examinados

- `spec.md`, `clarifications.md`, `plan.md` e `tasks.md`.
- Relatório `gates.json`, execução `gate-20261031T144107Z`, validada pelo script de gates.
- Snapshot independente `reviews/review-20261031T144831Z/` e diff contra `origin/main`.
- Catálogo V5, inferência de loops, propagação interprocedural, `PERF107`, testes e documentação.
- `.spectro/constitution.md` e `.spectro/config.yaml`.

## Consistência da evidência

- O snapshot e o relatório de gates identificam o mesmo commit de código: `1517c175ec2592d29f13186cbf1fca630269a337`.
- Os quatro gates passaram: build, testes (259), segurança e cobertura. Cobertura: 97,54% de linhas, 85,74% de branches, 97,28% de linhas novas e 85,94% de branches novos.
- A validação de rastreabilidade retornou `complete`, sem requisitos ou critérios de aceite descobertos.
- Após a execução dos gates, esta revisão acrescentou tarefas de remediação a `tasks.md`. Por isso, o hash atual de `tasks.md` difere do hash registrado no gate; o gate continua referente ao mesmo código, mas precisa ser renovado depois que as novas remediações forem implementadas.
- A ferramenta de criação de tarefas precisou de uma conversão explícita para `int` porque o PowerShell promovia o próximo número para `Double`. A correção foi exercitada ao criar as tarefas `TASK-REV-006` a `TASK-REV-008`.

## Requisitos verificados

- O catálogo publica `PERF101`–`PERF107` com severidade `High` e preserva os contratos V1–V4.
- A propagação interprocedural agora transporta multiplicadores conhecidos e mantém limites dinâmicos desconhecidos; testes cobrem os dois casos.
- `PERF107` não compara ramos com corpos diferentes ou referências a símbolos diferentes; há testes positivos e negativos.
- A comparação não informa percentuais nem benchmarks. A suíte e os gates são locais e não adicionam envio de código a serviços externos.

## Achados

### FINDING-006 — Contagens exatas de loops não validam operador nem incremento

- **Severity:** High
- **Category:** Functional / consistency
- **Location:** `src/StrictSharp.Analyzers/V5/LoopCostInference.cs:93-115`
- **Evidence:** `GetKnownCount` usa o início e o literal à direita da condição para retornar `end - start`, mas não restringe o operador da condição nem valida os incrementos. Por exemplo, `for (var i = 0; i <= 5; i++)` informa 5 embora execute 6 iterações; `i += 2` ou `i--` também recebe uma contagem que não representa seu comportamento.
- **Impact:** Os diagnósticos V5 podem publicar contagens e multiplicadores exatos incorretos, contrariando `FR-003`, `FR-004` e `FR-011`.
- **Recommendation:** Derivar a contagem somente para comparadores, inicializadores e incrementos cuja semântica esteja comprovada; retornar desconhecido nos demais casos. Cobrir `<`, `<=`, passos diferentes de um e decrementos.
- **Return stage:** `implement`
- **Remediation task:** `TASK-REV-006`.

### FINDING-007 — `PERF107` ignora efeitos nos cabeçalhos dos loops

- **Severity:** High
- **Category:** Functional / scope
- **Location:** `src/StrictSharp.Analyzers/Analyzers/Perf107Analyzer.cs:62-78`
- **Evidence:** `HaveComparableLoopEffects` compara o texto dos corpos e os símbolos usados nesses corpos, mas não compara inicializadores, condições, incrementos ou a expressão enumerada. Dois ramos com `Process()` no corpo passam pela verificação mesmo se um incremento também chamar `Persist()`.
- **Impact:** A comparação não descreve todo o trabalho executado e pode apresentar ramos com efeitos distintos como alternativas comparáveis, contrariando `FR-009`, `AC-009` e `CON-008`.
- **Recommendation:** Incluir efeitos observáveis de cabeçalho no critério de comparabilidade, ou permanecer em silêncio quando não puder demonstrar que os caminhos são comparáveis. Adicionar um teste negativo com efeito extra no incremento e casos equivalentes para cada forma de loop.
- **Return stage:** `implement`
- **Remediation task:** `TASK-REV-007`.

### FINDING-008 — `TASK-010` diverge do gate aprovado

- **Severity:** Medium
- **Category:** Consistency / scope
- **Location:** `.spectro/features/csharp-performance-analyzer-v5/tasks.md`, linha de `TASK-010`
- **Evidence:** `TASK-010` permanece `blocked` e o progresso diz que falta executar o gate formal após commit, embora `gate-20261031T144107Z` tenha passado. A atualização das tarefas de remediação desta revisão também altera o hash de `tasks.md` desde o gate.
- **Impact:** O status da feature e o registro de tarefas não refletem a evidência atual; a revisão não pode considerar a trilha de auditoria sincronizada.
- **Recommendation:** Após as remediações funcionais, sincronizar o status de `TASK-010` com os gates e renovar a evidência na revisão final.
- **Return stage:** `plan`
- **Remediation task:** `TASK-REV-008`.

## Riscos residuais

- A inferência é estática e limitada; limites de entrada, dispatch, chamadas externas e comportamento de runtime continuam desconhecidos.
- A comparação de caminhos requer evidência de equivalência dos efeitos, incluindo cabeçalhos, antes de ser confiável.
- A evidência de gates precisa ser renovada depois das correções e da atualização final das tarefas.

## Decisão

`changes_requested`. Os achados High `FINDING-006` e `FINDING-007` retornam à implementação. `FINDING-008` requer sincronização do plano e da evidência ao final.

## Atualização de implementação — 2026-10-31

As correções de `FINDING-006` e `FINDING-007` foram implementadas no workspace e os testes focados foram adicionados. A contagem exata agora exige modelo semântico, limites integrais `int`/`long`, comparador estrito e incremento/decremento unitário compatível; as demais formas permanecem desconhecidas. O `PERF107` só compara cabeçalhos de loops compatíveis e evita relatar alternativas com a mesma complexidade e contagem conhecida igual (ou sem diferença demonstrável).

Verificação local de cobertura executada com `-AllowDirtyTree`: 273 testes passaram; cobertura de linhas 96,56%, branches 85,21%, linhas novas 96,90% e branches novos 85,21%. Esse resultado é uma medição local da árvore modificada, não substitui o gate formal. `gates.json` ainda aponta para a revisão `1517c175ec2592d29f13186cbf1fca630269a337`; os gates formais e a revisão final precisam ser renovados após commit. Até lá, `TASK-REV-006` e `TASK-REV-007` permanecem em andamento, e `TASK-010` permanece bloqueada para o gate formal.

## Atualização dos gates — 2026-10-31

A execução formal `gate-20261031T150004Z`, vinculada ao commit `d8932076fc1e7df168cf9898bbc58987f48c0d3d`, passou em build, testes (273) e segurança. A cobertura geral de linhas foi 96,56%, branches 84,99%, linhas novas 95,53% e branches novos 84,35%; o mínimo de branches novos é 85%, portanto `GATE-004` falhou. A recuperação classificou o resultado como `implementation-defect` e bloqueou novas tentativas porque o contador `implementation-defect:GATE-004` excedeu o limite 3 configurado. Depois dessa execução foram adicionados testes e corrigido o comparador de inicializadores de `for`; os testes focados passaram (47), mas essa árvore alterada ainda não possui nova medição de cobertura nem gate formal.
