<!-- spectro-managed: true -->
# Evidência de validação V3

## Hipóteses

- `PERF011`: adicionar repetidamente elementos a uma coleção mantida em campo, sem remoção/limpeza observável, pode aumentar a memória viva ao longo da execução.
- `PERF030`: reconstruir em loop uma instância com argumentos invariantes e inicialização não trivial repete custo de CPU e alocação; reutilização pode reduzir esse custo quando o contrato permitir.

## Cenários comparativos reproduzíveis

Os cenários comparam uma implementação que repete a retenção/reconstrução com uma implementação que limita a retenção ou reutiliza uma instância equivalente. O harness isolado está em `benchmarks/StrictSharp.Analyzers.V3.Benchmarks` e não é referenciado pelo analyzer, pela solução ou pelos gates.

Execução reproduzível com BenchmarkDotNet `0.15.8` registrada em `evidence/benchmarkdotnet-20261029T151000Z/`:

- Comando: `$env:NuGetAudit="false"; dotnet run --no-build -c Release --project benchmarks/StrictSharp.Analyzers.V3.Benchmarks/StrictSharp.Analyzers.V3.Benchmarks.csproj -- --filter "*Perf*Benchmarks*" --job short --iterationCount 3 --warmupCount 1 --launchCount 1`.
- Artefatos: `BenchmarkDotNet-run.log`, `perf011-live-memory.log`, `results/Perf011Benchmarks-report-full.json`, `results/Perf030Benchmarks-report-full.json`, CSV, Markdown e HTML.
- Ambiente: BenchmarkDotNet `0.15.8`, `net10.0`, runtime `10.0.12`, Windows 11 `10.0.26300.0`, SDK `10.0.401`, configuração `Release`.
- `PERF011`: retenção teve memória viva pós-GC entre `1,774,864` e `9,067,400` bytes, crescendo entre iterações; o controle limitado ficou entre `393,288` e `399,680` bytes. O BDN mediu médias de `747.967 us/op` e `613.700 us/op`, respectivamente.
- `PERF030`: reconstrução teve média de `1.497 ms/op` e `1,520,000 B/op` contra `607.859 us/op` e nenhuma alocação mensurável no cenário reutilizado.

Os números são evidência destes cenários, não limiares do analyzer. A saída também preserva parâmetros, métricas por repetição e limitações.

## Evidência estática disponível

- Testes `Perf011AnalyzerTests` cobrem diagnóstico positivo, coleção local e limpeza explícita.
- Testes `Perf030AnalyzerTests` cobrem diagnóstico positivo, argumentos dependentes da iteração e criação fora do loop.
- `V3TestMatrixTests` confirma IDs, ordem, neutralidade, compatibilidade C# 14 e separação dos catálogos V1/V2.

## Limitação

`BenchmarkDotNet` é dependência exclusiva do harness e não é referenciado pelo analyzer, testes ou solução principal. A rodada é sintética e curta, portanto não representa produção nem prova memory leak; o BDN reportou `Unknown processor` e falha de permissão ao consultar `Win32_Processor`, além de avisos de controle de power plan. O restore do projeto gerado exigiu `NuGetAudit=false` por indisponibilidade ambiental do endpoint de auditoria; o gate de segurança do repositório permanece separado e aprovado. GC, JIT, CPU e sistema operacional podem alterar os resultados. O diagnóstico permanece baseado apenas em evidência estática.
