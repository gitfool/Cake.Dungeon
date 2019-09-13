public class ToolSettings
{
    public ToolSettings(
        Builder builder,

        bool? buildBinaryLoggerEnabled,
        bool? buildEmbedAllSources,
        int? buildMaxCpuCount,
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
        BuildTreatWarningsAsErrors = buildTreatWarningsAsErrors ?? false;

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
    public bool BuildTreatWarningsAsErrors { get; }

    public string UnitTestsLogger { get; }
    public string IntegrationTestsLogger { get; }

    public bool DockerBuildPull { get; }
    public bool DockerPushLatest { get; }
    public string DockerRegistry { get; }

    public bool NuGetPackSymbols { get; }
}
