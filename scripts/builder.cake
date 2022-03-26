#load bootstrap.cake

public class Builder
{
    public Builder(BuildSystem buildSystem, ICakeContext context, Action<string> runTarget)
    {
        BuildSystem = buildSystem;
        Context = context;
        _runTarget = runTarget;

        SetVersion();
        SetParameters(title: ""); // defaults
    }

    public void Info()
    {
        if (BuildSystem.IsRunningOnGitHubActions)
        {
            Context.Information($"::set-output name=build::{ToJson()}");
        }

        var secrets = typeof(Environment).GetProperties().Select(property => property.GetValue(Environment)).ToArray();
        var variables = Context.EnvironmentVariables()
            .OrderBy(entry => entry.Key)
            .ToDictionary(entry => entry.Key, entry => secrets.Contains(entry.Key) ? entry.Value.Redact() : entry.Value); // redact secrets

        var properties = ToTokens()
            .Where(entry => (Parameters.LogBuildSystem && entry.Key.StartsWith($"Build.BuildSystem.{BuildSystem.Provider}.")) ||
                (Parameters.LogContext && entry.Key.StartsWith("Build.Context.")) ||
                (Parameters.DefaultLog && !entry.Key.StartsWith("Build.BuildSystem.") && !entry.Key.StartsWith("Build.Context.")))
            .ToDictionary(entry => entry.Key.Substring(6), entry => entry.Key.StartsWith("Build.Credentials.") && !entry.Key.EndsWith(".IsConfigured")
                ? entry.Value?.ToString()?.Redact() : entry.Value); // redact secrets

        var padding = variables.Keys.Concat(properties.Keys).Select(key => key.Length).Max() + 4;

        Context.Information(Version.Summary);
        if (Parameters.LogEnvironment)
        {
            Context.Information("");
            foreach (var variable in variables)
            {
                Context.Information(string.Concat(variable.Key.PadRight(padding), variable.Value.ToValueString()));
            }
        }

        var groups = new HashSet<string>();
        foreach (var property in properties)
        {
            var group = property.Key.Split('.').First();
            if (!groups.Contains(group))
            {
                groups.Add(group);
                Context.Information("");
            }
            Context.Information(string.Concat(property.Key.PadRight(padding), property.Value.ToValueString()));
        }
    }

    public void Run()
    {
        _runTarget(Parameters.Target);
    }

    public Builder SetParameters(
        string title = null, // general
        string target = null,
        string configuration = null,
        bool? publish = null,
        bool? deploy = null,
        string deployEnvironment = null,
        bool? defaultLog = null,
        bool? logEnvironment = null,
        bool? logBuildSystem = null,
        bool? logContext = null,
        bool? defaultRun = null,
        bool? runBuildSolutions = null,
        bool? runDockerBuild = null,
        bool? runUnitTests = null,
        bool? runIntegrationTests = null,
        bool? runTestCoverageReports = null,
        bool? runNuGetPack = null,
        bool? runPublishToDocker = null,
        bool? runPublishToNuGet = null,
        bool? runDockerDeploy = null,

        string gitHubTokenVariable = null, // environment
        string gitHubUserNameVariable = null,
        string gitHubPasswordVariable = null,
        string nuGetApiKeyVariable = null,
        string nuGetUserNameVariable = null,
        string nuGetPasswordVariable = null,

        DirectoryPath rootDirectory = null, // directories
        DirectoryPath sourceDirectory = null,
        DirectoryPath artifactsDirectory = null,
        DirectoryPath artifactsTestsDirectory = null,
        DirectoryPath artifactsNuGetDirectory = null,

        string[] buildSolutionPatterns = null, // patterns
        string[] unitTestProjectPatterns = null,
        string[] integrationTestProjectPatterns = null,
        string[] testCoverageReportPatterns = null,
        string[] nuGetProjectPatterns = null,

        bool? dotNetNoLogo = null, // tool settings
        bool? buildBinaryLoggerEnabled = null,
        bool? buildEmbedAllSources = null,
        int? buildMaxCpuCount = null,
        bool? buildRestoreLockedMode = null,
        bool? buildTreatWarningsAsErrors = null,
        string[] unitTestCollectors = null,
        string[] unitTestLoggers = null,
        string[] unitTestRunSettings = null,
        FilePath unitTestRunSettingsFile = null,
        string[] integrationTestCollectors = null,
        string[] integrationTestLoggers = null,
        string[] integrationTestRunSettings = null,
        FilePath integrationTestRunSettingsFile = null,
        string[] testCoverageReportAssemblyFilters = null,
        string[] testCoverageReportClassFilters = null,
        string[] testCoverageReportTypes = null,
        bool? dockerBuildPull = null,
        bool? dockerPushLatest = null,
        bool? dockerPushSkipDuplicate = null,
        string[] dockerTagsDefault = null,
        string[] dockerTagsLatest = null,
        bool? nuGetPackSymbols = null,
        string nuGetPackSymbolsFormat = null,
        bool? nuGetPushSkipDuplicate = null,
        string nuGetSource = null,
        string nuGetSourceName = null,
        string nuGetSourceConfigFile = null,

        DockerImage[] dockerImages = null, // docker images
        DockerDeployer[] dockerDeployers = null) // docker deployers
    {
        Parameters = new Parameters(
            this,
            title,
            target,
            configuration,
            publish,
            deploy,
            deployEnvironment,
            defaultLog,
            logEnvironment,
            logBuildSystem,
            logContext,
            defaultRun,
            runBuildSolutions,
            runDockerBuild,
            runUnitTests,
            runIntegrationTests,
            runTestCoverageReports,
            runNuGetPack,
            runPublishToDocker,
            runPublishToNuGet,
            runDockerDeploy);

        Environment = new Environment(
            gitHubTokenVariable,
            gitHubUserNameVariable,
            gitHubPasswordVariable,
            nuGetApiKeyVariable,
            nuGetUserNameVariable,
            nuGetPasswordVariable);

        Credentials = new Credentials(
            Context,
            Environment);

        Directories = new Directories(
            Context,
            rootDirectory,
            sourceDirectory,
            artifactsDirectory,
            artifactsTestsDirectory,
            artifactsNuGetDirectory);

        Files = new Files(
            Context,
            Directories);

        Patterns = new Patterns(
            buildSolutionPatterns,
            unitTestProjectPatterns,
            integrationTestProjectPatterns,
            testCoverageReportPatterns,
            nuGetProjectPatterns);

        ToolSettings = new ToolSettings(
            this,
            dotNetNoLogo,
            buildBinaryLoggerEnabled,
            buildEmbedAllSources,
            buildMaxCpuCount,
            buildRestoreLockedMode,
            buildTreatWarningsAsErrors,
            unitTestCollectors,
            unitTestLoggers,
            unitTestRunSettings,
            unitTestRunSettingsFile,
            integrationTestCollectors,
            integrationTestLoggers,
            integrationTestRunSettings,
            integrationTestRunSettingsFile,
            testCoverageReportAssemblyFilters,
            testCoverageReportClassFilters,
            testCoverageReportTypes,
            dockerBuildPull,
            dockerPushLatest,
            dockerPushSkipDuplicate,
            dockerTagsDefault,
            dockerTagsLatest,
            nuGetPackSymbols,
            nuGetPackSymbolsFormat,
            nuGetPushSkipDuplicate,
            nuGetSource,
            nuGetSourceName,
            nuGetSourceConfigFile);

        DockerImages = dockerImages;
        DockerDeployers = dockerDeployers;

        return this;
    }

    public string ToJson() =>
        _json ??= new { Parameters = new { Parameters.Title, Parameters.Configuration, Parameters.Publish, Parameters.Deploy, Parameters.DeployEnvironment }, Version }
            .ToJson(JsonSerializerDefaults.Web);

    public Dictionary<string, string> ToEnvVars() =>
        _envVars ??= ToTokens()
            .Where(entry => Regex.IsMatch(entry.Key, @"^Build\.(?:(?:Parameters\.(?:Title|Configuration|Publish|Deploy))|Version)"))
            .ToDictionary(entry => entry.Key.ToEnvVar(), entry => entry.Value?.ToString());

    public Dictionary<string, object> ToTokens() =>
        _tokens ??= this.ToTokens("Build")
            .ToDictionary(entry => entry.Key, entry => entry.Value);

    public void TransformTokens(FilePathCollection files)
    {
        foreach (var file in files)
        {
            TransformTokens(file, file);
        }
    }

    public void TransformTokens(FilePath source, FilePath destination) =>
        Context.TransformTextFile(source, "{{", "}}")
            .WithTokens(ToTokens())
            .Save(destination);

    private void SetVersion()
    {
        Version = new Version(
            BuildSystem,
            Context);
    }

    public BuildSystem BuildSystem { get; }
    public ICakeContext Context { get; }

    public Parameters Parameters { get; private set; }
    public Environment Environment { get; private set; }
    public Credentials Credentials { get; private set; }
    public Directories Directories { get; private set; }
    public Files Files { get; private set; }
    public Patterns Patterns { get; private set; }
    public ToolSettings ToolSettings { get; private set; }

    public DockerImage[] DockerImages { get; private set; }
    public DockerDeployer[] DockerDeployers { get; private set; }

    public Version Version { get; private set; }

    private readonly Action<string> _runTarget;

    private string _json;
    private Dictionary<string, string> _envVars;
    private Dictionary<string, object> _tokens;
}
