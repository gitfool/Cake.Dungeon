public class DockerContainer
{
    public DockerContainer(string registry, string repository, string context, string file)
    {
        Registry = registry ?? "hub.docker.com";
        Repository = repository;
        Context = context ?? ".";
        File = file;
    }

    public string Registry { get; }
    public string Repository { get; }
    public string Context { get; }
    public string File { get; }

    public bool IsConfigured => Registry.IsConfigured() && Repository.IsConfigured() && Context.IsConfigured();
}
