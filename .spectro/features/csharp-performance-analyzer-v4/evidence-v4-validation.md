<!-- spectro-managed: true -->
# Evidência de validação V4

Data: 2026-10-31

## Escopo validado

- `PERF100` está registrado na fase V4, com categoria fixa `Performance` e severidade `RiskSeverity.High`, exibida como warning pelo Roslyn.
- O núcleo representa chamadas conhecidas/desconhecidas, detecta ciclos, classifica fluxos síncronos, `await` e bloqueios e preserva corpos externos como desconhecidos.
- A propagação cobre repetição em loops, alocações, materializações e chamadas redundantes em contexto repetido.
- O analyzer agrega evidências no mesmo ponto e evita múltiplos diagnósticos equivalentes.

## Comandos executados

- `dotnet test tests/StrictSharp.Analyzers.Tests/StrictSharp.Analyzers.Tests.csproj --no-restore --filter FullyQualifiedName~RuleCatalogTests`
- `dotnet test tests/StrictSharp.Analyzers.Tests/StrictSharp.Analyzers.Tests.csproj --no-restore --filter FullyQualifiedName~InterproceduralGraphTests`
- `dotnet test tests/StrictSharp.Analyzers.Tests/StrictSharp.Analyzers.Tests.csproj --no-restore --filter FullyQualifiedName~InterproceduralEvidenceTests`
- `dotnet test tests/StrictSharp.Analyzers.Tests/StrictSharp.Analyzers.Tests.csproj --no-restore --filter "FullyQualifiedName~Perf100AnalyzerTests|FullyQualifiedName~V4TestMatrixTests"`
- `dotnet test tests/StrictSharp.Analyzers.Tests/StrictSharp.Analyzers.Tests.csproj --no-restore`
- `node .codex/skills/spectro-gates/scripts/run_gates.mjs --feature-id csharp-performance-analyzer-v4`

## Resultados

- Catálogo: 13 testes aprovados.
- Grafo interprocedural: 6 testes aprovados.
- Evidência propagada: 2 testes aprovados.
- Integração/matriz V4: 4 testes aprovados.
- Suíte completa: 204 testes aprovados, 0 falhas, 0 ignorados.

## Benchmark comparativo

- Projeto: `benchmarks/StrictSharp.Analyzers.V4.Benchmarks`.
- Comando: `STRICTSHARP_BENCHMARK_ARTIFACTS=.spectro/features/csharp-performance-analyzer-v4/benchmark-artifacts dotnet run --project benchmarks/StrictSharp.Analyzers.V4.Benchmarks/StrictSharp.Analyzers.V4.Benchmarks.csproj --no-build -c Release -- --filter '*Perf100Benchmarks*' --job Dry`.
- Resultado: cenário repetido `661.7 us` e `72,000 B` alocados; cenário reutilizado `779.7 us` e `72 B` alocados.
- Interpretação: o cenário demonstra diferença de alocação para a carga sintética, mas a execução `Dry` tem uma única iteração e não serve para declarar ganho de CPU ou comportamento universal.
- Artefatos: `.spectro/features/csharp-performance-analyzer-v4/benchmark-artifacts/results/Perf100Benchmarks-report-full.json` e relatório CSV/Markdown/HTML no mesmo diretório.

## Limitações observadas

- Não há limites numéricos próprios de profundidade, alvos ou grafo nesta fase; a análise usa memoização, detecção de ciclos, cancelamento e gates.
- Interfaces e delegates sem corpo alvo resolvido permanecem desconhecidos e não geram custo zero implícito.
- O resultado é evidência estática; não mede frequência, CPU, memória, GC, I/O ou ganho percentual em runtime.
- O benchmark é uma evidência comparativa sintética e não altera a heurística estática nem cria limiar para `PERF100`.

## Gates finais

- Execução oficial: `gate-20261031T011418Z`, revisão `fc94d24a364dbcd1b194a07e9330091bcdd8f409`.
- Build: aprovado com `dotnet build --no-restore -v d`.
- Testes: aprovado com 204 testes, 0 falhas e 0 ignorados.
- Segurança: aprovado; o projeto `StrictSharp.Analyzers` não possui pacote vulnerável nas fontes NuGet consultadas.
- Cobertura: aprovada em árvore limpa; linhas `97,57%`, branches `86,13%`, linhas novas `96,88%` e branches novos `87,64%`.
- Evidências: `.spectro/features/csharp-performance-analyzer-v4/outputs/gate-20261031T011418Z/evidence/`.
