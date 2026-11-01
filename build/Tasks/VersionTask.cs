using System.Text.RegularExpressions;
using Cake.Common;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Frosting;

/// <summary>
/// Resolves the version to release and writes version=X.Y.Z to $GITHUB_OUTPUT under Actions.
/// --bump=patch rewrites Directory.Build.props; the default reads it.
/// </summary>
[TaskName("Version")]
public sealed partial class VersionTask : FrostingTask<BuildContext>
{
    private const string VersionFile = "Directory.Build.props";

    [GeneratedRegex(@"<VersionPrefix>(?<version>\d+\.\d+\.\d+)</VersionPrefix>")]
    private static partial Regex VersionPattern();

    public override void Run(BuildContext context)
    {
        var text = File.ReadAllText(VersionFile);
        var match = VersionPattern().Match(text);
        if (!match.Success)
            throw new CakeException($"No <VersionPrefix>MAJOR.MINOR.PATCH</VersionPrefix> in {VersionFile}.");

        var version = match.Groups["version"].Value;
        var bump = context.Argument("bump", "none");
        if (bump != "none")
        {
            var parts = version.Split('.').Select(int.Parse).ToArray();
            version = bump switch
            {
                "major" => $"{parts[0] + 1}.0.0",
                "minor" => $"{parts[0]}.{parts[1] + 1}.0",
                "patch" => $"{parts[0]}.{parts[1]}.{parts[2] + 1}",
                _ => throw new CakeException($"Unknown bump '{bump}' (expected none, major, minor or patch)."),
            };
            File.WriteAllText(VersionFile, VersionPattern().Replace(text, $"<VersionPrefix>{version}</VersionPrefix>"));
        }

        context.Log.Information("Version: {0}", version);
        var output = context.EnvironmentVariable("GITHUB_OUTPUT");
        if (!string.IsNullOrEmpty(output))
            File.AppendAllText(output, $"version={version}\n");
    }
}
