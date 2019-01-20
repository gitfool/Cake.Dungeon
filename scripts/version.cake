public class Version
{
    public Version(BuildSystem buildSystem, ICakeContext context)
    {
        BuildSystem = buildSystem;
        GitVersion = context.GitVersion();

        IsPullRequest = (buildSystem.Provider & (BuildProvider.AzurePipelines | BuildProvider.AzurePipelinesHosted)) != 0 &&
            context.EnvironmentVariable("SYSTEM_PULLREQUEST_PULLREQUESTID") != null;
    }

    public string SemVer => GitVersion.SemVer;
    public string FullSemVer => GitVersion.FullSemVer;
    public string AssemblyVersion => GitVersion.AssemblySemVer;
    public string AssemblyFileVersion => GitVersion.AssemblySemFileVer;
    public string InformationalVersion => GitVersion.InformationalVersion;

    public bool IsLocal => BuildSystem.IsLocalBuild;
    public bool IsPullRequest { get; }
    public bool IsTagged => string.IsNullOrEmpty(GitVersion.BuildMetaData);
    public bool IsPrelease => IsTagged && !string.IsNullOrEmpty(GitVersion.PreReleaseTag);
    public bool IsRelease => IsTagged && string.IsNullOrEmpty(GitVersion.PreReleaseTag);
    public bool IsPublic => !IsLocal && !IsPullRequest && (IsPrelease || IsRelease);

    public string Summary => $"{(IsPublic ? "Public " : "")}{(IsPrelease ? "Prelease " : IsRelease ? "Release " : "")}Version {FullSemVer}";

    private BuildSystem BuildSystem { get; }
    private GitVersion GitVersion { get; }
}
