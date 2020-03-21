#module nuget:?package=Cake.DotNetTool.Module&version=0.4.0

#addin nuget:?package=Cake.Docker&version=0.11.0
#addin nuget:?package=Cake.FileHelpers&version=3.2.1
#addin nuget:?package=Cake.Incubator&version=5.1.0

#tool dotnet:?package=GitVersion.Tool&version=5.2.4

var Build = new Builder(BuildSystem, Context, target => RunTarget(target));
