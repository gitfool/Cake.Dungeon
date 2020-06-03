public class ToolSettings
{
    public ToolSettings(
        Builder build,

        bool? dotNetNoLogo,
        bool? buildBinaryLoggerEnabled,
        bool? buildEmbedAllSources,
        int? buildMaxCpuCount,
        bool? buildRestoreLockedMode,
        bool? buildTreatWarningsAsErrors,
        string unitTestsLogger,
        string integrationTestsLogger,
        bool? dockerBuildPull,
        bool? dockerPushLatest,
        bool? nuGetPackSymbols,
        bool? nuGetPushSkipDuplicate,
        string nuGetSource,
        string nuGetSourceName,
        string nuGetSourceConfigFile)
    {
        DotNetNoLogo = dotNetNoLogo ?? build.Context.Argument("dotnet-no-logo", build.Context.EnvironmentVariable("CAKE_DOTNET_NO_LOGO", false));

        BuildBinaryLoggerEnabled = buildBinaryLoggerEnabled ?? build.Context.Argument("build-binary-logger-enabled", build.Context.EnvironmentVariable("CAKE_BUILD_BINARY_LOGGER_ENABLED", false));
        BuildEmbedAllSources = buildEmbedAllSources ?? build.Context.Argument("build-embed-all-sources", build.Context.EnvironmentVariable("CAKE_BUILD_EMBED_ALL_SOURCES", false));
        BuildMaxCpuCount = buildMaxCpuCount ?? build.Context.Argument("build-max-cpu-count", build.Context.EnvironmentVariable("CAKE_BUILD_MAX_CPU_COUNT", (int?)null));
        BuildRestoreLockedMode = buildRestoreLockedMode ?? build.Context.Argument("build-restore-locked-mode", build.Context.EnvironmentVariable("CAKE_BUILD_RESTORE_LOCKED_MODE", false));
        BuildTreatWarningsAsErrors = buildTreatWarningsAsErrors ?? build.Context.Argument("build-treat-warnings-as-errors", build.Context.EnvironmentVariable("CAKE_BUILD_TREAT_WARNINGS_AS_ERRORS", false));

        UnitTestsLogger = unitTestsLogger ?? build.Context.Argument("unit-tests-logger", build.Context.EnvironmentVariable("CAKE_UNIT_TESTS_LOGGER", "console;verbosity=minimal"));
        IntegrationTestsLogger = integrationTestsLogger ?? build.Context.Argument("integration-tests-logger", build.Context.EnvironmentVariable("CAKE_INTEGRATION_TESTS_LOGGER", "console;verbosity=minimal"));

        DockerBuildPull = dockerBuildPull ?? build.Context.Argument("docker-build-pull", build.Context.EnvironmentVariable("CAKE_DOCKER_BUILD_PULL", false));
        DockerPushLatest = dockerPushLatest ?? build.Context.Argument("docker-push-latest", build.Context.EnvironmentVariable("CAKE_DOCKER_PUSH_LATEST", build.Version.IsRelease));

        NuGetPackSymbols = nuGetPackSymbols ?? build.Context.Argument("nuget-pack-symbols", build.Context.EnvironmentVariable("CAKE_NUGET_PACK_SYMBOLS", false));
        NuGetPushSkipDuplicate = nuGetPushSkipDuplicate ?? build.Context.Argument("nuget-push-skip-duplicate", build.Context.EnvironmentVariable("CAKE_NUGET_PUSH_SKIP_DUPLICATE", false));
        NuGetSource = nuGetSource ?? build.Context.Argument("nuget-source", build.Context.EnvironmentVariable("CAKE_NUGET_SOURCE", build.Context.EnvironmentVariable("NUGET_SOURCE", "https://api.nuget.org/v3/index.json")));
        NuGetSourceName = nuGetSourceName ?? build.Context.Argument("nuget-source-name", build.Context.EnvironmentVariable("CAKE_NUGET_SOURCE_NAME", build.Context.EnvironmentVariable("NUGET_SOURCE_NAME", "nuget.org")));
        NuGetSourceConfigFile = nuGetSourceConfigFile ?? build.Context.Argument("nuget-source-config-file", build.Context.EnvironmentVariable("CAKE_NUGET_SOURCE_CONFIG_FILE", build.Context.EnvironmentVariable("NUGET_SOURCE_CONFIG_FILE")));
    }

    public bool DotNetNoLogo { get; }

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
    public bool NuGetPushSkipDuplicate { get; }
    public string NuGetSource { get; }
    public string NuGetSourceName { get; }
    public string NuGetSourceConfigFile { get; }
}
