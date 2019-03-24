#load scripts/*.cake

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
