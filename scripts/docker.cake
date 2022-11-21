#load bootstrap.cake

public class DockerImage
{
    public DockerImage(string registry = null, string repository = null, string context = null, string file = null, string target = null, string[] args = null, string[] tags = null, string[] platforms = null)
    {
        Registry = registry;
        Repository = repository;
        Context = context ?? ".";
        File = file;
        Target = target;
        Args = args;
        Tags = tags;
        Platforms = platforms;
    }

    public string Registry { get; set; }
    public string Repository { get; set; }
    public string Context { get; set; }
    public string File { get; set; }
    public string Target { get; set; }
    public string[] Args { get; set; }
    public string[] Tags { get; set; }
    public string[] Platforms { get; set; }

    public bool IsConfigured => Repository.IsConfigured() && Context.IsConfigured() &&
        (Tags == null || Tags.All(tag => tag.IsConfigured())) && (Platforms == null || Platforms.All(platform => platform.IsConfigured()));

    public DockerImageReference ToReference(ICakeContext context, string tag, bool latest)
    {
        var source = $"{Repository}:{tag}";
        var target = Registry.IsConfigured() ? $"{Registry}/{source}" : source;
        var exists = latest || context.DockerManifestExists(target);
        return new DockerImageReference { Tag = tag, Source = source, Target = target, Exists = exists };
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
