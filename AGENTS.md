# AGENTS.md

## Strict Sharp

Biblioteca de analise de codigo C#, com foco em performance.

## Fluxo de Desenvolvimento

Todo desenvolvimento deve seguir o fluxo abaixo:

1. **Especificação**: Descrição detalhada do que deve ser desenvolvido
2. **Planejamento**: Com a especificação, devemos planejar o que vamos implementar e o que precisamos alterar, além do que testar e como fazer o benchmark para avaliar a regra.
3. **Testes**: Sempre antes de implementar a especificação, temos criar os testes
4. **Implementação**: Com os testes, podemos implementar o analisador
5. **Benchmark**: Após todos os testes passarem, precisamos fazer benchmark da implementação apontada como gargalo de memoria.
6. **Documentação**: Precisamos documentar o que foi implementado para analises futuras

## Estrutura do projeto 

```
specs/
    feats/
    plans/
    docs/
src/
    StrictSharp.Analyzers/
tests/
    StrictSharp.Analyzers.Tests/
    StrictSharp.Analyzers.Benchmark/
```

- **specs/feats**: Especificações
- **specs/plans**: Planejamentos dos testes, implementações e documentação
- **specs/docs**: Documentação da implementação
- **src/StrictSharp.Analyzers**: Codigo do analisador
- **tests/StrictSharp.Analyzers.Tests**: Testes do analisador
- **tests/StrictSharp.Analyzers.Benchmark**: Benchmark que comprova que os itens analisador impactam a performance

## Regras de Especificação

Toda especificação deve ter:

- **Objetivo**
- **Contexto**
- **Fora do Contexto**
- **Regras de Aceitação**

## Estilo de Codigo

- Evite usar else
- Tente manter o codigo o mais proximo da margem
- Evite funções de mais de 20 linhas
- Evite alocações de memoria desnecessaria
- Evite boxing
- Evite operações de O(n) ou acima

## Mudanças Proibidas

- Não deve ser implementado nada que não tenha testes
- Não deve ser testado nada que não foi planejado
- Não devemos planejar nada que não foi especificado

## Criterios de Conclusão

- Para uma especificação ser conciderada concluida deve ter:
    - Planejamento
    - Testes
    - Implementação
    - Benchmark
    - Documentação
- Benchmark deve ter:
    - Minimo possivel de coletas de GC G0, e numca deve ter G1 e G2
    - Minimo possivel de memoria alocada

# Comportamento do agente

- Caso a especificação deixe brexas ou não de conteudo o suficiente para o planejamento, o agente deve questionar o que falta
- Deve sempre buscar novas expecificações
- E deve sempre aguardar a aprovação para a proxima etapa do desenvolvimento