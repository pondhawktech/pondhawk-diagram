# pondhawk-diagram

Draws an interactive ER diagram as one self-contained HTML file: tables and views with their
columns, keys and unique markers, enums, foreign-key lines, tables grouped by the root of their
foreign-key chain, a searchable sidebar, and pan and zoom. The layout engine (dagre) is embedded,
so the file opens offline and can be attached or committed as it is.

It knows nothing about databases. It reads a **JSON data model** in a format this project defines,
and producing that file is the consuming project's job — from an EF Core model, a migration
history, a live database, or anything else. Any language that can write JSON can use it.

## The model

```json
{
  "$schema": "../schema/diagram-model.schema.json",
  "version": 1,
  "name": "Shop",
  "tables": [
    {
      "name": "Customers",
      "columns": [
        { "name": "Id", "type": "int", "primaryKey": true },
        { "name": "Email", "type": "varchar(255)" }
      ],
      "indexes": [{ "columns": ["Email"], "unique": true }]
    },
    {
      "name": "Orders",
      "note": "One row per checkout",
      "columns": [
        { "name": "Id", "type": "int", "primaryKey": true },
        { "name": "CustomerId", "type": "int" },
        { "name": "Status", "type": "int", "enum": "OrderStatus" }
      ],
      "foreignKeys": [
        { "columns": ["CustomerId"], "references": { "table": "Customers", "columns": ["Id"] } }
      ]
    }
  ],
  "enums": [{ "name": "OrderStatus", "values": ["Open", "Paid", "Shipped"] }]
}
```

The full definition is [`schema/diagram-model.schema.json`](schema/diagram-model.schema.json)
(JSON Schema 2020-12); `pondhawk-diagram --print-schema` prints the copy built into the tool.
A complete example is in [`examples/shop.json`](examples/shop.json).

- **`version`** is required and is `1`. A file with a newer version is refused outright.
- **`type`** is shown exactly as written; use whatever reads best for the project.
- **`nullable`**, **`primaryKey`**, **`unique`** and **`view`** default to `false`, so an omitted
  `nullable` means NOT NULL.
- **`enum`** on a column names an entry in `enums`, and **`note`** on a table, column or enum
  becomes a tooltip.
- **`schema`** on a table is optional. Schema names appear in the diagram only when the model has
  more than one. A foreign key that omits `references.schema` means the referring table's own
  schema, then any schema where that table name is unique.
- Unknown properties are errors, so a misspelt key is caught rather than ignored. Comments and
  trailing commas are allowed.

The model is checked before anything is drawn, and every problem is reported at once: duplicate
tables or columns, a foreign key or index naming a column the table lacks, a reference to a table
or column that is not in the model, a column using an enum that is not defined.

## Command line

```bash
pondhawk-diagram model.json -o docs/schema.html
my-exporter | pondhawk-diagram - -o docs/schema.html
```

| Option | |
|---|---|
| `<model.json>` | The model to draw; `-` reads standard input |
| `-o`, `--output` | Output file (default: the model's name with `.html`, or `er-diagram.html` for standard input) |
| `-t`, `--title`, `-d`, `--description` | Override the model's `name` and `description` |
| `--print-schema` | Print the JSON Schema and exit |

Exit code is 0 on success, 1 if the model could not be read or is not valid, 2 for bad arguments.

Until a package is published, run it from source:

```bash
dotnet run --project src/Pondhawk.Diagram.Tool -- examples/shop.json
```

## Library

`Pondhawk.Diagram` has no package dependencies. A .NET project can skip the JSON file and build
the model in memory with the same types:

```csharp
using Pondhawk.Diagram;

var model = DiagramModel.Load("model.json");   // or new DiagramModel { Tables = [...] }
var html = DiagramGenerator.Generate(model);
```

`DiagramModel.Parse` and `Load` throw `DiagramModelException`, whose `Errors` lists every problem.
`Generate` does not validate — call `model.Validate()` first on a model built in code — and draws
what it can: a foreign key to a table that is not there simply gets no line. `model.ToJson()`
writes the format, which is the easy way to produce it from .NET.

## Limits

- Relationships are the foreign keys in the model; nothing is inferred from column names.
- Each foreign key column is drawn against the first referenced column, so a composite key shows
  the right tables but not column-to-column pairing.
- There are no exporters here. Each consuming project writes its own.

## Building

```bash
dotnet build pondhawk-diagram.slnx
dotnet run --project tests/Pondhawk.Diagram.Tests
dotnet run --project tests/Pondhawk.Diagram.Tool.Tests
```

Run the tests with `dotnet run`, not `dotnet test`: they are xunit v3 self-hosted executables.

## Origin and licence

The renderer was extracted from [pondhawk-generation](../pondhawk-generation), which dropped
diagram generation when it was refocused. MIT; see `LICENSE`. The embedded dagre and graphlib are
MIT as well; see `THIRD-PARTY-NOTICES.md`.
