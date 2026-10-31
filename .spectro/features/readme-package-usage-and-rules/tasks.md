<!-- spectro-managed: true -->
# Tarefas: Documentação de uso do pacote e significado das regras

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Status |
|---|---|---|---|---|---|
| `TASK-001` | Auditar `RuleCatalog.cs`, `docs/csharp-performance-analyzer.md` e o README atual, produzindo uma matriz de `PERF001`–`PERF030` com nome, fase, severidade, confiança, risco, orientação, exceções e link de referência. | `FR-004`, `CON-001`, `CON-004` | `AC-004` | Nenhuma | completed |
| `TASK-002` | Reescrever a seção de instalação e configuração do README com `dotnet add package StrictSharp`, `--version 1.0.28`, explicação do analyzer Roslyn, alternativa local e exemplos `.editorconfig`. | `FR-001`, `FR-002`, `FR-003`, `CON-002` | `AC-001`, `AC-002`, `AC-003`, `AC-008` | `TASK-001` | completed |
| `TASK-003` | Atualizar as tabelas/descrições das regras V1, V2 e V3 para explicar padrão, risco potencial, orientação condicional e limitações, sem alterar IDs ou contrato do catálogo. | `FR-004`, `FR-005`, `FR-006`, `NFR-001`, `NFR-003`, `CON-001`, `CON-004` | `AC-004`, `AC-005`, `AC-006` | `TASK-001` | completed |
| `TASK-004` | Revisar a seção de documentação e contribuição, links, avisos de profiling/benchmark, significado de severidade/confiança e navegabilidade do Markdown. | `FR-005`, `FR-006`, `FR-007`, `NFR-001`, `NFR-003`, `NFR-004` | `AC-005`, `AC-006`, `AC-007` | `TASK-002`, `TASK-003` | completed |
| `TASK-005` | Validar o README final por buscas/contagens, conferência de links e exemplos; executar build/test do repositório como regressão e registrar qualquer divergência sem corrigir código fora do escopo. | `NFR-002`, `NFR-004`, `CON-001`, `CON-002`, `CON-003` | `AC-001` a `AC-008` | `TASK-004` | completed |

## Rastreabilidade operacional

### TASK-001 — Auditoria das fontes

- Requisitos: `FR-004`, `CON-001`, `CON-004`.
- Critério: `AC-004`.
- Validação: comparar os 30 IDs, nomes, fases, severidades, confianças, riscos, orientações e exceções com `RuleCatalog.cs` e a documentação detalhada.

### TASK-002 — Instalação e configuração

- Requisitos: `FR-001`, `FR-002`, `FR-003`, `CON-002`.
- Critérios: `AC-001`, `AC-002`, `AC-003`, `AC-008`.
- Validação: conferir os comandos NuGet, a versão `1.0.28`, a explicação de analyzer Roslyn e os exemplos `.editorconfig`.

### TASK-003 — Catálogo e interpretação das regras

- Requisitos: `FR-004`, `FR-005`, `FR-006`, `NFR-001`, `NFR-003`, `CON-001`, `CON-004`.
- Critérios: `AC-004`, `AC-005`, `AC-006`.
- Validação: conferir a cobertura `PERF001`–`PERF030` e a distinção entre risco, severidade, confiança e impacto medido.

### TASK-004 — Links, limites e navegabilidade

- Requisitos: `FR-005`, `FR-006`, `FR-007`, `NFR-001`, `NFR-003`, `NFR-004`.
- Critérios: `AC-005`, `AC-006`, `AC-007`.
- Validação: verificar links relativos, avisos de profiling/benchmark e legibilidade das seções Markdown.

### TASK-005 — Validação final

- Requisitos: `NFR-002`, `NFR-004`, `CON-001`, `CON-002`, `CON-003`.
- Critérios: `AC-001` a `AC-008`.
- Validação: executar buscas/contagens documentais, `git diff --check`, `dotnet build StrictSharp.sln` e `dotnet test StrictSharp.sln --no-build`.

## Regras de execução

- Alterar somente `README.md` durante a implementação; `plan.md`, `tasks.md` e evidências Spectro podem ser atualizados pelo fluxo da feature.
- Não alterar `RuleCatalog.cs`, analyzers, `.csproj`, versão do pacote, testes ou documentação detalhada como parte desta feature.
- Tratar `RuleCatalog.cs` como fonte para metadados e `docs/csharp-performance-analyzer.md` como fonte para explicações e exemplos.
- Usar `StrictSharp` e `1.0.28` na instrução de pacote conforme decisão registrada, mesmo que o `.csproj` local contenha outra versão.
- Não afirmar que uma recomendação é sempre mais rápida; preservar ressalvas sobre semântica, mutabilidade, ciclo de vida, thread safety e medição.

## Progresso

- Concluídas: `TASK-001`, `TASK-002`, `TASK-003`, `TASK-004`, `TASK-005`
- Bloqueadas: nenhuma

