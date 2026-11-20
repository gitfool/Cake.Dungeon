#addin nuget:?package=Cake.Docker&version=1.5.0

#tool dotnet:?package=dotnet-reportgenerator-globaltool&version=5.6.0
#tool dotnet:?package=GitVersion.Tool&version=7.0.1

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
    TaskSetup(context => GitHubActions.Commands.StartGroup(context.Task.Name));
    TaskTeardown(context => GitHubActions.Commands.EndGroup());
}

var Build = new Builder(BuildSystem, Context, target => RunTarget(target));
