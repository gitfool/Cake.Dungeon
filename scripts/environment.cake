public class Environment
{
    public Environment(
        string nuGetApiKey,
        string nuGetSource)
    {
        NuGetApiKey = nuGetApiKey ?? "NUGET_API_KEY";
        NuGetSource = nuGetSource ?? "NUGET_SOURCE";
    }

    public string NuGetApiKey { get; }
    public string NuGetSource { get; }
}
