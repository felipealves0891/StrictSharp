<!-- spectro-managed: true -->
# Plano: Modelo de custo computacional V5 do C# Performance Analyzer

## Contexto e objetivo técnico

A V5 deve transformar evidências estáticas já observáveis no código e na análise interprocedural em um modelo determinístico de custo, sem confundir Big-O, contagens de payload ou multiplicadores estruturais com medições de runtime. A primeira entrega publicará `PERF101`–`PERF107`, todos com severidade `High`, cobrindo as dimensões de complexidade, alocação, memória, repetição, I/O e concorrência conforme a documentação V5.

O plano preserva a base aprovada do projeto: analyzer `netstandard2.1`, testes `net10.0`, C# 14, Roslyn 5.0.0 e `Microsoft.CodeAnalysis.Analyzers` 3.11.0. Não haverá profiling, serviço externo, benchmark embutido nem alteração funcional das regras V1–V4.

## Solução proposta

1. Evoluir o contrato de fases e o catálogo para representar `RulePhase.V5`, `RuleCatalog.V5` e `V5AnalyzerCatalog`, com sete regras `PERF101`–`PERF107`, severidade `RiskSeverity.High` e categorias derivadas das dimensões documentadas. O mapeamento inicial será `PERF101` Complexidade, `PERF102` Alocação, `PERF103` Memória, `PERF104` Repetição, `PERF105` I/O, `PERF106` Concorrência e `PERF107` Performance transversal para comparação pública.
2. Criar um núcleo estático de evidências V5, separado da integração Roslyn, com tipos por valor quando apropriado e estruturas imutáveis para:
   - complexidade (`O(1)`, `O(n)`, `O(n log n)`, `O(n²)` ou desconhecida);
   - alocações e payload conhecido, mantendo cabeçalhos/alinhamento separados;
   - multiplicadores de loops e chamadas;
   - operações externas e tarefas concorrentes;
   - confiança, premissas, limitações e estado desconhecido.
3. Reutilizar o grafo interprocedural V4 quando houver alvo e corpo resolvidos, mas não tratar corpo ausente, dispatch, entrada, I/O ou runtime desconhecidos como custo zero.
4. Implementar um analyzer por diagnóstico V5. Cada analyzer consumirá somente as evidências/dimensões relevantes, fará deduplicação local e emitirá mensagem pública com evidência, premissas, multiplicadores, confiança, impacto, limitações e orientação condicional.
5. Adicionar uma API pública de comparação entre alternativas limitada a diferenças estruturais demonstráveis; ela não produzirá percentuais, custo monetário ou resultado de benchmark.

O agrupamento das sete regras usa uma regra transversal para a comparação pública (`PERF107`) e seis regras diretamente alinhadas às dimensões do documento. Nenhuma regra criará score agregado: `PERF107` comparará apenas diferenças estruturais demonstráveis e explicitará as dimensões comparadas.

## Arquitetura e contratos

### Componentes a criar ou modificar

- `src/StrictSharp.Analyzers/Rules/RuleDefinition.cs`: adicionar `RulePhase.V5`, `EnabledInV5` e preservar o mapeamento de `High` para `DiagnosticSeverity.Warning`.
- `src/StrictSharp.Analyzers/Rules/RuleCatalog.cs`: registrar `PERF101`–`PERF107`, referências V5, metadados, categorias e `RuleCatalog.V5`; não alterar IDs, severidades ou contratos V1–V4.
- `src/StrictSharp.Analyzers/Analyzers/V5AnalyzerCatalog.cs`: expor os sete analyzers, um por diagnóstico.
- `src/StrictSharp.Analyzers/Analyzers/Perf101Analyzer.cs` até `Perf107Analyzer.cs`: integrar callbacks Roslyn ao núcleo de evidências, mantendo a lógica de análise fora dos analyzers.
- `src/StrictSharp.Analyzers/V5/` (ou módulo equivalente): tipos de dimensão, classificação Big-O, contadores de payload/multiplicadores, comparação e análise estática. A escolha final de nomes deve seguir os namespaces e convenções atuais.
- `src/StrictSharp.Analyzers/Interprocedural/`: adaptar somente os contratos necessários para fornecer chamadas, ciclos, corpos ausentes e operações conhecidas ao núcleo V5; manter a semântica V4 intacta.
- `docs/csharp-performance-analyzer-v5.md`: documentar cada diagnóstico, dimensões, exemplos, desconhecidos, limites e comparação pública.
- `tests/StrictSharp.Analyzers.Tests/`: testes de contrato do catálogo, matriz V5, serviços de inferência, analyzers individuais, deduplicação, configuração e regressão V1–V4.

### Contrato de evidência

O resultado interno deve ser estruturado por dimensão, nunca reduzido a um score único de unidades incompatíveis. Cada evidência deve carregar, quando aplicável:

- classificação assintótica e justificativa estrutural;
- contagem mínima, máxima conhecida ou desconhecida;
- payload de bytes conhecido/estimável e indicação separada de cabeçalho/alinhamento desconhecidos;
- multiplicadores de loop/chamada, incluindo recursão e fronteiras não resolvidas;
- operação externa ou tarefa concorrente identificada;
- confiança, premissas, impacto potencial e limitações.

Valores desconhecidos devem ser representados explicitamente. Uma operação sem corpo ou alvo resolvido pode participar da evidência como fronteira desconhecida, mas não como custo zero.

### Fluxo de dados

`SyntaxNodeAnalysisContext`/símbolos Roslyn → coleta de estrutura local → enriquecimento opcional pelo grafo interprocedural V4 → inferência V5 por dimensão → deduplicação por localização/regra/dimensão → `Diagnostic.Create` com mensagem estruturada.

### Erros, cancelamento e concorrência

- Propagar `CancellationToken` em toda travessia de árvore, símbolo, grafo e inferência.
- Interromper análise em símbolos inválidos, corpos ausentes ou dispatch não resolvido com evidência desconhecida, sem exceção para o usuário.
- Não compartilhar estado mutável entre invocações concorrentes do analyzer; caches devem ser locais, imutáveis ou explicitamente seguros.
- Respeitar os mecanismos gerais de cancelamento/timeout do Roslyn e os gates do projeto; não introduzir um limite numérico arbitrário de iterações Big-O.
- Para I/O e concorrência, não definir limiar numérico nesta fase: quantidades estruturalmente deriváveis são reportadas; as demais ficam desconhecidas/condicionais.

### Persistência e observabilidade

Não há persistência nem serviço externo. A observabilidade pública é o diagnóstico Roslyn e a documentação. Evidências de teste e, quando aplicável, benchmarks externos serão armazenadas nos artefatos Spectro, sem enviar código ou dados sensíveis.

## Alternativas consideradas

- Reutilizar apenas `PERF100`: rejeitado; V5 possui sete contratos públicos aprovados e precisa distinguir custo computacional estático de propagação interprocedural V4.
- Criar um score numérico agregado: rejeitado; combina unidades incompatíveis e aumenta falsa precisão.
- Implementar toda a lógica dentro de cada analyzer: rejeitado; viola a separação arquitetural e aumenta duplicação/alocações.
- Definir limites numéricos de I/O/concorrência agora: rejeitado; a decisão aprovada deixa esses valores sem limiar fixo, com desconhecido explícito.
- Executar profiling ou benchmarks durante a análise: rejeitado; está fora do escopo V5 e quebraria determinismo/privacidade.

## Riscos e compatibilidade

- Inferência Big-O pode gerar falso positivo em limites dinâmicos, recursão, LINQ e dispatch; mitigar exigindo evidência estrutural e reduzindo para desconhecido.
- O grafo interprocedural pode crescer combinatoriamente; mitigar com memoização, detecção de ciclos, cancelamento e medição nos gates, sem inventar precisão.
- `PERF101`–`PERF107` podem duplicar V1–V4; mitigar com deduplicação por regra/dimensão e testes que comprovem valor contextual adicional.
- Payload não representa custo total de objeto, GC, LOH ou memória viva; mensagens devem declarar essa limitação.
- Mudanças no catálogo podem quebrar consumidores e testes de contagem; manter contratos anteriores e atualizar a matriz de fases de forma explícita.
- A comparação pública pode ser interpretada como recomendação de performance; limitar o contrato a diferenças estruturais demonstráveis e declarar que validação experimental permanece externa.

## Estratégia de testes

- Testes unitários de classificação Big-O para loops constantes, lineares, aninhados, ordenação típica, recursão e desconhecidos.
- Testes de payload para arrays/buffers constantes, multiplicação por iteração e distinção entre payload e cabeçalho/alinhamento.
- Testes de propagação interprocedural para chamadas conhecidas, chamadas em loops, chamadas repetidas, ciclos, corpo ausente e dispatch não resolvido.
- Testes de I/O e concorrência para quantidades estruturais conhecidas e fronteiras sem limiar numérico.
- Testes públicos de comparação entre alternativas sem percentual ou benchmark inventado.
- Testes de catálogo/matriz garantindo exatamente `PERF101`–`PERF107`, severidade Warning, fase V5, categorias válidas e um analyzer por diagnóstico.
- Testes positivos, negativos e de exceção para cada regra, incluindo deduplicação, configuração e silêncio em código neutro.
- Regressão V1–V4: suíte existente sem alteração de diagnósticos, IDs, severidades ou mensagens contratadas.
- Gates do projeto: build, testes, segurança e cobertura; custo do analyzer deve ser verificado nos gates aplicáveis.

## Validação dos requisitos

Cada `FR` e `NFR` será validado por testes de serviço/analyzer, testes de catálogo/matriz, documentação e gates. Em particular, `AC-003` deve verificar 1.000 alocações e 100.000.000 bytes de payload (aprox. 95,4 MiB), sem incluir cabeçalhos/alinhamento; `AC-008` deve comprovar dimensões separadas; `AC-009` deve comprovar comparação pública sem percentual; `AC-015` deve ser atendido por análise somente local e revisão de dependências.

## Rastreabilidade

| Requisito | Critério de aceite | Tarefa |
|---|---|---|
| `FR-001`, `FR-012`, `CON-006` | `AC-001`, `AC-011` | `TASK-001`, `TASK-009` |
| `FR-002`, `FR-004`, `FR-011`, `CON-005` | `AC-002`, `AC-004`, `AC-010` | `TASK-003`, `TASK-004`, `TASK-006` |
| `FR-003`, `FR-005` | `AC-003`, `AC-005` | `TASK-005` |
| `FR-006`, `FR-007` | `AC-006`, `AC-007` | `TASK-006` |
| `FR-008`, `FR-010` | `AC-008`, `AC-010` | `TASK-003`, `TASK-006`, `TASK-007` |
| `FR-009` | `AC-009` | `TASK-007` |
| `FR-013`, `CON-007`, `CON-008` | `AC-012` | `TASK-008`, `TASK-009` |
| `NFR-001`, `NFR-002` | `AC-013` | `TASK-003`, `TASK-006`, `TASK-009` |
| `NFR-003` | `AC-014` | `TASK-010` |
| `NFR-004`, `NFR-005`, `NFR-006` | `AC-014` | `TASK-001`, `TASK-002`, `TASK-006`, `TASK-008` |
| `NFR-007` | `AC-015` | `TASK-010` |
