public class Environment
{
    public Environment(
        string gitHubToken,
        string gitHubUserName,
        string gitHubPassword,
        string nuGetApiKey,
        string nuGetUserName,
        string nuGetPassword)
    {
        GitHubToken = gitHubToken ?? "GITHUB_TOKEN";
        GitHubUserName = gitHubUserName ?? "GITHUB_USERNAME";
        GitHubPassword = gitHubPassword ?? "GITHUB_PASSWORD";

        NuGetApiKey = nuGetApiKey ?? "NUGET_API_KEY";
        NuGetUserName = nuGetUserName ?? "NUGET_USERNAME";
        NuGetPassword = nuGetPassword ?? "NUGET_PASSWORD";
    }

    public string GitHubToken { get; }
    public string GitHubUserName { get; }
    public string GitHubPassword { get; }
    public string NuGetApiKey { get; }
    public string NuGetUserName { get; }
    public string NuGetPassword { get; }
}
