# CLAUDE.md

## Build System

This project uses **Cake Frosting** (`build/` directory). Use it instead of running `dotnet build`
or `dotnet pack` directly.

```bash
dotnet run --project build                                              # Clean → Restore → Build → Test
dotnet run --project build -- --target=Pack                             # ... → Pack, into artifacts/
dotnet run --project build -- --target=Pack --release-version=1.2.0     # a release build
```

`Push` sends whatever is in `artifacts/` to `--source`, with the feed's key in the
`NUGET_API_KEY` environment variable. It does not depend on `Pack` on purpose — promotion to
nuget.org pushes a release's existing packages rather than rebuilding them — and it refuses
`0.0.0-dev` packages. Do not run it by hand against a real feed; releases go through the
workflows in `.github/workflows` (tag → GitHub Packages and a GitHub Release; nuget.org by the
manual Promote workflow).

### Running tests

Always run tests using `dotnet run` (not `dotnet test`) because xunit v3 projects are self-hosted
executables:

```bash
dotnet run --project tests/Pondhawk.Diagram.Tests --configuration Release
dotnet run --project tests/Pondhawk.Diagram.Tool.Tests --configuration Release
dotnet run --project tests/Cake.Pondhawk.Diagram.Tests --configuration Release
```

A new test project must be added to `Projects` in `build/Tasks/TestTask.cs`, or the build will
not run it.

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
