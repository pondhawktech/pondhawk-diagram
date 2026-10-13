namespace Cake.Pondhawk.Diagram;

/// <summary>Optional overrides for <c>GenerateDiagram</c>.</summary>
public sealed class DiagramSettings
{
    /// <summary>Title to show instead of the model's <c>name</c>.</summary>
    public string? Title { get; set; }

    /// <summary>Subtitle to show instead of the model's <c>description</c>.</summary>
    public string? Description { get; set; }
}
