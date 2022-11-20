#load bootstrap.cake

public class Parameters
{
    public Parameters(
        ICakeContext context,
        string title,
        string target,
        string configuration,
        bool? publish,
        bool? deploy,
        string deployEnvironment,
        string redactRegex,
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
        Target = target ?? context.Argument("target", context.EnvironmentVariable("CAKE_TARGET", "Build"));
        Configuration = configuration ?? context.Argument("configuration", context.EnvironmentVariable("CAKE_CONFIGURATION", "Release"));

        Publish = publish ?? context.Argument("publish", context.EnvironmentVariable("CAKE_PUBLISH", false));
        Deploy = deploy ?? context.Argument("deploy", context.EnvironmentVariable("CAKE_DEPLOY", Target.Equals("Deploy", StringComparison.OrdinalIgnoreCase)));
        DeployEnvironment = deployEnvironment ?? context.Argument("deploy-environment", context.EnvironmentVariable("CAKE_DEPLOY_ENVIRONMENT", "CI"));

        RedactRegex = redactRegex ?? context.Argument("redact-regex", context.EnvironmentVariable("CAKE_REDACT_REGEX", @"Api_?Key|Password|Secret|Token"));

        DefaultLog = defaultLog ?? context.Argument("default-log", context.EnvironmentVariable("CAKE_DEFAULT_LOG", false));
        LogEnvironment = logEnvironment ?? context.Argument("log-environment", context.EnvironmentVariable("CAKE_LOG_ENVIRONMENT", DefaultLog));
        LogBuildSystem = logBuildSystem ?? context.Argument("log-build-system", context.EnvironmentVariable("CAKE_LOG_BUILD_SYSTEM", DefaultLog));
        LogContext = logContext ?? context.Argument("log-context", context.EnvironmentVariable("CAKE_LOG_CONTEXT", DefaultLog));

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

    public string RedactRegex { get; }

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
