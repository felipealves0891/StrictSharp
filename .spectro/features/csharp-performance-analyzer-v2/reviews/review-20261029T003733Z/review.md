# Revisão independente: Implementação V2 do C# Performance Analyzer

## Resumo

Decisão: `changes_requested`.

A implementação contém os 13 analyzers V2, catálogo explícito, testes comportamentais e integração de configuração. Build, segurança e 106 testes passam nas evidências disponíveis. A cobertura exigida ainda não passa: a última execução registrou `New Branch Coverage` de 78,23%, abaixo do mínimo de 85%. Além disso, há regras cuja implementação atual não sustenta integralmente os requisitos de baixo ruído, exceções e contexto definidos na especificação.

## Artefatos examinados

- `spec.md`, `clarifications.md`, `plan.md`, `tasks.md`.
- `.spectro/constitution.md` e `.spectro/config.yaml`.
- `gates.json`, `state.json` e evidências da execução `gate-20261029T003215Z`.
- Review packet `review-20261029T003733Z` e diff contra `origin/develop`.
- Catálogo, infraestrutura, 13 analyzers V2 e testes do projeto.

## Verificações

- Catálogo V2 contém os 13 IDs aprovados, separados do catálogo V1 e dos itens V3.
- `dotnet build --no-restore -v d`: aprovado na execução dos gates.
- `dotnet test --no-restore --no-build`: aprovado; 93 testes na execução dos gates.
- Após a correção de cobertura, a suíte local passou com 106 testes.
- `dotnet list package --vulnerable`: aprovado na execução dos gates.
- Cobertura: line 95,72%, branch 78,23%, new line 95,72%, new branch 78,23%; falha no requisito de new branch 85%.
- O validador automatizado de rastreabilidade retornou gaps para todos os requisitos porque procura seções `## TASK-*`, enquanto `tasks.md` usa títulos `### TASK-*`; o resultado precisa de revisão manual ou correção do validador/artefato.

## Findings

### FINDING-001

- Severity: `High`
- Category: `test`
- Location: `.spectro/features/csharp-performance-analyzer-v2/gates.json`, `GATE-004`
- Evidence: `New Branch Coverage 78,23% (min 85,00%) [FAIL]`.
- Impact: a qualidade mínima exigida para branches novos não foi demonstrada; a feature não está pronta para aprovação.
- Recommendation: completar testes de branches não cobertos, executar novamente o gate oficial e preservar a nova evidência.
- Return stage: `implement`

### FINDING-002

- Severity: `High`
- Category: `functional`
- Location: `src/StrictSharp.Analyzers/Analyzers/Perf016Analyzer.cs:17-23` e `Perf017Analyzer.cs:17-34`
- Evidence: `PERF016` reporta qualquer ordenação dentro de loop sem verificar mutação da fonte ou se a ordenação é necessária; `PERF017` usa apenas nomes de variáveis de `foreach` para decidir invariância e não verifica mutações nem efeitos colaterais.
- Impact: casos permitidos pela especificação podem receber diagnóstico, contrariando `FR-008`, `FR-009`, `NFR-001` e as exceções de `AC-008`/`AC-009`.
- Recommendation: exigir evidência de fonte não mutada e analisar dependência semântica da iteração; adicionar testes de ordenação após mutação e consultas com efeitos observáveis.
- Return stage: `implement`

### FINDING-003

- Severity: `High`
- Category: `functional`
- Location: `src/StrictSharp.Analyzers/Analyzers/Perf028Analyzer.cs:17-29`
- Evidence: o analyzer reporta toda criação de tipo que implementa `IDisposable`/`IAsyncDisposable` fora de `using`, inclusive quando a instância é retornada ou a propriedade é transferida; não há análise de propriedade, descarte explícito, retorno ou gerenciamento externo.
- Impact: falsos positivos em cenários explicitamente preservados por `FR-013`, `CON-004` e `AC-013`, reduzindo confiança do diagnóstico de ciclo de vida.
- Recommendation: limitar o diagnóstico a recursos locais com ciclo de vida observável e adicionar exceções/testes para retorno, atribuição a propriedade, descarte explícito e transferência de propriedade.
- Return stage: `implement`

### FINDING-004

- Severity: `Medium`
- Category: `functional`
- Location: `src/StrictSharp.Analyzers/Analyzers/Perf009Analyzer.cs:18-28` e `Perf021Analyzer.cs:17-23`
- Evidence: `PERF009` só observa inicializadores de variáveis, não argumentos, casts explícitos ou conversões em expressões; `PERF021` reconhece apenas `new Regex` dentro de loops, embora o requisito inclua opções de serialização e metadados/configurações pesadas.
- Impact: cobertura funcional incompleta para `FR-006` e `FR-010`, sem cumprir todo o catálogo documentado.
- Recommendation: ampliar reconhecimento somente para APIs documentadas, com testes de contrato e exceções; manter omissão quando a alocação/cache não puder ser demonstrada.
- Return stage: `implement`

### FINDING-005

- Severity: `Medium`
- Category: `consistency`
- Location: `.spectro/features/csharp-performance-analyzer-v2/tasks.md` e `validate_traceability.ps1`
- Evidence: o validador retornou `uncovered_requirements` para todos os requisitos e `uncovered_acceptance_criteria` para todos os critérios, pois o regex das seções espera `## TASK-*`, enquanto o arquivo usa `### TASK-*`.
- Impact: a rastreabilidade automatizada não pode ser usada como evidência confiável de cobertura da especificação.
- Recommendation: corrigir o parser para aceitar níveis de heading usados pelo template ou normalizar o artefato; executar novamente a validação e anexar o resultado.
- Return stage: `review`

## Riscos residuais

- A análise continua deliberadamente sintática/semântica local e não prova frequência, volume, carga, propriedade externa ou custo de produção.
- A execução de gates foi invalidada pelo recovery após falha de cobertura; os resultados anteriores não constituem aprovação final.
- `V2BranchCoverageTests.cs` está presente no working tree como arquivo não rastreado no momento da revisão; ele não aparece no diff gerado pelo packet até ser incluído no controle de versão.

## Decisão final

`changes_requested`. Retornar a `spectro-implement` para completar cobertura de branches e corrigir os falsos positivos/incompletudes funcionais de maior severidade. Depois, executar `spectro-gates` novamente e repetir a revisão.
