<!-- spectro-managed: true -->
# Especificação: Documentação da V4 e da regra PERF100 no README

## Problema e objetivo

- Problema: o README documenta apenas as fases V1, V2 e V3 e apresenta as regras `PERF001`–`PERF030`, mas não informa que o StrictSharp possui a etapa V4 nem explica o funcionamento do diagnóstico `PERF100`. Isso deixa incompleta a descrição do catálogo atual e pode fazer o consumidor desconhecer a análise interprocedural.
- Objetivo: atualizar o README para incluir a V4 e explicar `PERF100` de forma consistente com o catálogo, a documentação V4 e as limitações da análise estática.

## Usuários e contexto

- Desenvolvedores e revisores que consultam o README para entender quais diagnósticos o pacote publicado oferece.
- Consumidores que precisam interpretar um alerta interprocedural sem confundi-lo com profiling, medição de custo efetivo ou uma regra oficial do .NET.

## Escopo

### Incluído

- Atualizar o estado atual do catálogo para incluir a fase V4 e o diagnóstico `PERF100`.
- Explicar que V4 analisa a propagação de custo entre métodos, chamadas, loops e fluxos relacionados quando a evidência for resolvida.
- Adicionar uma seção V4 com `PERF100`, incluindo o padrão identificado, significado do risco, severidade `High`, confiança `Média`, categoria `Performance` e orientação condicional.
- Explicar que dimensões como CPU, memória, I/O, concorrência, recursão e assincronicidade podem aparecer na evidência, sem alterar a categoria fixa da regra.
- Documentar limites: alvos desconhecidos, corpos ausentes, dependências externas, interfaces, delegates e dispatch não resolvido permanecem desconhecidos e não são tratados como custo zero.
- Linkar a documentação detalhada em `docs/csharp-performance-analyzer-v4.md` e, quando útil, o roadmap V4 em `docs/csharp-performance-analyzer-2.md`.
- Preservar a explicação de que `PERF100` não mede frequência real, CPU, memória, GC, I/O, complexidade efetiva ou percentual de ganho.

### Excluído

- Alterar a implementação de `PERF100`, o catálogo, os analyzers, os testes ou a documentação detalhada V4.
- Alterar a severidade, categoria, ID ou contrato funcional de `PERF100`.
- Adicionar novas regras V4 ou descrever capacidades futuras V5–V10 como se estivessem implementadas.
- Executar gates Spectro como parte desta feature documental; validações proporcionais de texto, links e regressão podem ser realizadas.

## Requisitos

### Requisitos funcionais

- `FR-001`: O README deve informar que o catálogo inclui 31 diagnósticos, distribuídos em V1 (15), V2 (13), V3 (2) e V4 (1), com `PERF100` identificado como diagnóstico V4, sem remover ou renomear as fases V1–V3.
- `FR-002`: O README deve explicar que a V4 identifica evidências de custo propagado entre métodos e relaciona chamadas, repetição, loops, alocações, materializações, bloqueios ou outros contextos observáveis quando resolvidos.
- `FR-003`: O README deve documentar `PERF100` com ID, nome, categoria `Performance`, severidade `High`, confiança `Média`, significado do risco e orientação condicional.
- `FR-004`: O README deve explicar que as dimensões dinâmicas da evidência podem incluir CPU, `Memory`, `I/O`, `Concurrency`, `Recursion` e `Async`, enquanto a categoria do diagnóstico permanece `Performance`.
- `FR-005`: O README deve declarar as limitações interprocedurais: corpos ausentes, alvos externos, interfaces, dispatch virtual, delegates e dependências não resolvidas permanecem desconhecidos e não equivalem a custo zero.
- `FR-006`: O README deve indicar que `PERF100` não mede frequência real, custo efetivo, CPU, memória, GC, I/O, complexidade efetiva ou percentual de ganho.
- `FR-007`: O README deve fornecer links funcionais para a documentação detalhada de `PERF100` e para o contexto/roadmap V4 quando referenciado.

### Requisitos não funcionais

- `NFR-001`: A documentação V4 deve usar linguagem consistente com as seções existentes de instalação, severidade, confiança e limitações.
- `NFR-002`: O README não deve apresentar V5–V10 ou capacidades futuras como funcionalidades implementadas do pacote.
- `NFR-003`: A atualização deve manter o Markdown legível e permitir consulta rápida por `PERF100`.
- `NFR-004`: A documentação deve preservar a distinção entre evidência estática e impacto dependente de entrada, frequência, carga, ambiente e comportamento de runtime.

### Restrições

- `CON-001`: `PERF100` deve ser documentado com os metadados existentes no `RuleCatalog`: categoria `Performance`, severidade `High`, confiança `Média`, fase V4 e referência `docs/csharp-performance-analyzer-v4.md`.
- `CON-002`: A alteração deve ser exclusivamente documental e limitar a mudança de produto ao `README.md` e aos artefatos de rastreabilidade da feature.
- `CON-003`: Os IDs `PERF001`–`PERF030` permanecem estáveis; `PERF100` é um ID do StrictSharp e não um ID oficial do .NET.
- `CON-004`: A recomendação para `PERF100` deve permanecer condicional e não pode tratar alvos desconhecidos como custo zero.

## Critérios de aceite

- `AC-001`: Dado o README atual, quando um leitor consultar o estado do catálogo, então verá V1, V2, V3 e V4, com `PERF100` identificado como diagnóstico V4.
- `AC-002`: Dado um leitor que procure a documentação V4, quando consultar a seção de diagnósticos, então encontrará `PERF100` com nome, categoria, severidade, confiança, significado e orientação.
- `AC-003`: Dado um fluxo em que um método chama outro dentro de repetição, quando o leitor consultar o funcionamento da V4, então entenderá que a regra relaciona a operação custosa ao chamador e à repetição quando a evidência é resolvida.
- `AC-004`: Dado um alerta `PERF100`, quando o leitor consultar as dimensões reportadas, então saberá que CPU, memória, I/O, concorrência, recursão e assincronicidade são dimensões de evidência, não categorias independentes do diagnóstico.
- `AC-005`: Dado um fluxo com corpo ausente, alvo externo, interface, delegate ou dispatch não resolvido, quando o leitor consultar as limitações, então verá que a incerteza é preservada e não convertida em custo zero.
- `AC-006`: Dado um leitor que interprete `PERF100` como medição, quando consultar a seção, então verá explicitamente que frequência, custo efetivo, complexidade e ganho percentual dependem de medição externa.
- `AC-007`: Dado um consumidor que queira aprofundar `PERF100`, quando seguir os links do README, então chegará a `docs/csharp-performance-analyzer-v4.md` e, se referenciado, ao roadmap V4 existente.

## Riscos, dependências e perguntas abertas

- Decisão registrada: o catálogo atual contém 31 diagnósticos, distribuídos em V1 (15), V2 (13), V3 (2) e V4 (1, `PERF100`); ver `clarifications.md`, `CL-001`.
- Risco: resumir análise interprocedural como prova de custo pode gerar interpretação incorreta; manter linguagem de evidência e risco potencial.
- Dependência: `src/StrictSharp.Analyzers/Rules/RuleCatalog.cs`, `src/StrictSharp.Analyzers/Analyzers/V4AnalyzerCatalog.cs`, `docs/csharp-performance-analyzer-v4.md` e `docs/csharp-performance-analyzer-2.md`.
- Dependência: a documentação existente de V1–V3 e a seção de severidade/confiança do README.
- Não há perguntas abertas bloqueantes; metadados, limites e escopo de validação estão registrados em `clarifications.md`.

