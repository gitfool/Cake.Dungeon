public class Directories
{
    public Directories(
        ICakeContext context,
        DirectoryPath root,
        DirectoryPath source,
        DirectoryPath artifacts,
        DirectoryPath artifactsTests,
        DirectoryPath artifactsNuGet)
    {
        Root = root ?? context.MakeAbsolute(context.Directory("./"));
        Source = source ?? Root.Combine("Source");
        Artifacts = artifacts ?? Root.Combine("Artifacts");
        ArtifactsTests = artifactsTests ?? Artifacts.Combine("Tests");
        ArtifactsNuGet = artifactsNuGet ?? Artifacts.Combine("NuGet");
    }

    public DirectoryPath Root { get; }
    public DirectoryPath Source { get; }
    public DirectoryPath Artifacts { get; }
    public DirectoryPath ArtifactsTests { get; }
    public DirectoryPath ArtifactsNuGet { get; }
}

public class Files
{
    public Files(
        ICakeContext context,
        Directories directories)
    {
    }
}
