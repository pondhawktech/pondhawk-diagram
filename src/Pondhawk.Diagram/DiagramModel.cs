using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pondhawk.Diagram;

/// <summary>The document a consuming project supplies: everything the diagram shows.</summary>
public sealed class DiagramModel
{
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    /// <summary>Lets a file point its editor at the JSON Schema; ignored otherwise.</summary>
    [JsonPropertyName("$schema")]
    public string? SchemaUri { get; set; }

    [JsonRequired]
    public int Version { get; set; } = CurrentVersion;

    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<Table> Tables { get; set; } = [];
    public List<EnumType> Enums { get; set; } = [];

    /// <summary>The JSON Schema for this format, as shipped in <c>schema/diagram-model.schema.json</c>.</summary>
    public static string JsonSchema
    {
        get
        {
            using var stream = typeof(DiagramModel).Assembly
                .GetManifestResourceStream("Pondhawk.Diagram.diagram-model.schema.json");
            using var reader = new StreamReader(stream!);
            return reader.ReadToEnd();
        }
    }

    /// <summary>Reads and validates a model. Throws <see cref="DiagramModelException"/> listing every problem found.</summary>
    public static DiagramModel Parse(string json)
    {
        DiagramModel? model;
        try
        {
            // Checked first, so a newer file is reported as newer rather than as full of unknown properties.
            using (var document = JsonDocument.Parse(json,
                       new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }))
            {
                if (document.RootElement.ValueKind == JsonValueKind.Object &&
                    document.RootElement.TryGetProperty("version", out var version) &&
                    version.ValueKind == JsonValueKind.Number &&
                    version.TryGetInt32(out var number) &&
                    number != CurrentVersion)
                {
                    throw new DiagramModelException(
                        [$"version {number} is not supported; this build reads version {CurrentVersion}."]);
                }
            }

            model = JsonSerializer.Deserialize<DiagramModel>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new DiagramModelException([ex.Message]);
        }

        if (model is null)
            throw new DiagramModelException(["the document is null; expected an object."]);

        var errors = model.Validate();
        if (errors.Count > 0)
            throw new DiagramModelException(errors);

        return model;
    }

    public static DiagramModel Load(string path) => Parse(File.ReadAllText(path));

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    /// <summary>Every inconsistency in the model, empty when it is sound.</summary>
    public List<string> Validate()
    {
        var errors = new List<string>();
        var resolver = new TableResolver(Tables);
        var enumNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < Enums.Count; i++)
        {
            var e = Enums[i];
            var where = $"enums[{i}]";
            if (e is null) { errors.Add($"{where}: is null."); continue; }
            if (e.Name.Length == 0) errors.Add($"{where}: name is required.");
            else if (!enumNames.Add(e.Name)) errors.Add($"{where}: duplicate enum \"{e.Name}\".");
        }

        var seenTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < Tables.Count; i++)
        {
            var table = Tables[i];
            if (table is null) { errors.Add($"tables[{i}]: is null."); continue; }

            var label = table.Schema.Length > 0 ? $"{table.Schema}.{table.Name}" : table.Name;
            var where = $"tables[{i}] \"{label}\"";

            if (table.Name.Length == 0) errors.Add($"tables[{i}]: name is required.");
            else if (!seenTables.Add($"{table.Schema}\n{table.Name}")) errors.Add($"{where}: duplicate table.");

            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var c = 0; c < table.Columns.Count; c++)
            {
                var column = table.Columns[c];
                if (column is null) { errors.Add($"{where}: columns[{c}] is null."); continue; }
                if (column.Name.Length == 0) errors.Add($"{where}: columns[{c}] name is required.");
                else if (!columns.Add(column.Name)) errors.Add($"{where}: duplicate column \"{column.Name}\".");

                if (!string.IsNullOrEmpty(column.Enum) && !enumNames.Contains(column.Enum))
                    errors.Add($"{where}: column \"{column.Name}\" uses enum \"{column.Enum}\", which is not in the model.");
            }

            for (var x = 0; x < table.Indexes.Count; x++)
            {
                var index = table.Indexes[x];
                if (index is null) { errors.Add($"{where}: indexes[{x}] is null."); continue; }
                foreach (var name in index.Columns.Where(n => !columns.Contains(n ?? "")))
                    errors.Add($"{where}: indexes[{x}] names column \"{name}\", which the table does not have.");
            }

            for (var f = 0; f < table.ForeignKeys.Count; f++)
            {
                var fk = table.ForeignKeys[f];
                if (fk is null) { errors.Add($"{where}: foreignKeys[{f}] is null."); continue; }

                var what = $"{where}: foreignKeys[{f}]";
                if (table.View) errors.Add($"{what} is on a view; only tables have foreign keys.");
                if (fk.Columns.Count == 0) errors.Add($"{what} has no columns.");
                foreach (var name in fk.Columns.Where(n => !columns.Contains(n ?? "")))
                    errors.Add($"{what} names column \"{name}\", which the table does not have.");

                var reference = fk.References;
                var referenceLabel = reference.Schema.Length > 0 ? $"{reference.Schema}.{reference.Table}" : reference.Table;
                if (reference.Table.Length == 0)
                {
                    errors.Add($"{what} references.table is required.");
                    continue;
                }

                var target = resolver.Resolve(table, reference);
                if (target is null)
                {
                    var ambiguous = reference.Schema.Length == 0 &&
                                    Tables.Count(t => t is { View: false } &&
                                                      string.Equals(t.Name, reference.Table, StringComparison.OrdinalIgnoreCase)) > 1;
                    errors.Add(ambiguous
                        ? $"{what} references \"{referenceLabel}\", which exists in several schemas; set references.schema."
                        : $"{what} references table \"{referenceLabel}\", which is not in the model.");
                    continue;
                }

                if (reference.Columns.Count != fk.Columns.Count)
                    errors.Add($"{what} has {fk.Columns.Count} column(s) but references {reference.Columns.Count}.");
                foreach (var name in reference.Columns.Where(n =>
                             !target.Columns.Any(c => string.Equals(c?.Name, n, StringComparison.OrdinalIgnoreCase))))
                    errors.Add($"{what} references column \"{name}\", which \"{referenceLabel}\" does not have.");
            }
        }

        return errors;
    }
}

public sealed class DiagramModelException(IReadOnlyList<string> errors)
    : Exception("The diagram model is not valid:" + string.Concat(errors.Select(e => $"{Environment.NewLine}  - {e}")))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}
