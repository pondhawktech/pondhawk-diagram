using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.MSBuild;
using Cake.Common.Tools.DotNet.Pack;
using Cake.Frosting;

[TaskName("Pack")]
[IsDependentOn(typeof(TestTask))]
public sealed class PackTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var settings = new DotNetPackSettings
        {
            Configuration = context.Configuration,
            NoBuild = true,
            NoRestore = true,
            OutputDirectory = BuildContext.ArtifactsDirectory
        };

        // The same version the build was given, so each package's dependency on
        // Pondhawk.Diagram names the version being packed beside it.
        if (context.Version is not null)
            settings.MSBuildSettings = new DotNetMSBuildSettings().SetVersion(context.Version);

        // Packs every project that has not opted out with IsPackable=false.
        context.DotNetPack(context.SolutionPath, settings);
    }
}
