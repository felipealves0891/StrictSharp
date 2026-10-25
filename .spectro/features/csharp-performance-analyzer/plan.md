<!-- spectro-managed: true -->
# Plano: C# Performance Analyzer — V1

## Contexto e objetivo técnico

O repositório ainda não possui solução .NET, projetos, código de analyzer ou testes. O plano parte de um workspace greenfield e implementa as 15 regras classificadas como V1, mantendo `PERF001`–`PERF030` rastreados no catálogo e deixando V2/V3 fora da implementação.

O objetivo técnico é entregar um analyzer Roslyn para .NET Standard 2.1, testado em .NET 10 e C# 14, com diagnósticos estáticos de baixo ruído, sem Code Fix, configuráveis via `.editorconfig` e com lógica de análise separada da integração Roslyn.

As 15 regras V1 são: `PERF001`, `PERF003`, `PERF007`, `PERF008`, `PERF012`, `PERF013`, `PERF014`, `PERF015`, `PERF018`, `PERF019`, `PERF020`, `PERF022`, `PERF025`, `PERF026` e `PERF027`. As prioridades demonstrativas são `PERF001`, `PERF008`, `PERF014`, `PERF025` e `PERF027`.

## Solução proposta

- Criar uma solução .NET com um projeto de analyzer targeting `netstandard2.1` e um projeto de testes targeting `net10.0`.
- Usar Roslyn Analyzer APIs e um contrato compartilhado de metadados para IDs, títulos, severidade, categoria, confiança, impacto, escalabilidade, correção, exceções e referências.
- Manter um analyzer por diagnóstico, com serviços estáticos reutilizáveis para reconhecimento sintático/semântico, filtros de contexto e construção de mensagens.
- Registrar execução concorrente quando seguro, evitar análise semântica desnecessária e limitar o trabalho ao contexto necessário para cada regra.
- Emitir diagnósticos apenas com evidência estática suficiente; não usar profiling, benchmarks de runtime ou Code Fix nesta fase.
- Configurar severidade por `dotnet_diagnostic.PERFxxx.severity` em `.editorconfig`, seguindo as convenções dos analyzers Roslyn/Microsoft.
- Validar cada regra com casos positivos, negativos e exceções documentadas usando testes de analyzer; validar o custo do próprio analyzer por inspeção/gates estáticos, sem limiar numérico nesta fase.

## Arquitetura e contratos

### Componentes planejados

- `StrictSharp.sln`: solução principal.
- Projeto de analyzer: integração Roslyn, registro dos 15 analyzers V1 e empacotamento como analyzer.
- Projeto de testes: harness de compilação/análise, fontes C# inline e asserts de diagnóstico.
- Catálogo de regras: descritores e metadados estáveis para `PERF001`–`PERF030`; somente V1 terá implementação nesta entrega.
- Serviços de análise: helpers estáticos para símbolos, tipos, operações, loops, fluxo assíncrono, coleções, APIs conhecidas, exceções e contexto.
- Testes por regra: casos positivos, negativos, exceções, severidade, localização, mensagem, referência e compatibilidade.
- `.editorconfig` de teste: habilitação, desabilitação e alteração de severidade por ID.

### Contratos de diagnóstico

Cada regra implementada deve declarar um `DiagnosticDescriptor` estável e produzir uma mensagem que contenha, diretamente ou por documentação associada, identificador, título, severidade, categoria, evidência, impacto, escalabilidade, correção, exceções, confiança e referência. A severidade será a definida no catálogo da documentação, não uma estimativa de ganho.

O diagnóstico deve apontar para a expressão/padrão que fornece a evidência. Quando o contexto não permitir provar a exceção ou a equivalência da correção, a saída será orientação condicional, nunca Code Fix automático.

### Dados, persistência e observabilidade

- Não haverá persistência de dados de aplicação nem coleta de runtime.
- O analyzer não enviará código ou telemetria para serviços externos.
- A observabilidade da V1 será composta por diagnósticos Roslyn, mensagens estáveis, IDs, categorias, referências e evidências de testes/gates.
- Benchmarks, profiling, correlação com carga e métricas de produção ficam para evolução futura.

### Concorrência e desempenho

- Os analyzers serão registrados para execução concorrente quando seus serviços forem sem estado compartilhado mutável.
- Catálogos e descritores serão imutáveis/reutilizáveis; a análise evitará alocações e buscas repetidas desnecessárias.
- Análise semântica será feita somente quando a regra precisar dela; verificações sintáticas simples permanecerão sintáticas.
- Nenhuma regra V1 fará análise interprocedural ampla ou dependerá de dados externos.

## Ordem técnica e dependências

1. Bootstrap da solução, projetos e compatibilidade de target.
2. Contrato/catálogo de diagnósticos e infraestrutura compartilhada.
3. Configuração `.editorconfig` e harness de testes.
4. Implementação das regras V1 por grupos de risco.
5. Documentação das mensagens, exceções e referências.
6. Testes completos, revisão de ruído e gates estáticos.
7. Empacotamento/integracão e atualização do rastreamento do roadmap.

Não há dependências cíclicas planejadas. As regras dependem do catálogo e dos serviços compartilhados; os testes dependem das regras; os gates dependem dos projetos e testes.

## Alternativas consideradas

- Um analyzer monolítico foi rejeitado porque viola a constituição de um analyzer por diagnóstico e dificulta isolamento, testes e custo incremental.
- Code Fix foi rejeitado para V1 por decisão de clarificação; alterações condicionais serão apresentadas como orientação.
- Benchmarks de execução foram adiados porque a fase atual é exclusivamente estática.
- Análise baseada apenas em sintaxe foi rejeitada para regras que exigem símbolos, tipos, APIs ou fluxo; será usada análise semântica somente onde necessária.
- Implementar as 30 regras imediatamente foi rejeitado: V1 implementa 15 regras e mantém V2/V3 como backlog rastreado.
- Introduzir profiling ou telemetria foi rejeitado por estar fora do escopo e por requisitos de segurança/privacidade.

## Riscos e compatibilidade

- Risco: não há arquitetura existente para reutilizar. Mitigação: manter bootstrap pequeno, contratos explícitos e testes desde a primeira regra.
- Risco: falsos positivos em padrões cujo custo depende de carga, contrato ou ciclo de vida. Mitigação: filtros de exceção, confiança explícita, casos negativos e orientação condicional.
- Risco: sobreposição com `CA18xx` e outras regras oficiais. Mitigação: matriz de referências/deduplicação e mensagens com valor complementar.
- Risco: incompatibilidade entre Roslyn, .NET Standard 2.1, .NET 10 e C# 14. Mitigação: fixar versões no projeto, compilar na matriz definida e evitar APIs fora do target.
- Risco: analyzer lento na IDE/build. Mitigação: execução concorrente segura, descritores estáticos, análise semântica mínima e inspeção de alocações/trabalho repetido.
- Risco: configuração de gate incompleta. O `config.yaml` atual referencia `.scpectro/tools/coverage-gate.ps1` e `./src/MinhaSolucao.sln`, que não existem. Antes de executar gates, deve-se corrigir esses caminhos em tarefa aprovada; não fazem parte da implementação das regras.

## Estratégia de testes e validação

- Testes de contrato: IDs únicos, metadados obrigatórios, severidade e referências.
- Testes de regra: positivo, negativo, exceção, localização e mensagem para cada uma das 15 regras V1.
- Testes de configuração: severidade por ID, desabilitação com `none` e preservação do ID.
- Testes de compatibilidade: compilação do analyzer em `netstandard2.1`, testes em `net10.0` e linguagem C# 14.
- Testes de regressão: exemplos documentados para `PERF001`, `PERF002`, `PERF008`, `PERF014`, `PERF027` e demais regras V1 conforme o catálogo.
- Gates estáticos: build, testes, segurança de dependências e cobertura quando os caminhos da configuração forem corrigidos.
- Não haverá benchmark de custo da aplicação analisada nem profiling nesta fase; limitações devem aparecer na documentação/diagnóstico.

## Rastreabilidade

| Requisito | Critério de aceite | Tarefa |
|---|---|---|
| `FR-001`, `CON-001` | `AC-001` | `TASK-001`, `TASK-003` |
| `FR-002`, `CON-002` | `AC-002` | `TASK-002` |
| `FR-003`, `FR-004`, `CON-004` | `AC-003` | `TASK-002`, `TASK-009` |
| `FR-005`, `NFR-001`, `NFR-006` | `AC-004` | `TASK-003`, `TASK-005` a `TASK-007` |
| `FR-006` | `AC-005` | `TASK-005` a `TASK-007`, `TASK-010` |
| `FR-007` | `AC-006` | `TASK-010` |
| `FR-008` | `AC-007` | `TASK-004`, `TASK-008` |
| `FR-009`, `NFR-002` | `AC-008` | `TASK-009` |
| `FR-010`, `CON-003` | `AC-009` | `TASK-002`, `TASK-009` |
| `FR-011`, `NFR-008` | `AC-010`, `AC-013` | `TASK-008`, `TASK-010` |
| `FR-012`, `NFR-005` | `AC-011` | `TASK-009` |
| `NFR-003` | `AC-012` | `TASK-001`, `TASK-008` |
| `NFR-004` | `AC-012` | `TASK-003`, `TASK-010` |
| `NFR-007` | `AC-013` | `TASK-005` a `TASK-007` |
| `CON-005` | `AC-001` a `AC-013` | `TASK-001` a `TASK-010` |

## Aprovação e pré-condições

- A especificação está aprovada para planejamento e não possui bloqueios de clarificação.
- A implementação exige aprovação humana antes de mudar escopo, adicionar dependências além das necessárias ao harness Roslyn ou alterar o contrato dos diagnósticos.
- Antes dos gates finais, deve ser aprovada a correção dos caminhos inválidos em `.spectro/config.yaml`.
