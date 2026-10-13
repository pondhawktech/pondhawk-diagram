using System.Text.Json;
using System.Text.Json.Serialization;
using Shouldly;

namespace Pondhawk.Diagram.Tests;

/// <summary>The schema file is written by hand, so these hold it to the types it describes.</summary>
public class JsonSchemaTests
{
    private static readonly JsonElement Schema = JsonDocument.Parse(DiagramModel.JsonSchema).RootElement;

    [Fact]
    public void EmbeddedSchema_IsTheFileInTheRepository()
    {
        DiagramModel.JsonSchema.ShouldBe(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "schema", "diagram-model.schema.json")));
    }

    [Theory]
    [InlineData(typeof(DiagramModel), null)]
    [InlineData(typeof(Table), "table")]
    [InlineData(typeof(Column), "column")]
    [InlineData(typeof(ForeignKey), "foreignKey")]
    [InlineData(typeof(TableReference), "tableReference")]
    [InlineData(typeof(TableIndex), "tableIndex")]
    [InlineData(typeof(EnumType), "enumType")]
    public void Schema_ListsExactlyThePropertiesOfTheType(Type type, string? definition)
    {
        var node = definition is null ? Schema : Schema.GetProperty("$defs").GetProperty(definition);

        var expected = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Select(p => p.GetCustomAttributes(typeof(JsonPropertyNameAttribute), false)
                             .Cast<JsonPropertyNameAttribute>().FirstOrDefault()?.Name
                         ?? JsonNamingPolicy.CamelCase.ConvertName(p.Name))
            .Order();

        node.GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();
        node.GetProperty("properties").EnumerateObject().Select(p => p.Name).Order().ShouldBe(expected);
    }

    [Fact]
    public void Schema_VersionMatchesTheLoader()
    {
        Schema.GetProperty("properties").GetProperty("version").GetProperty("const").GetInt32()
            .ShouldBe(DiagramModel.CurrentVersion);
    }
}
