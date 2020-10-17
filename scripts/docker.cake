public class DockerImage
{
    public DockerImage(string registry = null, string repository = null, string context = null, string file = null, string[] args = null, string[] tags = null)
    {
        Registry = registry;
        Repository = repository;
        Context = context ?? ".";
        File = file;
        Args = args;
        Tags = tags ?? new[] { "{{ Build.Version.SemVer }}", "latest" };
    }

    public string Registry { get; set; }
    public string Repository { get; set; }
    public string Context { get; set; }
    public string File { get; set; }
    public string[] Args { get; set; }
    public string[] Tags { get; set; }

    public bool IsConfigured => Repository.IsConfigured() && Context.IsConfigured() && Tags != null && Tags.All(tag => tag.IsConfigured());

    public DockerImageReference ToReference(ICakeContext context, string tag)
    {
        var source = $"{Repository}:{tag}";
        var target = Registry.IsConfigured() ? $"{Registry}/{Repository}:{tag}" : source;
        var exists = tag == "latest" || Exists(context, target);
        return new DockerImageReference { Tag = tag, Source = source, Target = target, Exists = exists };
    }

    private static bool Exists(ICakeContext context, string manifest)
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
}

public class DockerImageReference
{
    public string Tag { get; set; }
    public string Source { get; set; }
    public string Target { get; set; }
    public bool Exists { get; set; }
}

public class DockerDeployer
{
    public DockerDeployer(string registry = null, string repository = null, string tag = null, string[] environment = null, string[] volumes = null, string[] args = null)
    {
        Registry = registry;
        Repository = repository;
        Tag = tag ?? "latest";
        Environment = environment;
        Volumes = volumes;
        Args = args;
    }

    public string Registry { get; set; }
    public string Repository { get; set; }
    public string Tag { get; set; }
    public string[] Environment { get; set; }
    public string[] Volumes { get; set; }
    public string[] Args { get; set; }

    public bool IsConfigured => Repository.IsConfigured() && Tag.IsConfigured() && (Args == null || Args.Length > 0);
}
