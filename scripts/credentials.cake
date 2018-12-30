public class Credentials
{
    public Credentials(ICakeContext context, Environment environment)
    {
        NuGet = new NuGetCredentials(
            context.EnvironmentVariable(environment.NuGetApiKeyVariable),
            context.EnvironmentVariable(environment.NuGetSourceVariable));
    }

    public NuGetCredentials NuGet { get; }
}

public class NuGetCredentials
{
    public NuGetCredentials(string apiKey, string source)
    {
        ApiKey = apiKey;
        Source = source;
    }

    public string ApiKey { get; }
    public string Source { get; }
}
