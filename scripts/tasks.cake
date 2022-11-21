#load bootstrap.cake

public static class Tasks
{
    public static CakeTaskBuilder<Build> Info { get; set; }
    public static CakeTaskBuilder<Build> BuildSolutions { get; set; }
    public static CakeTaskBuilder<Build> DockerBuild { get; set; }
    public static CakeTaskBuilder<Build> UnitTests { get; set; }
    public static CakeTaskBuilder<Build> IntegrationTests { get; set; }
    public static CakeTaskBuilder<Build> TestCoverageReports { get; set; }
    public static CakeTaskBuilder<Build> NuGetPack { get; set; }
    public static CakeTaskBuilder<Build> StageArtifacts { get; set; }
    public static CakeTaskBuilder<Build> PublishArtifacts { get; set; }
    public static CakeTaskBuilder<Build> PublishToDocker { get; set; }
    public static CakeTaskBuilder<Build> PublishToNuGet { get; set; }
    public static CakeTaskBuilder<Build> DeployArtifacts { get; set; }
    public static CakeTaskBuilder<Build> DockerDeploy { get; set; }
    public static CakeTaskBuilder<Build> Build { get; set; }
    public static CakeTaskBuilder<Build> Deploy { get; set; }
}

Tasks.Info = TaskOf<Build>("Info")
    .Does((context, build) =>
{
    build.Info();
});

Tasks.BuildSolutions = TaskOf<Build>("BuildSolutions")
    .IsDependentOn("Info")
    .WithCriteria((context, build) => build.Parameters.RunBuildSolutions, "Not run")
    .Does((context, build) =>
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

Tasks.DockerBuild = TaskOf<Build>("DockerBuild")
    .IsDependentOn("BuildSolutions")
    .WithCriteria((context, build) => build.Parameters.RunDockerBuild, "Not run")
    .WithCriteria((context, build) => build.DockerImages != null && build.DockerImages.All(image => image.IsConfigured), "Not configured")
    .DoesForEach((build, context) => build.DockerImages, (build, image, context) =>
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

Tasks.UnitTests = TaskOf<Build>("UnitTests")
    .IsDependentOn("DockerBuild")
    .WithCriteria((context, build) => build.Parameters.RunUnitTests, "Not run")
    .Does((context, build) =>
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

Tasks.IntegrationTests = TaskOf<Build>("IntegrationTests")
    .IsDependentOn("DockerBuild")
    .WithCriteria((context, build) => build.Parameters.RunIntegrationTests, "Not run")
    .Does((context, build) =>
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

Tasks.TestCoverageReports = TaskOf<Build>("TestCoverageReports")
    .IsDependentOn("UnitTests")
    .IsDependentOn("IntegrationTests")
    .WithCriteria((context, build) => build.Parameters.RunTestCoverageReports, "Not run")
    .Does((context, build) =>
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

Tasks.NuGetPack = TaskOf<Build>("NuGetPack")
    .IsDependentOn("BuildSolutions")
    .WithCriteria((context, build) => build.Parameters.RunNuGetPack, "Not run")
    .Does((context, build) =>
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

Tasks.StageArtifacts = TaskOf<Build>("StageArtifacts")
    .IsDependentOn("DockerBuild")
    .IsDependentOn("NuGetPack");

Tasks.PublishArtifacts = TaskOf<Build>("PublishArtifacts")
    .IsDependentOn("PublishToDocker")
    .IsDependentOn("PublishToNuGet");

Tasks.PublishToDocker = TaskOf<Build>("PublishToDocker")
    .IsDependentOn("DockerBuild")
    .WithCriteria((context, build) => build.Parameters.RunPublishToDocker, "Not run")
    .WithCriteria((context, build) => build.DockerImages != null && build.DockerImages.All(image => image.IsConfigured), "Not configured")
    .WithCriteria((context, build) => build.Version.IsPublic, "Not public")
    .WithCriteria((context, build) => build.Parameters.Publish, "Not publisher")
    .DoesForEach((build, context) => build.DockerImages, (build, image, context) =>
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

Tasks.PublishToNuGet = TaskOf<Build>("PublishToNuGet")
    .IsDependentOn("NuGetPack")
    .WithCriteria((context, build) => build.Parameters.RunPublishToNuGet, "Not run")
    .WithCriteria((context, build) => build.Credentials.NuGet.IsConfigured && build.ToolSettings.NuGetSource.IsConfigured(), "Not configured")
    .WithCriteria((context, build) => build.Version.IsPublic, "Not public")
    .WithCriteria((context, build) => build.Parameters.Publish, "Not publisher")
    .Does((context, build) =>
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

Tasks.DeployArtifacts = TaskOf<Build>("DeployArtifacts")
    .IsDependentOn("Info")
    .IsDependentOn("DockerDeploy");

Tasks.DockerDeploy = TaskOf<Build>("DockerDeploy")
    .WithCriteria((context, build) => build.Parameters.RunDockerDeploy, "Not run")
    .WithCriteria((context, build) => build.DockerDeployers != null && build.DockerDeployers.All(deployer => deployer.IsConfigured), "Not configured")
    .WithCriteria((context, build) => build.Version.IsPublic, "Not public")
    .WithCriteria((context, build) => build.Parameters.Deploy, "Not deployer")
    .DoesForEach((build, context) => build.DockerDeployers, (build, deployer, context) =>
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

Tasks.Build = TaskOf<Build>("Build")
    .IsDependentOn("Info")
    .IsDependentOn("BuildSolutions")
    .IsDependentOn("DockerBuild")
    .IsDependentOn("UnitTests")
    .IsDependentOn("IntegrationTests")
    .IsDependentOn("TestCoverageReports")
    .IsDependentOn("StageArtifacts")
    .IsDependentOn("PublishArtifacts");

Tasks.Deploy = TaskOf<Build>("Deploy")
    .IsDependentOn("Info")
    .IsDependentOn("DeployArtifacts");
