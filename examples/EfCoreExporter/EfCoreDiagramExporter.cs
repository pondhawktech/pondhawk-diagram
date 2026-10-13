using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Pondhawk.Diagram;

namespace EfCoreExporter;

/// <summary>
/// Builds a diagram model from an EF Core context. Copy this file into the consuming project.
/// It reads EF Core's relational model — the tables EF would create — so owned types, join
/// tables and inheritance are already resolved, and no database connection is opened.
/// </summary>
public static class EfCoreDiagramExporter
{
    public static DiagramModel Export(DbContext context, string? name = null, string? description = null)
    {
        // The design-time model, not context.Model: the runtime one drops comments and other
        // details it does not need for queries.
        var relational = context.GetService<IDesignTimeModel>().Model.GetRelationalModel();
        var enums = new SortedDictionary<string, EnumType>(StringComparer.Ordinal);

        var model = new DiagramModel
        {
            Name = name ?? context.GetType().Name,
            Description = description
        };

        foreach (var table in relational.Tables.OrderBy(t => t.Schema).ThenBy(t => t.Name))
        {
            var keyColumns = table.PrimaryKey?.Columns.Select(c => c.Name).ToHashSet() ?? [];

            var entry = new Table
            {
                Name = table.Name,
                Schema = table.Schema ?? "",
                Note = table.Comment,

                // EF lists columns alphabetically; put the key first, as a CREATE TABLE would.
                Columns = table.Columns
                    .OrderByDescending(c => keyColumns.Contains(c.Name))
                    .Select(c => new Column
                    {
                        Name = c.Name,
                        Type = c.StoreType,
                        Nullable = c.IsNullable,
                        PrimaryKey = keyColumns.Contains(c.Name),
                        Enum = EnumOf(c.PropertyMappings.Select(m => m.Property), enums),
                        Note = c.Comment
                    })
                    .ToList(),

                ForeignKeys = table.ForeignKeyConstraints
                    .Select(fk => new ForeignKey
                    {
                        Name = fk.Name,
                        Columns = fk.Columns.Select(c => c.Name).ToList(),
                        References = new TableReference
                        {
                            Schema = fk.PrincipalTable.Schema ?? "",
                            Table = fk.PrincipalTable.Name,
                            Columns = fk.PrincipalColumns.Select(c => c.Name).ToList()
                        }
                    })
                    .ToList(),

                Indexes = table.Indexes
                    .Select(i => new TableIndex
                    {
                        Name = i.Name,
                        Columns = i.Columns.Select(c => c.Name).ToList(),
                        Unique = i.IsUnique
                    })
                    // Alternate keys are unique constraints rather than indexes, and belong on the diagram too.
                    .Concat(table.UniqueConstraints
                        .Where(u => !u.GetIsPrimaryKey())
                        .Select(u => new TableIndex
                        {
                            Name = u.Name,
                            Columns = u.Columns.Select(c => c.Name).ToList(),
                            Unique = true
                        }))
                    .ToList()
            };

            model.Tables.Add(entry);
        }

        foreach (var view in relational.Views.OrderBy(v => v.Schema).ThenBy(v => v.Name))
        {
            model.Tables.Add(new Table
            {
                Name = view.Name,
                Schema = view.Schema ?? "",
                View = true,
                Columns = view.Columns
                    .Select(c => new Column
                    {
                        Name = c.Name,
                        Type = c.StoreType,
                        Nullable = c.IsNullable,
                        Enum = EnumOf(c.PropertyMappings.Select(m => m.Property), enums)
                    })
                    .ToList()
            });
        }

        model.Enums = enums.Values.ToList();
        return model;
    }

    /// <summary>The database stores an int or a string; the enum is only visible from the CLR property.</summary>
    private static string? EnumOf(IEnumerable<IProperty> properties, IDictionary<string, EnumType> enums)
    {
        foreach (var property in properties)
        {
            var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
            if (!type.IsEnum) continue;

            enums.TryAdd(type.Name, new EnumType { Name = type.Name, Values = Enum.GetNames(type).ToList() });
            return type.Name;
        }

        return null;
    }
}
