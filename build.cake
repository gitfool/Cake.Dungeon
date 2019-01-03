#load scripts/bootstrap.cake
#load scripts/builder.cake
#load scripts/credentials.cake
#load scripts/docker.cake
#load scripts/environment.cake
#load scripts/extensions.cake
#load scripts/parameters.cake
#load scripts/paths.cake
#load scripts/patterns.cake
#load scripts/tasks.cake
#load scripts/toolsettings.cake
#load scripts/version.cake

Build.SetParameters
(
    title: "Cake.Dungeon",
    configuration: "Release",

    defaultLog: true,

    runBuild: true,
    runNuGetPack: true,
    runPublishToNuGet: true,

    sourceDirectory: Build.Directories.Root
);

Build.Run();
