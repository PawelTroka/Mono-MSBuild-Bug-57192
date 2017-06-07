

//////////////////////////////////////////////////////////////////////
// ENVIRONMENTAL VARIABLES
//////////////////////////////////////////////////////////////////////
var coverallsRepoToken = EnvironmentVariable("COVERALLS_REPO_TOKEN");//"KEH5rJaqCoWoCV2MhkrMlClj3SVIlB2Eu0YK4mqmhRM+ANEfGiFyROo2RWHkJXQz"
var configuration = (EnvironmentVariable("configuration") ?? EnvironmentVariable("build_config")) ?? "Release";
var netmoniker = EnvironmentVariable("netmoniker") ?? "net461";
var travisOsName = EnvironmentVariable("TRAVIS_OS_NAME");
var dotNetCore = EnvironmentVariable("DOTNETCORE");
string monoVersion = null;
string monoVersionShort = null;

Type type = Type.GetType("Mono.Runtime");
if (type != null)
{
	var displayName = type.GetMethod("GetDisplayName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
	if (displayName != null)
		monoVersion = displayName.Invoke(null, null).ToString();
}

var isMonoButSupportsMsBuild = monoVersion!=null && System.Text.RegularExpressions.Regex.IsMatch(monoVersion,@"([5-9]|\d{2,})\.\d+\.\d+(\.\d+)?");



//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");


//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var netVersion =  System.Text.RegularExpressions.Regex.Replace(netmoniker.ToLowerInvariant().Replace("net",""), ".{1}", "$0.").TrimEnd('.');

var msBuildSettings = new MSBuildSettings {
	ArgumentCustomization = args=>args.Append(@" /p:TargetFramework="+netmoniker),//args=>args.Append(@" /p:TargetFrameworkVersion=v"+netVersion),
    Verbosity = Verbosity.Minimal,
    ToolVersion = MSBuildToolVersion.Default,//The highest available MSBuild tool version//VS2017
    Configuration = configuration,
    PlatformTarget = PlatformTarget.MSIL,
	MSBuildPlatform = MSBuildPlatform.Automatic,
	DetailedSummary = true,
    };

	if(!IsRunningOnWindows() && isMonoButSupportsMsBuild)
	{
		msBuildSettings.ToolPath = new FilePath(
		  System.Environment.OSVersion.Platform != System.PlatformID.MacOSX
		  ? @"/usr/lib/mono/msbuild/15.0/bin/MSBuild.dll"
		  : @"/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/msbuild/15.0/bin/MSBuild.exe"
		  );//hack for Linux and Mac OS X bug - missing MSBuild path	
	}

var xBuildSettings = new XBuildSettings {
	ArgumentCustomization = args=>args.Append(@" /p:TargetFramework="+netmoniker),//args=>args.Append(@" /p:TargetFrameworkVersion=v"+netVersion),
    Verbosity = Verbosity.Minimal,
    ToolVersion = XBuildToolVersion.Default,//The highest available XBuild tool version//NET40
    Configuration = configuration,
    //PlatformTarget = PlatformTarget.MSIL,
    };

var monoEnvVars = new Dictionary<string,string>() { {"DISPLAY", "99.0"},{"MONO_WINFORMS_XIM_STYLE", "disabled"} };

if(!IsRunningOnWindows())
{
	if(msBuildSettings.EnvironmentVariables==null)
		msBuildSettings.EnvironmentVariables = new Dictionary<string,string>();
	if(xBuildSettings.EnvironmentVariables==null)
		xBuildSettings.EnvironmentVariables = new Dictionary<string,string>();

	foreach(var monoEnvVar in monoEnvVars)
	{
		msBuildSettings.EnvironmentVariables.Add(monoEnvVar.Key,monoEnvVar.Value);
		xBuildSettings.EnvironmentVariables.Add(monoEnvVar.Key,monoEnvVar.Value);
	}
}

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////


Task("Clean")
	.Does(() =>
{
	DeleteDirectories(GetDirectories("Mono-MSBuild-Bug-57192*/**/bin"), recursive:true);
	DeleteDirectories(GetDirectories("Mono-MSBuild-Bug-57192*/**/obj"), recursive:true);
});

Task("Restore")
	.IsDependentOn("Clean")
	.Does(() =>
{

NuGetRestore("./Mono-MSBuild-Bug-57192.sln");

if (travisOsName == "linux")
{
	StartProcess("sudo", "apt-get install libgsl2");
	System.Environment.SetEnvironmentVariable("DISPLAY", "99.0", System.EnvironmentVariableTarget.Process);//StartProcess("export", "DISPLAY=:99.0");
	StartProcess("sh", "-e /etc/init.d/xvfb start");
	System.Environment.SetEnvironmentVariable("DISPLAY", "99.0", System.EnvironmentVariableTarget.Process);//StartProcess("export", "DISPLAY=:99.0");
	StartProcess("sleep", "3");//give xvfb some time to start
}
else if(travisOsName == "osx")
{
	StartProcess("brew", "install gsl");
}

if(travisOsName=="linux" || travisOsName=="osx")
	System.Environment.SetEnvironmentVariable("MONO_WINFORMS_XIM_STYLE", "disabled", System.EnvironmentVariableTarget.Process);//StartProcess("export", "MONO_WINFORMS_XIM_STYLE=disabled");
});

Task("Build")
	.IsDependentOn("Restore")
	.Does(() =>
{
	if(IsRunningOnWindows() || isMonoButSupportsMsBuild)
	{
	  // Use MSBuild
	  MSBuild("Mono-MSBuild-Bug-57192/Mono-MSBuild-Bug-57192.csproj", msBuildSettings);
	}
	else
	{
	  // Use XBuild
	  XBuild("Mono-MSBuild-Bug-57192/Mono-MSBuild-Bug-57192.csproj", xBuildSettings);
	}
});



//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
	.IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
