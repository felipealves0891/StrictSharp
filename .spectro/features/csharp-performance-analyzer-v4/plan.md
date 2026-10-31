<!-- spectro-managed: true -->
# Plano: Análise interprocedural V4 do C# Performance Analyzer

## Contexto e objetivo técnico

- A especificação V4 está esclarecida: `PERF100` foi aprovado, não há limites numéricos próprios nesta fase e a primeira entrega inclui todas as capacidades interprocedurais descritas.
- O projeto usa analyzers individuais derivados de `RuleAnalyzerBase`, catálogos explícitos por fase, `RuleCatalog` como contrato de metadados e testes Roslyn com `AnalyzerTestHarness`.
- O objetivo é analisar relações entre métodos, propagar evidências de custo e emitir `PERF100` apenas quando a cadeia de chamadas e o padrão custoso forem observáveis.
- A implementação não altera regras V1–V3, não coleta dados de runtime e não adiciona dependência ao analyzer.

## Solução proposta

- Criar um núcleo de análise interprocedural separado do callback Roslyn, com serviços estáticos e estruturas locais à compilação.
- Representar métodos como nós identificados por símbolos Roslyn e chamadas como arestas com estado: alvo conhecido, múltiplos alvos conhecidos ou desconhecido.
- Resolver chamadas diretas por `IMethodSymbol`; considerar interfaces, virtual dispatch e delegates somente quando os alvos puderem ser obtidos com evidência. Corpos ausentes e dependências externas permanecem desconhecidos.
- Construir o grafo sob demanda, memoizar resultados por símbolo e detectar ciclos na trilha atual. Não usar profundidade, quantidade de alvos ou tamanho de grafo como limiar funcional.
- Propagar evidência estruturada contendo operação, localização, cadeia de chamadas, repetição, alocação/materialização, recursão, bloqueio/async e confiança.
- Detectar operações custosas em loops, alocações/materializações repetidas, recursão, trabalho redundante/invariante e pontos de bloqueio em caminhos síncronos/assíncronos.
- Integrar o núcleo a `Perf100Analyzer`, um analyzer por diagnóstico, com `RuleCatalog` e `V4AnalyzerCatalog`. A mensagem deve distinguir evidência estática de impacto não medido.
- Preservar cancelamento e concorrência Roslyn; nenhum estado mutável deve ser compartilhado entre compilações ou callbacks.

## Arquitetura e contratos

### Componentes

- `src/StrictSharp.Analyzers/Rules/RuleDefinition.cs`: ampliar `RulePhase` para V4 sem alterar fases anteriores.
- `src/StrictSharp.Analyzers/Rules/RuleCatalog.cs`: adicionar `PERF100` com severidade `High`, categoria fixa `Performance`, `RuleCatalog.V4` e preservar os 30 IDs anteriores.
- `src/StrictSharp.Analyzers/Analyzers/Perf100Analyzer.cs`: selecionar raízes, invocar o núcleo e reportar diagnósticos.
- `src/StrictSharp.Analyzers/Analyzers/V4AnalyzerCatalog.cs`: expor exatamente `Perf100Analyzer`.
- `src/StrictSharp.Analyzers/Interprocedural/`: núcleo separado para grafo, resolução, contexto e evidência.
- `tests/StrictSharp.Analyzers.Tests/`: testes de comportamento, matriz, configuração, deduplicação e regressão.
- `docs/`: documentação do diagnóstico e exemplos.

### Contrato e fluxo de dados

- Entrada: `Compilation`, `SemanticModel`, símbolos Roslyn, sintaxe/operações e cancelamento.
- Saída interna: evidências interprocedurais com estado de resolução e confiança; ciclos e desconhecidos são explícitos.
- Saída pública: diagnóstico `PERF100` em localização representativa, com metadados de `RuleDefinition`; tipos internos do grafo não são expostos.
- Fluxo: selecionar candidatos; resolver símbolo e corpo; registrar alvos/ciclos; propagar contexto de loop/async/bloqueio; agrupar evidências; reportar somente contexto interprocedural adicional.
- Corpo ausente, alvo externo ou dispatch não resolvido não são custo zero.

### Erros, concorrência e observabilidade

- Símbolo ausente, binding ambíguo ou operação não reconhecida reduzem confiança ou encerram a ramificação sem falso custo zero.
- Recursão é registrada como ciclo e nunca causa recursão infinita do analyzer; a profundidade efetiva permanece dependente de execução.
- Serviços são estáticos por padrão; caches são locais à análise e seguros para `EnableConcurrentExecution`.
- Não há persistência, telemetria, rede ou observabilidade externa.

## Alternativas consideradas

- Analisar somente o método atual foi rejeitado porque não propaga custo por chamadas.
- Grafo global eager foi rejeitado por custo; será usada análise sob demanda com memoização.
- Tratar interfaces, delegates e externos como custo zero foi rejeitado por contradizer `CON-002`.
- Limites numéricos fixos foram rejeitados por `Q-V4-002`; ciclos, cancelamento e gates controlam segurança operacional nesta fase.
- Um analyzer por capacidade foi rejeitado para esta entrega: `PERF100` é o diagnóstico interprocedural único, com detectores separados no núcleo.
- Profiling, telemetria e dependência externa foram rejeitados por escopo e segurança; benchmarks, se necessários, ficam isolados.

## Riscos e compatibilidade

- Dispatch pode gerar grafos incompletos ou muitos alvos; mitigar com estados explícitos, memoização, deduplicação e omissão conservadora.
- Sem limites numéricos, soluções grandes podem ter custo elevado; mitigar com análise sob demanda, ciclos, cancelamento e gates, sem inventar limite funcional.
- `PERF100` amplia o contrato além dos 30 IDs; a exceção foi aprovada em `Q-V4-001` e terá testes de catálogo/documentação.
- Propagação pode duplicar `PERF007`, `PERF008`, `PERF013`, `PERF014`, `PERF017`, `PERF021`, `PERF026` ou `PERF029`; exigir contexto interprocedural adicional.
- Async pode ser confundido com bloqueio; distinguir `await`, bloqueio síncrono e desconhecido com testes positivos/negativos.
- Compatibilidade permanece `netstandard2.1`, C# 14, Roslyn 5.0 e suíte `net10.0`, sem dependência nova de produção.

## Estratégia de testes

- Testar o núcleo para resolução direta, múltiplos alvos, desconhecidos, ciclos, memoização, loops, alocação/materialização, redundância, recursão e async/bloqueio.
- Testar `PERF100` com métodos separados, cadeia de chamadas, interface/virtual/delegate conhecido e desconhecido, método externo e corpo ausente.
- Cobrir negativos: chamada única fora de loop, dependência real da iteração, mutação, async sem bloqueio, sincronização intencional, alvo ambíguo e operação sem evidência.
- Verificar exatamente `PERF100`, metadados, fase, configuração individual, um analyzer por diagnóstico e regressão V1–V3.
- Validar segurança sem chamadas externas e sem exposição desnecessária de literais/identificadores.
- Usar benchmark comparativo isolado somente se os metadados aprovados classificarem `PERF100` como diagnóstico de maior impacto; resultados não viram limiar.
- Executar build, testes, segurança, cobertura e revisão de rastreabilidade conforme `.spectro/config.yaml`.

## Rastreabilidade

| Requisito | Critério de aceite | Tarefa |
|---|---|---|
| `FR-001`, `FR-011`, `CON-004` | `AC-001`, `AC-010` | `TASK-001`, `TASK-005`, `TASK-008` |
| `FR-002`, `FR-008` | `AC-002`, `AC-008` | `TASK-002`, `TASK-003`, `TASK-006` |
| `FR-003` | `AC-003` | `TASK-004`, `TASK-006` |
| `FR-004` | `AC-004` | `TASK-004`, `TASK-006` |
| `FR-005` | `AC-005` | `TASK-003`, `TASK-006` |
| `FR-006` | `AC-006` | `TASK-004`, `TASK-006` |
| `FR-007` | `AC-007` | `TASK-003`, `TASK-004`, `TASK-006` |
| `FR-009`, `FR-010` | `AC-009` | `TASK-005`, `TASK-006`, `TASK-007` |
| `FR-012` | `AC-011` | `TASK-007`, `TASK-008` |
| `NFR-001`–`NFR-005` | `AC-012` | `TASK-002`, `TASK-003`, `TASK-005`, `TASK-006`, `TASK-008` |
| `NFR-006` | `AC-013` | `TASK-005`, `TASK-008` |
| `CON-001`–`CON-006` | `AC-001`, `AC-002`, `AC-009`, `AC-012` | `TASK-001`, `TASK-002`, `TASK-005`, `TASK-008` |

## Ordem de execução

1. `TASK-001` — contrato, metadados e catálogo V4.
2. `TASK-002` — evidência e grafo interprocedural.
3. `TASK-003` — resolução, ciclos, recursão e fluxos async/síncronos.
4. `TASK-004` — propagação em loops, alocações, materializações e redundância.
5. `TASK-005` — integração, deduplicação e mensagem `PERF100`.
6. `TASK-006` — testes completos e regressão.
7. `TASK-007` — documentação e evidência comparativa condicional.
8. `TASK-008` — gates, segurança e rastreabilidade final.

Não há dependências cíclicas. `TASK-003` e `TASK-004` dependem de `TASK-002`; testes dependem da integração; documentação e gates dependem do comportamento estabilizado.

## Estado

- Estado do plano: `ready_for_implementation`.
- Aprovação adicional necessária: nenhuma para severidade ou categoria; `PERF100` usa `RiskSeverity.High`/`DiagnosticSeverity.Warning` e categoria fixa `Performance`. Dimensões de custo são dinâmicas na evidência.
