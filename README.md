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

**Writing an exporter?** [`docs/producing-a-model.md`](docs/producing-a-model.md) has the full
field reference, every rule the loader enforces, how to map an ORM onto the format, and a working
EF Core exporter in [`examples/EfCoreExporter`](examples/EfCoreExporter).

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

Nothing is published to NuGet yet. Run it from source, or build and install it locally as
[`docs/producing-a-model.md`](docs/producing-a-model.md#getting-the-tool) describes:

```bash
dotnet run --project src/Pondhawk.Diagram.Tool -- examples/shop.json
```

## Cake Frosting

`Cake.Pondhawk.Diagram` is a Cake addin for builds that already run on Frosting. Reference it
from the build project and call it from a task:

```csharp
using Cake.Pondhawk.Diagram;

[TaskName("Diagram")]
public sealed class DiagramTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) =>
        context.GenerateDiagram("docs/model.json", "docs/schema.html");
}
```

Relative paths resolve against the build's working directory, the output directory is created if
it is missing, and a missing or invalid model fails the build with every error listed. Pass a
`DiagramSettings` to override the title or description, or pass a `DiagramModel` built in code
instead of a path. It runs the library in-process, so the command-line tool need not be installed.
It is built against Cake 6.

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
- No exporter ships with the tool. Each consuming project writes its own; the EF Core one in
  `examples/` is a starting point to copy.

## Building

The build is [Cake Frosting](https://cakebuild.net/docs/running-builds/runners/cake-frosting), in
`build/`:

```bash
dotnet run --project build                    # clean, restore, build, test
dotnet run --project build -- --target=Pack   # the same, then packages into artifacts/
```

To run one test suite on its own, use `dotnet run --project tests/<name>`, not `dotnet test`:
the tests are xunit v3 self-hosted executables.

## Releasing

The tag is the version. Pushing `v1.2.0` runs the Release workflow, which builds and tests that
commit, pushes the three packages to GitHub Packages, and attaches them to a GitHub Release.

nuget.org is a separate, manual step: run the **Promote to nuget.org** workflow with the tag. It
pushes the files attached to that release rather than rebuilding, so what reaches nuget.org is
what was already published and tried. It needs a repository secret `NUGET_ORG_API_KEY`.

## Origin and licence

The renderer was extracted from [pondhawk-generation](https://github.com/pondhawktech/pondhawk-generation), which dropped
diagram generation when it was refocused. MIT; see `LICENSE`. The embedded dagre and graphlib are
MIT as well; see `THIRD-PARTY-NOTICES.md`.
