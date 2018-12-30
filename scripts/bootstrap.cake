#addin nuget:?package=Cake.Docker&version=0.9.7
#addin nuget:?package=Cake.FileHelpers&version=3.1.0
#addin nuget:?package=Cake.Incubator&version=3.1.0
#tool nuget:?package=GitVersion.CommandLine&version=4.0.0

var Build = new Builder(BuildSystem, Context, target => RunTarget(target));
