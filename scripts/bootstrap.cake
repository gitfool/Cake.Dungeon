#addin nuget:?package=Cake.Docker&version=1.1.2
#addin nuget:?package=Cake.FileHelpers&version=5.0.0
#addin nuget:?package=Cake.Incubator&version=7.0.0

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

TaskSetup(context =>
{
    if (BuildSystem.IsRunningOnGitHubActions)
    {
        Information($"::group::{context.Task.Name}");
    }
});

TaskTeardown(context =>
{
    if (BuildSystem.IsRunningOnGitHubActions)
    {
        Information($"::endgroup::{context.Task.Name}");
    }
});

var Build = new Builder(BuildSystem, Context, target => RunTarget(target));
