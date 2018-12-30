public class Parameters
{
    public Parameters(
        Builder builder,

        string title,
        string target,
        string configuration,

        bool? defaultLog,
        bool? logBuildSystem,
        bool? logContext,

        bool? defaultRun,
        bool? runBuild,
        bool? runBuildPublish,
        bool? runUnitTests,
        bool? runDockerBuild,
        bool? runIntegrationTests,
        bool? runNuGetPack,
        bool? runPublishToDocker,
        bool? runPublishToNuGet)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title), @"Set the build title to the ""artifact"" name");
        Target = target ?? builder.Context.Argument("target", "Default");
        Configuration = configuration ?? builder.Context.Argument("configuration", "Release");

        DefaultLog = defaultLog ?? false;
        LogBuildSystem = logBuildSystem ?? DefaultLog;
        LogContext = logContext ?? DefaultLog;

        DefaultRun = defaultRun ?? false;
        RunBuild = runBuild ?? DefaultRun;
        RunBuildPublish = runBuildPublish ?? DefaultRun;
        RunUnitTests = runUnitTests ?? DefaultRun;
        RunDockerBuild = runDockerBuild ?? DefaultRun;
        RunIntegrationTests = runIntegrationTests ?? DefaultRun;
        RunNuGetPack = runNuGetPack ?? DefaultRun;
        RunPublishToDocker = runPublishToDocker ?? DefaultRun;
        RunPublishToNuGet = runPublishToNuGet ?? DefaultRun;
    }

    public string Title { get; }
    public string Target { get; }
    public string Configuration { get; }

    public bool DefaultLog { get; }
    public bool LogBuildSystem { get; }
    public bool LogContext { get; }

    public bool DefaultRun { get; }
    public bool RunBuild { get; }
    public bool RunBuildPublish { get; }
    public bool RunUnitTests { get; }
    public bool RunDockerBuild { get; }
    public bool RunIntegrationTests { get; }
    public bool RunNuGetPack { get; }
    public bool RunPublishToDocker { get; }
    public bool RunPublishToNuGet { get; }
}
