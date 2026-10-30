# Revisão independente: csharp-performance-analyzer-v3

## Resumo

Decisão: `changes_requested`.

Os gates obrigatórios passaram e a rastreabilidade está completa. A implementação do harness BenchmarkDotNet está isolada do analyzer e da solução principal, mas a evidência ainda não cobre integralmente o contrato de medição aprovado para `PERF011` e `TASK-011`.

## Artefatos examinados

- `spec.md`, `clarifications.md`, `plan.md` e `tasks.md`.
- `gates.json` da execução `gate-20261029T142801Z`.
- Evidências dos gates em `outputs/gate-20261029T142801Z/evidence/`.
- Harness em `benchmarks/StrictSharp.Analyzers.V3.Benchmarks`.
- Evidências BenchmarkDotNet em `evidence/benchmarkdotnet-20261029T133000Z/`.
- `manifest.json`, `diff.patch`, `review-packet.md` e `traceability.json` desta revisão.

## Verificações

- Revisão de código: `4d8fe2203e4170c7fb2216f05b7c7841bd43f008`.
- A revisão e os gates referem-se à mesma revisão de código.
- `validate_traceability.ps1`: `complete`; requisitos descobertos: nenhum; critérios sem validação: nenhum.
- Build, testes, segurança e cobertura: passaram.
- BenchmarkDotNet `0.15.8`: quatro benchmarks executados com sucesso.
- Nenhuma referência ao BenchmarkDotNet foi adicionada ao analyzer ou à solução principal.

## Findings

### FINDING-005

- **Severity:** Medium
- **Category:** performance
- **Location:** `benchmarks/StrictSharp.Analyzers.V3.Benchmarks/Program.cs`, `Perf011Benchmarks`.
- **Evidence:** os benchmarks usam `[MemoryDiagnoser]`, que exporta alocação gerenciada e contadores de GC. Os artefatos `Perf011Benchmarks-report-full.json` não registram memória viva após `GC.GetTotalMemory(true)` ou coleta completa equivalente.
- **Impact:** `NFR-006`, `AC-006` e `AC-010` exigem evidência de memória viva para retenção. A alocação semelhante entre retenção e controle não demonstra, por si só, a diferença de referências vivas.
- **Recommendation:** adicionar uma medição explícita pós-GC para os dois cenários, mantendo o BDN para tempo/alocação e registrando os valores junto da evidência.
- **Return stage:** `implement`
- **Tracking:** `TASK-REV-005`.

### FINDING-006

- **Severity:** Medium
- **Category:** consistency
- **Location:** `TASK-011`, `evidence-v3-validation.md`, `benchmarks/StrictSharp.Analyzers.V3.Benchmarks/Program.cs`.
- **Evidence:** os artefatos preservam JSON/CSV/Markdown/HTML do BDN, mas não há log textual versionado contendo o comando completo, versão, ambiente e limitações. O console da execução não foi salvo como arquivo de evidência.
- **Impact:** o contrato de `TASK-011` exige preservar JSON, log, comando, revisão, ambiente, carga e limitações; a reprodução depende de informações dispersas na documentação e não de um log bruto.
- **Recommendation:** capturar a saída textual do BDN em um `.log` no diretório de evidência e registrar comando, versão, parâmetros e limitações no mesmo artefato.
- **Return stage:** `implement`
- **Tracking:** `TASK-REV-006`.

## Riscos residuais

- A execução reportou `Unknown processor`, falha de permissão ao consultar `Win32_Processor` e avisos de power plan; isso limita a comparabilidade entre máquinas, mas não invalida o build ou os testes.
- A carga é sintética e curta; os números não são limiares do analyzer nem prova de memory leak ou ganho universal.

## Conclusão

Os gates passam, mas a aprovação final fica pendente até `TASK-REV-005` e `TASK-REV-006` serem concluídas, com gates afetados e esta revisão renovados.
