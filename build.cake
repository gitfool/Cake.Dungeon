#load scripts/bootstrap.cake

build.SetParameters
(
    title: "Cake.Dungeon",

    defaultLog: true,

    runBuildSolutions: true,
    runNuGetPack: true,
    runPublishToNuGet: true,

    sourceDirectory: build.Directories.Root,

    nuGetPushSkipDuplicate: true
);

build.Run();
