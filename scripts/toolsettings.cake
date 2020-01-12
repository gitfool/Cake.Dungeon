public class ToolSettings
{
    public ToolSettings(
        Builder build,

        bool? buildBinaryLoggerEnabled,
        bool? buildEmbedAllSources,
        int? buildMaxCpuCount,
        bool? buildRestoreLockedMode,
        bool? buildTreatWarningsAsErrors,
        string unitTestsLogger,
        string integrationTestsLogger,
        bool? dockerBuildPull,
        bool? dockerPushLatest,
        bool? nuGetPackSymbols)
    {
        BuildBinaryLoggerEnabled = buildBinaryLoggerEnabled ?? false;
        BuildEmbedAllSources = buildEmbedAllSources ?? false;
        BuildMaxCpuCount = buildMaxCpuCount;
        BuildRestoreLockedMode = buildRestoreLockedMode ?? build.Context.Argument("BuildRestoreLockedMode", build.Context.EnvironmentVariable("CAKE_BUILD_RESTORE_LOCKED_MODE", false));
        BuildTreatWarningsAsErrors = buildTreatWarningsAsErrors ?? build.Context.Argument("BuildTreatWarningsAsErrors", build.Context.EnvironmentVariable("CAKE_BUILD_TREAT_WARNINGS_AS_ERRORS", false));

        UnitTestsLogger = unitTestsLogger ?? "console;verbosity=minimal";
        IntegrationTestsLogger = integrationTestsLogger ?? "console;verbosity=minimal";

        DockerBuildPull = dockerBuildPull ?? false;
        DockerPushLatest = dockerPushLatest ?? build.Version.IsRelease;

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

    public bool NuGetPackSymbols { get; }
}
