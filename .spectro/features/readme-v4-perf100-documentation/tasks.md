<!-- spectro-managed: true -->
# Tarefas: Documentação da V4 e da regra PERF100 no README

| ID | Tarefa | Requisito | Critério de aceite | Dependências | Status |
|---|---|---|---|---|---|
| `TASK-001` | Auditar `RuleCatalog.cs`, `V4AnalyzerCatalog.cs`, `docs/csharp-performance-analyzer-v4.md`, roadmap V4 e README para confirmar contagem, fase e metadados de `PERF100`. | `FR-001`, `FR-003`, `CON-001`, `CON-003` | `AC-001`, `AC-002` | Nenhuma | completed |
| `TASK-002` | Atualizar o estado atual do README para 31 diagnósticos e adicionar V4 com sua distribuição e referência ao `V4AnalyzerCatalog`. | `FR-001`, `NFR-001`, `NFR-002`, `CON-002` | `AC-001` | `TASK-001` | completed |
| `TASK-003` | Adicionar a seção documental de `PERF100`, explicando funcionamento interprocedural, dimensões da evidência, limitações, orientação condicional e ausência de medição de custo efetivo. | `FR-002`, `FR-003`, `FR-004`, `FR-005`, `FR-006`, `FR-007`, `NFR-001`, `NFR-003`, `NFR-004`, `CON-001`, `CON-004` | `AC-002`, `AC-003`, `AC-004`, `AC-005`, `AC-006`, `AC-007` | `TASK-001`, `TASK-002` | completed |
| `TASK-004` | Validar o README final, links, contagem, preservação de V1–V3 e ausência de capacidades futuras apresentadas como implementadas; executar `git diff --check` e registrar a dispensa de gates. | `NFR-002`, `NFR-003`, `CON-002`, `CON-003` | `AC-001` a `AC-007` | `TASK-003` | completed |

## Rastreabilidade operacional

### TASK-001 — Auditoria das fontes

- Requisitos: `FR-001`, `FR-003`, `CON-001`, `CON-003`.
- Critérios: `AC-001`, `AC-002`.
- Validação: comparar `RuleCatalog`, `V4AnalyzerCatalog`, documentação V4 e README.

### TASK-002 — Estado e distribuição do catálogo

- Requisitos: `FR-001`, `NFR-001`, `NFR-002`, `CON-002`.
- Critério: `AC-001`.
- Validação: conferir 31 diagnósticos e a distribuição 15/13/2/1 sem remover V1–V3.

### TASK-003 — Funcionamento e limites de PERF100

- Requisitos: `FR-002`, `FR-003`, `FR-004`, `FR-005`, `FR-006`, `FR-007`, `NFR-001`, `NFR-003`, `NFR-004`, `CON-001`, `CON-004`.
- Critérios: `AC-002`, `AC-003`, `AC-004`, `AC-005`, `AC-006`, `AC-007`.
- Validação: revisão textual contra `docs/csharp-performance-analyzer-v4.md` e verificação dos links.

### TASK-004 — Validação final

- Requisitos: `NFR-002`, `NFR-003`, `CON-002`, `CON-003`.
- Critérios: `AC-001` a `AC-007`.
- Validação: buscas/contagens, links existentes e `git diff --check`; gates não aplicáveis.

## Regras de execução

- Alterar somente `README.md` durante a implementação; plan, tasks e evidências Spectro podem ser atualizados pelo fluxo.
- Não alterar `RuleCatalog.cs`, analyzers, testes, `.csproj` ou documentos V4 detalhados.
- Usar `RuleCatalog.cs` como fonte de metadados e `docs/csharp-performance-analyzer-v4.md` como fonte de comportamento e limites.
- Não apresentar V5–V10 como implementados.
- Não transformar incerteza interprocedural em custo zero nem declarar ganhos sem medição.

## Progresso

- Concluídas: `TASK-001`, `TASK-002`, `TASK-003`, `TASK-004`
- Bloqueadas: nenhuma

