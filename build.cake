#load scripts/*.cake

Build.SetParameters
(
    title: "Cake.Dungeon",
    configuration: "Release",

    defaultLog: true,

    runBuildSolutions: true,
    runNuGetPack: true,
    runPublishToNuGet: true,

    sourceDirectory: Build.Directories.Root
);

Build.Run();
