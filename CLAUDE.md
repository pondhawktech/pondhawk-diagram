# CLAUDE.md

## Running tests

Use `dotnet run`, not `dotnet test` — the xunit v3 projects are self-hosted executables:

```bash
dotnet run --project tests/Pondhawk.Diagram.Tests --configuration Release
dotnet run --project tests/Pondhawk.Diagram.Tool.Tests --configuration Release
dotnet run --project tests/Cake.Pondhawk.Diagram.Tests --configuration Release
```

## Layout

- `src/Pondhawk.Diagram` — the JSON model (`Schema.cs`, `DiagramModel.cs`) and the HTML renderer. No package references, and no knowledge of any database: consuming projects supply the model.
- `src/Pondhawk.Diagram.Tool` — the `pondhawk-diagram` command.
- `src/Cake.Pondhawk.Diagram` — the Cake addin; calls the library in-process. `Cake.Core` stays at the oldest version of the supported major (6.0.0) so the addin loads into any Cake 6 build; moving to Cake 7 is a deliberate change.
- `examples/EfCoreExporter` — a consuming project in miniature. It is in the solution so it keeps compiling; besides the addin and the tests, it is the only project with package references.
- `docs/producing-a-model.md` — the guide for consuming projects. Anything a consumer needs goes there or in the README, not in this file.
- `schema/diagram-model.schema.json` — the published format, written by hand and embedded in the library.

## Changing the model format

The C# types in `Schema.cs` and `DiagramModel.cs` are the format. A property added or renamed
there must be made in `schema/diagram-model.schema.json` too; `JsonSchemaTests` fails until the
two agree. A change that an existing file would not survive needs a new `version`. The field tables
and rules in `docs/producing-a-model.md` are not checked by anything, so update them by hand.
