<!-- spectro-managed: true -->
# Plano: Implementação V3 do C# Performance Analyzer

## Contexto e objetivo técnico

- A especificação V3 foi clarificada e não possui bloqueios. A decisão `Q-V3-001` limita `PERF011` a padrões estruturais reconhecidos; `Q-V3-002` define construtores complexos, quantidade relevante de propriedades/dados e recriação com os mesmos valores como sinais para `PERF030`, sem limiar numérico fixo; `Q-V3-003` exige exemplos completos, benchmarks representativos e os gates existentes.
- O projeto já possui `RuleDefinition`, `RuleCatalog`, `RuleAnalyzerBase`, `RuleAnalysis`, catálogos explícitos V1/V2 e um harness Roslyn determinístico. Os IDs V3 já estão catalogados, mas ainda não há `V3AnalyzerCatalog`, analyzers V3 ou testes específicos.
- Objetivo técnico: habilitar exatamente `PERF011` e `PERF030` como analyzers executáveis, mantendo V1/V2 inalteradas, a análise concorrente segura e a distinção entre risco estrutural e impacto medido.

## Solução proposta

- Criar `Perf011Analyzer` e `Perf030Analyzer`, um por diagnóstico, derivados de `RuleAnalyzerBase`.
- Criar `V3AnalyzerCatalog` com os dois analyzers na mesma ordem de `RuleCatalog.V3`; adicionar `RuleCatalog.V3` sem incluir V3 nos catálogos V1/V2.
- `PERF011`: analisar somente padrões estruturais explicitamente reconhecidos no código e no modelo semântico, como referências de longa duração em campos/coleções/cache/eventos/delegates quando o vínculo e a ausência de limite, expiração, remoção ou descarte forem observáveis. O analyzer deve omitir a suspeita quando a propriedade, transferência, escopo delimitado ou retenção necessária forem plausíveis e não demonstráveis.
- `PERF030`: localizar criações repetidas em loops ou caminhos de repetição estrutural e avaliar sinais de custo/invariância: construtor com inicialização complexa observável, conjunto relevante de propriedades/dados inicializados e argumentos/valores repetidos. A regra não deve usar quantidade numérica fixa de propriedades nem afirmar que o tipo é universalmente pesado.
- Reutilizar `RuleAnalysis` somente para predicados compartilhados por ambas as regras ou por regras futuras; manter estado por compilação fora de serviços estáticos e preservar `EnableConcurrentExecution`.
- Manter metadados, severidade, confiança, limitações e referência no `RuleCatalog`; `PERF011` continua com severidade `Critical` e confiança baixa, e `PERF030` com severidade `High` e confiança baixa conforme o catálogo atual.
- Não criar Code Fix automático. As mensagens permanecem condicionais e o analyzer não executa profiling, telemetria, persistência ou chamadas externas.

## Arquitetura e contratos

### Componentes de produção

- `src/StrictSharp.Analyzers/Analyzers/Perf011Analyzer.cs`: detecção estrutural de retenção potencial e reporte no local que melhor representa a referência ou vínculo retentor.
- `src/StrictSharp.Analyzers/Analyzers/Perf030Analyzer.cs`: detecção de criação repetida com sinais observáveis de inicialização custosa/invariância.
- `src/StrictSharp.Analyzers/Analyzers/V3AnalyzerCatalog.cs`: catálogo explícito dos dois analyzers V3.
- `src/StrictSharp.Analyzers/Rules/RuleCatalog.cs`: exposição ordenada de `V3`, preservando os 30 IDs e as listas V1/V2 existentes.
- `src/StrictSharp.Analyzers/Infrastructure/RuleAnalysis.cs`: extensões estáticas somente quando a lógica for comum, pequena e semanticamente comprovável; não transformar o serviço em um grafo de análise global.

### Contrato de análise

- Entrada: árvore C#, `SemanticModel`, símbolos e operações Roslyn disponíveis nos callbacks registrados pelo analyzer.
- Saída: zero ou mais diagnósticos com exatamente um ID suportado pelo analyzer correspondente; cada analyzer expõe um único descriptor.
- Código gerado: permanece excluído por `RuleAnalyzerBase`.
- Configuração: severidade e supressão continuam delegadas ao Roslyn/`.editorconfig`; não haverá configuração proprietária de limiares.
- Concorrência: callbacks devem ser reentrantes e sem estado mutável compartilhado, pois `EnableConcurrentExecution` permanece habilitado.
- Segurança: nenhum código, literal, segredo ou dado de produção deixa o processo de análise.

### Fluxo de decisão

1. Aplicar filtro sintático barato para selecionar criações, atribuições, inscrições, adições a coleções, campos e caminhos de repetição candidatos.
2. Consultar o modelo semântico apenas após o filtro, resolver símbolos/tipos e confirmar que o padrão pertence a um caso documentado.
3. Procurar evidências de exceção: propriedade transferida, remoção/expiração, descarte, estado variável, configuração dependente da iteração, falta de thread safety ou isolamento por operação.
4. Reportar somente quando a evidência positiva superar a incerteza; caso contrário, omitir o diagnóstico e preservar baixo ruído.

## Alternativas consideradas

- **Análise geral de reachability ou memory leak:** rejeitada; excede o contrato estático aprovado, exigiria análise interprocedural/dinâmica e contradiz `CON-004`.
- **Lista de tipos “pesados” com limiar fixo de propriedades ou custo:** rejeitada; não há medição de runtime nem limiar numérico aprovado. Os sinais devem ser estruturais e conservadores.
- **Marcar toda criação em loop como `PERF030`:** rejeitada; duplicaria `PERF007`/`PERF021` e produziria ruído sem demonstrar invariância ou inicialização relevante.
- **Adicionar um analyzer único para as duas regras:** rejeitada; viola a convenção de um analyzer por diagnóstico e reduz a rastreabilidade do contrato.
- **Code Fix para mover instâncias, limpar referências ou alterar escopo:** rejeitada; propriedade, ciclo de vida, thread safety e semântica não podem ser demonstrados genericamente.
- **Dependência externa para profiling ou benchmark:** rejeitada na implementação; validações comparativas devem ser reproduzíveis sem alterar o runtime do analyzer.

## Riscos e compatibilidade

- **Falso positivo em retenção intencional:** mitigar com padrões reconhecidos, evidência de ausência de limite/expiração/remoção e testes para cache delimitado, singleton, transferência e descarte externo.
- **Falso positivo em objeto “pesado”:** mitigar exigindo repetição, sinais de inicialização e invariância observáveis; não usar contagem numérica fixa nem classificar toda classe com muitas propriedades.
- **Sobreposição com regras existentes:** `PERF011` deve acrescentar contexto de ciclo de vida além de `PERF007`, `PERF008`, `PERF010` e `PERF028`; `PERF030` deve acrescentar invariância/custo de reconstrução além de `PERF007` e `PERF021`.
- **Fluxo interprocedural incompleto:** omitir quando propriedade, mutabilidade, descarte ou configuração não forem demonstráveis, em vez de inferir contratos.
- **Custo do analyzer:** mitigar com filtros sintáticos, consultas semânticas condicionais, ausência de análise global e revisão de alocações; aplicar os gates existentes, sem novo limiar quantitativo.
- **Compatibilidade:** preservar `netstandard2.1`, C# 14, Roslyn 5.0 e o harness em `net10.0`; não adicionar dependências.

## Estratégia de testes

- Criar testes comportamentais isolados para cada analyzer, com caso positivo, negativo e exceções relevantes.
- `PERF011`: cobrir retenção estrutural reconhecida, retenção delimitada/expirada, transferência de propriedade, remoção explícita, descarte externo e código sem vínculo retentor.
- `PERF030`: cobrir criação repetida com construtor complexo, inicialização de múltiplas propriedades, mesmos argumentos/valores, configuração dependente da iteração, estado variável, tipo não thread-safe e instância criada fora da repetição.
- Criar `V3TestMatrixTests` para verificar exatamente os dois IDs, um descriptor por analyzer, ordem igual a `RuleCatalog.V3`, código neutro sem diagnósticos e compatibilidade com C# 14.
- Expandir `RuleCatalogTests` para confirmar listas 15/13/2, fases, metadados e severidades V3; reutilizar `RuleConfigurationTests` para supressão e severidade configurável.
- Manter regressão dos testes V1/V2 e executar a suíte sem habilitar V3 nos catálogos anteriores.
- Registrar benchmark comparativo representativo para cada regra, limitado a demonstrar o cenário de regressão da hipótese; o benchmark não deve ser usado para alterar a heurística por limiar de runtime.
- Executar `dotnet build StrictSharp.sln`, `dotnet test StrictSharp.sln` e os comandos de build, test, coverage e security definidos em `.spectro/config.yaml`.

## Rastreabilidade

| Requisito | Critério de aceite | Tarefa |
|---|---|---|
| `FR-001`, `CON-001`, `CON-003` | `AC-001` | `TASK-001`, `TASK-007` |
| `FR-002`, `FR-003` | `AC-002` | `TASK-002` |
| `FR-004`, `CON-004` | `AC-003` | `TASK-002`, `TASK-006` |
| `FR-005`, `FR-006` | `AC-004` | `TASK-003` |
| `FR-007` | `AC-005` | `TASK-003`, `TASK-006` |
| `FR-008`, `FR-009` | `AC-006` | `TASK-001`, `TASK-002`, `TASK-003`, `TASK-006` |
| `FR-010` | `AC-007` | `TASK-001`, `TASK-007` |
| `FR-011` | `AC-008` | `TASK-004`, `TASK-005`, `TASK-006`, `TASK-007` |
| `NFR-001`, `NFR-002`, `NFR-003`, `NFR-004`, `NFR-005` | `AC-009` | `TASK-001`–`TASK-007` |
| `NFR-006` | `AC-010` | `TASK-005`, `TASK-007` |
| `NFR-007` | `AC-011` | `TASK-006`, `TASK-007` |
| `CON-002`, `CON-005`, `CON-006`, `CON-007` | `AC-001`, `AC-005`, `AC-006`, `AC-009` | `TASK-001`, `TASK-003`, `TASK-006`, `TASK-007` |

## Ordem de execução

1. `TASK-001` — fundação, catálogo V3 e contratos compartilhados.
2. `TASK-002` — analyzer e testes de `PERF011`.
3. `TASK-003` — analyzer e testes de `PERF030`.
4. `TASK-004` — matriz V3, catálogo e configuração.
5. `TASK-005` — benchmarks e documentação funcional.
6. `TASK-006` — revisão de segurança, custo e não sobreposição.
7. `TASK-007` — build, testes e gates finais.

Não há dependências cíclicas. As tarefas 002 e 003 dependem da fundação e podem ser executadas em paralelo; as tarefas 004–007 dependem dos analyzers e seus testes.

## Estado

- Estado do plano: `ready_for_implementation`.
- Aprovação adicional necessária: aprovação humana para iniciar a implementação, conforme `require_human_approval_for_scope_changes` e os estágios configurados pelo projeto.
