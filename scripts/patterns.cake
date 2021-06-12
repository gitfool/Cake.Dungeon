public class Patterns
{
    public Patterns(
        string[] buildSolutions,
        string[] unitTestProjects,
        string[] integrationTestProjects,
        string[] testCoverageReports,
        string[] nuGetProjects)
    {
        BuildSolutions = buildSolutions ?? new[] { "**/*.sln" };
        UnitTestProjects = unitTestProjects ?? new[] { "**/*.csproj" };
        IntegrationTestProjects = integrationTestProjects ?? new[] { "**/*.csproj" };
        TestCoverageReports = testCoverageReports ?? new[] { "**/coverage.*.xml" };
        NuGetProjects = nuGetProjects ?? new[] { "**/*.csproj" };
    }

    public string[] BuildSolutions { get; }
    public string[] UnitTestProjects { get; }
    public string[] IntegrationTestProjects { get; }
    public string[] TestCoverageReports { get; }
    public string[] NuGetProjects { get; }
}
