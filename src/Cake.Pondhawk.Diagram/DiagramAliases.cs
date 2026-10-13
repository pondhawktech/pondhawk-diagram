using System.Text;
using Cake.Core;
using Cake.Core.Annotations;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using global::Pondhawk.Diagram;

namespace Cake.Pondhawk.Diagram;

/// <summary>Draws an interactive HTML ER diagram from a pondhawk-diagram JSON model.</summary>
[CakeAliasCategory("Pondhawk.Diagram")]
public static class DiagramAliases
{
    /// <summary>Reads the model at <paramref name="modelPath"/> and writes the diagram to <paramref name="outputPath"/>.</summary>
    /// <example>
    /// <code>
    /// context.GenerateDiagram("docs/model.json", "docs/schema.html");
    /// </code>
    /// </example>
    [CakeMethodAlias]
    public static void GenerateDiagram(this ICakeContext context, FilePath modelPath, FilePath outputPath) =>
        GenerateDiagram(context, modelPath, outputPath, new DiagramSettings());

    /// <summary>Reads the model at <paramref name="modelPath"/> and writes the diagram to <paramref name="outputPath"/>.</summary>
    /// <example>
    /// <code>
    /// context.GenerateDiagram("docs/model.json", "docs/schema.html", new DiagramSettings { Title = "Shop" });
    /// </code>
    /// </example>
    [CakeMethodAlias]
    public static void GenerateDiagram(this ICakeContext context, FilePath modelPath, FilePath outputPath,
        DiagramSettings settings)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(modelPath);
        ArgumentNullException.ThrowIfNull(outputPath);
        ArgumentNullException.ThrowIfNull(settings);

        var file = context.FileSystem.GetFile(modelPath.MakeAbsolute(context.Environment));
        if (!file.Exists)
            throw new CakeException($"Diagram model not found: {file.Path.FullPath}");

        string json;
        using (var reader = new StreamReader(file.OpenRead()))
            json = reader.ReadToEnd();

        DiagramModel model;
        try
        {
            model = DiagramModel.Parse(json);
        }
        catch (DiagramModelException ex)
        {
            throw new CakeException($"{file.Path.FullPath}: {ex.Message}", ex);
        }

        Write(context, model, outputPath, settings);
    }

    /// <summary>Writes the diagram for a model built in code, validating it first.</summary>
    /// <example>
    /// <code>
    /// context.GenerateDiagram(model, "docs/schema.html");
    /// </code>
    /// </example>
    [CakeMethodAlias]
    public static void GenerateDiagram(this ICakeContext context, DiagramModel model, FilePath outputPath)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(outputPath);

        var errors = model.Validate();
        if (errors.Count > 0)
            throw new CakeException(new DiagramModelException(errors).Message);

        Write(context, model, outputPath, new DiagramSettings());
    }

    private static void Write(ICakeContext context, DiagramModel model, FilePath outputPath, DiagramSettings settings)
    {
        var html = DiagramGenerator.Generate(model.Tables, model.Enums.Count > 0 ? model.Enums : null,
            settings.Title ?? model.Name, settings.Description ?? model.Description);

        var output = outputPath.MakeAbsolute(context.Environment);
        var directory = context.FileSystem.GetDirectory(output.GetDirectory());
        if (!directory.Exists)
            directory.Create();

        using (var stream = context.FileSystem.GetFile(output).Open(FileMode.Create, FileAccess.Write, FileShare.None))
        using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            writer.Write(html);

        var views = model.Tables.Count(t => t.View);
        context.Log.Information("Wrote {0} ({1} tables, {2} views, {3} enums, {4} relationships)",
            output.FullPath, model.Tables.Count - views, views, model.Enums.Count,
            model.Tables.Sum(t => t.ForeignKeys.Count));
    }
}
