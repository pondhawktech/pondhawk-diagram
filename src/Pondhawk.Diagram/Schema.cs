using System.Text.Json.Serialization;

namespace Pondhawk.Diagram;

// These types are the JSON format: property names map to camelCase, and
// schema/diagram-model.schema.json describes the same shape (a test keeps the two in step).

public sealed class Table
{
    public string Name { get; set; } = "";
    public string Schema { get; set; } = "";
    public bool View { get; set; }
    public string? Note { get; set; }
    public List<Column> Columns { get; set; } = [];
    public List<ForeignKey> ForeignKeys { get; set; } = [];
    public List<TableIndex> Indexes { get; set; } = [];
}

public sealed class Column
{
    public string Name { get; set; } = "";

    /// <summary>Shown as written; the renderer does not interpret it.</summary>
    public string Type { get; set; } = "";

    public bool Nullable { get; set; }
    public bool PrimaryKey { get; set; }

    /// <summary>Name of an entry in the model's enums that this column holds.</summary>
    public string? Enum { get; set; }

    public string? Note { get; set; }
}

public sealed class ForeignKey
{
    public string Name { get; set; } = "";
    public List<string> Columns { get; set; } = [];

    [JsonRequired]
    public TableReference References { get; set; } = new();
}

public sealed class TableReference
{
    /// <summary>Empty means the referring table's schema, then any schema where the name is unique.</summary>
    public string Schema { get; set; } = "";

    public string Table { get; set; } = "";
    public List<string> Columns { get; set; } = [];
}

public sealed class TableIndex
{
    public string Name { get; set; } = "";
    public List<string> Columns { get; set; } = [];
    public bool Unique { get; set; }
}

public sealed class EnumType
{
    public string Name { get; set; } = "";
    public string? Note { get; set; }
    public List<string> Values { get; set; } = [];
}
