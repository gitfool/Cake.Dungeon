#load bootstrap.cake

public class ToolSettings
{
    public ToolSettings(
        ICakeContext context,
        bool? dotNetNoLogo,
        bool? buildBinaryLoggerEnabled,
        bool? buildEmbedAllSources,
        int? buildMaxCpuCount,
        bool? buildRestoreLockedMode,
        bool? buildSkipClean,
        bool? buildTreatWarningsAsErrors,
        string[] unitTestCollectors,
        string[] unitTestLoggers,
        string[] unitTestRunSettings,
        FilePath unitTestRunSettingsFile,
        string[] integrationTestCollectors,
        string[] integrationTestLoggers,
        string[] integrationTestRunSettings,
        FilePath integrationTestRunSettingsFile,
        string[] testCoverageReportAssemblyFilters,
        string[] testCoverageReportClassFilters,
        string[] testCoverageReportTypes,
        bool? dockerBuildCache,
        bool? dockerBuildLoad,
        bool? dockerBuildPull,
        bool? dockerPushLatest,
        bool? dockerPushSkipDuplicate,
        string[] dockerTagsDefault,
        string[] dockerTagsLatest,
        bool? nuGetPackSymbols,
        string nuGetPackSymbolsFormat,
        bool? nuGetPushSkipDuplicate,
        string nuGetSource,
        string nuGetSourceName,
        string nuGetSourceConfigFile)
    {
        DotNetNoLogo = dotNetNoLogo ?? false;

        BuildBinaryLoggerEnabled = buildBinaryLoggerEnabled ?? false;
        BuildEmbedAllSources = buildEmbedAllSources ?? false;
        BuildMaxCpuCount = buildMaxCpuCount;
        BuildRestoreLockedMode = buildRestoreLockedMode ?? false;
        BuildSkipClean = buildSkipClean ?? false;
        BuildTreatWarningsAsErrors = buildTreatWarningsAsErrors ?? false;

        UnitTestCollectors = unitTestCollectors;
        UnitTestLoggers = unitTestLoggers ?? new[] { "console;verbosity=detailed" };
        UnitTestRunSettings = unitTestRunSettings;
        UnitTestRunSettingsFile = unitTestRunSettingsFile;
        IntegrationTestCollectors = integrationTestCollectors;
        IntegrationTestLoggers = integrationTestLoggers ?? new[] { "console;verbosity=detailed" };
        IntegrationTestRunSettings = integrationTestRunSettings;
        IntegrationTestRunSettingsFile = integrationTestRunSettingsFile;
        TestCoverageReportAssemblyFilters = testCoverageReportAssemblyFilters;
        TestCoverageReportClassFilters = testCoverageReportClassFilters;
        TestCoverageReportTypes = testCoverageReportTypes ?? new[] { "Cobertura", "TextSummary" };

        DockerBuildCache = dockerBuildCache ?? false;
        DockerBuildLoad = dockerBuildLoad ?? false;
        DockerBuildPull = dockerBuildPull ?? false;
        DockerPushLatest = dockerPushLatest ?? false;
        DockerPushSkipDuplicate = dockerPushSkipDuplicate ?? false;
        DockerTagsDefault = dockerTagsDefault ?? new[] { "latest" };
        DockerTagsLatest = dockerTagsLatest ?? new[] { "latest" };

        NuGetPackSymbols = nuGetPackSymbols ?? false;
        NuGetPackSymbolsFormat = nuGetPackSymbolsFormat;
        NuGetPushSkipDuplicate = nuGetPushSkipDuplicate ?? false;
        NuGetSource = nuGetSource ?? context.Argument("nuget-source", context.EnvironmentVariable("NUGET_SOURCE", "https://api.nuget.org/v3/index.json"));
        NuGetSourceName = nuGetSourceName ?? context.Argument("nuget-source-name", context.EnvironmentVariable("NUGET_SOURCE_NAME", "nuget.org"));
        NuGetSourceConfigFile = nuGetSourceConfigFile ?? context.Argument("nuget-source-config-file", context.EnvironmentVariable("NUGET_SOURCE_CONFIG_FILE"));
    }

    public bool DotNetNoLogo { get; }

    public bool BuildBinaryLoggerEnabled { get; }
    public bool BuildEmbedAllSources { get; }
    public int? BuildMaxCpuCount { get; }
    public bool BuildRestoreLockedMode { get; }
    public bool BuildSkipClean { get; }
    public bool BuildTreatWarningsAsErrors { get; }

    public string[] UnitTestCollectors { get; }
    public string[] UnitTestLoggers { get; }
    public string[] UnitTestRunSettings { get; }
    public FilePath UnitTestRunSettingsFile { get; }
    public string[] IntegrationTestCollectors { get; }
    public string[] IntegrationTestLoggers { get; }
    public string[] IntegrationTestRunSettings { get; }
    public FilePath IntegrationTestRunSettingsFile { get; }
    public string[] TestCoverageReportAssemblyFilters { get; }
    public string[] TestCoverageReportClassFilters { get; }
    public string[] TestCoverageReportTypes { get; }

    public bool DockerBuildCache { get; }
    public bool DockerBuildLoad { get; }
    public bool DockerBuildPull { get; }
    public bool DockerPushLatest { get; }
    public bool DockerPushSkipDuplicate { get; }
    public string[] DockerTagsDefault { get; }
    public string[] DockerTagsLatest { get; }

    public bool NuGetPackSymbols { get; }
    public string NuGetPackSymbolsFormat { get; }
    public bool NuGetPushSkipDuplicate { get; }
    public string NuGetSource { get; }
    public string NuGetSourceName { get; }
    public string NuGetSourceConfigFile { get; }
}
