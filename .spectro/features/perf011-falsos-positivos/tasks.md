<!-- spectro-managed: true -->
# Tarefas: Redução de falsos positivos na PERF011

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Status |
|---|---|---|---|---|---|
| `TASK-001` | Construir a matriz de testes comportamentais para ciclo de vida, tipo, limite e escape da `PERF011`. | `FR-002`, `FR-003`, `FR-006`, `NFR-001` | `AC-002`, `AC-003`, `AC-006`, `AC-007` | Nenhuma | completed |
| `TASK-002` | Implementar o serviço estático de evidência semântica para operação retentora, tipo declarado/implementado, propriedade e limite observável. | `FR-002`, `FR-003`, `FR-005`, `NFR-001`, `NFR-002`, `CON-002`, `CON-003` | `AC-002`, `AC-003`, `AC-005`, `AC-007` | `TASK-001` | completed |
| `TASK-003` | Implementar a classificação local de ciclo de vida, ownership e escape e integrar a decisão ao `Perf011Analyzer`. | `FR-001`, `FR-004`, `FR-005`, `NFR-002`, `CON-001`, `CON-003` | `AC-001`, `AC-004`, `AC-005` | `TASK-002` | completed |
| `TASK-004` | Atualizar documentação da regra e rastreabilidade com exemplos, limitações e distinção entre capacidade e limite. | `FR-006`, `NFR-001`, `CON-004` | `AC-006`, `AC-007` | `TASK-001`, `TASK-002`, `TASK-003` | completed |
| `TASK-005` | Executar validação final, gates e revisão de escopo/custo/compatibilidade. | `NFR-003`, `CON-001`, `CON-002`, `CON-004` | `AC-008` | `TASK-003`, `TASK-004` | completed |

## Detalhes das tarefas

## TASK-001 — Matriz de regressão e fixtures

- Requisitos: `FR-002`, `FR-003`, `FR-006`, `NFR-001`; critérios: `AC-002`, `AC-003`, `AC-006`, `AC-007`.
- Componentes: `tests/StrictSharp.Analyzers.Tests/Perf011AnalyzerTests.cs` e, somente se necessário, utilidades existentes do `AnalyzerTestHarness`.
- Ações: preservar os positivos atuais; adicionar negativos para `IEnumerable<T>` sem operação retentora comprovada, propriedade não demonstrável, `List<T>(n)` como capacidade inicial, métodos homônimos e cenários de limpeza/remoção; adicionar casos positivos com tipos reconhecidos e owner potencialmente duradouro.
- Conclusão: cada caso da especificação possui expectativa explícita de zero ou um diagnóstico `PERF011`, sem alterar produção nesta tarefa.
- Validação: testes direcionados do arquivo; conferir localização e quantidade de diagnósticos.
- Risco: fixtures podem pressupor semântica que Roslyn não resolve no harness; manter fontes autocontidas e usar apenas referências já disponíveis.

## TASK-002 — Evidência semântica e limites

- Requisitos: `FR-002`, `FR-003`, `FR-005`, `NFR-001`, `NFR-002`, `CON-002`, `CON-003`; critérios: `AC-002`, `AC-003`, `AC-005`, `AC-007`.
- Componentes: novo serviço estático em `src/StrictSharp.Analyzers/Retention/` e `src/StrictSharp.Analyzers/Analyzers/Perf011Analyzer.cs`; helpers compartilhados somente quando a reutilização não alterar outras regras.
- Ações: resolver o método/membro por símbolo, aceitar apenas operações e tipos retentores reconhecidos, tratar propriedades com evidência semântica suficiente, distinguir `IEnumerable<T>` de coleções que realmente suportam retenção e representar desconhecido/omitido sem criar estado compartilhado. Implementar limites efetivos observáveis; não tratar capacidade de `List<T>` como limite máximo.
- Conclusão: chamadas arbitrárias `Add`/`Subscribe`, abstrações sem contrato e limites externos não demonstráveis não geram diagnóstico; a integração mantém o contrato público da regra.
- Validação: `Perf011AnalyzerTests` para tipos, propriedades, abstrações, capacidade inicial e métodos homônimos; inspeção de diff para confirmar que `RuleAnalysis.IsRetainingCollection` não foi globalmente enfraquecido.
- Risco: ampliar a lista de tipos por nome recria o falso positivo; exigir símbolos/interfaces e omitir quando o contrato não for resolvido.

## TASK-003 — Ciclo de vida, ownership e escape

- Requisitos: `FR-001`, `FR-004`, `FR-005`, `NFR-002`, `CON-001`, `CON-003`; critérios: `AC-001`, `AC-004`, `AC-005`.
- Componentes: serviço de análise da `TASK-002`, `Perf011Analyzer` e testes de comportamento.
- Ações: adicionar análise local de owner transitório, transferência, limpeza, expiração, closure/delegate, evento, cache ou singleton somente nos padrões reconhecíveis no código analisado. Não inferir vida longa por nome nem provar reachability global. Quando o caller, API externa ou contrato dinâmico for necessário, omitir a suspeita em vez de afirmar segurança ou leak.
- Conclusão: caso demonstravelmente delimitado e sem escape não reporta; owner potencialmente duradouro sem limite/remoção mantém possibilidade de diagnóstico; ambiguidades permanecem silenciosas/condicionais.
- Validação: testes positivos/negativos de owner transitório, ausência de escape, transferência e retenção em owner duradouro; conferir que nenhuma mensagem afirma prova de leak.
- Risco: falso negativo por análise local; registrar a limitação na documentação e não expandir para análise interprocedural geral.

## TASK-004 — Documentação e rastreabilidade

- Requisitos: `FR-006`, `NFR-001`, `CON-004`; critérios: `AC-006`, `AC-007`.
- Componentes: `docs/perf011-falsos-positivos.md`, documentação de `PERF011` já empacotada pelo projeto, `.spectro/features/perf011-falsos-positivos/spec.md`, `plan.md` e `tasks.md`.
- Ações: registrar os casos corrigidos, a diferença entre capacidade inicial e limite efetivo, os limites da análise e os exemplos aprovados; manter o texto alinhado ao comportamento implementado e sem prometer ausência de memory leak.
- Conclusão: cada requisito e critério possui tarefa e validação detectáveis; documentação não transforma hipótese de runtime em regra estática.
- Validação: revisão textual, `git diff --check` e verificação da matriz de rastreabilidade.
- Risco: documentar um padrão não suportado; revisar exemplos contra os testes efetivamente existentes.

## TASK-005 — Gates e encerramento

- Requisitos: `NFR-003`, `CON-001`, `CON-002`, `CON-004`; critério: `AC-008`.
- Componentes: solução, projeto de analyzer, suíte de testes e artefatos `.spectro`.
- Ações: executar build, testes, segurança e cobertura configurados; revisar custo da análise, execução concorrente, ausência de dependências/I/O, catálogo e diff; preservar evidência correspondente à revisão final.
- Conclusão: build/testes/gates passam, nenhum ID ou contrato fora do escopo é alterado e a análise não introduz profiling, reachability global ou chamadas externas.
- Validação: comandos oficiais de `.spectro/config.yaml`, inspeção de status/diff e revisão Spectro posterior quando a implementação terminar.
- Risco: gate de cobertura ou custo falhar por novos ramos; manter testes para todos os ramos adicionados e medir somente com os gates existentes.

## Regras de execução

- Não implementar Code Fix, profiling, telemetria, persistência, rede, dependências novas ou mudanças de catálogo.
- Não tratar `List<T>(n)` como limite máximo; capacidade inicial só pode ser evidência de pré-alocação, não de bounded retention.
- Não fazer análise interprocedural/reachability completa nesta feature. Casos que dependem de caller, API externa ou contrato dinâmico permanecem desconhecidos/omitidos.
- Manter a lógica de análise fora do analyzer, serviços estáticos por padrão e execução concorrente segura.
- Não alterar `PERF011` para diagnóstico de crescimento/alocação; esse comportamento pertence a outra regra ou especificação.
- Executar comandos de build/teste/gates somente durante a implementação ou validação final, não como parte deste planejamento.

## Progresso

- Concluídas: `TASK-001`, `TASK-002`, `TASK-003`, `TASK-004`, `TASK-005`.
- Bloqueadas: nenhuma.
- Próxima tarefa: nenhuma.

## Evidências

- `dotnet test tests/StrictSharp.Analyzers.Tests/StrictSharp.Analyzers.Tests.csproj --no-restore --filter FullyQualifiedName~Perf011AnalyzerTests -v minimal -m:1`: aprovado, 27 testes.
- `dotnet test tests/StrictSharp.Analyzers.Tests/StrictSharp.Analyzers.Tests.csproj --no-restore -v minimal -m:1`: aprovado, 346 testes.
- `dotnet build src/StrictSharp.Analyzers/StrictSharp.Analyzers.csproj --no-restore --warnaserror -v minimal -m:1`: aprovado, 0 avisos e 0 erros.
- `dotnet list src/StrictSharp.Analyzers/StrictSharp.Analyzers.csproj package --vulnerable`: aprovado, nenhum pacote vulnerável.
- `pwsh -NoProfile -ExecutionPolicy Bypass -File .spectro/tools/coverage-gate.ps1 -AllowDirtyTree`: aprovado; 96,43% linhas, 85,66% branches, 95,68% novas linhas e 85,01% novos branches. O modo `-AllowDirtyTree` foi usado somente para a medição antes do commit.

## TASK-REV-001 - Remediate FINDING-001

- **Status:** completed
- **Finding:** FINDING-001
- **Severity:** High
- **Return stage:** implement
- **Description:** Completar a análise local da PERF011 para owner transitório e escapes/transferências reconhecíveis para singleton, cache, closure/delegate, evento ou campo externo; quando o ciclo de vida não puder ser demonstrado, omitir a suspeita. Adicionar testes positivos e negativos para AC-001 e AC-004 e renovar os gates.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Add or update behavior-focused coverage as required.

## Progresso da remediação

- `TASK-REV-001` concluída: owner local sem escape, escape não resolvido e transferência para método estático cobertos por testes comportamentais.
- Gates renovados após a correção; a evidência final está em `gates.json` e no diretório de outputs da execução.

## Remediação de cobertura

- Cobertura adicional para owner local vindo de factory e receptor de campo com raiz não resolvida; os cenários mantêm as decisões comportamentais da especificação e elevaram novos branches acima do mínimo configurado.

