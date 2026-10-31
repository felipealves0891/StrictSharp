<!-- spectro-managed: true -->
# Plano: Documentação da V4 e da regra PERF100 no README

## Contexto e objetivo técnico

- O README atualmente encerra o catálogo na V3 e informa 30 diagnósticos, enquanto `RuleCatalog.cs` e `V4AnalyzerCatalog.cs` já incluem `PERF100` como a única regra V4.
- A documentação detalhada V4 define o comportamento interprocedural, as dimensões dinâmicas da evidência e os limites de resolução de alvos.
- O objetivo é alinhar a documentação de entrada do pacote com o contrato implementado, sem modificar código, testes, catálogo ou documentação detalhada.

## Solução proposta

- Alterar somente `README.md` durante a implementação.
- Atualizar a contagem do catálogo para 31 diagnósticos e adicionar V4 ao quadro de fases: V1 (15), V2 (13), V3 (2), V4 (1).
- Criar uma seção `V4 — implementados` com uma tabela de `PERF100` contendo nome, significado, categoria, severidade, confiança e orientação.
- Explicar a análise interprocedural como propagação de evidência entre métodos, chamadores, loops, repetição, alocações/materializações, bloqueios e caminhos assíncronos quando resolvidos.
- Documentar as dimensões dinâmicas (`CPU`, `Memory`, `I/O`, `Concurrency`, `Recursion`, `Async`) como evidência da ocorrência, mantendo `Performance` como categoria fixa.
- Documentar limites e incerteza de corpos ausentes, dependências externas, interfaces, delegates e dispatch não resolvido.
- Adicionar links para `docs/csharp-performance-analyzer-v4.md` e, se usado para contexto, `docs/csharp-performance-analyzer-2.md`.

## Arquitetura e contratos

- **Componente alterado:** `README.md`.
- **Fonte de metadados:** `src/StrictSharp.Analyzers/Rules/RuleCatalog.cs`; contrato de `PERF100`: título `Custo interprocedural propagado`, categoria `Performance`, severidade `High`, confiança `Média`, fase V4.
- **Fonte de registro da fase:** `src/StrictSharp.Analyzers/Analyzers/V4AnalyzerCatalog.cs`.
- **Fonte comportamental:** `docs/csharp-performance-analyzer-v4.md`; contexto de roadmap em `docs/csharp-performance-analyzer-2.md`.
- **Contrato preservado:** `PERF001`–`PERF030` permanecem inalterados; `PERF100` é documentado como ID do StrictSharp, não como regra oficial do .NET.
- **Fluxo documental:** catálogo/código/documentação detalhada → resumo no README → consumidor interpreta o alerta e consulta a referência V4.
- **Erros e limites:** não apresentar custo desconhecido como custo zero; não afirmar medição de frequência, CPU, memória, GC, I/O, complexidade ou ganho.
- **Persistência, concorrência e observabilidade:** não aplicáveis à mudança; a feature não altera execução do analyzer nem coleta dados externos.

## Alternativas consideradas

- **Adicionar `PERF100` à tabela V3:** rejeitada porque o catálogo define `RulePhase.V4` e existe `V4AnalyzerCatalog`.
- **Listar V4 apenas como roadmap:** rejeitada porque `PERF100` está implementado e registrado no catálogo atual.
- **Copiar toda a documentação V4 para o README:** rejeitada para evitar duplicação; o README terá resumo operacional e links para a documentação detalhada.
- **Alterar a documentação detalhada V4 para corrigir o README:** fora do escopo; as fontes existentes já descrevem o contrato necessário.
- **Executar gates Spectro:** dispensado por decisão registrada; a validação será textual, de links, contagem, diff e, se desejado no fluxo de implementação, regressão local sem transformar gates em requisito.

## Riscos e compatibilidade

- **Contagem inconsistente:** atualizar fases sem atualizar o total pode manter a documentação enganosa. Mitigação: conferir 15 + 13 + 2 + 1 = 31.
- **Confusão entre categoria e dimensão:** `CPU` ou `Memory` podem ser interpretadas como categorias do Roslyn. Mitigação: declarar explicitamente que são dimensões da evidência e que a categoria é `Performance`.
- **Promessa de análise completa:** análise interprocedural pode parecer prova de custo real. Mitigação: declarar incerteza, limites e necessidade de medição externa.
- **Links inválidos:** referências relativas podem apontar para arquivos inexistentes. Mitigação: validar os dois caminhos a partir da raiz do repositório.
- **Escopo acidental:** mudanças no catálogo, analyzers ou docs detalhadas podem alterar o contrato. Mitigação: revisar o diff e aceitar apenas `README.md` mais artefatos Spectro.

## Estratégia de testes

- Comparar a distribuição documentada com os catálogos/fonte: V1=15, V2=13, V3=2, V4=1 e total=31.
- Verificar presença de `PERF100`, `Custo interprocedural propagado`, `Performance`, `High`, `Média` e referência V4.
- Verificar que o README explica propagação entre métodos e repetição por loop, dimensões dinâmicas, alvos desconhecidos e ausência de medição de custo efetivo.
- Verificar que `PERF001`–`PERF030` continuam presentes e que V5–V10 não são apresentados como implementados.
- Validar existência dos links para `docs/csharp-performance-analyzer-v4.md` e `docs/csharp-performance-analyzer-2.md`, quando incluído.
- Executar `git diff --check`; gates Spectro não serão executados para esta feature documental.

## Rastreabilidade

| Requisito | Critério de aceite | Tarefa |
|---|---|---|
| `FR-001` | `AC-001` | `TASK-001`, `TASK-002` |
| `FR-002` | `AC-003` | `TASK-001`, `TASK-003` |
| `FR-003` | `AC-002` | `TASK-001`, `TASK-002` |
| `FR-004` | `AC-004` | `TASK-003` |
| `FR-005` | `AC-005` | `TASK-003` |
| `FR-006` | `AC-006` | `TASK-003` |
| `FR-007` | `AC-007` | `TASK-003`, `TASK-004` |
| `NFR-001`, `NFR-002` | `AC-001`, `AC-002` | `TASK-002`, `TASK-004` |
| `NFR-003`, `NFR-004` | `AC-004`, `AC-006` | `TASK-003`, `TASK-004` |
| `CON-001`, `CON-003` | `AC-001`, `AC-002` | `TASK-001`, `TASK-002`, `TASK-004` |
| `CON-002`, `CON-004` | Todos os AC aplicáveis | `TASK-001` a `TASK-004` |

