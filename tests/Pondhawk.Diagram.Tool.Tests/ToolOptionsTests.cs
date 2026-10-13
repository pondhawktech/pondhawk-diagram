using Shouldly;

namespace Pondhawk.Diagram.Tool.Tests;

public class ToolOptionsTests
{
    [Fact]
    public void ParsesAllOptions()
    {
        var options = ToolOptions.Parse(["model.json", "-o", "out/x.html", "-t", "Shop", "-d", "Orders"]);

        options.Input.ShouldBe("model.json");
        options.Output.ShouldBe("out/x.html");
        options.Title.ShouldBe("Shop");
        options.Description.ShouldBe("Orders");
        options.ReadsStandardInput.ShouldBeFalse();
    }

    [Fact]
    public void OutputDefaultsToTheModelNameWithHtml()
    {
        ToolOptions.Parse(["docs/shop.json"]).Output.ShouldBe(Path.Combine("docs", "shop.html"));
    }

    [Fact]
    public void DashReadsStandardInput()
    {
        var options = ToolOptions.Parse(["-"]);

        options.ReadsStandardInput.ShouldBeTrue();
        options.Output.ShouldBe("er-diagram.html");
    }

    [Fact]
    public void InputMayFollowTheOptions()
    {
        ToolOptions.Parse(["-t", "Shop", "model.json"]).Input.ShouldBe("model.json");
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("--print-schema")]
    public void InformationalOptionsNeedNoModel(string arg)
    {
        var options = ToolOptions.Parse([arg]);

        (options.Help || options.PrintSchema).ShouldBeTrue();
    }

    [Theory]
    [InlineData("A model file is required")]
    [InlineData("Unknown option: --bogus", "model.json", "--bogus")]
    [InlineData("--output needs a value", "model.json", "--output")]
    [InlineData("Only one model", "a.json", "b.json")]
    public void RejectsBadInput(string expected, params string[] args)
    {
        Should.Throw<ArgumentException>(() => ToolOptions.Parse(args)).Message.ShouldContain(expected);
    }
}
