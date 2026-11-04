<!-- spectro-managed: true -->
# Plano: Redução de falsos positivos na PERF011

## Contexto e objetivo técnico

- A especificação foi esclarecida e não possui ambiguidades blocking ou major. A decisão `Q-PERF011-001` estabelece que convenções de APIs externas não são evidência suficiente para silenciar o diagnóstico.
- `Perf011Analyzer` atualmente combina chamada nominal (`Add`, `AddRange` ou `Subscribe`), receptor identificado como campo, presença em loop e ausência de uma operação de liberação observável. Embora já filtre tipos de coleção e `IObservable`, a decisão está concentrada no analyzer, não considera propriedades, ciclo de vida do owner, escape ou limites efetivos e não distingue capacidade inicial de limite máximo.
- Objetivo técnico: introduzir uma análise estática, determinística e conservadora de evidência de retenção, mantendo `PERF011`, sua severidade, localização e configuração, mas omitindo casos em que a retenção seja delimitada, transitória, não demonstrável ou semanticamente não reconhecida.

## Solução proposta

- Extrair a decisão de retenção para um serviço estático específico da regra, sob `src/StrictSharp.Analyzers/Retention/`, deixando `Perf011Analyzer` responsável apenas por registrar a ação Roslyn, solicitar a análise e reportar o resultado.
- Fazer a análise retornar uma classificação pequena e sem alocações desnecessárias, por exemplo: operação retentora reconhecida, evidência de owner duradouro, ciclo de vida delimitado, limite efetivo, escape/transferência reconhecível e decisão final (`Report`/`Omit`). A forma exata do tipo pode ser ajustada durante a implementação, sem criar estado compartilhado entre análises concorrentes.
- Resolver semanticamente o membro invocado e o símbolo do receptor. Métodos homônimos em tipos arbitrários continuam fora da regra. Campos e propriedades só serão aceitos quando o tipo e o vínculo retentor forem resolvidos; propriedade ou `IEnumerable<T>` abstrato isoladamente não será tratado como prova de retenção.
- Reconhecer como limite somente evidência preservada no código analisado, como estrutura realmente fixa ou guarda explícita que limite as inserções no caminho avaliado. `new(n)` em `List<T>` ou coleção semelhante será tratado apenas como capacidade inicial, não como limite máximo.
- Fazer análise local de ciclo de vida e escape: omitir quando a criação/uso e o owner forem demonstravelmente limitados ao escopo analisado e não houver escape; omitir ou manter desconhecido quando a conclusão depender de caller, API externa, closure, evento, cache ou transferência não resolvidos. Não implementar reachability global.
- Preservar o positivo somente quando houver evidência estrutural suficiente de retenção potencial em owner possivelmente duradouro, sem remoção, expiração, descarte ou limite reconhecível. O analyzer não deve afirmar que encontrou um leak.

## Arquitetura e contratos

- Componente de integração: `src/StrictSharp.Analyzers/Analyzers/Perf011Analyzer.cs`. Ele permanece um analyzer por diagnóstico, com execução concorrente, e não conterá a heurística completa de ciclo de vida.
- Novo componente de análise: serviço estático específico de `PERF011` em `src/StrictSharp.Analyzers/Retention/`, recebendo a invocação, `SemanticModel` e contexto sintático necessário e devolvendo uma decisão imutável/por valor. O serviço não terá I/O, estado global, persistência ou chamadas externas.
- Helpers existentes: reutilizar `RuleAnalysis.IsRetainingCollection`, `RuleAnalysis.IsObservableSource`, `RuleAnalysis.IsInsideLoop` e a resolução semântica Roslyn quando apropriado. Não alterar helpers compartilhados para excluir globalmente `IEnumerable`, `string` ou coleções sem analisar impacto em outras regras.
- Símbolos aceitos: membro retentor resolvido semanticamente para coleção/observável conhecido; `IEnumerable<T>` sem operação retentora comprovada permanece desconhecido. Propriedades não devem ser tratadas como campos automaticamente: getter, tipo e efeitos/transferência precisam ser seguros para o caso.
- Estado da decisão: ausência de evidência suficiente resulta em omissão; não será introduzido um novo contrato público de confiança por diagnóstico nesta correção. A confiança declarada no catálogo continua sendo a da regra e a mensagem mantém a limitação de não provar leak.
- Fluxo de dados: nó de invocação -> símbolo do membro/receptor -> evidência de operação retentora -> evidência de ciclo de vida/limite/escape -> decisão -> `RuleAnalysis.Report` no receptor ou localização equivalente já usada pela regra.
- Compatibilidade: analyzer `netstandard2.1`, Roslyn/C# aprovados e testes `net10.0`/C# 14. Nenhuma dependência nova, profiling, telemetria ou mudança de catálogo será necessária.
- Concorrência e custo: análise somente leitura, local ao nó/método, sem caches mutáveis compartilhados e sem travessia geral da solução. Consultas semânticas devem ser feitas apenas após filtros sintáticos baratos.

## Alternativas consideradas

- Manter a heurística no `Perf011Analyzer` e adicionar exceções nominais: rejeitada porque perpetuaria a mistura de integração e análise, não resolveria ownership/escape e aumentaria o risco de falsos positivos por convenção textual.
- Tratar toda coleção criada com `new(n)` como limitada: rejeitada porque em `List<T>` o argumento é capacidade inicial e a coleção pode crescer.
- Fazer análise interprocedural/reachability completa para provar que a instância nunca escapa: rejeitada nesta etapa por custo, complexidade e escopo excluído; casos não demonstráveis devem permanecer desconhecidos ou silenciosos.
- Excluir todos os campos de instâncias e manter somente campos `static`: rejeitada porque reduziria demais a cobertura e não representa toda retenção potencial; a decisão deve combinar owner, vínculo, ciclo de vida e evidência de ausência de limite.
- Alterar a semântica global de `RuleAnalysis.IsRetainingCollection`: rejeitada porque o helper é compartilhado e a correção é específica da evidência da `PERF011`.

## Riscos e compatibilidade

- Risco de falso negativo em owners transitórios ou escapes indiretos que o analyzer não consegue resolver; mitigação: documentar a limitação, testar casos conhecidos e não alegar cobertura completa.
- Risco de continuar reportando um campo de instância comum como owner duradouro; mitigação: separar “campo retentor” de “evidência de vida longa” e exigir evidência adicional para reportar ou omitir quando o caso for ambíguo.
- Risco de considerar uma guarda parcial como limite global; mitigação: reconhecer apenas limites preservados no caminho analisado e omitir conclusões quando houver mutação/bypass não resolvido.
- Risco de regressão nos positivos atuais de `List<T>`, `ICollection<T>` e `IObservable<T>`; mitigação: conservar testes positivos com owner sem limpeza e executar a suíte completa.
- Risco de aumento do custo na IDE; mitigação: filtros sintáticos prévios, análise local, serviços estáticos e validação nos gates de build/teste/cobertura.
- Compatibilidade de contrato: não alterar ID, título, severidade, categoria, fase V3, configuração, localização esperada sem nova decisão; não adicionar Code Fix.

## Estratégia de testes

- Expandir `tests/StrictSharp.Analyzers.Tests/Perf011AnalyzerTests.cs` com matriz comportamental: positivos de coleção/observável semanticamente reconhecidos, negativos para métodos arbitrários, tipo abstrato sem operação retentora, propriedade não demonstrável, owner transitório demonstrável, ausência de escape, remoção/limpeza e limites efetivos.
- Adicionar explicitamente o negativo de `List<T>(n)`: capacidade inicial sozinha não silencia por “limite”; se o caso continuar sem evidência de owner duradouro, o resultado deve seguir a decisão de ciclo de vida, não a capacidade nominal.
- Cobrir `IEnumerable<T>` com implementação concreta conhecida e com implementação desconhecida, verificando que a interface não é usada como prova isolada e que `List<T>`/`HashSet<T>` não são confundidos quando a operação retentora é resolvida.
- Cobrir transferências ou escapes reconhecíveis para cache/singleton/closure/evento apenas nos padrões que o plano implementar; padrões não resolvidos devem permanecer silenciosos/condicionais, sem testes que exijam prova global.
- Preservar testes existentes e verificar ID, quantidade, localização e ausência de diagnósticos espúrios. Nenhum benchmark de runtime será usado para decidir a heurística; gates de build, teste, segurança e cobertura serão executados na implementação.
- Validar `AC-001` a `AC-008`, `FR-001` a `FR-006`, `NFR-001` a `NFR-003` e `CON-001` a `CON-004` por testes, inspeção de diff, rastreabilidade e gates configurados.

## Rastreabilidade

| Requisito | Critério de aceite | Tarefa |
|---|---|---|
| `FR-001` | `AC-001` | `TASK-002`, `TASK-003` |
| `FR-002` | `AC-002` | `TASK-001`, `TASK-002` |
| `FR-003` | `AC-003` | `TASK-001`, `TASK-002` |
| `FR-004` | `AC-004` | `TASK-003` |
| `FR-005` | `AC-005` | `TASK-002`, `TASK-003` |
| `FR-006` | `AC-006` | `TASK-001`, `TASK-004` |
| `NFR-001` | `AC-007` | `TASK-002`, `TASK-004` |
| `NFR-002` | `AC-004`, `AC-007` | `TASK-002`, `TASK-003` |
| `NFR-003` | `AC-008` | `TASK-004`, `TASK-005` |
| `CON-001` | `AC-005` | `TASK-002`, `TASK-005` |
| `CON-002` | `AC-008` | `TASK-002`, `TASK-005` |
| `CON-003` | `AC-002`, `AC-003`, `AC-005` | `TASK-002`, `TASK-003` |
| `CON-004` | `AC-007` | `TASK-004`, `TASK-005` |

## Ordem de execução

1. `TASK-001`: consolidar a matriz de regressão e os fixtures semânticos.
2. `TASK-002`: criar a análise estática separada para operação, tipo, propriedade e limites.
3. `TASK-003`: acrescentar ciclo de vida, ownership e escape locais e integrar a decisão ao analyzer.
4. `TASK-004`: alinhar documentação, rastreabilidade e validações de baixo ruído.
5. `TASK-005`: executar gates, revisar diff e fechar evidências.

Não há ciclos de dependência. A implementação depende dos cenários de regressão; documentação e gates dependem do comportamento final.
