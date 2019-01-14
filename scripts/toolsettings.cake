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
        bool? dockerPushLatest,
        bool? nuGetPackSymbols)
    {
        BuildBinaryLoggerEnabled = buildBinaryLoggerEnabled ?? false;
        BuildEmbedAllSources = buildEmbedAllSources ?? false;
        BuildMaxCpuCount = buildMaxCpuCount;
        BuildTreatWarningsAsErrors = buildTreatWarningsAsErrors ?? false;

        UnitTestsLogger = unitTestsLogger ?? "console;verbosity=minimal";
        IntegrationTestsLogger = integrationTestsLogger ?? "console;verbosity=minimal";

        DockerPushLatest = dockerPushLatest ?? builder.Version.IsRelease;

        NuGetPackSymbols = nuGetPackSymbols ?? false;
    }

    public bool BuildBinaryLoggerEnabled { get; }
    public bool BuildEmbedAllSources { get; }
    public int? BuildMaxCpuCount { get; }
    public bool BuildTreatWarningsAsErrors { get; }

    public string UnitTestsLogger { get; }
    public string IntegrationTestsLogger { get; }

    public bool DockerPushLatest { get; }

    public bool NuGetPackSymbols { get; }
}
