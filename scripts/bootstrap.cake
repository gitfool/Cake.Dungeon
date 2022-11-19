#addin nuget:?package=Cake.Docker&version=1.1.2

#tool dotnet:?package=dotnet-reportgenerator-globaltool&version=5.1.12
#tool dotnet:?package=GitVersion.Tool&version=5.11.1

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
    TaskSetup(context => Information($"::group::{context.Task.Name}"));
    TaskTeardown(context => Information($"::endgroup::"));
}

var Build = new Builder(BuildSystem, Context, target => RunTarget(target));
