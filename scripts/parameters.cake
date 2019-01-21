public class Parameters
{
    public Parameters(
        Builder builder,

        string title,
        string target,
        string configuration,

        bool? isPublisher,

        bool? defaultLog,
        bool? logEnvironment,
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
        Target = target ?? builder.Context.Argument("Target", "Default");
        Configuration = configuration ?? builder.Context.Argument("Configuration", builder.Context.EnvironmentVariable("CAKE_CONFIGURATION", "Release"));

        IsPublisher = isPublisher ?? builder.Context.Argument("IsPublisher", builder.Context.EnvironmentVariable("CAKE_IS_PUBLISHER", false));

        DefaultLog = defaultLog ?? builder.Context.Argument("DefaultLog", builder.Context.EnvironmentVariable("CAKE_DEFAULT_LOG", false));
        LogEnvironment = logEnvironment ?? builder.Context.Argument("LogEnvironment", builder.Context.EnvironmentVariable("CAKE_LOG_ENVIRONMENT", DefaultLog));
        LogBuildSystem = logBuildSystem ?? builder.Context.Argument("LogBuildSystem", builder.Context.EnvironmentVariable("CAKE_LOG_BUILD_SYSTEM", DefaultLog));
        LogContext = logContext ?? builder.Context.Argument("LogContext", builder.Context.EnvironmentVariable("CAKE_LOG_CONTEXT", DefaultLog));

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

    public bool IsPublisher { get; }

    public bool DefaultLog { get; }
    public bool LogEnvironment { get; }
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
