using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Common.Tools.DotNet.MSBuild;
using Cake.Frosting;

[TaskName("Build")]
[IsDependentOn(typeof(RestoreTask))]
public sealed class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var settings = new DotNetBuildSettings
        {
            Configuration = context.Configuration,
            NoRestore = true
        };

        // Stamped here rather than at pack time: Pack does not rebuild, so the assemblies inside
        // a package carry whatever version this build gave them.
        if (context.Version is not null)
            settings.MSBuildSettings = new DotNetMSBuildSettings().SetVersion(context.Version);

        context.DotNetBuild(context.SolutionPath, settings);
    }
}
