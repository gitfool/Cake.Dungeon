public static class Tasks
{
    public static CakeTaskBuilder Info { get; set; }
    public static CakeTaskBuilder Build { get; set; }
    public static CakeTaskBuilder UnitTests { get; set; }
    public static CakeTaskBuilder DockerBuild { get; set; }
    public static CakeTaskBuilder IntegrationTests { get; set; }
    public static CakeTaskBuilder StageArtifacts { get; set; }
    public static CakeTaskBuilder NuGetPack { get; set; }
    public static CakeTaskBuilder PublishArtifacts { get; set; }
    public static CakeTaskBuilder PublishToDocker { get; set; }
    public static CakeTaskBuilder PublishToNuGet { get; set; }
    public static CakeTaskBuilder Default { get; set; }
}

Tasks.Info = Task("Info")
    .Does(() =>
{
    Build.Info();
});

Tasks.Build = Task("Build")
    .WithCriteria(() => Build.Parameters.RunBuild, "Not run")
    .Does(() =>
{
    var solutionPatterns = Build.Patterns.BuildSolutions
        .Select(pattern => Build.Directories.Source.CombineWithFilePath(pattern).FullPath).ToArray();
    var solutions = GetFiles(solutionPatterns);
    if (!solutions.Any())
    {
        Warning("Build solutions not found");
        return;
    }

    CleanDirectories(Build.Directories.Source.CombineWithFilePath("**/bin").FullPath);
    CleanDirectories(Build.Directories.Source.CombineWithFilePath("**/obj").FullPath);

    var msbuildSettings = new DotNetCoreMSBuildSettings
    {
        MaxCpuCount = Build.ToolSettings.BuildMaxCpuCount,
        TreatAllWarningsAs = Build.ToolSettings.BuildTreatWarningsAsErrors ? MSBuildTreatAllWarningsAs.Error : MSBuildTreatAllWarningsAs.Default
    }
        .WithProperty("EmbedAllSources", Build.ToolSettings.BuildEmbedAllSources.ToString().ToLower())
        .WithProperty("Version", Build.Version.AssemblyVersion)
        .WithProperty("FileVersion", Build.Version.AssemblyFileVersion)
        .WithProperty("InformationalVersion", Build.Version.InformationalVersion)
        .WithProperty("PackageVersion", Build.Version.FullSemVer);
    var buildSettings = new DotNetCoreBuildSettings
    {
        Configuration = Build.Parameters.Configuration,
        MSBuildSettings = msbuildSettings,
        ArgumentCustomization = args => { if (Build.ToolSettings.BuildBinaryLoggerEnabled) args.Append("-binarylogger"); return args; }
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

    var projectPatterns = Build.Patterns.BuildPublishProjects
        .Select(pattern => Build.Directories.Source.CombineWithFilePath(pattern).FullPath).ToArray();
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
        NoBuild = true,
        NoRestore = true
    };
    foreach (var project in projects)
    {
        DotNetCorePublish(project.FullPath, publishSettings);
    }
});

Tasks.UnitTests = Task("UnitTests")
    .IsDependentOn("Build")
    .WithCriteria(() => Build.Parameters.RunUnitTests, "Not run")
    .Does(() =>
{
    var patterns = Build.Patterns.UnitTestProjects
        .Select(pattern => Build.Directories.Source.CombineWithFilePath(pattern).FullPath).ToArray();
    var projects = GetFiles(patterns);
    if (!projects.Any())
    {
        Warning("Unit test projects not found");
        return;
    }

    var settings = new DotNetCoreTestSettings
    {
        Configuration = Build.Parameters.Configuration,
        Logger = Build.ToolSettings.UnitTestsLogger,
        NoBuild = true,
        NoRestore = true
    };
    foreach (var project in projects)
    {
        DotNetCoreTest(project.FullPath, settings);
    }
});

Tasks.DockerBuild = Task("DockerBuild")
    .IsDependentOn("Build")
    .WithCriteria(() => Build.Parameters.RunDockerBuild, "Not run")
    .WithCriteria(() => Build.DockerImages != null && Build.DockerImages.All(image => image.IsConfigured), "Not configured")
    .DoesForEach(() => Build.DockerImages, image =>
{
    var tokens = Build.ToTokens();
    var tags = image.Tags
        .Select(tag => string.Concat(image.Repository, ":", TransformText(tag, "{{", "}}").WithTokens(tokens))).ToArray();
    var settings = new DockerImageBuildSettings
    {
        File = image.File,
        BuildArg = new[] { $"configuration={Build.Parameters.Configuration}" },
        Tag = tags
    };
    DockerBuild(settings, image.Context);
});

Tasks.IntegrationTests = Task("IntegrationTests")
    .IsDependentOn("UnitTests")
    .IsDependentOn("DockerBuild")
    .WithCriteria(() => Build.Parameters.RunIntegrationTests, "Not run")
    .Does(() =>
{
    var patterns = Build.Patterns.IntegrationTestProjects
        .Select(pattern => Build.Directories.Source.CombineWithFilePath(pattern).FullPath).ToArray();
    var projects = GetFiles(patterns);
    if (!projects.Any())
    {
        Warning("Integration test projects not found");
        return;
    }

    var settings = new DotNetCoreTestSettings
    {
        Configuration = Build.Parameters.Configuration,
        EnvironmentVariables = Build.ToEnvVars(),
        Logger = Build.ToolSettings.IntegrationTestsLogger,
        NoBuild = true,
        NoRestore = true
    };
    foreach (var project in projects)
    {
        DotNetCoreTest(project.FullPath, settings);
    }
});

Tasks.StageArtifacts = Task("StageArtifacts")
    .IsDependentOn("DockerBuild")
    .IsDependentOn("NuGetPack");

Tasks.NuGetPack = Task("NuGetPack")
    .IsDependentOn("Build")
    .WithCriteria(() => Build.Parameters.RunNuGetPack, "Not run")
    .Does(() =>
{
    var patterns = Build.Patterns.NuGetProjects
        .Select(pattern => Build.Directories.Source.CombineWithFilePath(pattern).FullPath).ToArray();
    var projects = GetFiles(patterns);
    if (!projects.Any())
    {
        Warning("NuGet projects not found");
        return;
    }

    CleanDirectory(Build.Directories.ArtifactsNuGet);

    var msbuildSettings = new DotNetCoreMSBuildSettings()
        .WithProperty("PackageVersion", Build.Version.FullSemVer);
    var settings = new DotNetCorePackSettings
    {
        Configuration = Build.Parameters.Configuration,
        IncludeSymbols = Build.ToolSettings.NuGetPackSymbols,
        MSBuildSettings = msbuildSettings,
        NoBuild = true,
        NoRestore = true,
        OutputDirectory = Build.Directories.ArtifactsNuGet
    };
    foreach (var project in projects)
    {
        DotNetCorePack(project.FullPath, settings);
    }
});

Tasks.PublishArtifacts = Task("PublishArtifacts")
    .IsDependentOn("PublishToDocker")
    .IsDependentOn("PublishToNuGet");

Tasks.PublishToDocker = Task("PublishToDocker")
    .IsDependentOn("IntegrationTests")
    .IsDependentOn("DockerBuild")
    .WithCriteria(() => Build.Parameters.RunPublishToDocker, "Not run")
    .WithCriteria(() => Build.DockerImages != null && Build.DockerImages.All(image => image.IsConfigured), "Not configured")
    .WithCriteria(() => Build.Version.IsPublic, "Not public")
    .WithCriteria(() => Build.Parameters.IsPublisher, "Not publisher")
    .DoesForEach(() => Build.DockerImages, image =>
{
    var registry = image.Registry ?? Build.ToolSettings.DockerRegistry;
    var tokens = Build.ToTokens();
    var tags = image.Tags
        .Where(tag => Build.ToolSettings.DockerPushLatest || tag != "latest")
        .Select(tag => string.Concat(image.Repository, ":", TransformText(tag, "{{", "}}").WithTokens(tokens)));
    foreach (var tag in tags)
    {
        if (registry.IsConfigured())
        {
            DockerTag(tag, $"{registry}/{tag}");
            DockerPush($"{registry}/{tag}");
        }
        else
        {
            DockerPush(tag);
        }
    }
});

Tasks.PublishToNuGet = Task("PublishToNuGet")
    .IsDependentOn("IntegrationTests")
    .IsDependentOn("NuGetPack")
    .WithCriteria(() => Build.Parameters.RunPublishToNuGet, "Not run")
    .WithCriteria(() => Build.Credentials.NuGet.IsConfigured, "Not configured")
    .WithCriteria(() => Build.Version.IsPublic, "Not public")
    .WithCriteria(() => Build.Parameters.IsPublisher, "Not publisher")
    .Does(() =>
{
    var packages = GetFiles(Build.Directories.ArtifactsNuGet.CombineWithFilePath("**/*.nupkg").FullPath);
    if (!packages.Any())
    {
        Warning("NuGet packages not found");
        return;
    }

    var settings = new DotNetCoreNuGetPushSettings
    {
        ApiKey = Build.Credentials.NuGet.ApiKey,
        Source = Build.Credentials.NuGet.Source
    };
    foreach (var package in packages)
    {
        DotNetCoreNuGetPush(package.FullPath, settings);
    }
});

Tasks.Default = Task("Default")
    .IsDependentOn("Info")
    .IsDependentOn("Build")
    .IsDependentOn("UnitTests")
    .IsDependentOn("DockerBuild")
    .IsDependentOn("IntegrationTests")
    .IsDependentOn("StageArtifacts")
    .IsDependentOn("PublishArtifacts");
