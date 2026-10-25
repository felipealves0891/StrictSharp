<!-- spectro-managed: true -->
# Clarificações: C# Performance Analyzer

| ID | Dúvida | Classificação | Impacto | Decisão | Responsável | Data |
|---|---|---|---|---|---|---|
| `Q-001` | Quais versões mínimas e máximas de .NET, C#, Target Framework e Roslyn serão suportadas na V1? | Blocking | Define APIs disponíveis, matriz de testes e compatibilidade do analyzer. | Resolvido: .NET 10 e .NET Standard 2.1. A versão de C# e a versão compatível de Roslyn devem acompanhar essas plataformas. | Usuário/projeto | 2026-10-25 |
| `Q-002` | A primeira entrega deve implementar apenas as cinco regras prioritárias (`PERF001`, `PERF008`, `PERF014`, `PERF025`, `PERF027`) ou todas as 15 regras classificadas como V1? | Blocking | Define o escopo de implementação, testes, benchmarks e planejamento da V1. | Resolvido: implementar as 15 regras classificadas como V1. As cinco permanecem como prioridade de demonstração de valor. | Usuário/projeto | 2026-10-25 |
| `Q-003` | Quais regras terão Code Fix automático na V1 e quais oferecerão somente diagnóstico/orientação? | Major | Define superfície do produto e risco de alterações semanticamente inseguras. | Resolvido: nenhuma regra terá Code Fix; a V1 fornecerá diagnósticos e orientação. | Usuário/projeto | 2026-10-25 |
| `Q-004` | Quais serão os níveis padrão de severidade e as opções de configuração expostas ao usuário? | Major | Define o comportamento inicial no build/IDE e a política de adoção das regras. | Resolvido: usar os critérios de severidade definidos em `docs/csharp-performance-analyzer.md`; não foi definida uma política adicional de severidade padrão. | Usuário/projeto | 2026-10-25 |
| `Q-005` | Quais critérios objetivos definirão baixo ruído, custo aceitável do analyzer e benchmarks oficiais para regras de maior impacto? | Major | Define gates de aceite, limites de performance e evidência reproduzível. | Resolvido para a fase atual: haverá apenas análise estática. Benchmarks, profiling e quantificação de custo em runtime ficam fora do escopo atual. | Usuário/projeto | 2026-10-25 |
| `Q-006` | Qual versão explícita de Roslyn e qual versão de linguagem C# serão fixadas para a V1 em .NET 10/.NET Standard 2.1? | Blocking | Define APIs do compilador, compatibilidade do analyzer e matriz de testes. | Resolvido: C# 14; Roslyn deve ser compatível com .NET 10 e .NET Standard 2.1. | Usuário/projeto | 2026-10-25 |
| `Q-007` | Qual será o contrato de configuração para habilitar, desabilitar e ajustar a severidade das regras? | Major | Define a integração com IDE/build e a forma de adoção das 15 regras V1. | Resolvido: haverá configuração usando as convenções padrão de analyzers Roslyn/Microsoft em `.editorconfig`, incluindo `dotnet_diagnostic.<ID>.severity`. | Usuário/projeto | 2026-10-25 |
| `Q-008` | Qual métrica estática ou limite operacional definirá que o custo do analyzer não é demasiadamente alto para IDE/build, mesmo sem profiling do código analisado? | Major | Define o gate de desempenho do próprio analyzer. | Resolvido para a fase atual: seguir a documentação oficial da Microsoft para analyzers Roslyn, evitando trabalho semântico desnecessário e habilitando execução concorrente quando seguro; não será fixado limiar numérico nesta fase. | Usuário/projeto | 2026-10-25 |

## Resultado

- Bloqueios restantes: 0
- Questões major restantes: 0
- Pronto para planejamento: sim
- Estado: `completed`
