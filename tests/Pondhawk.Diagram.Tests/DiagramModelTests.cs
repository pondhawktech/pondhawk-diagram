using Shouldly;

namespace Pondhawk.Diagram.Tests;

public class DiagramModelTests
{
    private const string Minimal = """
        {
          "version": 1,
          "tables": [
            { "name": "Customers", "columns": [{ "name": "Id", "type": "int", "primaryKey": true }] },
            {
              "name": "Orders",
              "columns": [{ "name": "Id", "type": "int", "primaryKey": true }, { "name": "CustomerId", "type": "int" }],
              "foreignKeys": [{ "columns": ["CustomerId"], "references": { "table": "Customers", "columns": ["Id"] } }]
            }
          ]
        }
        """;

    private static IReadOnlyList<string> ErrorsFrom(string json) =>
        Should.Throw<DiagramModelException>(() => DiagramModel.Parse(json)).Errors;

    [Fact]
    public void Parse_ReadsTablesColumnsAndKeys()
    {
        var model = DiagramModel.Parse(Minimal);

        model.Tables.Select(t => t.Name).ShouldBe(["Customers", "Orders"]);
        model.Tables[0].Columns[0].PrimaryKey.ShouldBeTrue();
        model.Tables[1].Columns[1].Nullable.ShouldBeFalse();
        model.Tables[1].ForeignKeys[0].References.Table.ShouldBe("Customers");
    }

    [Fact]
    public void Parse_Example_IsValidAndRenders()
    {
        var model = DiagramModel.Load(Path.Combine(AppContext.BaseDirectory, "examples", "shop.json"));

        var html = DiagramGenerator.Generate(model);

        html.ShouldContain("Shop");
        html.ShouldContain("id=\"table-OrderLines\"");
        html.ShouldContain("id=\"enum-OrderStatus\"");
        html.ShouldContain("{ from: 'Orders', fromCol: 'CustomerId', to: 'Customers', toCol: 'Id' }");
    }

    [Fact]
    public void Parse_AllowsCommentsAndTrailingCommas()
    {
        DiagramModel.Parse("""{ "version": 1, /* none yet */ "tables": [], }""").Tables.ShouldBeEmpty();
    }

    [Fact]
    public void ToJson_RoundTrips()
    {
        var json = DiagramModel.Parse(Minimal).ToJson();

        json.ShouldContain("\"foreignKeys\"");
        json.ShouldNotContain("\"note\"");
        DiagramModel.Parse(json).Tables[1].ForeignKeys[0].Columns.ShouldBe(["CustomerId"]);
    }

    [Fact]
    public void Parse_RequiresVersion()
    {
        ErrorsFrom("""{ "tables": [] }""").ShouldHaveSingleItem().ShouldContain("version");
    }

    [Fact]
    public void Parse_NewerVersion_SaysSoRatherThanListingUnknownProperties()
    {
        ErrorsFrom("""{ "version": 2, "somethingNew": true }""")
            .ShouldHaveSingleItem().ShouldBe("version 2 is not supported; this build reads version 1.");
    }

    [Fact]
    public void Parse_RejectsUnknownProperties()
    {
        ErrorsFrom("""{ "version": 1, "tables": [{ "name": "A", "colums": [] }] }""")
            .ShouldHaveSingleItem().ShouldContain("colums");
    }

    [Fact]
    public void Parse_RejectsMalformedJson()
    {
        ErrorsFrom("{ not json").ShouldHaveSingleItem();
    }

    [Fact]
    public void Parse_RejectsNullWhereAListIsExpected()
    {
        ErrorsFrom("""{ "version": 1, "tables": null }""").ShouldHaveSingleItem();
    }

    [Fact]
    public void Parse_ReportsEveryProblemAtOnce()
    {
        var errors = ErrorsFrom("""
            {
              "version": 1,
              "tables": [
                { "name": "A", "columns": [{ "name": "Id" }, { "name": "id" }, { "name": "Kind", "enum": "Missing" }] },
                { "name": "A" },
                {
                  "name": "B",
                  "columns": [{ "name": "AId" }],
                  "indexes": [{ "columns": ["Nope"] }],
                  "foreignKeys": [
                    { "columns": ["Gone"], "references": { "table": "Nowhere", "columns": ["Id"] } }
                  ]
                }
              ]
            }
            """);

        errors.ShouldContain(e => e.Contains("duplicate column \"id\""));
        errors.ShouldContain(e => e.Contains("uses enum \"Missing\""));
        errors.ShouldContain(e => e.StartsWith("tables[1]") && e.Contains("duplicate table"));
        errors.ShouldContain(e => e.Contains("indexes[0] names column \"Nope\""));
        errors.ShouldContain(e => e.Contains("foreignKeys[0] names column \"Gone\""));
        errors.ShouldContain(e => e.Contains("references table \"Nowhere\", which is not in the model"));
    }

    [Fact]
    public void Parse_ForeignKey_ColumnsMustMatchTheReferencedTable()
    {
        var errors = ErrorsFrom("""
            {
              "version": 1,
              "tables": [
                { "name": "A", "columns": [{ "name": "Id" }] },
                {
                  "name": "B",
                  "columns": [{ "name": "X" }, { "name": "Y" }],
                  "foreignKeys": [{ "columns": ["X", "Y"], "references": { "table": "A", "columns": ["Key"] } }]
                }
              ]
            }
            """);

        errors.ShouldContain(e => e.Contains("has 2 column(s) but references 1"));
        errors.ShouldContain(e => e.Contains("references column \"Key\", which \"A\" does not have"));
    }

    [Fact]
    public void Parse_ForeignKey_RequiresReferences()
    {
        ErrorsFrom("""{ "version": 1, "tables": [{ "name": "A", "columns": [{ "name": "X" }], "foreignKeys": [{ "columns": ["X"] }] }] }""")
            .ShouldHaveSingleItem().ShouldContain("references");
    }

    [Fact]
    public void Parse_SameNameInTwoSchemas_IsAllowed()
    {
        var model = DiagramModel.Parse(TwoSchemas(referenceSchema: "\"schema\": \"sales\", "));

        var html = DiagramGenerator.Generate(model);

        html.ShouldContain("id=\"table-sales.Accounts\"");
        html.ShouldContain("id=\"table-billing.Accounts\"");
        html.ShouldContain("{ from: 'billing.Invoices', fromCol: 'AccountId', to: 'sales.Accounts', toCol: 'Id' }");
    }

    [Fact]
    public void Parse_ReferenceWithoutSchema_PrefersTheReferringTablesSchema()
    {
        var model = DiagramModel.Parse(TwoSchemas(referenceSchema: ""));

        DiagramGenerator.Generate(model)
            .ShouldContain("{ from: 'billing.Invoices', fromCol: 'AccountId', to: 'billing.Accounts', toCol: 'Id' }");
    }

    [Fact]
    public void Parse_ReferenceWithoutSchema_AmbiguousAcrossOtherSchemas_IsAnError()
    {
        ErrorsFrom(TwoSchemas(referenceSchema: "", invoiceSchema: "reporting"))
            .ShouldHaveSingleItem().ShouldContain("exists in several schemas; set references.schema");
    }

    [Fact]
    public void Generate_SingleSchema_LeavesNamesUnqualified()
    {
        var html = DiagramGenerator.Generate([new Table { Name = "Users", Schema = "public" }]);

        html.ShouldContain("id=\"table-Users\"");
    }

    [Fact]
    public void Generate_ShowsNotesAndColumnEnums()
    {
        var table = new Table
        {
            Name = "Orders",
            Note = "One row per <checkout>",
            Columns = [new Column { Name = "Status", Type = "int", Enum = "OrderStatus", Note = "Numeric value" }]
        };

        var html = DiagramGenerator.Generate([table], [new EnumType { Name = "OrderStatus", Values = ["Open"] }]);

        html.ShouldContain("title=\"One row per &lt;checkout&gt;\"");
        html.ShouldContain("title=\"Numeric value\"");
        html.ShouldContain("int <span class=\"col-enum\" title=\"Enum\">OrderStatus</span>");
    }

    private static string TwoSchemas(string referenceSchema, string invoiceSchema = "billing") => $$"""
        {
          "version": 1,
          "tables": [
            { "schema": "sales", "name": "Accounts", "columns": [{ "name": "Id" }] },
            { "schema": "billing", "name": "Accounts", "columns": [{ "name": "Id" }] },
            {
              "schema": "{{invoiceSchema}}",
              "name": "Invoices",
              "columns": [{ "name": "AccountId" }],
              "foreignKeys": [{ "columns": ["AccountId"], "references": { {{referenceSchema}}"table": "Accounts", "columns": ["Id"] } }]
            }
          ]
        }
        """;
}
