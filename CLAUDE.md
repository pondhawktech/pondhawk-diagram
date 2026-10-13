# CLAUDE.md

## Running tests

Use `dotnet run`, not `dotnet test` — the xunit v3 projects are self-hosted executables:

```bash
dotnet run --project tests/Pondhawk.Diagram.Tests --configuration Release
dotnet run --project tests/Pondhawk.Diagram.Tool.Tests --configuration Release
```

## Layout

- `src/Pondhawk.Diagram` — the JSON model (`Schema.cs`, `DiagramModel.cs`) and the HTML renderer. No package references, and no knowledge of any database: consuming projects supply the model.
- `src/Pondhawk.Diagram.Tool` — the `pondhawk-diagram` command.
- `schema/diagram-model.schema.json` — the published format, written by hand and embedded in the library.

## Changing the model format

The C# types in `Schema.cs` and `DiagramModel.cs` are the format. A property added or renamed
there must be made in `schema/diagram-model.schema.json` too; `JsonSchemaTests` fails until the
two agree. A change that an existing file would not survive needs a new `version`.
