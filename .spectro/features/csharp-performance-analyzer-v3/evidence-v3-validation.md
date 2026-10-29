<!-- spectro-managed: true -->
# Evidência de validação V3

## Hipóteses

- `PERF011`: adicionar repetidamente elementos a uma coleção mantida em campo, sem remoção/limpeza observável, pode aumentar a memória viva ao longo da execução.
- `PERF030`: reconstruir em loop uma instância com argumentos invariantes e inicialização não trivial repete custo de CPU e alocação; reutilização pode reduzir esse custo quando o contrato permitir.

## Cenários comparativos reproduzíveis

Os cenários devem comparar uma implementação que repete a retenção/reconstrução com uma implementação que limita a retenção ou reutiliza uma instância imutável. A validação deve coletar, em execução representativa, memória viva após períodos prolongados para `PERF011` e alocações/CPU para `PERF030`. Os números são evidência do cenário, não limiares do analyzer.

## Evidência estática disponível

- Testes `Perf011AnalyzerTests` cobrem diagnóstico positivo, coleção local e limpeza explícita.
- Testes `Perf030AnalyzerTests` cobrem diagnóstico positivo, argumentos dependentes da iteração e criação fora do loop.
- `V3TestMatrixTests` confirma IDs, ordem, neutralidade, compatibilidade C# 14 e separação dos catálogos V1/V2.

## Limitação

O repositório não adiciona dependência de benchmark nem telemetria ao analyzer. A medição comparativa deve ser executada com carga representativa antes de afirmar impacto quantitativo; o diagnóstico permanece baseado apenas em evidência estática.
