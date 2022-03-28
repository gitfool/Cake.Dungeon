#addin nuget:?package=Cake.Docker&version=1.1.2

#tool dotnet:?package=dotnet-reportgenerator-globaltool&version=5.1.13
#tool dotnet:?package=GitVersion.Tool&version=5.11.1

#load aliases.cake
#load build.cake
#load credentials.cake
#load docker.cake
#load extensions.cake
#load parameters.cake
#load paths.cake
#load patterns.cake
#load tasks.cake
#load toolsettings.cake
#load version.cake

using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

var build = new Build(Context, target => RunTarget(target));

Setup<Build>(context => build);

if (BuildSystem.IsRunningOnGitHubActions)
{
    TaskSetup<Build>((context, build) => BuildSystem.GitHubActions.Commands.StartGroup(context.Task.Name));
    TaskTeardown<Build>((context, build) => BuildSystem.GitHubActions.Commands.EndGroup());
}
