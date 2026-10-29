<!-- spectro-managed: true -->
# Plano: Implementação V2 do C# Performance Analyzer

## Contexto e objetivo técnico

- A especificação V2 está aprovada e não possui ambiguidades bloqueadoras. A clarificação `Q-V2-001` determina que a análise só reporte padrões sustentados por evidência estrutural; não haverá limiares numéricos arbitrários.
- A solução já usa um analyzer por diagnóstico, `RuleAnalyzerBase`, `RuleAnalysis`, `RuleCatalog` e testes Roslyn com `AnalyzerTestHarness`. A implementação deve estender esses padrões sem alterar o contrato da V1.
- O objetivo técnico é entregar os 13 analyzers V2, habilitá-los por um catálogo explícito, preservar metadados e configuração Roslyn e validar comportamento positivo, negativo e de exceção para cada regra.

## Solução proposta

- Criar um `V2AnalyzerCatalog` explícito contendo exatamente os 13 analyzers V2, em ordem correspondente a `RuleCatalog`.
- Criar um arquivo `Perf###Analyzer.cs` por diagnóstico, derivando de `RuleAnalyzerBase` e registrando apenas os callbacks Roslyn necessários para o padrão observado.
- Reutilizar e ampliar `RuleAnalysis` somente com predicados semânticos comprováveis, como identificação de tipos/API, pertencimento a loop, invariância observável e caminhos de descarte; evitar heurísticas baseadas em números sem evidência no código.
- Manter metadados, severidade, confiança, exceções e mensagens no `RuleCatalog`/`RuleDefinition`; a V2 não terá Code Fix automático.
- Para padrões compostos, emitir no local que melhor representa a causa estrutural e evitar múltiplos diagnósticos para a mesma evidência quando a regra puder ser deduplicada.
- Atualizar testes, documentação e matrizes de catálogo para distinguir V1, V2 e V3, mantendo as regras V3 apenas catalogadas.

## Arquitetura e contratos

### Componentes de produção

- `src/StrictSharp.Analyzers/Analyzers/V2AnalyzerCatalog.cs`: catálogo público interno dos 13 analyzers implementados na V2.
- `src/StrictSharp.Analyzers/Analyzers/Perf002Analyzer.cs` até `Perf029Analyzer.cs` para os 13 IDs V2, sem criar arquivo para IDs V1/V3.
- `src/StrictSharp.Analyzers/Infrastructure/RuleAnalysis.cs`: somente extensões compartilhadas justificadas por pelo menos duas regras; os serviços devem permanecer estáticos e sem estado mutável.
- `src/StrictSharp.Analyzers/Rules/RuleCatalog.cs` e `RuleDefinition.cs`: preservar IDs, metadados, severidades e habilitação configurável. A ativação V2 deve ser explícita no catálogo V2, sem transformar V3 em regra executável.

### Contrato de análise

- Entrada: árvore, símbolos e modelo semântico Roslyn do código C# analisado.
- Saída: zero ou mais `Diagnostic` com exatamente um ID V2 suportado pelo analyzer correspondente.
- Exclusões: código gerado continua fora da análise, seguindo `RuleAnalyzerBase`.
- Configuração: severidade e supressão continuam delegadas ao mecanismo Roslyn/`.editorconfig`; não criar configuração proprietária de limiares.
- Concorrência: analyzers devem permanecer compatíveis com `EnableConcurrentExecution`; serviços compartilhados não podem manter estado por compilação ou thread.
- Segurança: nenhuma chamada de rede, persistência ou envio de código analisado.

### Estratégia por domínio

- Concorrência (`PERF002`, `PERF004`, `PERF005`, `PERF006`): reconhecer `Task.WhenAll`, regiões `lock`, loops de polling e criação repetida de `Thread`/`Task.Run` apenas quando o contexto estrutural for claro; limites externos não demonstráveis não devem ser inventados.
- Alocação (`PERF009`, `PERF010`): usar conversões e capturas observáveis; não afirmar alocação quando o compilador/runtime puder eliminar o custo e isso não puder ser distinguido estaticamente.
- LINQ (`PERF016`, `PERF017`): correlacionar ordenação/consulta com loops e mutações observáveis; não tratar toda repetição como redundante.
- Regex/metadados (`PERF021`): reconhecer construções imutáveis e reutilizáveis documentadas, preservando configurações dinâmicas e APIs com cache conhecido.
- Coleções (`PERF023`, `PERF024`): usar tipo e operações dominantes observáveis; só recomendar capacidade quando um tamanho/capacidade já estiver explicitamente disponível no código.
- Recursos/exceções (`PERF028`, `PERF029`): avaliar caminhos sintáticos/semânticos observáveis e comunicar suspeita fundamentada, sem prometer análise completa de fluxo dinâmico.

## Alternativas consideradas

- **Catálogo único automático por reflexão:** rejeitado; o projeto usa catálogos explícitos e a ordem verificável da V1, o que facilita rastreabilidade e evita registrar V3 prematuramente.
- **Um analyzer genérico para todas as regras V2:** rejeitado; viola a convenção de um analyzer por diagnóstico e dificulta isolamento, testes e configuração.
- **Limiares numéricos globais para tamanho/frequência/duração:** rejeitado por `Q-V2-001`; aumentaria falsos positivos e converteria hipótese de custo em requisito implícito.
- **Code Fix para trocar coleções, inserir `using` ou limitar concorrência:** rejeitado; as mudanças dependem de semântica, propriedade, ordem, thread safety e contrato de API.
- **Adicionar dependência de análise ou benchmark:** rejeitado nesta fase; Roslyn e o harness existente cobrem a validação estática, e benchmarks/profiling estão fora do escopo.

## Riscos e compatibilidade

- **Falso positivo em padrões contextuais:** mitigar exigindo evidência estrutural, testes de exceção e mensagens de confiança média quando o catálogo assim define.
- **Alteração acidental da V1:** mitigar com regressão da matriz V1, testes do catálogo de 30 regras e validação dos IDs/ordem dos catálogos.
- **Custo do analyzer:** mitigar com callbacks sintáticos específicos, consultas semânticas somente após filtros baratos, execução concorrente e revisão de alocações; não há limiar numérico aprovado.
- **Incompatibilidade de APIs:** mitigar compilando contra `netstandard2.1`, usando C# 14 e limitando reconhecimento às APIs documentadas e disponíveis nos metadados de teste.
- **Sobreposição com CA18xx:** mitigar preservando referências oficiais no catálogo e omitindo regras quando não houver valor contextual adicional.
- **Descarte incompleto:** `PERF028` será uma suspeita baseada em caminhos observáveis; não será tratado como prova de vazamento ou como análise completa de todos os fluxos.

## Estratégia de testes

- Criar um arquivo de teste por analyzer V2, com pelo menos um caso positivo, um negativo e uma exceção documentada.
- Expandir o harness apenas quando uma regra exigir referência de assembly ou opção de compilação não coberta atualmente; manter fontes pequenas e determinísticas.
- Criar `V2TestMatrixTests` verificando os 13 IDs, um descriptor por analyzer, código neutro sem diagnósticos e compatibilidade com C# 14.
- Expandir `RuleCatalogTests` para confirmar 30 regras, 15 V1, 13 V2, 2 V3, severidades documentadas e separação entre catálogo V2 implementado e V3 apenas planejado.
- Reutilizar `RuleConfigurationTests` para demonstrar supressão e severidade configurável em regra V2, preservando identidade do diagnóstico.
- Validar mensagens/metadados de `FR-015`, segurança estática e ausência de chamadas externas por inspeção e testes existentes.
- Executar `dotnet build StrictSharp.sln`, `dotnet test StrictSharp.sln` e os gates configurados em `.spectro/config.yaml`; cobertura e segurança ficam sujeitas aos critérios já configurados, sem criar novos limites.

## Rastreabilidade

| Requisito | Critério de aceite | Tarefa |
|---|---|---|
| `FR-001`, `CON-001`, `CON-003` | `AC-001` | `TASK-001`, `TASK-008` |
| `FR-002` | `AC-002` | `TASK-002` |
| `FR-003` | `AC-003` | `TASK-002` |
| `FR-004` | `AC-004` | `TASK-002` |
| `FR-005` | `AC-005` | `TASK-002` |
| `FR-006` | `AC-006` | `TASK-003` |
| `FR-007` | `AC-007` | `TASK-003` |
| `FR-008` | `AC-008` | `TASK-004` |
| `FR-009` | `AC-009` | `TASK-004` |
| `FR-010` | `AC-010` | `TASK-005` |
| `FR-011` | `AC-011` | `TASK-006` |
| `FR-012` | `AC-012` | `TASK-006` |
| `FR-013` | `AC-013` | `TASK-007` |
| `FR-014` | `AC-014` | `TASK-007` |
| `FR-015`, `NFR-006`, `CON-004` | `AC-015` | `TASK-001`, `TASK-002`–`TASK-007`, `TASK-009` |
| `FR-016` | `AC-016` | `TASK-001`, `TASK-008` |
| `FR-017` | `AC-017` | `TASK-002`–`TASK-009` |
| `NFR-001`, `NFR-002`, `NFR-003`, `NFR-004`, `NFR-005` | `AC-018` | `TASK-001`–`TASK-010` |
| `NFR-007` | `AC-019` | `TASK-009`, `TASK-010` |
| `CON-002`, `CON-005`, `CON-006` | `AC-001`, `AC-015`, `AC-018` | `TASK-001`, `TASK-009`, `TASK-010` |

## Ordem de execução

1. `TASK-001` — fundação, catálogo V2 e extensões compartilhadas.
2. `TASK-002` a `TASK-007` — analyzers e testes por domínio, em qualquer ordem após `TASK-001`.
3. `TASK-008` — matrizes de catálogo, configuração e regressão, após `TASK-002`–`TASK-007`.
4. `TASK-009` — documentação e rastreabilidade, após a definição efetiva dos comportamentos.
5. `TASK-010` — build, testes e gates finais, após todas as tarefas anteriores.

Não há dependências cíclicas. Nenhum arquivo de produção será removido; os novos analyzers e catálogos são aditivos, e os arquivos existentes serão modificados apenas para integração e cobertura.

## Estado

- Estado do plano: `ready_for_implementation`.
- Aprovação adicional necessária: aprovação humana para iniciar a implementação, conforme a configuração Spectro.
