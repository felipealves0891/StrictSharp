<!-- spectro-managed: true -->
# Plano: Documentação de uso do pacote e significado das regras

## Contexto e objetivo técnico

- O README atualmente orienta principalmente por `ProjectReference` e afirma que o NuGet não está configurado, embora a decisão registrada confirme o pacote publicado `StrictSharp` na versão `1.0.28`.
- O catálogo resumido do README cobre as 30 regras, mas não apresenta de modo uniforme risco, orientação, severidade, confiança e limitações.
- O objetivo técnico é alinhar a documentação pública às fontes existentes do projeto, sem alterar analyzers, catálogo, empacotamento ou testes de comportamento.

## Solução proposta

- Alterar somente `README.md`.
- Substituir o fluxo principal de instalação por exemplos NuGet:
  - `dotnet add package StrictSharp`
  - `dotnet add package StrictSharp --version 1.0.28`
- Explicar que o pacote instala um analyzer Roslyn para IDE/build, sem adicionar dependência de runtime à aplicação.
- Manter a referência de projeto como alternativa explicitamente local/de desenvolvimento.
- Reorganizar o catálogo em tabelas consistentes por fase, com ID, nome, padrão observado, significado do risco, orientação e ressalva quando necessário.
- Usar `RuleCatalog.cs` como fonte para IDs, nomes, severidade, categoria, confiança, impacto, escalabilidade, correção e exceções; usar `docs/csharp-performance-analyzer.md` para exemplos, limitações e referências.
- Atualizar a seção de interpretação para distinguir risco estrutural, severidade, confiança e impacto medido.
- Incluir configuração `.editorconfig`, inclusive desabilitação, e links para a documentação detalhada.

## Arquitetura e contratos

- **Componente alterado:** `README.md`, documentação pública empacotada pelo projeto via `PackageReadmeFile`.
- **Fontes normativas:** `src/StrictSharp.Analyzers/Rules/RuleCatalog.cs` e `docs/csharp-performance-analyzer.md`.
- **Fonte de contexto do pacote:** confirmação humana registrada em `clarifications.md`, que define ID publicado `StrictSharp` e versão publicada `1.0.28`.
- **Contrato preservado:** IDs `PERF001`–`PERF030`, nomes, fases e severidades existentes; nenhuma mudança em `RuleCatalog`, analyzers, `.csproj` ou pacote.
- **Fluxo de dados documental:** fontes do catálogo/documentação → tabelas e explicações no README → consumidor instala o analyzer e configura severidade no projeto.
- **Erros e limites:** não documentar a versão `1.0.0` do `.csproj` como versão publicada; não afirmar que alertas provam gargalo, memory leak ou ganho percentual.
- **Persistência/concurrency/observabilidade:** não aplicáveis à alteração documental; a instalação e execução do analyzer permanecem responsabilidade do fluxo normal do Roslyn.

## Alternativas consideradas

- **Manter `ProjectReference` como instrução principal:** rejeitada porque não atende ao uso publicado do pacote e dificulta consumidores externos.
- **Documentar apenas `dotnet add package StrictSharp`:** insuficiente para instalações reproduzíveis; será acompanhado do exemplo fixado em `1.0.28`.
- **Copiar todo o documento detalhado para o README:** rejeitada para evitar duplicação excessiva e divergência; o README terá síntese acionável e links para `docs/`.
- **Alterar o `PackageVersion` do projeto para `1.0.28`:** fora do escopo e potencialmente incorreto para a próxima publicação; a confirmação vale para a documentação, sem alterar metadados.
- **Criar validação automatizada permanente:** não necessária para esta alteração documental; será feita validação determinística por busca, contagem, revisão de links e comandos, com build/test opcional como regressão do repositório.

## Riscos e compatibilidade

- **Divergência de catálogo:** uma regra pode ter nome ou severidade diferente entre README, `RuleCatalog.cs` e documentação detalhada. Mitigação: auditoria por ID antes da edição e conferência final de `PERF001`–`PERF030`.
- **Versão publicada divergente do projeto:** o `.csproj` declara `1.0.0`, enquanto a decisão confirma `1.0.28` publicada. Mitigação: usar `1.0.28` no README e não alterar o `.csproj`; manter a decisão registrada.
- **Promessa de performance:** tabelas podem sugerir ganho garantido. Mitigação: linguagem de risco potencial, condições e necessidade de profiling/benchmark.
- **Quebra de navegação:** mudanças em tabelas ou links podem dificultar consulta. Mitigação: validar âncoras relativas, títulos e legibilidade do Markdown.
- **Compatibilidade do consumidor:** os comandos são para o pacote analyzer; a documentação deve deixar explícito que não há API de runtime esperada.

## Estratégia de testes

- Fazer auditoria textual de todos os IDs `PERF001`–`PERF030` no README e comparar nomes/fases/severidades com `RuleCatalog.cs`.
- Verificar presença literal dos dois comandos NuGet, da versão `1.0.28`, da explicação de analyzer Roslyn, da configuração `.editorconfig` e de `none`/`silent`/`suggestion`/`warning`/`error` quando documentados.
- Verificar ausência da afirmação de que o NuGet não está configurado e ausência de `1.0.0` como versão de instalação publicada.
- Validar links relativos para `docs/csharp-performance-analyzer.md` e demais documentos referenciados.
- Revisar manualmente os critérios AC-004 a AC-006 contra as tabelas e a seção de severidade/confiança.
- Executar `dotnet build StrictSharp.sln` e `dotnet test StrictSharp.sln` como regressão opcional/recomendada; a mudança não deve exigir alteração de testes.

## Rastreabilidade

| Requisito | Critério de aceite | Tarefa |
|---|---|---|
| `FR-001`, `FR-002` | `AC-001`, `AC-008` | `TASK-002` |
| `FR-003` | `AC-003` | `TASK-002` |
| `FR-004` | `AC-004` | `TASK-001`, `TASK-003` |
| `FR-005`, `FR-006` | `AC-005`, `AC-006` | `TASK-003` |
| `FR-007` | `AC-007` | `TASK-004` |
| `NFR-001`, `NFR-003` | `AC-005`, `AC-006` | `TASK-003`, `TASK-004` |
| `NFR-002` | `AC-001`, `AC-003`, `AC-008` | `TASK-002`, `TASK-005` |
| `NFR-004` | `AC-004`, `AC-007` | `TASK-004`, `TASK-005` |
| `CON-001`, `CON-002` | `AC-001`, `AC-004` | `TASK-001`, `TASK-005` |
| `CON-003`, `CON-004` | Todos os AC aplicáveis | `TASK-001` a `TASK-005` |

