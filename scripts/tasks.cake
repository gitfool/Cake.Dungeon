public static class Tasks
{
    public static CakeTaskBuilder Info { get; set; }
    public static CakeTaskBuilder BuildSolutions { get; set; }
    public static CakeTaskBuilder DockerBuild { get; set; }
    public static CakeTaskBuilder UnitTests { get; set; }
    public static CakeTaskBuilder IntegrationTests { get; set; }
    public static CakeTaskBuilder TestCoverageReports { get; set; }
    public static CakeTaskBuilder NuGetPack { get; set; }
    public static CakeTaskBuilder StageArtifacts { get; set; }
    public static CakeTaskBuilder PublishArtifacts { get; set; }
    public static CakeTaskBuilder PublishToDocker { get; set; }
    public static CakeTaskBuilder PublishToNuGet { get; set; }
    public static CakeTaskBuilder DeployArtifacts { get; set; }
    public static CakeTaskBuilder DockerDeploy { get; set; }
    public static CakeTaskBuilder Build { get; set; }
    public static CakeTaskBuilder Deploy { get; set; }
}

Tasks.Info = Task("Info")
    .Does(() =>
{
    Build.Info();
});

Tasks.BuildSolutions = Task("BuildSolutions")
    .IsDependentOn("Info")
    .WithCriteria(() => Build.Parameters.RunBuildSolutions, "Not run")
    .Does(() =>
{
    CleanDirectories(Build.Directories.Source.CombineWithFilePath("**/bin").FullPath);
    CleanDirectories(Build.Directories.Source.CombineWithFilePath("**/obj").FullPath);

    var solutionPatterns = Build.Patterns.BuildSolutions.Select(pattern => Build.Directories.Source.CombineWithFilePath(pattern).FullPath).ToArray();
    var solutions = GetFiles(solutionPatterns);
    if (!solutions.Any())
    {
        Warning("Build solutions not found");
        return;
    }

    var msbuildSettings = new DotNetCoreMSBuildSettings
    {
        BinaryLogger = new MSBuildBinaryLoggerSettings { Enabled = Build.ToolSettings.BuildBinaryLoggerEnabled },
        MaxCpuCount = Build.ToolSettings.BuildMaxCpuCount,
        TreatAllWarningsAs = Build.ToolSettings.BuildTreatWarningsAsErrors ? MSBuildTreatAllWarningsAs.Error : MSBuildTreatAllWarningsAs.Default
    }
        .WithProperty("ContinuousIntegrationBuild", (!Build.Version.IsLocal).ToString().ToLower())
        .WithProperty("EmbedAllSources", Build.ToolSettings.BuildEmbedAllSources.ToString().ToLower())
        .WithProperty("RestoreLockedMode", Build.ToolSettings.BuildRestoreLockedMode.ToString().ToLower())
        .WithProperty("Version", Build.Version.AssemblyVersion)
        .WithProperty("FileVersion", Build.Version.AssemblyFileVersion)
        .WithProperty("InformationalVersion", Build.Version.InformationalVersion)
        .WithProperty("PackageVersion", Build.Version.FullSemVer);
    var buildSettings = new DotNetCoreBuildSettings
    {
        Configuration = Build.Parameters.Configuration,
        MSBuildSettings = msbuildSettings,
        NoLogo = Build.ToolSettings.DotNetNoLogo
    };
    foreach (var solution in solutions)
    {
        DotNetCoreBuild(solution.FullPath, buildSettings);
    }

    if (!Build.Parameters.RunBuildPublish)
    {
        return;
    }
    Information("...");

    var projectPatterns = Build.Patterns.BuildPublishProjects.Select(pattern => Build.Directories.Source.CombineWithFilePath(pattern).FullPath).ToArray();
    var projects = GetFiles(projectPatterns);
    if (!projects.Any())
    {
        Warning("Build publish projects not found");
        return;
    }

    var publishSettings = new DotNetCorePublishSettings
    {
        Configuration = Build.Parameters.Configuration,
        MSBuildSettings = msbuildSettings,
        NoLogo = Build.ToolSettings.DotNetNoLogo,
        NoBuild = true,
        NoRestore = true
    };
    foreach (var project in projects)
    {
        DotNetCorePublish(project.FullPath, publishSettings);
    }
});

Tasks.DockerBuild = Task("DockerBuild")
    .IsDependentOn("BuildSolutions")
    .WithCriteria(() => Build.Parameters.RunDockerBuild, "Not run")
    .WithCriteria(() => Build.DockerImages != null && Build.DockerImages.All(image => image.IsConfigured), "Not configured")
    .DoesForEach(() => Build.DockerImages, image =>
{
    var settings = new DockerImageBuildSettings
    {
        File = image.File,
        BuildArg = Build.TransformTokens(image.Args),
        Pull = Build.ToolSettings.DockerBuildPull,
        Tag = Build.TransformTokens(image.Tags.Select(tag => $"{image.Repository}:{tag}"))
    };
    DockerBuild(settings, image.Context);
});

Tasks.UnitTests = Task("UnitTests")
    .IsDependentOn("DockerBuild")
    .WithCriteria(() => Build.Parameters.RunUnitTests, "Not run")
    .Does(() =>
{
    var patterns = Build.Patterns.UnitTestProjects.Select(pattern => Build.Directories.Source.CombineWithFilePath(pattern).FullPath).ToArray();
    var projects = GetFiles(patterns);
    if (!projects.Any())
    {
        Warning("Unit test projects not found");
        return;
    }

    foreach (var project in projects)
    {
        var artifactsTestsProjectDirectory = Build.Directories.ArtifactsTests.Combine(Build.Directories.Source.GetRelativePath(project.GetDirectory()));
        CleanDirectory(artifactsTestsProjectDirectory);

        var arguments = Build.ToolSettings.UnitTestRunSettings?.ToProcessArguments();
        var settings = new DotNetCoreTestSettings
        {
            Configuration = Build.Parameters.Configuration,
            EnvironmentVariables = Build.ToEnvVars(),
            Collectors = Build.ToolSettings.UnitTestCollectors,
            Loggers = Build.ToolSettings.UnitTestLoggers,
            NoLogo = Build.ToolSettings.DotNetNoLogo,
            NoBuild = true,
            NoRestore = true,
            ResultsDirectory = artifactsTestsProjectDirectory,
            Settings = Build.ToolSettings.UnitTestRunSettingsFile
        };
        DotNetCoreTest(project.FullPath, arguments, settings);
    }
});

Tasks.IntegrationTests = Task("IntegrationTests")
    .IsDependentOn("DockerBuild")
    .WithCriteria(() => Build.Parameters.RunIntegrationTests, "Not run")
    .Does(() =>
{
    var patterns = Build.Patterns.IntegrationTestProjects.Select(pattern => Build.Directories.Source.CombineWithFilePath(pattern).FullPath).ToArray();
    var projects = GetFiles(patterns);
    if (!projects.Any())
    {
        Warning("Integration test projects not found");
        return;
    }

    foreach (var project in projects)
    {
        var artifactsTestsProjectDirectory = Build.Directories.ArtifactsTests.Combine(Build.Directories.Source.GetRelativePath(project.GetDirectory()));
        CleanDirectory(artifactsTestsProjectDirectory);

        var arguments = Build.ToolSettings.IntegrationTestRunSettings?.ToProcessArguments();
        var settings = new DotNetCoreTestSettings
        {
            Configuration = Build.Parameters.Configuration,
            EnvironmentVariables = Build.ToEnvVars(),
            Collectors = Build.ToolSettings.IntegrationTestCollectors,
            Loggers = Build.ToolSettings.IntegrationTestLoggers,
            NoLogo = Build.ToolSettings.DotNetNoLogo,
            NoBuild = true,
            NoRestore = true,
            ResultsDirectory = artifactsTestsProjectDirectory,
            Settings = Build.ToolSettings.IntegrationTestRunSettingsFile
        };
        DotNetCoreTest(project.FullPath, arguments, settings);
    }
});

Tasks.TestCoverageReports = Task("TestCoverageReports")
    .IsDependentOn("UnitTests")
    .IsDependentOn("IntegrationTests")
    .WithCriteria(() => Build.Parameters.RunTestCoverageReports, "Not run")
    .Does(() =>
{
    DeleteFiles(Build.Directories.ArtifactsTests.CombineWithFilePath("*.*").FullPath);

    var patterns = Build.Patterns.TestCoverageReports.Select(pattern => Build.Directories.ArtifactsTests.CombineWithFilePath(pattern).FullPath).ToArray();
    var reports = GetFiles(patterns);
    if (!reports.Any())
    {
        Warning("Test coverage reports not found");
        return;
    }

    var settings = new ReportGeneratorSettings
    {
        AssemblyFilters = Build.ToolSettings.TestCoverageReportAssemblyFilters,
        ClassFilters = Build.ToolSettings.TestCoverageReportClassFilters,
        ReportTypes = Build.ToolSettings.TestCoverageReportTypes.Select(Enum.Parse<ReportGeneratorReportType>).ToArray(),
        ToolPath = Context.Tools.Resolve("reportgenerator"), // workaround tool resolution
        Verbosity = ReportGeneratorVerbosity.Info
    };
    ReportGenerator(reports, Build.Directories.ArtifactsTests, settings);

    var summary = FileReadText($"{Build.Directories.ArtifactsTests}/Summary.txt");
    Information("");
    Information(summary);
});

Tasks.NuGetPack = Task("NuGetPack")
    .IsDependentOn("BuildSolutions")
    .WithCriteria(() => Build.Parameters.RunNuGetPack, "Not run")
    .Does(() =>
{
    CleanDirectory(Build.Directories.ArtifactsNuGet);

    var patterns = Build.Patterns.NuGetProjects.Select(pattern => Build.Directories.Source.CombineWithFilePath(pattern).FullPath).ToArray();
    var projects = GetFiles(patterns);
    if (!projects.Any())
    {
        Warning("NuGet projects not found");
        return;
    }

    var msbuildSettings = new DotNetCoreMSBuildSettings()
        .WithProperty("PackageVersion", Build.Version.FullSemVer);
    var settings = new DotNetCorePackSettings
    {
        Configuration = Build.Parameters.Configuration,
        IncludeSymbols = Build.ToolSettings.NuGetPackSymbols,
        MSBuildSettings = msbuildSettings,
        NoLogo = Build.ToolSettings.DotNetNoLogo,
        NoBuild = true,
        NoRestore = true,
        OutputDirectory = Build.Directories.ArtifactsNuGet
    };
    foreach (var project in projects)
    {
        DotNetCorePack(project.FullPath, settings);
    }
});

Tasks.StageArtifacts = Task("StageArtifacts")
    .IsDependentOn("DockerBuild")
    .IsDependentOn("NuGetPack");

Tasks.PublishArtifacts = Task("PublishArtifacts")
    .IsDependentOn("PublishToDocker")
    .IsDependentOn("PublishToNuGet");

Tasks.PublishToDocker = Task("PublishToDocker")
    .IsDependentOn("DockerBuild")
    .WithCriteria(() => Build.Parameters.RunPublishToDocker, "Not run")
    .WithCriteria(() => Build.DockerImages != null && Build.DockerImages.All(image => image.IsConfigured), "Not configured")
    .WithCriteria(() => Build.Version.IsPublic, "Not public")
    .WithCriteria(() => Build.Parameters.Publish, "Not publisher")
    .DoesForEach(() => Build.DockerImages, image =>
{
    var references = Build.TransformTokens(image.Tags)
        .Where(tag => tag != "latest" || Build.ToolSettings.DockerPushLatest)
        .Select(tag => image.ToReference(Context, tag))
        .ToArray();

    foreach (var reference in references)
    {
        if (reference.Exists)
        {
            if (reference.Tag != "latest" && !Build.ToolSettings.DockerPushSkipDuplicate)
            {
                throw new InvalidOperationException($"Docker image {reference.Target} already exists");
            }
            if (reference.Tag != "latest" || references.All(reference => reference.Exists))
            {
                Information($"Skipping docker image {reference.Target} already exists");
                continue;
            }
        }
        if (reference.Source != reference.Target)
        {
            DockerTag(reference.Source, reference.Target);
        }
        DockerPush(reference.Target);
    }
});

Tasks.PublishToNuGet = Task("PublishToNuGet")
    .IsDependentOn("NuGetPack")
    .WithCriteria(() => Build.Parameters.RunPublishToNuGet, "Not run")
    .WithCriteria(() => Build.Credentials.NuGet.IsConfigured && Build.ToolSettings.NuGetSource.IsConfigured(), "Not configured")
    .WithCriteria(() => Build.Version.IsPublic, "Not public")
    .WithCriteria(() => Build.Parameters.Publish, "Not publisher")
    .Does(() =>
{
    var packages = GetFiles(Build.Directories.ArtifactsNuGet.CombineWithFilePath("**/*.nupkg").FullPath);
    if (!packages.Any())
    {
        Warning("NuGet packages not found");
        return;
    }

    if (Build.Credentials.NuGet.UserName.IsConfigured() && Build.Credentials.NuGet.Password.IsConfigured())
    {
        var sourceSettings = new DotNetCoreNuGetSourceSettings
        {
            UserName = Build.Credentials.NuGet.UserName,
            Password = Build.Credentials.NuGet.Password,
            StorePasswordInClearText = true,
            Source = Build.ToolSettings.NuGetSource,
            ConfigFile = Build.ToolSettings.NuGetSourceConfigFile
        };
        if (!DotNetCoreNuGetHasSource(Build.ToolSettings.NuGetSourceName, sourceSettings))
        {
            DotNetCoreNuGetAddSource(Build.ToolSettings.NuGetSourceName, sourceSettings);
        }
        else
        {
            DotNetCoreNuGetUpdateSource(Build.ToolSettings.NuGetSourceName, sourceSettings);
        }
    }

    var settings = new DotNetCoreNuGetPushSettings
    {
        ApiKey = Build.Credentials.NuGet.ApiKey,
        Source = Build.ToolSettings.NuGetSource,
        SkipDuplicate = Build.ToolSettings.NuGetPushSkipDuplicate
    };
    foreach (var package in packages)
    {
        DotNetCoreNuGetPush(package.FullPath, settings);
    }
});

Tasks.DeployArtifacts = Task("DeployArtifacts")
    .IsDependentOn("Info")
    .IsDependentOn("DockerDeploy");

Tasks.DockerDeploy = Task("DockerDeploy")
    .WithCriteria(() => Build.Parameters.RunDockerDeploy, "Not run")
    .WithCriteria(() => Build.Parameters.Title.IsConfigured() && Build.DockerDeployers != null && Build.DockerDeployers.All(deployer => deployer.IsConfigured), "Not configured")
    .WithCriteria(() => Build.Version.IsPublic, "Not public")
    .WithCriteria(() => Build.Parameters.Deploy, "Not deployer")
    .DoesForEach(() => Build.DockerDeployers, deployer =>
{
    var image = deployer.Registry.IsConfigured() ? $"{deployer.Registry}/{deployer.Repository}:{deployer.Tag}" : $"{deployer.Repository}:{deployer.Tag}";
    DockerPull(image);

    var settings = new DockerContainerRunSettings
    {
        Env = Build.TransformTokens(deployer.Environment),
        Volume = Build.TransformTokens(deployer.Volumes),
        Tty = true
    };
    var args = Build.TransformTokens(deployer.Args);
    DockerRunWithoutResult(settings, image, args?[0], args?[1..]);
});

Tasks.Build = Task("Build")
    .IsDependentOn("Info")
    .IsDependentOn("BuildSolutions")
    .IsDependentOn("DockerBuild")
    .IsDependentOn("UnitTests")
    .IsDependentOn("IntegrationTests")
    .IsDependentOn("TestCoverageReports")
    .IsDependentOn("StageArtifacts")
    .IsDependentOn("PublishArtifacts");

Tasks.Deploy = Task("Deploy")
    .IsDependentOn("Info")
    .IsDependentOn("DeployArtifacts");
