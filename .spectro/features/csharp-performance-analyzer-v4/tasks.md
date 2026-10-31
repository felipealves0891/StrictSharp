<!-- spectro-managed: true -->
# Tarefas: Análise interprocedural V4 do C# Performance Analyzer

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Status |
|---|---|---|---|---|---|
| `TASK-001` | Adicionar fase V4, registrar `PERF100` com `RiskSeverity.High`/`DiagnosticSeverity.Warning` e categoria fixa `Performance`, criar `RuleCatalog.V4` e `V4AnalyzerCatalog`, preservando 30 IDs e catálogos V1–V3. | `FR-001`, `FR-011`, `NFR-003`, `NFR-005`, `CON-003`, `CON-004`, `CON-006` | `AC-001`, `AC-010`, `AC-012` | — | completed |
| `TASK-002` | Implementar núcleo de evidência e grafo sob demanda, com símbolos, arestas, estados desconhecidos, memoização local e detecção de ciclos, separado da integração Roslyn. | `FR-002`, `FR-008`, `NFR-001`, `NFR-002`, `NFR-003`, `NFR-004`, `CON-001`, `CON-002` | `AC-002`, `AC-008`, `AC-012` | `TASK-001` | completed |
| `TASK-003` | Implementar resolução direta/indireta conhecida, interfaces, virtual dispatch, delegates, recursão e distinção de `await`, caminhos síncronos e bloqueios, sem limites numéricos próprios. | `FR-002`, `FR-005`, `FR-007`, `FR-008`, `NFR-001`, `NFR-002`, `CON-002` | `AC-002`, `AC-005`, `AC-007`, `AC-008`, `AC-012` | `TASK-002` | completed |
| `TASK-004` | Implementar propagação para operações custosas em loops, por item, alocações, materializações, trabalho redundante e invariância entre métodos. | `FR-003`, `FR-004`, `FR-006`, `NFR-001`, `NFR-002`, `NFR-004` | `AC-003`, `AC-004`, `AC-006`, `AC-012` | `TASK-002`, `TASK-003` | completed |
| `TASK-005` | Criar `Perf100Analyzer` como integração fina do núcleo, reportar evidência agregada, deduplicar diagnósticos locais sem contexto adicional e manter configuração Roslyn individual. | `FR-001`, `FR-009`, `FR-010`, `FR-011`, `NFR-003`, `NFR-004`, `CON-003`, `CON-005` | `AC-001`, `AC-009`, `AC-010`, `AC-012` | `TASK-003`, `TASK-004` | completed |
| `TASK-006` | Criar testes do núcleo, testes comportamentais de `PERF100`, casos positivos/negativos/exceções de todas as capacidades, dispatch desconhecido, recursão, async/bloqueio e regressão V1–V3. | `FR-002`–`FR-012`, `NFR-001`–`NFR-005`, `CON-002`, `CON-003` | `AC-002`–`AC-012` | `TASK-005` | completed |
| `TASK-007` | Atualizar documentação, exemplos, limitações e rastreabilidade; produzir benchmark isolado somente se a severidade aprovada caracterizar `PERF100` como diagnóstico de maior impacto. | `FR-009`, `FR-010`, `FR-012`, `NFR-006`, `CON-003`, `CON-006` | `AC-009`, `AC-011`, `AC-013` | `TASK-006` | completed |
| `TASK-008` | Executar build, testes, cobertura, segurança, revisão de custo/deduplicação e validação final da matriz requisito–aceite–tarefa, preservando evidências. | `FR-001`–`FR-012`, `NFR-001`–`NFR-006`, `CON-001`–`CON-006` | `AC-001`–`AC-013` | `TASK-006`, `TASK-007` | completed |

## Regras de execução

- Não implementar profiling, telemetria, persistência, rede, Code Fix ou dependência nova no analyzer.
- Não alterar V1–V3 nem tratar corpo ausente, alvo externo, interface, virtual dispatch ou delegate não resolvido como custo zero.
- Não criar limites numéricos de profundidade, alvos ou tamanho do grafo nesta fase; usar análise sob demanda, memoização, ciclos, cancelamento e gates.
- Manter um analyzer por diagnóstico, serviços estáticos por padrão, execução concorrente segura e estado local à compilação.
- `PERF100` só deve reportar quando o contexto interprocedural acrescentar evidência; não duplicar regra local sem valor adicional.
- Toda tarefa deve incluir testes ou documentação de validação correspondente e preservar mensagens condicionais, sem frequência, ganho percentual ou complexidade efetiva inventados.
- Benchmarks, caso exigidos pelos metadados de impacto, devem ser isolados do projeto do analyzer e não alterar a heurística estática.

## Progresso

- Concluídas: `TASK-001`, `TASK-002`, `TASK-003`, `TASK-004`, `TASK-005`, `TASK-006`, `TASK-007`, `TASK-008`.
- Bloqueadas: nenhuma.
- Próxima tarefa: nenhuma.

## Detalhamento das tarefas principais

### TASK-001 — Contrato e catálogo V4

- **Requisitos:** `FR-001`, `FR-011`, `NFR-003`, `NFR-005`, `CON-003`, `CON-004`, `CON-006`.
- **Critérios:** `AC-001`, `AC-010`, `AC-012`.
- **Componentes afetados:** `RulePhase`, `RuleCatalog`, `RuleDefinition`, `V4AnalyzerCatalog`, testes de catálogo/configuração.
- **Conclusão:** `PERF100` aparece uma vez, com fase V4, severidade alta exibida como warning e categoria fixa `Performance`; V1–V3 permanecem inalteradas.
- **Validação:** testes de catálogo, fase, ordem, configuração e compilação do novo catálogo.
- **Risco:** quebrar contagens/ordem dos IDs existentes; mitigar com regressão explícita.

### TASK-002 — Núcleo de grafo e evidência

- **Requisitos:** `FR-002`, `FR-008`, `NFR-001`, `NFR-002`, `NFR-003`, `NFR-004`, `CON-001`, `CON-002`.
- **Critérios:** `AC-002`, `AC-008`, `AC-012`.
- **Componentes afetados:** novo namespace de análise interprocedural e testes unitários do núcleo.
- **Dependências:** `TASK-001`.
- **Conclusão:** representa arestas conhecidas/desconhecidas, memoiza por símbolo, detecta ciclos e retorna evidência sem emitir diagnóstico.
- **Validação:** testes de símbolo único, ausência de corpo, múltiplos alvos, ciclo e reentrada concorrente.
- **Risco:** alocações/travessias repetidas; mitigar com estruturas locais e testes de custo.

### TASK-003 — Resolução e fluxos

- **Requisitos:** `FR-002`, `FR-005`, `FR-007`, `FR-008`, `NFR-001`, `NFR-002`, `CON-002`.
- **Critérios:** `AC-002`, `AC-005`, `AC-007`, `AC-008`, `AC-012`.
- **Componentes afetados:** resolvedor de chamadas, classificação de `await`/bloqueio e testes semânticos.
- **Dependências:** `TASK-002`.
- **Conclusão:** chamadas diretas, alvos conhecidos, recursão e async/bloqueio têm estados observáveis; desconhecidos não viram custo zero.
- **Validação:** fontes com interface, override, delegate, método externo, ciclo recursivo, `await`, `.Wait`/`.Result` e fronteiras desconhecidas.
- **Risco:** explosão de alvos; mitigar por deduplicação e cancelamento, sem limite numérico de produto.

### TASK-004 — Propagação de custo

- **Requisitos:** `FR-003`, `FR-004`, `FR-006`, `NFR-001`, `NFR-002`, `NFR-004`.
- **Critérios:** `AC-003`, `AC-004`, `AC-006`, `AC-012`.
- **Componentes afetados:** propagador de contexto, detectores de loop/alocação/materialização/redundância e testes.
- **Dependências:** `TASK-002`, `TASK-003`.
- **Conclusão:** relaciona operação custosa ao loop do chamador e carrega caminho de evidência para alocação/materialização e redundância.
- **Validação:** cadeia de três métodos, loop externo, mutação/variação entre chamadas, materialização e trabalho invariável.
- **Risco:** associar contexto incorreto; validar locais, símbolos de iteração e dependências de dados.

### TASK-005 — Integração do diagnóstico

- **Requisitos:** `FR-001`, `FR-009`, `FR-010`, `FR-011`, `NFR-003`, `NFR-004`, `CON-003`, `CON-005`.
- **Critérios:** `AC-001`, `AC-009`, `AC-010`, `AC-012`.
- **Componentes afetados:** `Perf100Analyzer`, `V4AnalyzerCatalog`, `RuleAnalysis` somente se houver predicado comum comprovado.
- **Dependências:** `TASK-003`, `TASK-004`.
- **Conclusão:** reporta cadeia, repetição, operação, confiança e limitações; não emite duplicata sem contexto adicional.
- **Validação:** mensagem/metadados, configuração `.editorconfig`, localização e deduplicação contra regras existentes.
- **Risco:** mensagem genérica ou múltiplos reports; agrupar evidências por localização/caminho.

### TASK-006 — Cobertura comportamental

- **Requisitos:** `FR-002`–`FR-012`, `NFR-001`–`NFR-005`, `CON-002`, `CON-003`.
- **Critérios:** `AC-002`–`AC-012`.
- **Componentes afetados:** testes do núcleo, `Perf100AnalyzerTests`, `V4TestMatrixTests`, catálogo/configuração e regressão.
- **Dependências:** `TASK-005`.
- **Conclusão:** todas as capacidades V4 têm cenários positivos, negativos, exceções e limitações; V1–V3 permanecem estáveis.
- **Validação:** `dotnet test`, matriz C# 14/net10.0 e catálogo completo.
- **Risco:** cobertura sintática esconder falhas de resolução; combinar comportamento com símbolos reais e alvos ausentes.

### TASK-007 — Documentação e evidência

- **Requisitos:** `FR-009`, `FR-010`, `FR-012`, `NFR-006`, `CON-003`, `CON-006`.
- **Critérios:** `AC-009`, `AC-011`, `AC-013`.
- **Componentes afetados:** documentação V4, exemplos, referências e benchmark isolado condicional.
- **Dependências:** `TASK-006`.
- **Conclusão:** documentação descreve `PERF100`, desconhecidos, exemplos, exceções e limitações; benchmark, se exigido, é separado.
- **Validação:** revisão de rastreabilidade e inspeção de artefatos sem dados externos/sensíveis.
- **Risco:** documentar comportamento não implementado; comparar cada exemplo com teste.

### TASK-008 — Gates e fechamento

- **Requisitos:** `FR-001`–`FR-012`, `NFR-001`–`NFR-006`, `CON-001`–`CON-006`.
- **Critérios:** `AC-001`–`AC-013`.
- **Componentes afetados:** solução, testes, `.spectro`, evidências e revisão final.
- **Dependências:** `TASK-006`, `TASK-007`.
- **Conclusão:** build, testes, segurança, cobertura, custo, deduplicação e rastreabilidade passam com evidências correspondentes.
- **Validação:** gates configurados, revisão de diff e confirmação de ausência de dependências/calls externos.
- **Risco:** evidência invalidada por mudança posterior; renovar gates quando hashes/configuração mudarem.

## TASK-REV-001 - Remediate FINDING-001

- **Status:** pending
- **Finding:** FINDING-001
- **Severity:** High
- **Return stage:** gates
- **Description:** Versionar CoverageGapTests.cs e rerodar os gates para que a evidência corresponda à revisão registrada.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Add or update behavior-focused coverage as required.

## TASK-REV-002 - Remediate FINDING-002

- **Status:** pending
- **Finding:** FINDING-002
- **Severity:** Medium
- **Return stage:** implement
- **Description:** Atualizar evidence-v4-validation.md com os testes, métricas e execution ID da execução aprovada atual.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Add or update behavior-focused coverage as required.

## TASK-REV-003 - Remediate FINDING-003

- **Status:** pending
- **Finding:** FINDING-003
- **Severity:** Medium
- **Return stage:** gates
- **Description:** Corrigir a validação de rastreabilidade para reconhecer a tabela e os headings TASK usados nos artefatos e rerodar a validação.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Add or update behavior-focused coverage as required.

## TASK-REV-004 - Remediate FINDING-004

- **Status:** pending
- **Finding:** FINDING-004
- **Severity:** Low
- **Return stage:** implement
- **Description:** Simplificar ou reorganizar a detecção de reentrada do grafo e preservar teste de ciclo.
- **Dependencies:** none
- **Completion criteria:** Finding is resolved and relevant gates/review evidence are renewed.
- **Expected tests:** Add or update behavior-focused coverage as required.

