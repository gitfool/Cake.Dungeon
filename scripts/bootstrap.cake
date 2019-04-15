#module nuget:?package=Cake.DotNetTool.Module&version=0.2.0

#addin nuget:?package=Cake.Docker&version=0.9.9
#addin nuget:?package=Cake.FileHelpers&version=3.1.0
#addin nuget:?package=Cake.Incubator&version=5.0.1

#tool dotnet:?package=GitVersion.Tool&version=4.0.1-beta1-58

var Build = new Builder(BuildSystem, Context, target => RunTarget(target));
