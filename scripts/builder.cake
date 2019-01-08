public class Builder
{
    public Builder(BuildSystem buildSystem, ICakeContext context, Action<string> runTarget)
    {
        BuildSystem = buildSystem;
        Context = context;
        _runTarget = runTarget;

        SetParameters(title: ""); // defaults
        SetVersion();
    }

    public void Info()
    {
        var secrets = typeof(Environment)
            .GetProperties()
            .Select(property => property.GetValue(Environment))
            .ToList();
        var variables = Context.EnvironmentVariables()
            .OrderBy(entry => entry.Key)
            .ToDictionary(entry => entry.Key, entry => secrets.Contains(entry.Key) ? entry.Value.Redact() : entry.Value); // redact secrets

        var provider = BuildSystem.Provider == BuildProvider.AzurePipelines || BuildSystem.Provider == BuildProvider.AzurePipelinesHosted
            ? "TFBuild" : BuildSystem.Provider.ToString(); // map AzurePipelines & AzurePipelinesHosted providers to TFBuild properties
        var properties = this.ToTokens()
            .Where(entry => (Parameters.LogBuildSystem && entry.Key.StartsWith($"BuildSystem.{provider}.")) ||
                (Parameters.LogContext && entry.Key.StartsWith("Context.")) ||
                (Parameters.DefaultLog && !entry.Key.StartsWith("BuildSystem.") && !entry.Key.StartsWith("Context.")))
            .ToDictionary(entry => entry.Key, entry => entry.Key.StartsWith("Credentials.") ? entry.Value?.ToString()?.Redact() : entry.Value); // redact secrets

        var padding = variables.Keys.Concat(properties.Keys).Select(key => key.Length).Max() + 4;

        Context.Information(Version.Summary);
        if (Parameters.LogEnvironment)
        {
            Context.Information("");
            foreach (var variable in variables)
            {
                Context.Information(string.Concat(variable.Key.PadRight(padding), variable.Value.ToTokenString()));
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
            Context.Information(string.Concat(property.Key.PadRight(padding), property.Value.ToTokenString()));
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

        bool? defaultLog = null,
        bool? logEnvironment = null,
        bool? logBuildSystem = null,
        bool? logContext = null,

        bool? defaultRun = null,
        bool? runBuild = null,
        bool? runBuildPublish = null,
        bool? runUnitTests = null,
        bool? runDockerBuild = null,
        bool? runIntegrationTests = null,
        bool? runNuGetPack = null,
        bool? runPublishToDocker = null,
        bool? runPublishToNuGet = null,

        string nuGetApiKeyVariable = null, // environment
        string nuGetSourceVariable = null,

        DirectoryPath rootDirectory = null, // directories
        DirectoryPath sourceDirectory = null,
        DirectoryPath artifactsDirectory = null,
        DirectoryPath artifactsNuGetDirectory = null,
        DirectoryPath artifactsTestsDirectory = null,

        string[] buildSolutionPatterns = null, // patterns
        string[] buildPublishProjectPatterns = null,
        string[] unitTestProjectPatterns = null,
        string[] integrationTestProjectPatterns = null,
        string[] nuGetProjectPatterns = null,

        bool? buildBinaryLoggerEnabled = null, // tool settings
        bool? buildEmbedAllSources = null,
        int? buildMaxCpuCount = null,
        bool? buildTreatWarningsAsErrors = null,
        string unitTestsLogger = null,
        string integrationTestsLogger = null,
        bool? dockerPushLatest = null,
        bool? nuGetPackSymbols = null,

        string containerRegistry = null, // container
        string containerRepository = null,
        string containerContext = null,
        string containerFile = null)
    {
        Parameters = new Parameters(
            this,

            title,
            target,
            configuration,

            defaultLog,
            logEnvironment,
            logBuildSystem,
            logContext,

            defaultRun,
            runBuild,
            runBuildPublish,
            runUnitTests,
            runDockerBuild,
            runIntegrationTests,
            runNuGetPack,
            runPublishToDocker,
            runPublishToNuGet);

        Environment = new Environment(
            nuGetApiKeyVariable,
            nuGetSourceVariable);

        Credentials = new Credentials(
            Context,
            Environment);

        Directories = new Directories(
            Context,
            rootDirectory,
            sourceDirectory,
            artifactsDirectory,
            artifactsNuGetDirectory,
            artifactsTestsDirectory);

        Files = new Files(
            Context,
            Directories);

        Patterns = new Patterns(
            buildSolutionPatterns,
            buildPublishProjectPatterns,
            unitTestProjectPatterns,
            integrationTestProjectPatterns,
            nuGetProjectPatterns);

        ToolSettings = new ToolSettings(
            buildBinaryLoggerEnabled,
            buildEmbedAllSources,
            buildMaxCpuCount,
            buildTreatWarningsAsErrors,
            unitTestsLogger,
            integrationTestsLogger,
            dockerPushLatest,
            nuGetPackSymbols);

        Container = new DockerContainer(
            containerRegistry,
            containerRepository,
            containerContext,
            containerFile);

        return this;
    }

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

    public DockerContainer Container { get; private set; }

    public Version Version { get; private set; }

    private readonly Action<string> _runTarget;
}
