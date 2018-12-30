public class Environment
{
    public Environment(
        string nuGetApiKeyVariable,
        string nuGetSourceVariable)
    {
        NuGetApiKeyVariable = nuGetApiKeyVariable ?? "NUGET_API_KEY";
        NuGetSourceVariable = nuGetSourceVariable ?? "NUGET_SOURCE";
    }

    public string NuGetApiKeyVariable { get; }
    public string NuGetSourceVariable { get; }
}
