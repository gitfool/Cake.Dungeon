#addin nuget:?package=Cake.Docker&version=1.3.0

#tool dotnet:?package=dotnet-reportgenerator-globaltool&version=5.3.11
#tool dotnet:?package=GitVersion.Tool&version=5.12.0

#load aliases.cake
#load builder.cake
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

if (BuildSystem.IsRunningOnGitHubActions)
{
    TaskSetup(context => BuildSystem.GitHubActions.Commands.StartGroup(context.Task.Name));
    TaskTeardown(context => BuildSystem.GitHubActions.Commands.EndGroup());
}

var Build = new Builder(BuildSystem, Context, target => RunTarget(target));
