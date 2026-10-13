namespace Pondhawk.Diagram;

/// <summary>Works out which table a foreign key points at, and what each table is called in the diagram.</summary>
internal sealed class TableResolver
{
    private readonly List<Table> _tables;
    private readonly bool _qualify;

    public TableResolver(List<Table> tables)
    {
        _tables = tables;

        // Schema names only earn their space when there is more than one of them.
        _qualify = tables.Select(t => t.Schema)
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() > 1;
    }

    public string Key(Table table) => Key(table.Schema, table.Name);

    private string Key(string schema, string name) =>
        _qualify && schema.Length > 0 ? $"{schema}.{name}" : name;

    /// <summary>Null when the table is missing, or when no schema was given and the name fits several.</summary>
    public Table? Resolve(Table from, TableReference reference)
    {
        var named = _tables
            .Where(t => !t.View && Same(t.Name, reference.Table))
            .ToList();

        if (reference.Schema.Length > 0)
            return named.FirstOrDefault(t => Same(t.Schema, reference.Schema));

        return named.FirstOrDefault(t => Same(t.Schema, from.Schema))
               ?? (named.Count == 1 ? named[0] : null);
    }

    /// <summary>
    /// Copies of the tables named by their diagram key, with every foreign key pointing at the
    /// key of the table it resolves to. The renderer identifies tables by name alone.
    /// </summary>
    public static List<Table> Normalize(List<Table> tables)
    {
        var resolver = new TableResolver(tables);

        return tables.Select(table => new Table
        {
            Name = resolver.Key(table),
            Schema = table.Schema,
            View = table.View,
            Note = table.Note,
            Columns = table.Columns,
            Indexes = table.Indexes,
            ForeignKeys = table.ForeignKeys.Select(fk =>
            {
                var target = resolver.Resolve(table, fk.References);
                return new ForeignKey
                {
                    Name = fk.Name,
                    Columns = fk.Columns,
                    References = new TableReference
                    {
                        Schema = target?.Schema ?? fk.References.Schema,
                        Table = target is null
                            ? resolver.Key(fk.References.Schema, fk.References.Table)
                            : resolver.Key(target),
                        Columns = fk.References.Columns
                    }
                };
            }).ToList()
        }).ToList();
    }

    private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
