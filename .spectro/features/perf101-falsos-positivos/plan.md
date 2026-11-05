<!-- spectro-managed: true -->
# Plano: Redução de falsos positivos na PERF101

## Contexto e objetivo técnico

- `Perf101Analyzer` atualmente combina evidências de `LoopCostInference` e `InterproceduralCostInference` e reporta qualquer item cuja complexidade seja conhecida. Isso transforma uma classificação como `O(n)` em warning mesmo quando o percurso é necessário ou não existe alternativa demonstrável.
- O objetivo técnico é separar evidência de complexidade da decisão de diagnóstico. A `PERF101` só deve reportar uma oportunidade acionável quando houver padrão repetido, chave estável, alternativa estrutural semanticamente compatível e vantagem demonstrável; sem isso, deve permanecer silenciosa.
- A mudança é restrita à `PERF101`, preserva o ID, a severidade pública, a etapa V5 e o comportamento das demais regras V5.

## Solução proposta

- Criar um serviço estático de decisão de oportunidade da `PERF101`, separado do adaptador Roslyn, que receba a evidência estrutural e o contexto semântico necessário e retorne uma evidência acionável somente quando o padrão aprovado for comprovado.
- Modelar a decisão como evidência adicional, não como alteração da classificação Big-O: `LoopCostInference` continua responsável por classificar loops/ordenações e a nova análise decide se há uma melhoria demonstrável.
- Na primeira entrega, reconhecer de forma conservadora busca linear repetida sobre a mesma fonte/coleção, com chave estável e alternativa de índice/conjunto semanticamente compatível que possa ser explicada estruturalmente. Não reconhecer nomes, tipos ou métodos isolados como prova suficiente.
- Fazer `Perf101Analyzer` reportar apenas itens com oportunidade acionável. `V5DiagnosticAnalysis` e os demais analisadores V5 não devem ter seu contrato alterado sem necessidade.
- Quando o padrão for ambíguo, houver dispatch/entrada/alvo não resolvido, ou a alternativa puder alterar ordem, duplicidade, mutabilidade, propriedade, thread safety ou custo de construção, retornar ausência de oportunidade e não publicar diagnóstico.

## Arquitetura e contratos

- Componentes afetados:
  - `src/StrictSharp.Analyzers/Analyzers/Perf101Analyzer.cs`: orquestra classificação, análise interprocedural e filtro de oportunidade antes do reporte.
  - `src/StrictSharp.Analyzers/V5/LoopCostInference.cs`: permanece fonte da classificação estrutural; só deve ser ajustado se for necessário expor contexto já calculado sem duplicar travessias.
  - Novo serviço em `src/StrictSharp.Analyzers/V5/` para inferência da oportunidade acionável da `PERF101`, preferencialmente estático e baseado em estruturas Roslyn já disponíveis.
  - `tests/StrictSharp.Analyzers.Tests/`: testes unitários do serviço e testes de analyzer via `AnalyzerTestHarness`.
  - `docs/perf101-falsos-positivos.md` e documentação V5: registrar o contrato silencioso e os limites da oportunidade reconhecida.
- Contrato lógico: classificação (`ComplexityClassification`) continua representando forma de crescimento; oportunidade deve carregar, no mínimo, explicação da alternativa, premissas/compatibilidade e vantagem estrutural, ou representar ausência de oportunidade.
- Fluxo: método Roslyn → classificação local/interprocedural existente → inferência de oportunidade → filtro `PERF101` → diagnóstico com classificação, evidência e limitações. Ausência de oportunidade encerra o fluxo sem diagnóstico.
- Erros e desconhecidos: cancelamento deve continuar sendo propagado; símbolos ausentes, fronteiras externas, dispatch não resolvido e semântica ambígua devem produzir ausência de oportunidade, não exceção nem custo zero.
- Persistência e estado: não há persistência, cache global ou estado mutável compartilhado. A análise deve ser determinística e segura com execução concorrente do Roslyn.
- Observabilidade: a mensagem do diagnóstico deve explicar apenas a oportunidade comprovada, sua mudança estrutural e a classificação; não deve incluir percentual, benchmark, frequência real ou custo de CPU.

## Alternativas consideradas

- Reportar todo item com complexidade conhecida: rejeitada porque é a causa direta dos falsos positivos documentados e viola `FR-001`/`FR-002`.
- Alterar `LoopCostInference` para não classificar `O(n)`: rejeitada porque mistura evidência estrutural com decisão de produto e perde informação necessária quando uma oportunidade real existir.
- Usar somente nomes de APIs/tipos (`HashSet`, `Dictionary`, `Add`, `Contains`): rejeitada porque não comprova repetição, chave estável, equivalência semântica ou custo de construção.
- Adicionar Code Fix para substituir a coleção: rejeitada por depender de contratos, mutabilidade, ordem, duplicidade e propriedade que não são demonstráveis em todos os casos.
- Fazer profiling ou benchmark durante a análise: rejeitada por estar fora do escopo V5, exigir execução externa e não ser determinística para IDE/build.

## Riscos e compatibilidade

- Risco de falso negativo: a política conservadora pode deixar de reportar oportunidades reais. Mitigação: começar pelo padrão repetido explicitamente aprovado e expandir apenas com nova especificação/evidência.
- Risco de equivalência incorreta: ordem, duplicidade, comparador, mutabilidade ou custo de indexação podem mudar o comportamento. Mitigação: exigir compatibilidade estrutural observável e silenciar casos não resolvidos.
- Risco de regressão V5: alterar a infraestrutura comum pode afetar `PERF102`–`PERF107`. Mitigação: manter o filtro no caminho da `PERF101` e executar matriz V5 completa.
- Compatibilidade: manter analyzer `netstandard2.1`, testes `net10.0`, C# 14 e Roslyn 5.0.0; não adicionar dependências.

## Estratégia de testes

- Serviço de oportunidade: casos positivos para busca repetida com chave estável e alternativa compatível; negativos para percurso único, `O(n)` necessário, ausência de alternativa e chamada apenas nominal; exceções para ordem/duplicidade/mutabilidade/custo de construção ambíguos, dispatch e símbolos não resolvidos.
- Analyzer: validar que ausência de oportunidade produz zero diagnósticos, que oportunidade produz exatamente um `PERF101` com classificação e explicação, e que a mensagem não contém percentual ou alegação de medição.
- Regressão: manter testes de classificação de `LoopCostInference`, matriz de catálogo V5, silêncio em código neutro e testes das demais regras sem alteração de expectativa.
- Gates: build, testes, cobertura, segurança e verificação de diff/documentação conforme `.spectro/config.yaml`; comparar cobertura de linhas/ramos com os limites configurados.

## Rastreabilidade

| Requisito | Critério de aceite | Tarefa |
|---|---|---|
| `FR-001`, `FR-002` | `AC-001`, `AC-002` | `TASK-002`, `TASK-003` |
| `FR-003` | `AC-003`, `AC-004` | `TASK-002`, `TASK-003`, `TASK-004` |
| `FR-004` | `AC-005`, `AC-006` | `TASK-003`, `TASK-004` |
| `FR-005` | `AC-007` | `TASK-004`, `TASK-005` |
| `NFR-001` | `AC-001`, `AC-004`, `AC-006` | `TASK-002`, `TASK-004` |
| `NFR-002` | `AC-008` | `TASK-003`, `TASK-005` |
| `NFR-003` | `AC-004` | `TASK-002`, `TASK-004` |
| `CON-001`–`CON-004` | `AC-003`, `AC-005`, `AC-008` | `TASK-001`–`TASK-005` |

## Ordem de execução

1. `TASK-001` fecha o contrato técnico e os critérios de reconhecimento sem alterar produção.
2. `TASK-002` implementa a inferência de oportunidade e seus testes unitários.
3. `TASK-003` integra o filtro exclusivamente à `PERF101`.
4. `TASK-004` completa testes de analyzer, regressão e cobertura.
5. `TASK-005` atualiza documentação e executa os gates.
