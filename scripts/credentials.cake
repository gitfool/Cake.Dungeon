public class Credentials
{
    public Credentials(ICakeContext context, Environment environment)
    {
        NuGet = new NuGetCredentials(
            context.EnvironmentVariable(environment.NuGetApiKey),
            context.EnvironmentVariable(environment.NuGetSource));
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

    public bool IsConfigured => ApiKey.IsConfigured() && Source.IsConfigured();
}
