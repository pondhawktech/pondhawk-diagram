using Cake.Core;
using Cake.Frosting;

return new CakeHost()
    .UseContext<BuildContext>()
    .Run(args);

public class BuildContext : FrostingContext
{
    /// <summary>What a package is versioned when no release version is given; see Directory.Build.props.</summary>
    public const string LocalVersion = "0.0.0-dev";

    /// <summary>Where Pack writes packages and Push reads them.</summary>
    public const string ArtifactsDirectory = "artifacts";

    public string SolutionPath { get; }
    public new string Configuration { get; }

    /// <summary>
    /// Version stamped into the assemblies and packages, from --release-version (Cake reserves
    /// --version for itself). Null leaves the default in Directory.Build.props, which marks the
    /// build as a local one rather than a release.
    /// </summary>
    public string? Version { get; }

    /// <summary>The feed Push sends packages to, from --source.</summary>
    public string? PackageSource { get; }

    public BuildContext(ICakeContext context) : base(context)
    {
        SolutionPath = context.Arguments.GetArgument("solution") ?? "pondhawk-diagram.slnx";
        Configuration = context.Arguments.GetArgument("configuration") ?? "Release";

        // Accepts either "1.0.0" or the tag it came from, "v1.0.0", so the release workflow can
        // pass the tag name through untouched.
        var version = context.Arguments.GetArgument("release-version");
        Version = string.IsNullOrWhiteSpace(version) ? null : version.TrimStart('v', 'V');

        PackageSource = context.Arguments.GetArgument("source");
    }
}
