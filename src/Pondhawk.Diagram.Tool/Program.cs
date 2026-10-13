using System.Text;
using Pondhawk.Diagram;
using Pondhawk.Diagram.Tool;

ToolOptions options;
try
{
    options = ToolOptions.Parse(args);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine();
    Console.Error.WriteLine(ToolOptions.Usage);
    return 2;
}

if (options.Help)
{
    Console.WriteLine(ToolOptions.Usage);
    return 0;
}

if (options.PrintSchema)
{
    Console.Write(DiagramModel.JsonSchema);
    return 0;
}

DiagramModel model;
try
{
    var json = options.ReadsStandardInput ? Console.In.ReadToEnd() : File.ReadAllText(options.Input);
    model = DiagramModel.Parse(json);
}
catch (DiagramModelException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine($"Could not read the model: {ex.Message}");
    return 1;
}

model.Name = options.Title ?? model.Name;
model.Description = options.Description ?? model.Description;

var path = Path.GetFullPath(options.Output);
Directory.CreateDirectory(Path.GetDirectoryName(path)!);
File.WriteAllText(path, DiagramGenerator.Generate(model), new UTF8Encoding(false));

var views = model.Tables.Count(t => t.View);
var relationships = model.Tables.Sum(t => t.ForeignKeys.Count);
Console.WriteLine(
    $"Wrote {path} ({model.Tables.Count - views} tables, {views} views, {model.Enums.Count} enums, {relationships} relationships)");
return 0;
