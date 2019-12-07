public class ToolSettings
{
    public ToolSettings(
        Builder builder,

        bool? buildBinaryLoggerEnabled,
        bool? buildEmbedAllSources,
        int? buildMaxCpuCount,
        bool? buildRestoreLockedMode,
        bool? buildTreatWarningsAsErrors,
        string unitTestsLogger,
        string integrationTestsLogger,
        bool? dockerBuildPull,
        bool? dockerPushLatest,
        string dockerRegistry,
        bool? nuGetPackSymbols)
    {
        BuildBinaryLoggerEnabled = buildBinaryLoggerEnabled ?? false;
        BuildEmbedAllSources = buildEmbedAllSources ?? false;
        BuildMaxCpuCount = buildMaxCpuCount;
        BuildRestoreLockedMode = buildRestoreLockedMode ?? builder.Context.Argument("BuildRestoreLockedMode", builder.Context.EnvironmentVariable("CAKE_BUILD_RESTORE_LOCKED_MODE", false));
        BuildTreatWarningsAsErrors = buildTreatWarningsAsErrors ?? builder.Context.Argument("BuildTreatWarningsAsErrors", builder.Context.EnvironmentVariable("CAKE_BUILD_TREAT_WARNINGS_AS_ERRORS", false));

        UnitTestsLogger = unitTestsLogger ?? "console;verbosity=minimal";
        IntegrationTestsLogger = integrationTestsLogger ?? "console;verbosity=minimal";

        DockerBuildPull = dockerBuildPull ?? false;
        DockerPushLatest = dockerPushLatest ?? builder.Version.IsRelease;
        DockerRegistry = dockerRegistry;

        NuGetPackSymbols = nuGetPackSymbols ?? false;
    }

    public bool BuildBinaryLoggerEnabled { get; }
    public bool BuildEmbedAllSources { get; }
    public int? BuildMaxCpuCount { get; }
    public bool BuildRestoreLockedMode { get; }
    public bool BuildTreatWarningsAsErrors { get; }

    public string UnitTestsLogger { get; }
    public string IntegrationTestsLogger { get; }

    public bool DockerBuildPull { get; }
    public bool DockerPushLatest { get; }
    public string DockerRegistry { get; }

    public bool NuGetPackSymbols { get; }
}
