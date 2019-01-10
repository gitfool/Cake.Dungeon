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
    .WithCriteria(() => Build.Parameters.RunBuild)
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
        .WithProperty("PackageVersion", Build.Version.FullSemVer)
        .WithProperty("NoWarn", "NU5105");
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
    .WithCriteria(() => Build.Parameters.RunUnitTests)
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
    .WithCriteria(() => Build.Parameters.RunDockerBuild)
    .WithCriteria(() => Build.Container.IsConfigured, "Not configured")
    .Does(() =>
{
    var settings = new DockerImageBuildSettings
    {
        File = Build.Container.File,
        BuildArg = new[] { $"configuration={Build.Parameters.Configuration}" },
        Tag = new[] { $"{Build.Container.Repository}:{Build.Version.SemVer}", $"{Build.Container.Repository}:latest" }
    };
    DockerBuild(settings, Build.Container.Context);
});

Tasks.IntegrationTests = Task("IntegrationTests")
    .IsDependentOn("UnitTests")
    .IsDependentOn("DockerBuild")
    .WithCriteria(() => Build.Parameters.RunIntegrationTests)
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
    .WithCriteria(() => Build.Parameters.RunNuGetPack)
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

    var msbuildSettings = new DotNetCoreMSBuildSettings { }
        .WithProperty("PackageVersion", Build.Version.FullSemVer)
        .WithProperty("NoWarn", "NU5105");
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
    .WithCriteria(() => Build.Parameters.RunPublishToDocker)
    .WithCriteria(() => Build.Container.IsConfigured, "Not configured")
    .WithCriteria(() => Build.Version.IsPublic, "Not publishable")
    .WithCriteria(() => Build.Parameters.IsPublisher, "Not publisher")
    .Does(() =>
{
    if (Build.Container.Registry.IsConfigured())
    {
        DockerTag($"{Build.Container.Repository}:{Build.Version.SemVer}", $"{Build.Container.Registry}/{Build.Container.Repository}:{Build.Version.SemVer}");
        DockerPush($"{Build.Container.Registry}/{Build.Container.Repository}:{Build.Version.SemVer}");
        if (Build.Version.IsRelease || Build.ToolSettings.DockerPushLatest)
        {
            DockerTag($"{Build.Container.Repository}:{Build.Version.SemVer}", $"{Build.Container.Registry}/{Build.Container.Repository}:latest");
            DockerPush($"{Build.Container.Registry}/{Build.Container.Repository}:latest");
        }
    }
    else
    {
        DockerPush($"{Build.Container.Repository}:{Build.Version.SemVer}");
        if (Build.Version.IsRelease || Build.ToolSettings.DockerPushLatest)
        {
            DockerPush($"{Build.Container.Repository}:latest");
        }
    }
});

Tasks.PublishToNuGet = Task("PublishToNuGet")
    .IsDependentOn("IntegrationTests")
    .IsDependentOn("NuGetPack")
    .WithCriteria(() => Build.Parameters.RunPublishToNuGet)
    .WithCriteria(() => Build.Credentials.NuGet.IsConfigured, "Not configured")
    .WithCriteria(() => Build.Version.IsPublic, "Not publishable")
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
