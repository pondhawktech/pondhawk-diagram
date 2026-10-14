using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Run;
using Cake.Frosting;

[TaskName("Test")]
[IsDependentOn(typeof(BuildTask))]
public sealed class TestTask : FrostingTask<BuildContext>
{
    private static readonly string[] Projects =
    [
        "tests/Pondhawk.Diagram.Tests",
        "tests/Pondhawk.Diagram.Tool.Tests",
        "tests/Cake.Pondhawk.Diagram.Tests"
    ];

    public override void Run(BuildContext context)
    {
        // xUnit v3 tests are standalone executables, run with dotnet run
        foreach (var project in Projects)
        {
            context.DotNetRun(project, settings: new DotNetRunSettings
            {
                Configuration = context.Configuration,
                NoBuild = true
            });
        }
    }
}
