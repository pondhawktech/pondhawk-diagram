using Cake.Core;
using Cake.Core.IO;
using Cake.Testing;
using global::Pondhawk.Diagram;
using NSubstitute;
using Shouldly;

namespace Cake.Pondhawk.Diagram.Tests;

public class DiagramAliasesTests
{
    private const string Model = """
        {
          "version": 1,
          "name": "Shop",
          "tables": [
            { "name": "Customers", "columns": [{ "name": "Id", "primaryKey": true }] },
            {
              "name": "Orders",
              "columns": [{ "name": "Id", "primaryKey": true }, { "name": "CustomerId" }],
              "foreignKeys": [{ "columns": ["CustomerId"], "references": { "table": "Customers", "columns": ["Id"] } }]
            }
          ]
        }
        """;

    private readonly FakeEnvironment _environment = FakeEnvironment.CreateUnixEnvironment();
    private readonly FakeFileSystem _fileSystem;
    private readonly FakeLog _log = new();
    private readonly ICakeContext _context = Substitute.For<ICakeContext>();

    public DiagramAliasesTests()
    {
        _fileSystem = new FakeFileSystem(_environment);
        _context.Environment.Returns(_environment);
        _context.FileSystem.Returns(_fileSystem);
        _context.Log.Returns(_log);
    }

    private string Output(string path) => _fileSystem.GetFile(path).GetTextContent();

    [Fact]
    public void WritesTheDiagram()
    {
        _fileSystem.CreateFile("/Working/docs/model.json").SetContent(Model);

        _context.GenerateDiagram("/Working/docs/model.json", "/Working/docs/schema.html");

        var html = Output("/Working/docs/schema.html");
        html.ShouldStartWith("<!DOCTYPE html>");
        html.ShouldContain("id=\"table-Orders\"");
        html.ShouldContain("<span class=\"titlebar-name\">Shop</span>");
    }

    [Fact]
    public void RelativePathsResolveAgainstTheWorkingDirectory()
    {
        _fileSystem.CreateFile("/Working/docs/model.json").SetContent(Model);

        _context.GenerateDiagram("docs/model.json", "docs/schema.html");

        _fileSystem.GetFile("/Working/docs/schema.html").Exists.ShouldBeTrue();
    }

    [Fact]
    public void CreatesTheOutputDirectory()
    {
        _fileSystem.CreateFile("/Working/model.json").SetContent(Model);

        _context.GenerateDiagram("model.json", "artifacts/docs/schema.html");

        _fileSystem.GetFile("/Working/artifacts/docs/schema.html").Exists.ShouldBeTrue();
    }

    [Fact]
    public void ReplacesAnExistingDiagramRatherThanOverwritingItsStart()
    {
        _fileSystem.CreateFile("/Working/model.json").SetContent(Model);
        _fileSystem.CreateFile("/Working/schema.html").SetContent(new string('x', 500_000));

        _context.GenerateDiagram("model.json", "schema.html");

        Output("/Working/schema.html").TrimEnd().ShouldEndWith("</html>");
    }

    [Fact]
    public void SettingsOverrideTheModelsTitle()
    {
        _fileSystem.CreateFile("/Working/model.json").SetContent(Model);

        _context.GenerateDiagram("model.json", "schema.html",
            new DiagramSettings { Title = "Storefront", Description = "Nightly" });

        var html = Output("/Working/schema.html");
        html.ShouldContain("<span class=\"titlebar-name\">Storefront</span>");
        html.ShouldContain("<span class=\"titlebar-desc\">Nightly</span>");
    }

    [Fact]
    public void LogsWhatItWrote()
    {
        _fileSystem.CreateFile("/Working/model.json").SetContent(Model);

        _context.GenerateDiagram("model.json", "schema.html");

        _log.Entries.ShouldHaveSingleItem().Message
            .ShouldBe("Wrote /Working/schema.html (2 tables, 0 views, 0 enums, 1 relationships)");
    }

    [Fact]
    public void MissingModel_FailsTheBuild()
    {
        Should.Throw<CakeException>(() => _context.GenerateDiagram("nope.json", "schema.html"))
            .Message.ShouldBe("Diagram model not found: /Working/nope.json");
    }

    [Fact]
    public void InvalidModel_FailsTheBuildNamingTheFileAndEveryError()
    {
        _fileSystem.CreateFile("/Working/model.json").SetContent("""
            {
              "version": 1,
              "tables": [
                { "name": "A", "columns": [{ "name": "X", "enum": "Missing" }],
                  "foreignKeys": [{ "columns": ["X"], "references": { "table": "Nowhere", "columns": ["Id"] } }] }
              ]
            }
            """);

        var message = Should.Throw<CakeException>(() => _context.GenerateDiagram("model.json", "schema.html")).Message;

        message.ShouldStartWith("/Working/model.json: The diagram model is not valid:");
        message.ShouldContain("uses enum \"Missing\"");
        message.ShouldContain("references table \"Nowhere\"");
        _fileSystem.GetFile("/Working/schema.html").Exists.ShouldBeFalse();
    }

    [Fact]
    public void ModelBuiltInCode_IsWritten()
    {
        var model = new DiagramModel { Name = "Built", Tables = [new Table { Name = "Things" }] };

        _context.GenerateDiagram(model, "schema.html");

        Output("/Working/schema.html").ShouldContain("id=\"table-Things\"");
    }

    [Fact]
    public void ModelBuiltInCode_IsValidatedFirst()
    {
        var model = new DiagramModel { Tables = [new Table { Name = "Things" }, new Table { Name = "things" }] };

        Should.Throw<CakeException>(() => _context.GenerateDiagram(model, "schema.html"))
            .Message.ShouldContain("duplicate table");
    }
}
