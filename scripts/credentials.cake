#load bootstrap.cake

public class Credentials
{
    public Credentials(ICakeContext context)
    {
        GitHub = new GitHubCredentials(
            context.EnvironmentVariable("GITHUB_TOKEN"),
            context.EnvironmentVariable("GITHUB_USERNAME"),
            context.EnvironmentVariable("GITHUB_PASSWORD"));

        NuGet = new NuGetCredentials(
            context.EnvironmentVariable("NUGET_API_KEY"),
            context.EnvironmentVariable("NUGET_USERNAME"),
            context.EnvironmentVariable("NUGET_PASSWORD"));
    }

    public GitHubCredentials GitHub { get; }
    public NuGetCredentials NuGet { get; }
}

public class GitHubCredentials
{
    public GitHubCredentials(string token, string userName, string password)
    {
        Token = token;
        UserName = userName;
        Password = password;
    }

    public string Token { get; }
    public string UserName { get; }
    public string Password { get; }

    public bool IsConfigured => Token.IsConfigured() || (UserName.IsConfigured() && Password.IsConfigured());
}

public class NuGetCredentials
{
    public NuGetCredentials(string apiKey, string userName, string password)
    {
        ApiKey = apiKey;
        UserName = userName;
        Password = password;
    }

    public string ApiKey { get; }
    public string UserName { get; }
    public string Password { get; }

    public bool IsConfigured => ApiKey.IsConfigured() || (UserName.IsConfigured() && Password.IsConfigured());
}
