using Cake.Common;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.NuGet.Push;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Frosting;

/// <summary>
/// Sends the packages already in the artifacts directory to --source. Deliberately not dependent
/// on Pack: promoting a release to another feed must push the packages that were tested and
/// released, not a fresh build of them.
/// </summary>
[TaskName("Push")]
public sealed class PushTask : FrostingTask<BuildContext>
{
    public const string ApiKeyVariable = "NUGET_API_KEY";

    public override void Run(BuildContext context)
    {
        if (string.IsNullOrWhiteSpace(context.PackageSource))
            throw new CakeException("Push needs --source=<feed url>.");

        var packages = context.GetFiles($"{BuildContext.ArtifactsDirectory}/*.nupkg");
        if (packages.Count == 0)
            throw new CakeException($"No packages in {BuildContext.ArtifactsDirectory}/. Run Pack first, or download a release's packages into it.");

        var local = packages.Where(p => p.GetFilename().FullPath.Contains(BuildContext.LocalVersion)).ToList();
        if (local.Count > 0)
            throw new CakeException(
                $"Refusing to push local builds ({string.Join(", ", local.Select(p => p.GetFilename()))}). Pack with --release-version.");

        // From the environment, never an argument: arguments end up in process lists and CI logs.
        var apiKey = context.EnvironmentVariable(ApiKeyVariable);
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new CakeException($"Push needs the feed's API key in the {ApiKeyVariable} environment variable.");

        foreach (var package in packages)
        {
            context.Log.Information("Pushing {0} to {1}", package.GetFilename(), context.PackageSource);
            context.DotNetNuGetPush(package, new DotNetNuGetPushSettings
            {
                Source = context.PackageSource,
                ApiKey = apiKey,

                // Makes a re-run after a partial failure finish the job instead of stopping at
                // the first package that already went up.
                SkipDuplicate = true
            });
        }
    }
}
