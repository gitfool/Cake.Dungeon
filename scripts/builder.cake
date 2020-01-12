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
        var secrets = typeof(Environment)
            .GetProperties()
            .Select(property => property.GetValue(Environment))
            .ToList();
        var variables = Context.EnvironmentVariables()
            .OrderBy(entry => entry.Key)
            .ToDictionary(entry => entry.Key, entry => secrets.Contains(entry.Key) ? entry.Value.Redact() : entry.Value); // redact secrets

        var provider = (BuildSystem.Provider & (BuildProvider.AzurePipelines | BuildProvider.AzurePipelinesHosted)) != 0
            ? "TFBuild" : BuildSystem.Provider.ToString(); // map AzurePipelines* providers to TFBuild properties
        var properties = this.ToTokens()
            .Where(entry => (Parameters.LogBuildSystem && entry.Key.StartsWith($"Build.BuildSystem.{provider}.")) ||
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
        bool? runBuildPublish = null,
        bool? runUnitTests = null,
        bool? runDockerBuild = null,
        bool? runIntegrationTests = null,
        bool? runNuGetPack = null,
        bool? runPublishToDocker = null,
        bool? runPublishToNuGet = null,
        bool? runDockerDeploy = null,

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
        bool? buildRestoreLockedMode = null,
        bool? buildTreatWarningsAsErrors = null,
        string unitTestsLogger = null,
        string integrationTestsLogger = null,
        bool? dockerBuildPull = null,
        bool? dockerPushLatest = null,
        bool? nuGetPackSymbols = null,

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
            runBuildPublish,
            runUnitTests,
            runDockerBuild,
            runIntegrationTests,
            runNuGetPack,
            runPublishToDocker,
            runPublishToNuGet,
            runDockerDeploy);

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
            this,
            buildBinaryLoggerEnabled,
            buildEmbedAllSources,
            buildMaxCpuCount,
            buildRestoreLockedMode,
            buildTreatWarningsAsErrors,
            unitTestsLogger,
            integrationTestsLogger,
            dockerBuildPull,
            dockerPushLatest,
            nuGetPackSymbols);

        DockerImages = dockerImages;
        DockerDeployers = dockerDeployers;

        return this;
    }

    public string[] TransformTokens(IEnumerable<string> strings) =>
        strings?.Select(text => Context.TransformText(text, "{{", "}}").WithTokens(this.ToTokens()).ToString()).ToArray();

    public void TransformTokens(FilePath source, FilePath destination) =>
        Context.TransformTextFile(source, "{{", "}}")
            .WithTokens(this.ToTokens())
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
}
