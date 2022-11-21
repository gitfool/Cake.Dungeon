#load bootstrap.cake

public bool DockerManifestExists(string manifest) => Context.DockerManifestExists(manifest);

public static bool DockerManifestExists(this ICakeContext context, string manifest)
{
    try
    {
        context.DockerManifestInspect(manifest);
        return true;
    }
    catch (Exception)
    {
        return false;
    }
}

public string FileReadText(FilePath file) => Context.FileReadText(file);

public static string FileReadText(this ICakeContext context, FilePath file) =>
    System.IO.File.ReadAllText(file.MakeAbsolute(context.Environment).FullPath);

public FilePathCollection GetFiles(IEnumerable<string> patterns) => Context.GetFiles(patterns);

public static FilePathCollection GetFiles(this ICakeContext context, IEnumerable<string> patterns) =>
    new FilePathCollection(patterns.Select(pattern => context.Globber.GetFiles(pattern)).SelectMany(paths => paths));

public void QuietVerbosity(Action<ICakeContext> action) => Context.QuietVerbosity(action);

public static void QuietVerbosity(this ICakeContext context, Action<ICakeContext> action)
{
    using var _ = context.QuietVerbosity();
    action(context);
}
