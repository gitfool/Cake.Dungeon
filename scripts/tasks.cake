public static class Tasks
{
    public static CakeTaskBuilder Info { get; set; }
    public static CakeTaskBuilder BuildSolutions { get; set; }
    public static CakeTaskBuilder UnitTests { get; set; }
    public static CakeTaskBuilder DockerBuild { get; set; }
    public static CakeTaskBuilder IntegrationTests { get; set; }
    public static CakeTaskBuilder StageArtifacts { get; set; }
    public static CakeTaskBuilder NuGetPack { get; set; }
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
        .WithProperty("RestoreLockedMode", Build.ToolSettings.BuildRestoreLockedMode.ToString().ToLower())
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
    .IsDependentOn("BuildSolutions")
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
    .IsDependentOn("BuildSolutions")
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
    .WithCriteria(() => Build.Parameters.Publish, "Not publisher")
    .DoesForEach(() => Build.DockerImages, image =>
{
    var tags = Build.TransformTokens(image.Tags)
        .Where(tag => tag != "latest" || Build.ToolSettings.DockerPushLatest)
        .Select(tag =>
        {
            var local = $"{image.Repository}:{tag}";
            var remote = image.Registry.IsConfigured() ? $"{image.Registry}/{image.Repository}:{tag}" : local;
            return (tag != "latest" && DockerImageExists(remote))
                ? throw new InvalidOperationException($"Docker image {remote} already exists")
                : new { Local = local, Remote = remote };
        }).ToArray(); // eager check any image exists

    foreach (var tag in tags)
    {
        if (tag.Local != tag.Remote)
        {
            DockerTag(tag.Local, tag.Remote);
        }
        DockerPush(tag.Remote);
    }
});

Tasks.PublishToNuGet = Task("PublishToNuGet")
    .IsDependentOn("IntegrationTests")
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

    var settings = new DotNetCoreNuGetPushSettings
    {
        ApiKey = Build.Credentials.NuGet.ApiKey,
        Source = Build.ToolSettings.NuGetSource
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
    .IsDependentOn("UnitTests")
    .IsDependentOn("DockerBuild")
    .IsDependentOn("IntegrationTests")
    .IsDependentOn("StageArtifacts")
    .IsDependentOn("PublishArtifacts");

Tasks.Deploy = Task("Deploy")
    .IsDependentOn("Info")
    .IsDependentOn("DeployArtifacts");
