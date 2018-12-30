public class Directories
{
    public Directories(
        ICakeContext context,
        DirectoryPath root,
        DirectoryPath source,
        DirectoryPath artifacts,
        DirectoryPath artifactsNuGet,
        DirectoryPath artifactsTests)
    {
        Root = root ?? context.MakeAbsolute(context.Directory("./"));
        Source = source ?? Root.Combine("Source");
        Artifacts = artifacts ?? Root.Combine("Artifacts");
        ArtifactsNuGet = artifactsNuGet ?? Artifacts.Combine("NuGet");
        ArtifactsTests = artifactsTests ?? Artifacts.Combine("Tests");
    }

    public DirectoryPath Root { get; }
    public DirectoryPath Source { get; }
    public DirectoryPath Artifacts { get; }
    public DirectoryPath ArtifactsNuGet { get; }
    public DirectoryPath ArtifactsTests { get; }
}

public class Files
{
    public Files(
        ICakeContext context,
        Directories directories)
    {
    }
}
