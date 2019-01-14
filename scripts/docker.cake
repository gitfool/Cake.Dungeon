public class DockerImage
{
    public DockerImage(string registry = null, string repository = null, string context = null, string file = null, string[] tags = null)
    {
        Registry = registry;
        Repository = repository;
        Context = context ?? ".";
        File = file;
        Tags = tags ?? new[] { "{{ Build.Version.SemVer }}", "latest" };
    }

    public string Registry { get; set; }
    public string Repository { get; set; }
    public string Context { get; set; }
    public string File { get; set; }
    public string[] Tags { get; set; }

    public bool IsConfigured => Repository.IsConfigured() && Context.IsConfigured() && Tags != null && Tags.All(tag => tag.IsConfigured());
}
