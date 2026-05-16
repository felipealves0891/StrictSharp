# 001 - Analise - Big(O)

## Objetivo

Planejar um analisador de custo de processamento, devemos planejar em etapas menores

## Contexto

### Ideia central

Big O descreve **como o custo de um algoritmo cresce conforme a entrada aumenta**.

Exemplo:

- `T(n) = 4n² - 2n + 2`
- Ignorando constantes e termos menores:
  - `O(n²)`

O foco não é tempo exato em segundos, mas **escalabilidade**.

---

### Classes principais de complexidade

Da mais eficiente para a menos eficiente:

| Complexidade | Nome | Característica |
|---|---|---|
| `O(1)` | Constante | Não cresce |
| `O(log n)` | Logarítmica | Crescimento muito lento |
| `O(n)` | Linear | Cresce proporcionalmente |
| `O(n²)` | Quadrática | Loops aninhados |
| `O(n^c)` | Polinomial | Escalável moderadamente |
| `O(c^n)` | Exponencial | Explode rapidamente |

Ponto importante:

- `O(n^c)` e `O(c^n)` são radicalmente diferentes.
- Exponencial fica inviável rapidamente.

---

### Conceito formal

Uma função `f(x)` é `O(g(x))` quando existe uma constante `C` tal que:

```math
|f(x)| <= C|g(x)|
```

Ou seja:

> `f` não cresce mais rápido que `g`.

---

### Big O vs outras notações

O documento também apresenta outras notações assintóticas:

| Notação | Significado |
|---|---|
| `O(g(n))` | limite superior |
| `o(g(n))` | cresce muito mais devagar |
| `Ω(g(n))` | limite inferior |
| `Θ(g(n))` | crescimento exato assintótico |

Exemplo importante:

- Dizer:
  - “Heapsort é `O(n log n)`”
- é correto, mas fraco.
- Melhor:
  - “Heapsort é `Θ(n log n)`”

Porque descreve o comportamento real do algoritmo.

---

### Complexidade vs Performance

O documento separa claramente:

#### Performance

Tempo real na máquina.

Depende de:
- hardware
- compilador
- cache
- SO
- otimizações

#### Complexidade

Como o algoritmo escala.

Independente da máquina.

Essa é a parte realmente importante em algoritmos.

---

### Operações básicas

A análise considera operações primitivas como:

- soma
- multiplicação
- atribuição
- leitura
- escrita
- comparação

A complexidade é proporcional ao número dessas operações.

---

### Worst Case

O foco normalmente é o **pior caso**.

Exemplo:
Inserir no início de um array exige mover todos os elementos.

Resultado:

- custo linear:
  - `O(n)`

---

### Como calcular complexidade

#### Sequência de instruções

```text
a;
b;
c;
```

Somam os custos.

Se tudo for constante:

- `O(1)`

---

#### If/Else

A complexidade é o maior bloco:

```text
max(bloco1, bloco2)
```

Exemplo:
- um lado `O(1)`
- outro `O(n)`

Resultado:
- `O(n)`

---

#### Loops

Loop simples:

```text
for 1..N
```

Executa N vezes:

- `O(n)`

---

#### Loops aninhados

```text
for 1..N
   for 1..M
```

Complexidade:

- `O(N*M)`

Caso comum:
- `N == M`

Resultado:
- `O(n²)`

---

#### Chamadas de função

A complexidade da função chamada entra na conta.

Exemplo:

```text
for J in 1..N
    g(J)
```

Se:
- `g(k) = O(k)`

Então:

- complexidade total:
  - `O(n²)`

---

### Mensagem principal do PDF

Big O existe para responder:

> “Esse algoritmo continua viável quando a entrada cresce?”

Essa é a métrica fundamental de engenharia de algoritmos.

Constantes importam pouco.
Escalabilidade importa muito.

## Fora do contexto

Não vamos fazer nada alem de planejar analisador

## Regras de Aceitação

Os planos devem ser incrementais
Devem cubrir todo o Big(O)
