<!-- spectro-managed: true -->
# Especificação: Documentação de uso do pacote e significado das regras

## Problema e objetivo

- Problema: o README orienta o consumidor principalmente pela referência de projeto e informa que a distribuição NuGet ainda não está configurada, embora o projeto tenha metadados de pacote para `StrictSharp`. Além disso, as tabelas de diagnósticos descrevem as regras de forma desigual, dificultando entender o risco que cada uma representa, o que ela não prova e como interpretar sua severidade.
- Objetivo: atualizar o README para orientar a instalação do analisador por `dotnet add package StrictSharp` e apresentar as regras `PERF001`–`PERF030` com descrições consistentes, significado do risco, orientação geral e limitações da análise estática.

## Usuários e contexto

- Desenvolvedores que adicionam o analisador a um projeto .NET consumidor.
- Revisores que precisam interpretar um diagnóstico StrictSharp sem confundi-lo com medição de performance ou com uma regra oficial do .NET.

## Escopo

### Incluído

- Atualizar a seção de instalação/uso do README para o pacote NuGet `StrictSharp`, usando o comando `dotnet add package StrictSharp`.
- Explicar que o pacote é um analyzer Roslyn e que sua instalação não representa uma referência de runtime à biblioteca de aplicação.
- Manter a alternativa de referência de projeto apenas como fluxo local, se ela continuar útil para desenvolvimento do repositório.
- Revisar a documentação das regras `PERF001` a `PERF030`, incluindo o padrão identificado e o significado do risco.
- Explicar severidade, confiança, impacto potencial, limitações e a configuração por `.editorconfig`.
- Manter os avisos de que a análise estática não substitui profiling, benchmarks ou validação em carga representativa.

### Excluído

- Alterar a implementação dos analyzers, o catálogo, os IDs, as severidades ou os critérios de detecção.
- Criar Code Fix, alterar o empacotamento NuGet ou publicar uma versão do pacote.
- Adicionar novas regras ou afirmar ganhos percentuais sem evidência de benchmark.
- Documentar comportamento de runtime que não possa ser sustentado pelo catálogo e pela especificação existentes.

## Requisitos

### Requisitos funcionais

- `FR-001`: O README deve instruir a instalação do analyzer pelo pacote NuGet publicado `StrictSharp`, com os exemplos executáveis `dotnet add package StrictSharp` e, para instalações reproduzíveis, `dotnet add package StrictSharp --version 1.0.28`.
- `FR-002`: O README deve explicar o efeito da instalação do pacote no projeto consumidor, incluindo que os diagnósticos são executados pelo Roslyn durante build/IDE e que não são uma dependência de runtime da aplicação.
- `FR-003`: O README deve explicar como ajustar a severidade ou desabilitar diagnósticos por ID usando `.editorconfig`, sem sugerir que a severidade seja uma medição de custo ou de ganho.
- `FR-004`: O README deve descrever todas as regras `PERF001`–`PERF030` de maneira consistente, indicando o padrão observado e o risco potencial que motivou o diagnóstico.
- `FR-005`: O README deve distinguir custo estrutural evidenciado no código de impacto dependente de frequência, volume de dados, carga, ambiente, concorrência ou ciclo de vida.
- `FR-006`: O README deve explicar o significado das categorias de severidade (`Critical`, `High`, `Medium` e `Low`) e da confiança da detecção, incluindo que os IDs `PERFxxx` são do StrictSharp e não IDs oficiais do .NET.
- `FR-007`: O README deve preservar ou atualizar os links para a documentação detalhada das regras e indicar onde encontrar exemplos, exceções e limitações.

### Requisitos não funcionais

- `NFR-001`: A documentação deve ser escrita em português claro, com terminologia consistente entre instalação, catálogo, severidade e limitações.
- `NFR-002`: Os exemplos de comandos, XML e `.editorconfig` devem ser copiáveis e sintaticamente válidos para o fluxo documentado.
- `NFR-003`: A documentação não deve prometer detecção completa de memory leaks, ganho de performance, redução percentual de custo ou equivalência universal de uma recomendação.
- `NFR-004`: O README deve permanecer navegável em Markdown, com tabelas legíveis e organização adequada para consulta rápida por ID.

### Restrições

- `CON-001`: Os IDs, nomes, fases e severidades atuais das regras devem ser tratados como contrato documental; qualquer divergência encontrada deve ser registrada para decisão, não corrigida silenciosamente no README.
- `CON-002`: A documentação deve refletir o pacote publicado com ID `StrictSharp` e versão confirmada `1.0.28`; não deve apresentar a versão `1.0.0` do arquivo de projeto como versão publicada.
- `CON-003`: A alteração desta feature é exclusivamente documental e não deve modificar código de produção, testes ou metadados do pacote.
- `CON-004`: Recomendações devem permanecer condicionais quando dependem de mutabilidade, igualdade, thread safety, propriedade dos dados, ciclo de vida ou semântica da API.

## Critérios de aceite

- `AC-001`: Dado um desenvolvedor com um projeto .NET consumidor, quando ele consultar a seção de instalação do README, então encontrará o comando `dotnet add package StrictSharp` e uma explicação de que o pacote adiciona o analyzer ao build/IDE.
- `AC-002`: Dado um leitor que procura o fluxo local, quando ele consultar a documentação, então encontrará a referência de projeto claramente separada do fluxo recomendado de pacote, sem a afirmação desatualizada de que o NuGet não está configurado.
- `AC-003`: Dado um leitor que precisa configurar um diagnóstico, quando ele consultar a seção de configuração, então encontrará exemplos de `dotnet_diagnostic.PERFxxx.severity` e os valores aceitos, incluindo como desabilitar uma regra.
- `AC-004`: Dado qualquer ID entre `PERF001` e `PERF030`, quando ele for procurado no catálogo do README, então haverá uma descrição do padrão identificado, do risco que significa e da orientação ou ressalva aplicável.
- `AC-005`: Dado um diagnóstico reportado em um caminho pouco executado ou com entrada pequena, quando o leitor consultar seu significado, então a documentação deixará claro que o alerta representa risco potencial e não prova de impacto relevante em produção.
- `AC-006`: Dado um leitor que confunda severidade com retorno de otimização, quando ele consultar a seção de interpretação, então encontrará a distinção entre severidade, confiança e impacto medido.
- `AC-007`: Dado um consumidor que queira aprofundar uma regra, quando ele seguir a documentação do README, então chegará ao documento detalhado correspondente, com exemplos e limitações quando disponíveis.
- `AC-008`: Dado o README atualizado, quando exemplos de comandos, XML e `.editorconfig` forem copiados para um projeto compatível, então não haverá instrução que exija referência manual ao assembly do analyzer para o fluxo NuGet.

## Riscos, dependências e perguntas abertas

- Risco: o catálogo do README pode divergir dos metadados ou da documentação detalhada; a revisão deve comparar os IDs e nomes antes da implementação.
- Decisão registrada: o pacote `StrictSharp` está publicado na versão `1.0.28`; o README deve documentar o comando sem versão e o comando com `--version 1.0.28` para reprodutibilidade. Ver `clarifications.md`, `CL-001`.
- Dependência: `src/StrictSharp.Analyzers/StrictSharp.Analyzers.csproj`, o catálogo de regras e `docs/csharp-performance-analyzer*.md` são as fontes para validar nomes, IDs, fases e limitações.
- Não há perguntas abertas bloqueantes para esta especificação.

