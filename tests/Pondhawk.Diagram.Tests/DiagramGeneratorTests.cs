using Shouldly;

namespace Pondhawk.Diagram.Tests;

public class DiagramGeneratorTests
{
    [Fact]
    public void GeneratesValidHtml()
    {
        var model = new Table
        {
            Name = "Users",
            Schema = "public",
            Columns =
            [
                new Column { Name = "Id", Type = "int", PrimaryKey = true },
                new Column { Name = "Email", Type = "varchar(255)" }
            ]
        };

        var html = DiagramGenerator.Generate([model]);

        html.ShouldContain("<!DOCTYPE html>");
        html.ShouldContain("</html>");
        html.ShouldContain("<title>");
    }

    [Fact]
    public void ContainsTableBoxes()
    {
        var model = new Table
        {
            Name = "Products",
            Columns =
            [
                new Column { Name = "Id", Type = "int", PrimaryKey = true },
                new Column { Name = "Name", Type = "varchar(100)" },
                new Column { Name = "Price", Type = "decimal", Nullable = true }
            ]
        };

        var html = DiagramGenerator.Generate([model]);
        html.ShouldContain("Products");
        html.ShouldContain("Id");
        html.ShouldContain("Name");
        html.ShouldContain("Price");
        html.ShouldContain("int");
        html.ShouldContain("varchar(100)");
    }

    [Fact]
    public void ContainsFkRelationshipLines()
    {
        var categories = new Table
        {
            Name = "Categories",
            Columns = [new Column { Name = "Id", Type = "int", PrimaryKey = true }]
        };
        var products = new Table
        {
            Name = "Products",
            Columns =
            [
                new Column { Name = "Id", Type = "int", PrimaryKey = true },
                new Column { Name = "CategoryId", Type = "int" }
            ],
            ForeignKeys =
            [
                new ForeignKey { Columns = ["CategoryId"], References = new() { Table = "Categories", Columns = ["Id"] } }
            ]
        };

        var html = DiagramGenerator.Generate([categories, products]);
        html.ShouldContain("relationships");
        html.ShouldContain("Products");
        html.ShouldContain("Categories");
    }

    [Fact]
    public void ContainsInteractiveJs()
    {
        var html = DiagramGenerator.Generate([]);
        html.ShouldContain("addEventListener");
        html.ShouldContain("mousedown");
        html.ShouldContain("wheel");
        html.ShouldContain("drawLines");
    }

    [Fact]
    public void SelfContained_NoExternalCdnUrls()
    {
        var model = new Table
        {
            Name = "Test",
            Columns = [new Column { Name = "Id", Type = "int" }]
        };
        var html = DiagramGenerator.Generate([model]);
        // Should not reference external CDN/JS/CSS — SVG namespace URIs are fine
        html.ShouldNotContain("cdn.");
        html.ShouldNotContain("<script src=");
        html.ShouldNotContain("<link rel=\"stylesheet\" href=");
    }

    [Fact]
    public void DagreBundleIncludesGraphlib()
    {
        var model = new Table
        {
            Name = "Test",
            Columns = [new Column { Name = "Id", Type = "int" }]
        };
        var html = DiagramGenerator.Generate([model]);
        // The embedded bundle must include graphlib so dagre can find it in the browser
        html.ShouldContain("var graphlib=");
        html.ShouldContain("var dagre=");
    }

    [Fact]
    public void EmptySchema_ProducesValidHtml()
    {
        var html = DiagramGenerator.Generate([]);
        html.ShouldContain("<!DOCTYPE html>");
        html.ShouldContain("</html>");
        // No actual table div elements rendered (CSS class definitions are OK)
        html.ShouldNotContain("id=\"table-");
    }

    [Fact]
    public void EnumBoxes()
    {
        var enums = new List<EnumType>
        {
            new() { Name = "Status", Values = ["Active", "Inactive"] }
        };
        var html = DiagramGenerator.Generate([], enums);
        html.ShouldContain("Status");
        html.ShouldContain("Active");
        html.ShouldContain("Inactive");
        html.ShouldContain("enum-box");
    }

    [Fact]
    public void UniqueColumnIcon()
    {
        var model = new Table
        {
            Name = "Users",
            Columns =
            [
                new Column { Name = "Id", Type = "int", PrimaryKey = true },
                new Column { Name = "Email", Type = "varchar(255)" }
            ],
            Indexes = [new TableIndex { Name = "IX_Users_Email", Columns = ["Email"], Unique = true }]
        };

        var html = DiagramGenerator.Generate([model]);
        html.ShouldContain("unique-icon");
        html.ShouldContain("title=\"Unique\"");
    }

    [Fact]
    public void ViewBoxDistinctFromTable()
    {
        var table = new Table
        {
            Name = "Users",
            Columns = [new Column { Name = "Id", Type = "int", PrimaryKey = true }]
        };
        var view = new Table
        {
            Name = "ActiveUsers",
            View = true,
            Columns = [new Column { Name = "Id", Type = "int" }]
        };

        var html = DiagramGenerator.Generate([table, view]);
        html.ShouldContain("class=\"table-box\"");
        html.ShouldContain("class=\"view-box\"");
        html.ShouldContain("view-header");
        html.ShouldContain("ActiveUsers (view)");
    }

    [Fact]
    public void ContainsDagreLayout()
    {
        var model = new Table
        {
            Name = "Users",
            Columns = [new Column { Name = "Id", Type = "int", PrimaryKey = true }]
        };
        var html = DiagramGenerator.Generate([model]);
        html.ShouldContain("dagre.graphlib.Graph");
        html.ShouldContain("dagre.layout");
        html.ShouldContain("rankdir");
    }

    [Fact]
    public void PkAndFkStyling()
    {
        var model = new Table
        {
            Name = "Orders",
            Columns =
            [
                new Column { Name = "Id", Type = "int", PrimaryKey = true },
                new Column { Name = "CustomerId", Type = "int" }
            ],
            ForeignKeys =
            [
                new ForeignKey { Columns = ["CustomerId"], References = new() { Table = "Customers", Columns = ["Id"] } }
            ]
        };

        var html = DiagramGenerator.Generate([model]);
        html.ShouldContain("pk-row");
        html.ShouldContain("fk-row");
    }

    [Fact]
    public void ComputeGroups_ViewsGetViewsGroup()
    {
        var view = new Table { Name = "ActiveUsers", View = true };
        var groups = DiagramGenerator.ComputeGroups([view], null);
        groups["table-ActiveUsers"].ShouldBe("Views");
    }

    [Fact]
    public void ComputeGroups_EnumsGetEnumsGroup()
    {
        var enums = new List<EnumType>
        {
            new() { Name = "Status", Values = ["Active"] }
        };
        var groups = DiagramGenerator.ComputeGroups([], enums);
        groups["enum-Status"].ShouldBe("Enums");
    }

    [Fact]
    public void ComputeGroups_UnconnectedTable()
    {
        var model = new Table
        {
            Name = "Settings",
            Columns = [new Column { Name = "Id", Type = "int", PrimaryKey = true }]
        };
        var groups = DiagramGenerator.ComputeGroups([model], null);
        groups["table-Settings"].ShouldBe("Unconnected");
    }

    [Fact]
    public void ComputeGroups_FkChainWalksToRoot()
    {
        var root = new Table
        {
            Name = "Customers",
            Columns = [new Column { Name = "Id", Type = "int", PrimaryKey = true }]
        };
        var orders = new Table
        {
            Name = "Orders",
            Columns =
            [
                new Column { Name = "Id", Type = "int", PrimaryKey = true },
                new Column { Name = "CustomerId", Type = "int" }
            ],
            ForeignKeys = [new ForeignKey { Columns = ["CustomerId"], References = new() { Table = "Customers", Columns = ["Id"] } }]
        };
        var lines = new Table
        {
            Name = "OrderLines",
            Columns =
            [
                new Column { Name = "Id", Type = "int", PrimaryKey = true },
                new Column { Name = "OrderId", Type = "int" }
            ],
            ForeignKeys = [new ForeignKey { Columns = ["OrderId"], References = new() { Table = "Orders", Columns = ["Id"] } }]
        };

        var groups = DiagramGenerator.ComputeGroups([root, orders, lines], null);
        groups["table-Customers"].ShouldBe("Customers");
        groups["table-Orders"].ShouldBe("Customers");
        groups["table-OrderLines"].ShouldBe("Customers");
    }

    [Fact]
    public void SidebarRendered()
    {
        var model = new Table
        {
            Name = "Users",
            Columns = [new Column { Name = "Id", Type = "int", PrimaryKey = true }]
        };
        var html = DiagramGenerator.Generate([model]);
        html.ShouldContain("id=\"sidebar\"");
        html.ShouldContain("class=\"sidebar-header\"");
        html.ShouldContain("group-btn");
        html.ShouldContain("data-group=\"__all__\"");
    }

    [Fact]
    public void DataGroupAttribute()
    {
        var model = new Table
        {
            Name = "Users",
            Columns = [new Column { Name = "Id", Type = "int", PrimaryKey = true }]
        };
        var html = DiagramGenerator.Generate([model]);
        html.ShouldContain("data-group=\"");
    }

    [Fact]
    public void GroupsJsEmitted()
    {
        var parent = new Table
        {
            Name = "Customers",
            Columns = [new Column { Name = "Id", Type = "int", PrimaryKey = true }]
        };
        var child = new Table
        {
            Name = "Orders",
            Columns =
            [
                new Column { Name = "Id", Type = "int", PrimaryKey = true },
                new Column { Name = "CustomerId", Type = "int" }
            ],
            ForeignKeys = [new ForeignKey { Columns = ["CustomerId"], References = new() { Table = "Customers", Columns = ["Id"] } }]
        };
        var html = DiagramGenerator.Generate([parent, child]);
        html.ShouldContain("const groups = {");
        html.ShouldContain("elGroupMap");
    }

    [Fact]
    public void EnumDataGroupAttribute()
    {
        var enums = new List<EnumType>
        {
            new() { Name = "Status", Values = ["Active"] }
        };
        var html = DiagramGenerator.Generate([], enums);
        html.ShouldContain("data-group=\"Enums\"");
    }

    [Fact]
    public void MutuallyExclusiveGroupSelection()
    {
        var model = new Table
        {
            Name = "Users",
            Columns = [new Column { Name = "Id", Type = "int", PrimaryKey = true }]
        };
        var html = DiagramGenerator.Generate([model]);
        html.ShouldContain("selectGroup");
        html.ShouldContain("runLayout");
        html.ShouldContain("zoomToFit");
        html.ShouldContain("activeGroup");
    }

    [Fact]
    public void TitleUsesProjectName()
    {
        var model = new Table
        {
            Name = "Users",
            Columns = [new Column { Name = "Id", Type = "int", PrimaryKey = true }]
        };
        var html = DiagramGenerator.Generate([model], projectName: "my-accounting");
        html.ShouldContain("<title>ER Diagram — my-accounting</title>");
    }

    [Fact]
    public void TitleDefaultsWithoutProjectName()
    {
        var model = new Table
        {
            Name = "Users",
            Columns = [new Column { Name = "Id", Type = "int", PrimaryKey = true }]
        };
        var html = DiagramGenerator.Generate([model]);
        html.ShouldContain("<title>ER Diagram</title>");
    }

    [Fact]
    public void TitleBarRendersWithProjectNameDescriptionAndDate()
    {
        var model = new Table
        {
            Name = "Users",
            Columns = [new Column { Name = "Id", Type = "int", PrimaryKey = true }]
        };
        var html = DiagramGenerator.Generate([model], projectName: "my-accounting", description: "Financial system schema");
        html.ShouldContain("id=\"titlebar\"");
        html.ShouldContain("<span class=\"titlebar-name\">my-accounting</span>");
        html.ShouldContain("<span class=\"titlebar-desc\">Financial system schema</span>");
        var now = DateTime.Now;
        html.ShouldContain($"Generated {now:ddd, MMM d, yyyy}");
    }

    [Fact]
    public void TitleBarDefaultsToErDiagramWithoutProjectName()
    {
        var model = new Table
        {
            Name = "Users",
            Columns = [new Column { Name = "Id", Type = "int", PrimaryKey = true }]
        };
        var html = DiagramGenerator.Generate([model]);
        html.ShouldContain("<span class=\"titlebar-name\">ER Diagram</span>");
    }

    [Fact]
    public void TitleBarOmitsDescriptionWhenEmpty()
    {
        var model = new Table
        {
            Name = "Users",
            Columns = [new Column { Name = "Id", Type = "int", PrimaryKey = true }]
        };
        var html = DiagramGenerator.Generate([model]);
        html.ShouldNotContain("<span class=\"titlebar-desc\">");
    }

    [Fact]
    public void SidebarHeaderSaysEntities()
    {
        var model = new Table
        {
            Name = "Users",
            Columns = [new Column { Name = "Id", Type = "int", PrimaryKey = true }]
        };
        var html = DiagramGenerator.Generate([model]);
        html.ShouldContain(">Entities</div>");
        html.ShouldNotContain(">Groups</div>");
    }

    [Fact]
    public void LayoutDirectionPerGroup()
    {
        var model = new Table
        {
            Name = "Users",
            Columns = [new Column { Name = "Id", Type = "int", PrimaryKey = true }]
        };
        var html = DiagramGenerator.Generate([model]);
        // Grid layout for flat groups, dagre TB for FK-chain groups
        html.ShouldContain("lrGroups");
        html.ShouldContain("gridLayout");
        html.ShouldContain("runLayout('TB')");
    }
}
