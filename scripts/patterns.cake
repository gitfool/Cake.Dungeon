public class Patterns
{
    public Patterns(
        string[] buildSolutions,
        string[] buildPublishProjects,
        string[] unitTestProjects,
        string[] integrationTestProjects,
        string[] nuGetProjects)
    {
        BuildSolutions = buildSolutions ?? new[] { "**/*.sln" };
        BuildPublishProjects = buildPublishProjects ?? new[] { "**/*.csproj" };
        UnitTestProjects = unitTestProjects ?? new[] { "**/*.csproj" };
        IntegrationTestProjects = integrationTestProjects ?? new[] { "**/*.csproj" };
        NuGetProjects = nuGetProjects ?? new[] { "**/*.csproj" };
    }

    public string[] BuildSolutions { get; }
    public string[] BuildPublishProjects { get; }
    public string[] UnitTestProjects { get; }
    public string[] IntegrationTestProjects { get; }
    public string[] NuGetProjects { get; }
}
