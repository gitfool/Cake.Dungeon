#load environment.cake
#load extensions.cake

public class Credentials
{
    public Credentials(ICakeContext context, Environment environment)
    {
        GitHub = new GitHubCredentials(
            context.EnvironmentVariable(environment.GitHubToken),
            context.EnvironmentVariable(environment.GitHubUserName),
            context.EnvironmentVariable(environment.GitHubPassword));

        NuGet = new NuGetCredentials(
            context.EnvironmentVariable(environment.NuGetApiKey),
            context.EnvironmentVariable(environment.NuGetUserName),
            context.EnvironmentVariable(environment.NuGetPassword));
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
