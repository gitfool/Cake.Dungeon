#addin nuget:?package=Cake.Docker&version=1.0.0
#addin nuget:?package=Cake.FileHelpers&version=4.0.0
#addin nuget:?package=Cake.Incubator&version=6.0.0

#tool dotnet:?package=dotnet-reportgenerator-globaltool&version=4.8.7
#tool dotnet:?package=GitVersion.Tool&version=5.6.6

var Build = new Builder(BuildSystem, Context, target => RunTarget(target));
