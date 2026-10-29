# Revisão independente: Implementação V2 do C# Performance Analyzer

## Resumo

Decisão: `changes_requested`.

A suíte atual contém 148 testes e a medição local mais recente atingiu 85,29% de New Branch Coverage. Porém, o `gates.json` versionado ainda registra a execução `gate-20261029T004729Z`, baseada no código `a88375d...`, com 114 testes e New Branch Coverage de 80,76%. Além disso, permanecem lacunas funcionais em `PERF016`/`PERF017` e `PERF016` não reconhece `Sort` conforme o requisito aprovado.

## Artefatos examinados

- `spec.md`, `clarifications.md`, `plan.md` e `tasks.md`.
- `.spectro/constitution.md` e `.spectro/config.yaml`.
- `gates.json`, `state.json` e o pacote `review-20261029T010427Z`.
- Diff contra `origin/develop`, catálogo V1/V2, analyzers, infraestrutura e testes.
- Resultado do `validate_traceability.ps1`: `complete`, sem requisitos ou critérios descobertos.

## Verificações

- Build e testes locais passam; a última suíte registrou 148 testes aprovados.
- Medição local posterior: Line Coverage 98,10%, Branch Coverage 85,29%, New Line Coverage 98,10% e New Branch Coverage 85,29%.
- O relatório oficial armazenado ainda aponta falha: New Branch Coverage 80,76% contra mínimo 85%, e não corresponde ao código atual.
- Rastreabilidade automatizada retornou `complete` para todos os requisitos e critérios.
- Não foram identificadas dependências novas, chamadas externas ou alterações de Code Fix/profiling.

## Findings

### FINDING-001

- Severity: `High`
- Category: `gates`
- Location: `.spectro/features/csharp-performance-analyzer-v2/gates.json`, `GATE-004`
- Evidence: `code_revision` é `a88375da...`, enquanto a revisão atual é `6486ee675b7c6d6b84cc43530ee0633b5c23178a`; o relatório oficial registra 114 testes e New Branch Coverage de 80,76% [FAIL].
- Impact: a aprovação não pode ser baseada em evidência de outra revisão; o gate obrigatório permanece formalmente falho.
- Recommendation: executar novamente `spectro-gates` sobre a revisão atual e substituir a evidência somente se todos os gates passarem.
- Return stage: `gates`

### FINDING-002

- Severity: `High`
- Category: `functional`
- Location: `src/StrictSharp.Analyzers/Infrastructure/RuleAnalysis.cs:160-166`, `Perf016Analyzer.cs:22` e `Perf017Analyzer.cs:26-40`
- Evidence: `IsSourceMutatedInContainingLoop` retorna `false` quando não resolve o símbolo da expressão-fonte. Assim, chamadas como `GetValues().OrderBy(...)` ou `GetValues().Where(...).Count()` continuam sendo diagnosticadas, embora não exista prova estrutural de que a fonte seja invariável. O teste `Perf016IgnoresUnknownSourceAndNonMutatingCalls` confirma esse comportamento ao esperar um diagnóstico para `GetValues().OrderBy(...)`.
- Impact: pode emitir falsos positivos em getters, factories ou fontes dinâmicas; contraria `FR-008`, `FR-009`, `NFR-001` e a decisão `Q-V2-001` de omitir quando a invariância não é demonstrável.
- Recommendation: exigir uma fonte simples com símbolo resolvido e evidência de estabilidade, ou omitir o diagnóstico quando o símbolo não puder ser resolvido; adicionar testes negativos para fontes retornadas por métodos e propriedades potencialmente mutáveis.
- Return stage: `implement`

### FINDING-003

- Severity: `Medium`
- Category: `functional`
- Location: `src/StrictSharp.Analyzers/Analyzers/Perf016Analyzer.cs:17-22`
- Evidence: o conjunto aceito de métodos é somente `OrderBy`, `OrderByDescending`, `ThenBy` e `ThenByDescending`; não há reconhecimento de `List<T>.Sort`, embora `FR-008` e `AC-008` incluam `OrderBy`/`Sort`.
- Impact: o diagnóstico não cobre uma forma aprovada de ordenação repetida, deixando o requisito funcional incompleto.
- Recommendation: reconhecer `Sort` apenas para tipos/ símbolos de coleção documentados e preservar silêncio quando a mutação ou a necessidade da ordenação for observável.
- Return stage: `implement`

## Riscos residuais

- A análise permanece local e não prova frequência, volume, duração ou custo real em produção.
- `PERF028` continua sendo uma suspeita de ciclo de vida, não uma prova completa de vazamento.
- A medição local de cobertura atende ao limite, mas precisa ser capturada pelo runner oficial para validade de gate.

## Decisão final

`changes_requested`. Corrigir as fontes não resolvidas e a cobertura de `Sort` em `spectro-implement`; depois executar `spectro-gates` novamente e repetir a revisão.
