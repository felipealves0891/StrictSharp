# AGENTS.md

Este projeto é um analisador estatico de C#, focado em performance e anti-padrões.

## Tecnologias

- Roslyn
- C# / .NET
- Specification First

## Padrões de Codigo

- Evitar o uso de Else
- Manter o codigo o mais proximo da margem
- Reutilizar codigo sempre que possivel
- Evitar alocar memoria desnecessariamente
- Prefirir tipos por valor

## Padrões de Design

- Não manter a logica de analise junto com o analisador
- Devemos ter um analisador por diagnosticos
- Serviços devem ser metodos estaticos, podendo usar estancias em casos especificos
- Devemos pensar na performance, em primeiro lugar

