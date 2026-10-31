<!-- spectro-managed: true -->
# Esclarecimentos: Documentação de uso do pacote e significado das regras

## CL-001 — Estado de publicação e versionamento do pacote

- **Severidade:** Blocking
- **Pergunta:** O README deve documentar apenas `dotnet add package StrictSharp` para consumir a versão mais recente publicada, ou também deve incluir `dotnet add package StrictSharp --version 1.0.28`? O pacote `StrictSharp` já está publicado em um feed NuGet acessível ao consumidor?
- **Decisão:** O pacote está publicado com o ID `StrictSharp` na versão `1.0.28`. O README deve recomendar `dotnet add package StrictSharp` e pode incluir `dotnet add package StrictSharp --version 1.0.28` para instalações reproduzíveis.
- **Racional:** A confirmação humana estabelece a disponibilidade do pacote e a versão publicada recomendada. A versão `1.0.0` declarada atualmente no projeto não deve ser usada como versão publicada na documentação.
- **Requisitos afetados:** `FR-001`, `CON-002`, `AC-001`, `AC-008`.
- **Data:** 2026-10-30.

## Itens resolvidos por evidência

- **CL-002 — Escopo da alteração:** Resolvido. A feature permanece exclusivamente documental; não altera implementação, testes ou metadados do pacote, conforme `CON-003`.
- **CL-003 — Identidade do pacote:** Resolvido. O nome do pacote é `StrictSharp` e a versão publicada confirmada é `1.0.28`.
- **CL-004 — Significado das regras:** Resolvido. A documentação deve cobrir `PERF001`–`PERF030` sem alterar IDs, nomes, severidades ou critérios, conforme `CON-001` e `FR-004`.

