namespace StrictSharp.Analyzers.Rules;

public static class RuleCatalog
{
    private const string Documentation = "docs/csharp-performance-analyzer.md";
    private const string DocumentationV4 = "docs/csharp-performance-analyzer-v4.md";
    private const string DocumentationV5 = "docs/csharp-performance-analyzer-v5.md";

    private static readonly RuleDefinition[] s_all =
    [
        Create("PERF001", "Bloqueio síncrono de operação assíncrona", RiskSeverity.Critical, "Concurrency", "Alta", "Bloqueia threads e pode reduzir throughput.", "O custo cresce com a concorrência e o tempo de espera.", "Usar await e propagar o fluxo assíncrono.", "Não emitir para tarefa comprovadamente concluída ou fronteira síncrona intencional.", Documentation, RulePhase.V1),
        Create("PERF002", "Paralelismo sem limite", RiskSeverity.Critical, "Concurrency", "Média", "Retém tarefas, memória e recursos concorrentes.", "O custo cresce com o tamanho da coleção e a concorrência.", "Limitar concorrência com processamento em lotes ou mecanismo apropriado.", "Não emitir para coleção comprovadamente pequena ou limite garantido externamente.", Documentation, RulePhase.V2),
        Create("PERF003", "Task.Run desnecessário em I/O", RiskSeverity.High, "Concurrency", "Alta", "Adiciona agendamento e filas no ThreadPool sem benefício de I/O.", "O overhead se repete a cada operação encapsulada.", "Aguardar diretamente a operação assíncrona.", "Permitir isolamento de API síncrona bloqueante ou processamento CPU-bound.", Documentation, RulePhase.V1),
        Create("PERF004", "Lock durante operação longa ou bloqueante", RiskSeverity.Critical, "Concurrency", "Média", "Mantém exclusão mútua durante espera e aumenta contenção.", "A fila cresce com a duração e o número de concorrentes.", "Reduzir a região crítica e separar I/O do estado compartilhado.", "Não emitir para operação curta com custo delimitado e sincronização necessária.", Documentation, RulePhase.V2),
        Create("PERF005", "Espera ativa ou polling excessivo", RiskSeverity.High, "Concurrency", "Média", "Consome CPU e repete chamadas sem necessidade.", "O custo cresce com a frequência do polling e o tempo de espera.", "Usar sinalização, filas, Channel<T> ou intervalos adequados.", "Permitir spin-wait curto e deliberadamente justificado.", Documentation, RulePhase.V2),
        Create("PERF006", "Criação excessiva de threads ou tarefas", RiskSeverity.High, "Concurrency", "Média", "Aumenta agendamento, troca de contexto e coordenação.", "O custo cresce com a quantidade de itens ou execuções.", "Reutilizar workers e limitar concorrência.", "Permitir thread dedicada de longa duração ou tarefa pontual justificada.", Documentation, RulePhase.V2),
        Create("PERF007", "Alocação recorrente dentro de loops", RiskSeverity.High, "Memory", "Média", "Eleva alocações e pressão sobre o GC.", "O custo cresce com o número de iterações.", "Reutilizar recursos ou usar pool quando seguro.", "Não emitir para objeto pequeno, necessário e de vida curta quando reuso for desproporcional.", Documentation, RulePhase.V1),
        Create("PERF008", "Alocações grandes recorrentes", RiskSeverity.Critical, "Memory", "Alta", "Pode pressionar LOH e aumentar coletas Gen 2.", "O custo cresce com repetições e tamanho dos buffers.", "Usar streaming, segmentação ou ArrayPool<T> quando seguro.", "Não emitir para alocação única e controlada.", Documentation, RulePhase.V1),
        Create("PERF009", "Boxing evitável", RiskSeverity.Medium, "Memory", "Média", "Adiciona conversão e possível alocação de value types.", "O custo é relevante em muitas operações pequenas.", "Preferir APIs genéricas e tipadas.", "Permitir boxing exigido pelo contrato ou eliminado internamente.", Documentation, RulePhase.V2),
        Create("PERF010", "Capturas e closures desnecessárias", RiskSeverity.Medium, "Memory", "Média", "Pode alocar closures/delegates e reter referências.", "O custo cresce com a frequência de criação e retenção.", "Preferir lambda static, delegate reutilizado ou estado explícito.", "Não emitir quando não houver alocação efetiva ou a execução for pouco frequente.", Documentation, RulePhase.V2),
        Create("PERF011", "Retenção indevida de objetos", RiskSeverity.Critical, "Memory", "Baixa", "Pode aumentar memória viva, Gen 2 e risco de esgotamento.", "O custo cresce com o tempo e o volume retido.", "Limitar tamanho/tempo, expirar entradas e liberar inscrições.", "Não emitir para retenção delimitada ou necessária por toda a aplicação.", Documentation, RulePhase.V3),
        Create("PERF012", "Cópias desnecessárias de coleções e buffers", RiskSeverity.High, "Memory", "Alta", "Adiciona custo linear de CPU, memória temporária e cópia.", "O custo cresce com o tamanho dos dados.", "Reutilizar a representação original quando compatível.", "Permitir snapshot, isolamento de mutabilidade ou transferência de propriedade.", Documentation, RulePhase.V1),
        Create("PERF013", "Enumeração repetida de IEnumerable", RiskSeverity.High, "LINQ", "Alta", "Repete processamento e pode repetir consultas ou I/O.", "O custo multiplica-se pelo número de enumerações.", "Materializar uma vez ou reutilizar coleção materializada.", "Não emitir para sequência barata, reutilizável e intencional.", "CA1851; " + Documentation, RulePhase.V1),
        Create("PERF014", "Busca linear repetida em coleção", RiskSeverity.Critical, "LINQ", "Alta", "Pode gerar complexidade O(n × m) em loops aninhados.", "O custo cresce com o produto dos tamanhos das coleções.", "Avaliar HashSet<T> ou Dictionary<TKey,TValue>.", "Não emitir para conjunto pequeno, busca única ou índice mais caro que a economia.", Documentation, RulePhase.V1),
        Create("PERF015", "Uso inadequado de Count, Any e propriedades", RiskSeverity.Medium, "LINQ", "Alta", "Pode adicionar enumeração e overhead desnecessários.", "O custo cresce com a enumeração e a frequência da consulta.", "Preferir Count, Length, IsEmpty ou API direta adequada.", "Permitir Any(predicate) ou coleção sem contagem eficiente.", "CA1827, CA1829, CA1860; " + Documentation, RulePhase.V1),
        Create("PERF016", "Ordenação repetida ou desnecessária", RiskSeverity.High, "LINQ", "Média", "Repete custo de ordenação e buffers temporários.", "O custo típico é O(n log n) por ordenação.", "Ordenar uma vez ou usar seleção quando apropriado.", "Permitir ordenação necessária após mutação dos dados.", Documentation, RulePhase.V2),
        Create("PERF017", "Consultas LINQ repetidas dentro de loops", RiskSeverity.High, "LINQ", "Média", "Recalcula filtros, projeções e agregações invariantes.", "O custo cresce com iterações e tamanho da sequência.", "Antecipar cálculo ou criar índice reutilizável.", "Permitir consulta dependente da iteração ou com efeitos relevantes.", Documentation, RulePhase.V2),
        Create("PERF018", "Concatenação repetitiva de strings", RiskSeverity.High, "Strings", "Alta", "Pode gerar cópias e alocações repetidas.", "O custo de cópia pode aproximar-se de O(n²).", "Usar StringBuilder ou string.Join conforme o caso.", "Permitir concatenação pequena, delimitada e otimizada pelo compilador.", Documentation, RulePhase.V1),
        Create("PERF019", "Substrings e conversões desnecessárias", RiskSeverity.Medium, "Strings", "Média", "Cria strings e cópias intermediárias evitáveis.", "O custo cresce com o tamanho e a frequência do texto.", "Usar ReadOnlySpan<char> ou parsing sem materialização quando possível.", "Permitir necessidade de armazenar ou transferir o resultado.", Documentation, RulePhase.V1),
        Create("PERF020", "Normalização de strings para comparação", RiskSeverity.Medium, "Strings", "Alta", "Adiciona processamento e alocações antes da comparação.", "O custo cresce com tamanho e frequência das strings.", "Usar StringComparison adequado.", "Permitir normalização exigida para persistência ou uso posterior.", "CA1862; " + Documentation, RulePhase.V1),
        Create("PERF021", "Regex ou metadados recriados", RiskSeverity.High, "Strings", "Média", "Repete parsing, compilação ou preparação de metadados.", "O custo cresce com a frequência de construção.", "Reutilizar instâncias, cache ou GeneratedRegex quando suportado.", "Permitir configuração dinâmica ou API que já faça cache.", Documentation, RulePhase.V2),
        Create("PERF022", "Consulta redundante em Dictionary", RiskSeverity.Medium, "Collections", "Alta", "Repete hash e comparação em consultas sequenciais.", "O overhead se repete por acesso redundante.", "Usar TryGetValue ou Remove diretamente quando equivalente.", "Permitir contratos diferentes ou condições independentes.", "CA1853, CA1854; " + Documentation, RulePhase.V1),
        Create("PERF023", "Coleção inadequada para padrão de acesso", RiskSeverity.High, "Collections", "Média", "Pode exigir deslocamentos, cópias e buscas lineares.", "O custo cresce com tamanho e operações dominantes.", "Avaliar Queue<T>, HashSet<T>, Dictionary<TKey,TValue> ou estrutura adequada.", "Permitir coleção pequena ou localidade superior da alternativa original.", Documentation, RulePhase.V2),
        Create("PERF024", "Crescimento repetido de coleções", RiskSeverity.Medium, "Collections", "Média", "Pode realocar e copiar buffers internos.", "O custo cresce com inserções e expansões.", "Informar capacidade quando conhecida.", "Permitir capacidade desconhecida ou estratégia adequada existente.", Documentation, RulePhase.V2),
        Create("PERF025", "Uso inadequado de HttpClient", RiskSeverity.Critical, "I/O", "Alta", "Pode perder pooling e exaurir portas/conexões.", "O risco cresce com criação por operação e concorrência.", "Reutilizar cliente/handler ou usar IHttpClientFactory.", "Permitir factory ou reutilização comprovada do handler.", Documentation, RulePhase.V1),
        Create("PERF026", "I/O síncrono em fluxo assíncrono", RiskSeverity.High, "I/O", "Média", "Mantém threads bloqueadas aguardando rede, arquivo ou stream.", "O custo cresce com concorrência e latência externa.", "Usar APIs assíncronas quando houver benefício de escalabilidade.", "Permitir operação curta, API sem alternativa ou código genuinamente síncrono.", Documentation, RulePhase.V1),
        Create("PERF027", "Logging com custo evitável", RiskSeverity.High, "Observability", "Alta", "Pode formatar ou serializar mesmo quando o nível está desabilitado.", "O custo cresce com frequência e complexidade da mensagem.", "Usar logging estruturado, LoggerMessage e checagem condicional para trabalho caro.", "Permitir mensagem pré-construída, valor disponível ou baixa frequência.", "CA1848; " + Documentation, RulePhase.V1),
        Create("PERF028", "Recursos IDisposable não liberados", RiskSeverity.High, "I/O", "Média", "Pode reter handles, conexões e buffers.", "O custo cresce com operações e duração do processo.", "Usar using, await using ou ciclo de vida controlado.", "Permitir propriedade transferida ou ciclo de vida externo.", Documentation, RulePhase.V2),
        Create("PERF029", "Exceções como fluxo normal", RiskSeverity.High, "CPU", "Média", "Cria exceções e stack traces para falhas esperadas.", "O custo cresce com a frequência de falhas.", "Usar TryParse, TryGetValue ou resultado explícito.", "Permitir falha excepcional, rara ou exigida pelo contrato.", Documentation, RulePhase.V2),
        Create("PERF030", "Reconstrução de objetos pesados invariantes", RiskSeverity.High, "Memory", "Baixa", "Repete inicialização, alocações e pressão sobre o GC.", "O custo cresce com a frequência de reconstrução.", "Compartilhar instâncias imutáveis ou cachear no escopo apropriado.", "Permitir estado variável, não thread-safe ou isolamento por operação.", Documentation, RulePhase.V3),
        Create("PERF100", "Custo interprocedural propagado", RiskSeverity.High, "Performance", "Média", "Pode propagar CPU, memória, I/O ou bloqueios por uma cadeia de chamadas e repetição.", "O impacto potencial depende do caminho de chamadas, da repetição e do volume de entrada.", "Revisar a operação custosa no ponto apropriado do fluxo e validar a alternativa com medição representativa.", "Não emitir quando o alvo, a repetição, a invariância ou o bloqueio não forem demonstráveis; desconhecidos permanecem explícitos.", DocumentationV4, RulePhase.V4, "[PERF100] Custo interprocedural propagado. Severidade: High. Categoria: Performance. Evidência: {0}. Dimensão: {1}. Cadeia: {2} -> {3}. Impacto: pode ampliar custo no fluxo analisado. Escalabilidade: depende de repetição, entrada e carga. Correção: revisar o ponto apropriado e validar com medição representativa. Exceções: alvos ou comportamentos desconhecidos não são tratados como custo zero. Confiança: Média. Exemplo: consulte a documentação V4. Referência: docs/csharp-performance-analyzer-2.md. Limitação: custo efetivo, frequência e ganho percentual não são medidos pela análise estática.")
        ,Create("PERF101", "Complexidade algorítmica observável", RiskSeverity.High, "Complexity", "Média", "Pode ampliar o trabalho conforme o tamanho estrutural das entradas.", "A classificação informa O(1), O(n), O(n log n), O(n²) ou desconhecido quando sustentada pelo código.", "Revisar loops, buscas e transformações que determinam a forma de crescimento.", "Não emitir classificação exata para limites dinâmicos, dispatch ou comportamento não resolvido.", DocumentationV5, RulePhase.V5),
        Create("PERF102", "Alocação conhecida por repetição", RiskSeverity.High, "Allocation", "Média", "Pode multiplicar alocações por iterações ou chamadas conhecidas.", "O custo estrutural depende do multiplicador de repetição observável.", "Reutilizar ou mover a alocação quando a semântica permitir.", "Não emitir quando a quantidade de execuções não puder ser derivada com confiança.", DocumentationV5, RulePhase.V5),
        Create("PERF103", "Payload de memória estimável", RiskSeverity.High, "Memory", "Média", "Pode produzir pressão de memória proporcional ao payload conhecido.", "Bytes de payload são separados de cabeçalhos, alinhamento e efeitos de runtime.", "Revisar tamanho, ciclo de vida e estratégia de armazenamento do buffer.", "Não tratar payload como memória total, pausa de GC ou memória viva.", DocumentationV5, RulePhase.V5),
        Create("PERF104", "Trabalho amplificado por repetição", RiskSeverity.High, "Repetition", "Média", "Pode repetir uma operação ao longo de loops ou cadeias de chamadas.", "Multiplicadores conhecidos são apresentados por caminho; fronteiras desconhecidas permanecem explícitas.", "Revisar invariantes e o ponto de execução da operação repetida.", "Não emitir multiplicador exato quando o limite depender de entrada ou runtime não resolvido.", DocumentationV5, RulePhase.V5),
        Create("PERF105", "Operação de I/O repetida", RiskSeverity.High, "I/O", "Média", "Pode repetir operações externas e ampliar consumo de recursos ou espera.", "A análise informa quantidade estrutural conhecida, não duração ou latência.", "Revisar batching, cache, streaming ou posição da operação no fluxo.", "Não inferir disponibilidade, volume transferido ou tempo de resposta.", DocumentationV5, RulePhase.V5),
        Create("PERF106", "Concorrência estruturalmente amplificada", RiskSeverity.High, "Concurrency", "Média", "Pode aumentar tarefas, filas, bloqueios ou coordenação concorrente.", "Limites estruturais conhecidos são separados da capacidade efetiva do runtime.", "Revisar limites, filas e ciclo de vida das tarefas antes de alterar a concorrência.", "Não inferir throughput, execução simultânea efetiva ou capacidade do runtime.", DocumentationV5, RulePhase.V5),
        Create("PERF107", "Comparação estrutural de alternativas", RiskSeverity.High, "Performance", "Média", "Pode evidenciar uma alternativa com menor trabalho estrutural conhecido.", "A comparação limita-se a Big-O e contagens demonstráveis, sem percentual de ganho.", "Validar a alternativa com medição representativa antes de adotá-la.", "Não emitir comparação quando a equivalência semântica ou a forma estrutural não puder ser demonstrada.", DocumentationV5, RulePhase.V5)
    ];

    private static readonly RuleDefinition[] s_v1 =
    [
        s_all[0], s_all[2], s_all[6], s_all[7], s_all[11], s_all[12], s_all[13], s_all[14],
        s_all[17], s_all[18], s_all[19], s_all[21], s_all[24], s_all[25], s_all[26]
    ];

    private static readonly RuleDefinition[] s_v2 =
    [
        s_all[1], s_all[3], s_all[4], s_all[5], s_all[8], s_all[9], s_all[15],
        s_all[16], s_all[20], s_all[22], s_all[23], s_all[27], s_all[28]
    ];

    private static readonly RuleDefinition[] s_v3 =
    [
        s_all[10], s_all[29]
    ];

    private static readonly RuleDefinition[] s_v4 =
    [
        s_all[30]
    ];

    private static readonly RuleDefinition[] s_v5 =
    [
        s_all[31], s_all[32], s_all[33], s_all[34], s_all[35], s_all[36], s_all[37]
    ];

    public static IReadOnlyList<RuleDefinition> All => s_all;
    public static IReadOnlyList<RuleDefinition> V1 => s_v1;
    public static IReadOnlyList<RuleDefinition> V2 => s_v2;
    public static IReadOnlyList<RuleDefinition> V3 => s_v3;
    public static IReadOnlyList<RuleDefinition> V4 => s_v4;
    public static IReadOnlyList<RuleDefinition> V5 => s_v5;

    public static RuleDefinition Get(string id)
    {
        for (var index = 0; index < s_all.Length; index++)
        {
            if (string.Equals(s_all[index].Id, id, StringComparison.Ordinal))
                return s_all[index];
        }

        throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown performance rule.");
    }

    private static RuleDefinition Create(
        string id,
        string title,
        RiskSeverity severity,
        string category,
        string confidence,
        string impact,
        string scalability,
        string correction,
        string exceptions,
        string reference,
        RulePhase phase,
        string? messageFormat = null)
    {
        return new RuleDefinition(id, title, severity, category, confidence, impact, scalability, correction, exceptions, reference, phase, messageFormat);
    }
}
