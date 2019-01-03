public class Builder
{
    public Builder(BuildSystem buildSystem, ICakeContext context, Action<string> runTarget)
    {
        BuildSystem = buildSystem;
        Context = context;

        _buildSystemProvider = typeof(BuildSystem)
            .GetProperties()
            .Where(property => property.Name.StartsWith("IsRunningOn") && property.GetValue(buildSystem) is bool value && value)
            .Select(property => property.Name.Substring(11))
            .SingleOrDefault();
        _runTarget = runTarget;

        SetParameters(title: ""); // defaults
        SetVersion();
    }

    public void Info()
    {
        if (!Parameters.DefaultLog)
        {
            Context.Information(Version.Summary);
            return;
        }

        var tokens = this.ToTokens()
            .Where(x => (!x.Key.StartsWith("BuildSystem.") || (Parameters.LogBuildSystem && x.Key.StartsWith($"BuildSystem.{_buildSystemProvider}."))) &&
                (!x.Key.StartsWith("Context.") || Parameters.LogContext) &&
                !x.Key.StartsWith("Credentials.")) // always filter credentials
            .ToDictionary(x => x.Key, x => x.Value);

        var padding = tokens.Select(x => x.Key.Length).Max() + 4;
        var groups = new HashSet<string>();
        foreach (var token in tokens)
        {
            var group = token.Key.Split('.').First();
            if (!groups.Contains(group))
            {
                Context.Information("");
                groups.Add(group);
            }
            Context.Information(string.Concat(token.Key.PadRight(padding), token.Value.ToTokenString()));
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

    private readonly string _buildSystemProvider;
    private readonly Action<string> _runTarget;
}
