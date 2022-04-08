#load bootstrap.cake

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
    CleanDirectories($"{Build.Directories.Source}/**/bin");
    CleanDirectories($"{Build.Directories.Source}/**/obj");

    var patterns = Build.Patterns.BuildSolutions.Select(pattern => $"{Build.Directories.Source}/{pattern}").ToArray();
    var solutions = GetFiles(patterns);
    if (!solutions.Any())
    {
        Warning("Build solutions not found");
        return;
    }

    var msbuildSettings = new DotNetMSBuildSettings
    {
        BinaryLogger = new MSBuildBinaryLoggerSettings { Enabled = Build.ToolSettings.BuildBinaryLoggerEnabled },
        ContinuousIntegrationBuild = !Build.Version.IsLocal,
        MaxCpuCount = Build.ToolSettings.BuildMaxCpuCount,
        TreatAllWarningsAs = Build.ToolSettings.BuildTreatWarningsAsErrors ? MSBuildTreatAllWarningsAs.Error : MSBuildTreatAllWarningsAs.Default,
        Version = Build.Version.AssemblyVersion,
        FileVersion = Build.Version.AssemblyFileVersion,
        InformationalVersion = Build.Version.InformationalVersion,
        PackageVersion = Build.Version.FullSemVer
    }
        .WithProperty("EmbedAllSources", Build.ToolSettings.BuildEmbedAllSources.ToString().ToLowerInvariant())
        .WithProperty("RestoreLockedMode", Build.ToolSettings.BuildRestoreLockedMode.ToString().ToLowerInvariant());
    var buildSettings = new DotNetBuildSettings
    {
        Configuration = Build.Parameters.Configuration,
        MSBuildSettings = msbuildSettings,
        NoLogo = Build.ToolSettings.DotNetNoLogo
    };
    foreach (var solution in solutions)
    {
        DotNetBuild(solution.FullPath, buildSettings);
    }
});

Tasks.DockerBuild = Task("DockerBuild")
    .IsDependentOn("BuildSolutions")
    .WithCriteria(() => Build.Parameters.RunDockerBuild, "Not run")
    .WithCriteria(() => Build.DockerImages != null && Build.DockerImages.All(image => image.IsConfigured), "Not configured")
    .DoesForEach(() => Build.DockerImages, image =>
{
    var settings = new DockerBuildXBuildSettings
    {
        File = image.File,
        BuildArg = image.Args,
        Load = Build.ToolSettings.DockerBuildLoad,
        Pull = Build.ToolSettings.DockerBuildPull,
        Tag = (image.Tags ?? Build.ToolSettings.DockerTagsDefault).Select(tag => $"{image.Repository}:{tag}").ToArray()
    };
    if (BuildSystem.IsRunningOnGitHubActions)
    {
        settings.CacheFrom = new[] { $"type=gha,scope={BuildSystem.GitHubActions.Environment.Workflow.Workflow}" };
        settings.CacheTo = new[] { $"type=gha,mode=max,scope={BuildSystem.GitHubActions.Environment.Workflow.Workflow}" };
    }
    DockerBuildXBuild(settings, image.Context);
});

Tasks.UnitTests = Task("UnitTests")
    .IsDependentOn("DockerBuild")
    .WithCriteria(() => Build.Parameters.RunUnitTests, "Not run")
    .Does(() =>
{
    var patterns = Build.Patterns.UnitTestProjects.Select(pattern => $"{Build.Directories.Source}/{pattern}").ToArray();
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

        var arguments = Build.ToolSettings.UnitTestRunSettings.ToProcessArguments();
        var settings = new DotNetTestSettings
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
        DotNetTest(project.FullPath, arguments, settings);
    }
});

Tasks.IntegrationTests = Task("IntegrationTests")
    .IsDependentOn("DockerBuild")
    .WithCriteria(() => Build.Parameters.RunIntegrationTests, "Not run")
    .Does(() =>
{
    var patterns = Build.Patterns.IntegrationTestProjects.Select(pattern => $"{Build.Directories.Source}/{pattern}").ToArray();
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

        var arguments = Build.ToolSettings.IntegrationTestRunSettings.ToProcessArguments();
        var settings = new DotNetTestSettings
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
        DotNetTest(project.FullPath, arguments, settings);
    }
});

Tasks.TestCoverageReports = Task("TestCoverageReports")
    .IsDependentOn("UnitTests")
    .IsDependentOn("IntegrationTests")
    .WithCriteria(() => Build.Parameters.RunTestCoverageReports, "Not run")
    .Does(() =>
{
    var artifactsTestsCoverageDirectory = Build.Directories.ArtifactsTests.Combine("Coverage");
    CleanDirectory(artifactsTestsCoverageDirectory);

    var patterns = Build.Patterns.TestCoverageReports.Select(pattern => $"{Build.Directories.ArtifactsTests}/{pattern}").ToArray();
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
        Verbosity = ReportGeneratorVerbosity.Info
    };
    ReportGenerator(reports, artifactsTestsCoverageDirectory, settings);

    var summary = FileReadText($"{artifactsTestsCoverageDirectory}/Summary.txt");
    Information("");
    Information(summary);
});

Tasks.NuGetPack = Task("NuGetPack")
    .IsDependentOn("BuildSolutions")
    .WithCriteria(() => Build.Parameters.RunNuGetPack, "Not run")
    .Does(() =>
{
    CleanDirectory(Build.Directories.ArtifactsNuGet);

    var patterns = Build.Patterns.NuGetProjects.Select(pattern => $"{Build.Directories.Source}/{pattern}").ToArray();
    var projects = GetFiles(patterns);
    if (!projects.Any())
    {
        Warning("NuGet projects not found");
        return;
    }

    var settings = new DotNetPackSettings
    {
        Configuration = Build.Parameters.Configuration,
        IncludeSymbols = Build.ToolSettings.NuGetPackSymbols,
        SymbolPackageFormat = Build.ToolSettings.NuGetPackSymbolsFormat,
        MSBuildSettings = new DotNetMSBuildSettings { PackageVersion = Build.Version.FullSemVer },
        NoLogo = Build.ToolSettings.DotNetNoLogo,
        NoBuild = true,
        NoRestore = true,
        OutputDirectory = Build.Directories.ArtifactsNuGet
    };
    foreach (var project in projects)
    {
        DotNetPack(project.FullPath, settings);
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
    var references = (image.Tags ?? Build.ToolSettings.DockerTagsDefault)
        .Where(tag => !Build.ToolSettings.DockerTagsLatest.Contains(tag) || Build.ToolSettings.DockerPushLatest)
        .Select(tag => image.ToReference(Context, tag, Build.ToolSettings.DockerTagsLatest.Contains(tag)))
        .ToArray();

    var tags = references
        .Where(reference =>
        {
            if (reference.Exists)
            {
                if (!Build.ToolSettings.DockerTagsLatest.Contains(reference.Tag) && !Build.ToolSettings.DockerPushSkipDuplicate)
                {
                    throw new InvalidOperationException($"Docker image {reference.Target} already exists");
                }
                if (!Build.ToolSettings.DockerTagsLatest.Contains(reference.Tag) || references.All(reference => reference.Exists))
                {
                    Information($"Skipping docker image {reference.Target} already exists");
                    return false;
                }
            }
            return true;
        })
        .Select(reference => reference.Target)
        .ToArray();

    if (!tags.Any())
    {
        Warning("Docker tags not found");
        return;
    }

    var settings = new DockerBuildXBuildSettings
    {
        File = image.File,
        BuildArg = image.Args,
        Push = true,
        Tag = tags
    };
    if (BuildSystem.IsRunningOnGitHubActions)
    {
        settings.CacheFrom = new[] { $"type=gha,scope={BuildSystem.GitHubActions.Environment.Workflow.Workflow}" };
    }
    DockerBuildXBuild(settings, image.Context);
});

Tasks.PublishToNuGet = Task("PublishToNuGet")
    .IsDependentOn("NuGetPack")
    .WithCriteria(() => Build.Parameters.RunPublishToNuGet, "Not run")
    .WithCriteria(() => Build.Credentials.NuGet.IsConfigured && Build.ToolSettings.NuGetSource.IsConfigured(), "Not configured")
    .WithCriteria(() => Build.Version.IsPublic, "Not public")
    .WithCriteria(() => Build.Parameters.Publish, "Not publisher")
    .Does(() =>
{
    var packages = GetFiles($"{Build.Directories.ArtifactsNuGet}/**/*.nupkg");
    if (!packages.Any())
    {
        Warning("NuGet packages not found");
        return;
    }

    if (Build.Credentials.NuGet.UserName.IsConfigured() && Build.Credentials.NuGet.Password.IsConfigured())
    {
        var sourceSettings = new DotNetNuGetSourceSettings
        {
            UserName = Build.Credentials.NuGet.UserName,
            Password = Build.Credentials.NuGet.Password,
            StorePasswordInClearText = true,
            Source = Build.ToolSettings.NuGetSource,
            ConfigFile = Build.ToolSettings.NuGetSourceConfigFile
        };
        if (!DotNetNuGetHasSource(Build.ToolSettings.NuGetSourceName, sourceSettings))
        {
            DotNetNuGetAddSource(Build.ToolSettings.NuGetSourceName, sourceSettings);
        }
        else
        {
            DotNetNuGetUpdateSource(Build.ToolSettings.NuGetSourceName, sourceSettings);
        }
    }

    var settings = new DotNetNuGetPushSettings
    {
        ApiKey = Build.Credentials.NuGet.ApiKey,
        Source = Build.ToolSettings.NuGetSource,
        SkipDuplicate = Build.ToolSettings.NuGetPushSkipDuplicate
    };
    foreach (var package in packages)
    {
        DotNetNuGetPush(package.FullPath, settings);
    }
});

Tasks.DeployArtifacts = Task("DeployArtifacts")
    .IsDependentOn("Info")
    .IsDependentOn("DockerDeploy");

Tasks.DockerDeploy = Task("DockerDeploy")
    .WithCriteria(() => Build.Parameters.RunDockerDeploy, "Not run")
    .WithCriteria(() => Build.DockerDeployers != null && Build.DockerDeployers.All(deployer => deployer.IsConfigured), "Not configured")
    .WithCriteria(() => Build.Version.IsPublic, "Not public")
    .WithCriteria(() => Build.Parameters.Deploy, "Not deployer")
    .DoesForEach(() => Build.DockerDeployers, deployer =>
{
    var image = deployer.Registry.IsConfigured() ? $"{deployer.Registry}/{deployer.Repository}:{deployer.Tag}" : $"{deployer.Repository}:{deployer.Tag}";
    DockerPull(image);

    var settings = new DockerContainerRunSettings
    {
        Env = deployer.Environment,
        Volume = deployer.Volumes,
        Tty = true
    };
    DockerRunWithoutResult(settings, image, deployer.Args?[0], deployer.Args?[1..]);
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
