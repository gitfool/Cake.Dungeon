#load scripts/*.cake

Build.SetParameters
(
    title: "Cake.Dungeon",

    defaultLog: true,

    runBuildSolutions: true,
    runNuGetPack: true,
    runPublishToNuGet: true,

    sourceDirectory: Build.Directories.Root,

    nuGetPushSkipDuplicate: true
);

Build.Run();
