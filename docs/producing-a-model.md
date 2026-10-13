# Producing a model

pondhawk-diagram draws whatever its JSON model says. This is what a consuming project needs in
order to write that model: how to get the tool, every field, the rules the loader enforces, how
to map an ORM onto the format, and a working EF Core exporter.

## Getting the tool

Nothing is published to NuGet yet. Until it is, build the packages from a clone and install from
that folder:

```bash
dotnet pack pondhawk-diagram.slnx -c Release -o artifacts
dotnet tool install Pondhawk.Diagram.Tool --global --add-source artifacts --version 0.0.0-dev
```

The same folder holds `Pondhawk.Diagram`, the library, for a .NET project that wants the model
types and `DiagramGenerator` directly. Add the folder as a package source, or reference
`src/Pondhawk.Diagram/Pondhawk.Diagram.csproj` from a clone or submodule.

A project in another language needs only the tool: write the JSON however is convenient and pass
it as a file or on standard input.

## Editor validation

The JSON Schema is not hosted anywhere. Copy it into the consuming repository and point the model
at the copy:

```bash
pondhawk-diagram --print-schema > docs/diagram-model.schema.json
```

```json
{ "$schema": "./diagram-model.schema.json", "version": 1 }
```

`$schema` is only for editors; the tool ignores its value and validates against the schema it was
built with.

## Field reference

Every field not marked required may be left out. Leave optional fields out rather than writing
`null`: `note`, `enum`, `name` and `description` on the model accept `null`, nothing else does.

### Model (the root object)

| Field | Type | |
|---|---|---|
| `version` | number | **Required.** Always `1`. |
| `$schema` | string | Path or URL of the JSON Schema, for editors. |
| `name` | string | Diagram title. |
| `description` | string | Subtitle beside the title. |
| `tables` | array of table | Tables and views. Order does not matter. |
| `enums` | array of enum | Order is kept. |

### Table

| Field | Type | |
|---|---|---|
| `name` | string | **Required.** |
| `schema` | string | Database schema or namespace. Shown only when the model has more than one distinct schema. |
| `view` | boolean | `true` for a view. Default `false`. |
| `note` | string | Tooltip on the header. |
| `columns` | array of column | Drawn in the order given. |
| `foreignKeys` | array of foreign key | Not allowed on a view. |
| `indexes` | array of index | Only unique ones affect the drawing. |

### Column

| Field | Type | |
|---|---|---|
| `name` | string | **Required.** |
| `type` | string | Shown exactly as written. |
| `nullable` | boolean | Default `false`, so a column with no `nullable` is drawn as NOT NULL. |
| `primaryKey` | boolean | Default `false`. Set it on each column of a composite key. |
| `enum` | string | Name of an entry in `enums`. Shown beside the type. |
| `note` | string | Tooltip on the row. |

### Foreign key

| Field | Type | |
|---|---|---|
| `columns` | array of string | **Required**, at least one. Columns of this table. |
| `references` | reference | **Required.** |
| `name` | string | Constraint name. Not drawn. |

### Reference

| Field | Type | |
|---|---|---|
| `table` | string | **Required.** Must be a table in the model, not a view. |
| `columns` | array of string | **Required.** One per foreign key column, in the same order. |
| `schema` | string | Leave out to mean the referring table's own schema, then any schema where the table name is unique. |

### Index

| Field | Type | |
|---|---|---|
| `columns` | array of string | **Required.** Columns of this table. |
| `unique` | boolean | Default `false`. `true` marks each column with a U. |
| `name` | string | Not drawn. |

### Enum

| Field | Type | |
|---|---|---|
| `name` | string | **Required.** |
| `values` | array of string | Drawn in the order given. |
| `note` | string | Tooltip on the header. |

## Rules the loader enforces

A model that breaks any of these is refused, and every violation is listed, not just the first.

- `version` is present and is `1`.
- There are no properties other than the ones above. A misspelt key is an error.
- Names are compared **without regard to case**: `Orders` and `orders` are the same table, and a
  foreign key may name a column in a different case from its definition.
- A table is identified by `schema` plus `name`, and that pair is unique. The same name in two
  schemas is fine.
- Column names are unique within a table, and enum names within the model.
- A column's `enum` names an enum in the model.
- Every column an index or foreign key lists exists in its table.
- A foreign key has at least one column, belongs to a table rather than a view, and references a
  table rather than a view.
- The referenced table is in the model and has every referenced column, and there are as many
  referenced columns as foreign key columns.
- A reference with no `schema` that fits tables in several other schemas is ambiguous, and an
  error; give it a `schema`.

Comments (`//` and `/* */`) and trailing commas are accepted.

## Mapping an ORM onto the model

The model describes **tables**, not classes. Where the two differ, export the tables — the
diagram is of the database the code produces.

- **`type`.** Use the database type (`varchar(255)`, `timestamptz`) when the audience thinks in
  SQL, or the language type (`string`, `DateTime`) when it thinks in code. Pick one for the whole
  model. Either way it is only a label.
- **Enums.** A database stores an enum as an integer or a string, so the column's `type` says
  nothing about it. Set `enum` on the column and list the enum once in `enums`.
- **Owned and embedded types** that share their owner's table are just more columns of that
  table. Ones stored in their own table are a table with a foreign key to the owner.
- **Many-to-many.** Export the join table as a table with two foreign keys. There is no
  many-to-many relationship in the format.
- **Inheritance.** With one table per hierarchy, export the one table with every subclass's
  columns (the subclass columns are nullable). With a table per type, export each table, with the
  subclass tables' keys as foreign keys to the base table.
- **Relationships without a constraint.** The diagram draws a line for each entry in
  `foreignKeys`, whether or not the database enforces it. To show a relationship the database
  does not know about, add it there.
- **Infrastructure tables** — migration history, outbox, job queues — are yours to leave out.
  Nothing is skipped automatically.
- **Composite keys.** List every column in `columns` and in `references.columns`. The line goes
  to the right table; the diagram does not show which column pairs with which.

## An EF Core exporter

[`examples/EfCoreExporter`](../examples/EfCoreExporter) is a runnable project.
[`EfCoreDiagramExporter.cs`](../examples/EfCoreExporter/EfCoreDiagramExporter.cs) is the part to
copy: one static method that turns a `DbContext` into a `DiagramModel`.

```bash
dotnet run --project examples/EfCoreExporter | pondhawk-diagram - -o shop.html
```

In a consuming project it comes down to this:

```csharp
using var context = new ShopContext();   // however the project builds one at design time

var model = EfCoreDiagramExporter.Export(context, name: "Shop");
File.WriteAllText("docs/model.json", model.ToJson());
```

How it works, and what to keep if you rewrite it:

- It reads EF Core's **relational model** (`GetRelationalModel()`), which is already in terms of
  tables, columns, foreign key constraints and indexes. Owned types, join tables and inheritance
  are resolved by EF before the exporter sees them, so it needs no special cases for them.
- It asks for the **design-time model** (`context.GetService<IDesignTimeModel>().Model`).
  `context.Model` is a trimmed runtime copy, and reading a table comment from it throws.
- It **never opens a connection**. The context needs a relational provider configured, because
  the provider decides the column types, but the connection string is not used. The example's
  `Data Source=:memory:` is a placeholder.
- Column types therefore come out in **that provider's dialect**: `INTEGER` and `TEXT` under
  SQLite, `integer` and `character varying(255)` under PostgreSQL. Configure the context with the
  provider production uses.
- Enums are found from the **CLR type** of the property behind each column, since the column
  itself is an integer or a string.
- **Alternate keys** are exported as unique indexes. EF models them as unique constraints, not
  indexes, and they would otherwise be missing.
- EF lists a table's columns **alphabetically**. The exporter moves the key columns to the front
  and leaves the rest in that order.

The example was written against EF Core 10 with the SQLite provider, and has not been run against
another provider or an earlier EF Core version.

## Keeping the diagram current

Run the exporter and the tool together wherever the project already regenerates documentation —
a build target, a script, a CI step:

```bash
dotnet run --project tools/DiagramExport | pondhawk-diagram - -o docs/schema.html
```

The output embeds the time it was generated, so it differs on every run. Commit the model JSON if
you want a reviewable diff, and treat the HTML as a build product.
