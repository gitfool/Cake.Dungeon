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
    .Does<Build>(build =>
{
    build.Info();
});

Tasks.BuildSolutions = Task("BuildSolutions")
    .IsDependentOn("Info")
    .WithCriteria<Build>(build => build.Parameters.RunBuildSolutions, "Not run")
    .Does<Build>(build =>
{
    CleanDirectories($"{build.Directories.Source}/**/bin");
    CleanDirectories($"{build.Directories.Source}/**/obj");

    var patterns = build.Patterns.BuildSolutions.Select(pattern => $"{build.Directories.Source}/{pattern}");
    var solutions = GetFiles(patterns);
    if (!solutions.Any())
    {
        Warning("Build solutions not found");
        return;
    }

    var msbuildSettings = new DotNetMSBuildSettings
    {
        BinaryLogger = new MSBuildBinaryLoggerSettings { Enabled = build.ToolSettings.BuildBinaryLoggerEnabled },
        ContinuousIntegrationBuild = !build.Version.IsLocal,
        MaxCpuCount = build.ToolSettings.BuildMaxCpuCount,
        TreatAllWarningsAs = build.ToolSettings.BuildTreatWarningsAsErrors ? MSBuildTreatAllWarningsAs.Error : MSBuildTreatAllWarningsAs.Default,
        Version = build.Version.AssemblyVersion,
        FileVersion = build.Version.AssemblyFileVersion,
        InformationalVersion = build.Version.InformationalVersion,
        PackageVersion = build.Version.SemVer
    }
        .WithProperty("EmbedAllSources", build.ToolSettings.BuildEmbedAllSources.ToValueString())
        .WithProperty("RestoreLockedMode", build.ToolSettings.BuildRestoreLockedMode.ToValueString());
    var buildSettings = new DotNetBuildSettings
    {
        Configuration = build.Parameters.Configuration,
        MSBuildSettings = msbuildSettings,
        NoLogo = build.ToolSettings.DotNetNoLogo
    };
    foreach (var solution in solutions)
    {
        DotNetBuild(solution.FullPath, buildSettings);
    }
});

Tasks.DockerBuild = Task("DockerBuild")
    .IsDependentOn("BuildSolutions")
    .WithCriteria<Build>(build => build.Parameters.RunDockerBuild, "Not run")
    .WithCriteria<Build>(build => build.DockerImages != null && build.DockerImages.All(image => image.IsConfigured), "Not configured")
    .DoesForEach<Build, DockerImage>(build => build.DockerImages, (build, image) =>
{
    var settings = new DockerBuildXBuildSettings
    {
        File = image.File,
        Target = image.Target,
        BuildArg = image.Args,
        Load = build.ToolSettings.DockerBuildLoad,
        Pull = build.ToolSettings.DockerBuildPull,
        Tag = (image.Tags ?? build.ToolSettings.DockerTagsDefault).Select(tag => $"{image.Repository}:{tag}").ToArray()
    };
    if (BuildSystem.IsRunningOnGitHubActions)
    {
        settings.Platform = image.Platforms;
        if (build.ToolSettings.DockerBuildCache)
        {
            settings.CacheFrom = new[] { $"type=gha,scope={BuildSystem.GitHubActions.Environment.Workflow.Workflow}" };
            settings.CacheTo = new[] { $"type=gha,mode=max,scope={BuildSystem.GitHubActions.Environment.Workflow.Workflow}" };
        }
    }
    DockerBuildXBuild(settings, image.Context);
});

Tasks.UnitTests = Task("UnitTests")
    .IsDependentOn("DockerBuild")
    .WithCriteria<Build>(build => build.Parameters.RunUnitTests, "Not run")
    .Does<Build>(build =>
{
    var patterns = build.Patterns.UnitTestProjects.Select(pattern => $"{build.Directories.Source}/{pattern}");
    var projects = GetFiles(patterns);
    if (!projects.Any())
    {
        Warning("Unit test projects not found");
        return;
    }

    foreach (var project in projects)
    {
        var artifactsTestsProjectDirectory = build.Directories.ArtifactsTests.Combine(build.Directories.Source.GetRelativePath(project.GetDirectory()));
        CleanDirectory(artifactsTestsProjectDirectory);

        var arguments = build.ToolSettings.UnitTestRunSettings.ToProcessArguments();
        var settings = new DotNetTestSettings
        {
            Configuration = build.Parameters.Configuration,
            EnvironmentVariables = build.ToEnvVars(),
            Collectors = build.ToolSettings.UnitTestCollectors,
            Loggers = build.ToolSettings.UnitTestLoggers,
            NoLogo = build.ToolSettings.DotNetNoLogo,
            NoBuild = true,
            NoRestore = true,
            ResultsDirectory = artifactsTestsProjectDirectory,
            Settings = build.ToolSettings.UnitTestRunSettingsFile
        };
        DotNetTest(project.FullPath, arguments, settings);
    }
});

Tasks.IntegrationTests = Task("IntegrationTests")
    .IsDependentOn("DockerBuild")
    .WithCriteria<Build>(build => build.Parameters.RunIntegrationTests, "Not run")
    .Does<Build>(build =>
{
    var patterns = build.Patterns.IntegrationTestProjects.Select(pattern => $"{build.Directories.Source}/{pattern}");
    var projects = GetFiles(patterns);
    if (!projects.Any())
    {
        Warning("Integration test projects not found");
        return;
    }

    foreach (var project in projects)
    {
        var artifactsTestsProjectDirectory = build.Directories.ArtifactsTests.Combine(build.Directories.Source.GetRelativePath(project.GetDirectory()));
        CleanDirectory(artifactsTestsProjectDirectory);

        var arguments = build.ToolSettings.IntegrationTestRunSettings.ToProcessArguments();
        var settings = new DotNetTestSettings
        {
            Configuration = build.Parameters.Configuration,
            EnvironmentVariables = build.ToEnvVars(),
            Collectors = build.ToolSettings.IntegrationTestCollectors,
            Loggers = build.ToolSettings.IntegrationTestLoggers,
            NoLogo = build.ToolSettings.DotNetNoLogo,
            NoBuild = true,
            NoRestore = true,
            ResultsDirectory = artifactsTestsProjectDirectory,
            Settings = build.ToolSettings.IntegrationTestRunSettingsFile
        };
        DotNetTest(project.FullPath, arguments, settings);
    }
});

Tasks.TestCoverageReports = Task("TestCoverageReports")
    .IsDependentOn("UnitTests")
    .IsDependentOn("IntegrationTests")
    .WithCriteria<Build>(build => build.Parameters.RunTestCoverageReports, "Not run")
    .Does<Build>(build =>
{
    var artifactsTestsCoverageDirectory = build.Directories.ArtifactsTests.Combine("Coverage");
    CleanDirectory(artifactsTestsCoverageDirectory);

    var patterns = build.Patterns.TestCoverageReports.Select(pattern => $"{build.Directories.ArtifactsTests}/{pattern}");
    var reports = GetFiles(patterns);
    if (!reports.Any())
    {
        Warning("Test coverage reports not found");
        return;
    }

    var settings = new ReportGeneratorSettings
    {
        AssemblyFilters = build.ToolSettings.TestCoverageReportAssemblyFilters,
        ClassFilters = build.ToolSettings.TestCoverageReportClassFilters,
        ReportTypes = build.ToolSettings.TestCoverageReportTypes.Select(Enum.Parse<ReportGeneratorReportType>).ToArray(),
        Verbosity = ReportGeneratorVerbosity.Info
    };
    ReportGenerator(reports, artifactsTestsCoverageDirectory, settings);

    var summary = FileReadText($"{artifactsTestsCoverageDirectory}/Summary.txt");
    Information("");
    Information(summary);
});

Tasks.NuGetPack = Task("NuGetPack")
    .IsDependentOn("BuildSolutions")
    .WithCriteria<Build>(build => build.Parameters.RunNuGetPack, "Not run")
    .Does<Build>(build =>
{
    CleanDirectory(build.Directories.ArtifactsNuGet);

    var patterns = build.Patterns.NuGetProjects.Select(pattern => $"{build.Directories.Source}/{pattern}");
    var projects = GetFiles(patterns);
    if (!projects.Any())
    {
        Warning("NuGet projects not found");
        return;
    }

    var settings = new DotNetPackSettings
    {
        Configuration = build.Parameters.Configuration,
        IncludeSymbols = build.ToolSettings.NuGetPackSymbols,
        SymbolPackageFormat = build.ToolSettings.NuGetPackSymbolsFormat,
        MSBuildSettings = new DotNetMSBuildSettings { PackageVersion = build.Version.SemVer },
        NoLogo = build.ToolSettings.DotNetNoLogo,
        NoBuild = true,
        NoRestore = true,
        OutputDirectory = build.Directories.ArtifactsNuGet
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
    .WithCriteria<Build>(build => build.Parameters.RunPublishToDocker, "Not run")
    .WithCriteria<Build>(build => build.DockerImages != null && build.DockerImages.All(image => image.IsConfigured), "Not configured")
    .WithCriteria<Build>(build => build.Version.IsPublic, "Not public")
    .WithCriteria<Build>(build => build.Parameters.Publish, "Not publisher")
    .DoesForEach<Build, DockerImage>(build => build.DockerImages, (build, image) =>
{
    var references = (image.Tags ?? build.ToolSettings.DockerTagsDefault)
        .Where(tag => !build.ToolSettings.DockerTagsLatest.Contains(tag) || build.ToolSettings.DockerPushLatest)
        .Select(tag => image.ToReference(Context, tag, build.ToolSettings.DockerTagsLatest.Contains(tag)))
        .ToArray();

    var tags = references
        .Where(reference =>
        {
            if (reference.Exists)
            {
                if (!build.ToolSettings.DockerTagsLatest.Contains(reference.Tag) && !build.ToolSettings.DockerPushSkipDuplicate)
                {
                    throw new InvalidOperationException($"Docker image {reference.Target} already exists");
                }
                if (!build.ToolSettings.DockerTagsLatest.Contains(reference.Tag) || references.All(reference => reference.Exists))
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
        Target = image.Target,
        BuildArg = image.Args,
        Push = true,
        Tag = tags
    };
    if (BuildSystem.IsRunningOnGitHubActions)
    {
        settings.Platform = image.Platforms;
        if (build.ToolSettings.DockerBuildCache)
        {
            settings.CacheFrom = new[] { $"type=gha,scope={BuildSystem.GitHubActions.Environment.Workflow.Workflow}" };
        }
    }
    DockerBuildXBuild(settings, image.Context);
});

Tasks.PublishToNuGet = Task("PublishToNuGet")
    .IsDependentOn("NuGetPack")
    .WithCriteria<Build>(build => build.Parameters.RunPublishToNuGet, "Not run")
    .WithCriteria<Build>(build => build.Credentials.NuGet.IsConfigured && build.ToolSettings.NuGetSource.IsConfigured(), "Not configured")
    .WithCriteria<Build>(build => build.Version.IsPublic, "Not public")
    .WithCriteria<Build>(build => build.Parameters.Publish, "Not publisher")
    .Does<Build>(build =>
{
    var packages = GetFiles($"{build.Directories.ArtifactsNuGet}/**/*.nupkg");
    if (!packages.Any())
    {
        Warning("NuGet packages not found");
        return;
    }

    if (build.Credentials.NuGet.UserName.IsConfigured() && build.Credentials.NuGet.Password.IsConfigured())
    {
        var sourceSettings = new DotNetNuGetSourceSettings
        {
            UserName = build.Credentials.NuGet.UserName,
            Password = build.Credentials.NuGet.Password,
            StorePasswordInClearText = true,
            Source = build.ToolSettings.NuGetSource,
            ConfigFile = build.ToolSettings.NuGetSourceConfigFile
        };
        if (!DotNetNuGetHasSource(build.ToolSettings.NuGetSourceName, sourceSettings))
        {
            DotNetNuGetAddSource(build.ToolSettings.NuGetSourceName, sourceSettings);
        }
        else
        {
            DotNetNuGetUpdateSource(build.ToolSettings.NuGetSourceName, sourceSettings);
        }
    }

    var settings = new DotNetNuGetPushSettings
    {
        ApiKey = build.Credentials.NuGet.ApiKey,
        Source = build.ToolSettings.NuGetSource,
        SkipDuplicate = build.ToolSettings.NuGetPushSkipDuplicate
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
    .WithCriteria<Build>(build => build.Parameters.RunDockerDeploy, "Not run")
    .WithCriteria<Build>(build => build.DockerDeployers != null && build.DockerDeployers.All(deployer => deployer.IsConfigured), "Not configured")
    .WithCriteria<Build>(build => build.Version.IsPublic, "Not public")
    .WithCriteria<Build>(build => build.Parameters.Deploy, "Not deployer")
    .DoesForEach<Build, DockerDeployer>(build => build.DockerDeployers, (build, deployer) =>
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
