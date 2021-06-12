#load builder.cake

public class Parameters
{
    public Parameters(
        Builder build,
        string title,
        string target,
        string configuration,
        bool? publish,
        bool? deploy,
        string deployEnvironment,
        bool? defaultLog,
        bool? logEnvironment,
        bool? logBuildSystem,
        bool? logContext,
        bool? defaultRun,
        bool? runBuildSolutions,
        bool? runDockerBuild,
        bool? runUnitTests,
        bool? runIntegrationTests,
        bool? runTestCoverageReports,
        bool? runNuGetPack,
        bool? runPublishToDocker,
        bool? runPublishToNuGet,
        bool? runDockerDeploy)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title), @"Set the build title to the ""artifact"" name");
        Target = target ?? build.Context.Argument("target", build.Context.EnvironmentVariable("CAKE_TARGET", "Build"));
        Configuration = configuration ?? build.Context.Argument("configuration", build.Context.EnvironmentVariable("CAKE_CONFIGURATION", "Release"));

        Publish = publish ?? build.Context.Argument("publish", build.Context.EnvironmentVariable("CAKE_PUBLISH", false));
        Deploy = deploy ?? build.Context.Argument("deploy", build.Context.EnvironmentVariable("CAKE_DEPLOY", true));
        DeployEnvironment = deployEnvironment ?? build.Context.Argument("deploy-environment", build.Context.EnvironmentVariable("CAKE_DEPLOY_ENVIRONMENT", "CI"));

        DefaultLog = defaultLog ?? build.Context.Argument("default-log", build.Context.EnvironmentVariable("CAKE_DEFAULT_LOG", false));
        LogEnvironment = logEnvironment ?? build.Context.Argument("log-environment", build.Context.EnvironmentVariable("CAKE_LOG_ENVIRONMENT", DefaultLog));
        LogBuildSystem = logBuildSystem ?? build.Context.Argument("log-build-system", build.Context.EnvironmentVariable("CAKE_LOG_BUILD_SYSTEM", DefaultLog));
        LogContext = logContext ?? build.Context.Argument("log-context", build.Context.EnvironmentVariable("CAKE_LOG_CONTEXT", DefaultLog));

        DefaultRun = defaultRun ?? false;
        RunBuildSolutions = runBuildSolutions ?? DefaultRun;
        RunDockerBuild = runDockerBuild ?? DefaultRun;
        RunUnitTests = runUnitTests ?? DefaultRun;
        RunIntegrationTests = runIntegrationTests ?? DefaultRun;
        RunTestCoverageReports = runTestCoverageReports ?? DefaultRun;
        RunNuGetPack = runNuGetPack ?? DefaultRun;
        RunPublishToDocker = runPublishToDocker ?? DefaultRun;
        RunPublishToNuGet = runPublishToNuGet ?? DefaultRun;
        RunDockerDeploy = runDockerDeploy ?? DefaultRun;
    }

    public string Title { get; }
    public string Target { get; }
    public string Configuration { get; }

    public bool Publish { get; }
    public bool Deploy { get; }
    public string DeployEnvironment { get; }

    public bool DefaultLog { get; }
    public bool LogEnvironment { get; }
    public bool LogBuildSystem { get; }
    public bool LogContext { get; }

    public bool DefaultRun { get; }
    public bool RunBuildSolutions { get; }
    public bool RunDockerBuild { get; }
    public bool RunUnitTests { get; }
    public bool RunIntegrationTests { get; }
    public bool RunTestCoverageReports { get; }
    public bool RunNuGetPack { get; }
    public bool RunPublishToDocker { get; }
    public bool RunPublishToNuGet { get; }
    public bool RunDockerDeploy { get; }
}
