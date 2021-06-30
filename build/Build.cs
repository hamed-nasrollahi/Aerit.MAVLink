using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;

using Aerit.MAVLink.Generator;

using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;
	// [GitRepository] readonly GitRepository GitRepository;

	// AbsolutePath DefinitionsDirectory => RootDirectory / "mavlink" / "message_definitions" / "v1.0";
	AbsolutePath DefinitionsDirectory => RootDirectory / "dialects";

	AbsolutePath SourceDirectory => RootDirectory / "source";

    AbsolutePath GeneratedDestination => SourceDirectory / "Aerit.MAVLink" / "Generated";
    AbsolutePath EnumsDestination => GeneratedDestination / "Enums";
    AbsolutePath MessagesDestination => GeneratedDestination / "Messages";
    AbsolutePath CommandsDestination => GeneratedDestination / "Commands";

    AbsolutePath TestsDestination => SourceDirectory / "Aerit.MAVLink.Tests" / "Generated";

    // AbsolutePath OutputDirectory => RootDirectory / "output";

    [Parameter("Namespace")]
    string Namespace { get; set; } = "Aerit.MAVLink";

    [Parameter("Generate tests for deprecated messages")]
    bool TestDeprecated { get; set; }

    [PathExecutable("git")]
    readonly Tool Git;

    Target Init => _ => _
        .Before(Generate)
        .Executes(() =>
        {
            Git("submodule update --init --recursive", workingDirectory: RootDirectory);
        });

    Target Clean => _ => _
        .Before(Generate)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);

            EnsureCleanDirectory(EnumsDestination);
            EnsureCleanDirectory(MessagesDestination);
            EnsureCleanDirectory(CommandsDestination);
            EnsureCleanDirectory(GeneratedDestination);
            EnsureCleanDirectory(TestsDestination);

            //EnsureCleanDirectory(OutputDirectory);
        });

    Target Generate => _ => _
        .Executes(() =>
        {
            EnsureExistingDirectory(GeneratedDestination);
            EnsureExistingDirectory(EnumsDestination);
            EnsureExistingDirectory(MessagesDestination);
            EnsureExistingDirectory(CommandsDestination);
            EnsureExistingDirectory(TestsDestination);

            Generator.Run(new(
                // Definitions: (DefinitionsDirectory, "common.xml"),
                Definitions: (DefinitionsDirectory, "aerit.xml"),
                Destination: new(
                    Generated: GeneratedDestination,
                    Enums: EnumsDestination,
                    Messages: MessagesDestination,
                    Commands: CommandsDestination,
                    Tests: TestsDestination),
                Namespace: Namespace,
                TestDeprecated: TestDeprecated));
        });

    Target Restore => _ => _
        .DependsOn(Generate)
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild());
        });
}