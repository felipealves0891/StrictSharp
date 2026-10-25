namespace StrictSharp.Analyzers.Rules;

public static class RuleCatalog
{
    private const string Documentation = "docs/csharp-performance-analyzer.md";

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
        Create("PERF030", "Reconstrução de objetos pesados invariantes", RiskSeverity.High, "Memory", "Baixa", "Repete inicialização, alocações e pressão sobre o GC.", "O custo cresce com a frequência de reconstrução.", "Compartilhar instâncias imutáveis ou cachear no escopo apropriado.", "Permitir estado variável, não thread-safe ou isolamento por operação.", Documentation, RulePhase.V3)
    ];

    private static readonly RuleDefinition[] s_v1 =
    [
        s_all[0], s_all[2], s_all[6], s_all[7], s_all[11], s_all[12], s_all[13], s_all[14],
        s_all[17], s_all[18], s_all[19], s_all[21], s_all[24], s_all[25], s_all[26]
    ];

    public static IReadOnlyList<RuleDefinition> All => s_all;
    public static IReadOnlyList<RuleDefinition> V1 => s_v1;

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
        RulePhase phase)
    {
        return new RuleDefinition(id, title, severity, category, confidence, impact, scalability, correction, exceptions, reference, phase);
    }
}
