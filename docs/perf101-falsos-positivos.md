# PERF101 - Complexidade algorítmica observável

A `PERF101` não transforma uma classificação `O(n)` em warning por si só. Um percurso linear necessário ou sem alternativa estrutural demonstrável permanece silencioso.

Na primeira entrega de redução de falsos positivos, o alerta exige evidência estática de:

- busca linear repetida dentro de um loop;
- mesma coleção `List<T>` estável;
- chave local ou parâmetro com tipo compatível com o elemento;
- alternativa estrutural compatível, como uma estrutura baseada em hash, cuja vantagem de busca possa ser explicada sem alterar a semântica observável.

Casos com fonte mutável, chave composta ou calculada, dispatch não resolvido, ordem/duplicidade relevante ou custo de construção não demonstrável permanecem silenciosos. A mensagem informa a forma estrutural (`O(n)`) e a oportunidade identificada, mas não promete medição, frequência real ou percentual de ganho.
