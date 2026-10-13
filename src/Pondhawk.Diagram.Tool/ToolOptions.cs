namespace Pondhawk.Diagram.Tool;

internal sealed class ToolOptions
{
    public const string StandardInput = "-";

    public const string Usage = """
        Usage: pondhawk-diagram <model.json> [options]
               <exporter> | pondhawk-diagram - [options]

          <model.json>               The diagram model to draw; - reads it from standard input
          -o, --output <path>        Output file (default: the model's name with .html,
                                     or er-diagram.html when reading standard input)
          -t, --title <text>         Title, overriding the model's name
          -d, --description <text>   Subtitle, overriding the model's description
              --print-schema         Print the JSON Schema for the model format and exit
          -h, --help                 Show this help
        """;

    public string Input { get; private set; } = "";
    public string Output { get; private set; } = "";
    public string? Title { get; private set; }
    public string? Description { get; private set; }
    public bool PrintSchema { get; private set; }
    public bool Help { get; private set; }

    public bool ReadsStandardInput => Input == StandardInput;

    /// <summary>Throws <see cref="ArgumentException"/> with a user-facing message on bad input.</summary>
    public static ToolOptions Parse(string[] args)
    {
        var options = new ToolOptions();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            string Value()
            {
                if (i + 1 >= args.Length)
                    throw new ArgumentException($"{arg} needs a value.");
                return args[++i];
            }

            switch (arg)
            {
                case "-h" or "--help": options.Help = true; return options;
                case "--print-schema": options.PrintSchema = true; return options;
                case "-o" or "--output": options.Output = Value(); break;
                case "-t" or "--title": options.Title = Value(); break;
                case "-d" or "--description": options.Description = Value(); break;
                case StandardInput:
                case not null when !arg.StartsWith('-'):
                    if (options.Input.Length > 0)
                        throw new ArgumentException($"Only one model can be drawn at a time; got \"{options.Input}\" and \"{arg}\".");
                    options.Input = arg;
                    break;
                default: throw new ArgumentException($"Unknown option: {arg}");
            }
        }

        if (options.Input.Length == 0)
            throw new ArgumentException("A model file is required (or - to read standard input).");

        if (options.Output.Length == 0)
            options.Output = options.ReadsStandardInput ? "er-diagram.html" : Path.ChangeExtension(options.Input, ".html");

        return options;
    }
}
