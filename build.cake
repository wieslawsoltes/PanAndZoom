///////////////////////////////////////////////////////////////////////////////
// ADDINS
///////////////////////////////////////////////////////////////////////////////

#addin "nuget:?package=Polly&version=5.3.1"
#addin "nuget:?package=NuGet.Core&version=2.14.0"

///////////////////////////////////////////////////////////////////////////////		
// TOOLS		
///////////////////////////////////////////////////////////////////////////////

#tool "nuget:?package=NuGet.CommandLine&version=4.3.0"
#tool "nuget:?package=xunit.runner.console&version=2.3.1"
    
///////////////////////////////////////////////////////////////////////////////
// USINGS
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Polly;
using NuGet;

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var platform = Argument("platform", "Any CPU");
var configuration = Argument("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// CONFIGURATION
///////////////////////////////////////////////////////////////////////////////

var MainRepo = "wieslawsoltes/PanAndZoom";
var MasterBranch = "master";
var AssemblyInfoPath = File("./src/Shared/SharedAssemblyInfo.cs");
var ReleasePlatform = "Any CPU";
var ReleaseConfiguration = "Release";
var MSBuildSolution = "./PanAndZoom.sln";
var UnitTestsFramework = "net461";

///////////////////////////////////////////////////////////////////////////////
// .NET Core Projects
///////////////////////////////////////////////////////////////////////////////

var netCoreAppsRoot= "./samples";
var netCoreApps = new string[] { "AvaloniaDemo" };
var netCoreProjects = netCoreApps.Select(name => 
    new {
        Path = string.Format("{0}/{1}", netCoreAppsRoot, name),
        Name = name,
        Framework = XmlPeek(string.Format("{0}/{1}/{1}.csproj", netCoreAppsRoot, name), "//*[local-name()='TargetFramework']/text()"),
        Runtimes = XmlPeek(string.Format("{0}/{1}/{1}.csproj", netCoreAppsRoot, name), "//*[local-name()='RuntimeIdentifiers']/text()").Split(';')
    }).ToList();

///////////////////////////////////////////////////////////////////////////////
// .NET Core UnitTests
///////////////////////////////////////////////////////////////////////////////

var netCoreUnitTestsRoot= "./tests";
var netCoreUnitTests = new string[] { 
};
var netCoreUnitTestsProjects = netCoreUnitTests.Select(name => 
    new {
        Name = name,
        Path = string.Format("{0}/{1}", netCoreUnitTestsRoot, name),
        File = string.Format("{0}/{1}/{1}.csproj", netCoreUnitTestsRoot, name)
    }).ToList();
var netCoreUnitTestsFrameworks = new List<string>() { "netcoreapp2.0" };
if (IsRunningOnWindows())
{
    netCoreUnitTestsFrameworks.Add("net461");
}

///////////////////////////////////////////////////////////////////////////////
// PARAMETERS
///////////////////////////////////////////////////////////////////////////////

var isPlatformAnyCPU = StringComparer.OrdinalIgnoreCase.Equals(platform, "Any CPU");
var isPlatformX86 = StringComparer.OrdinalIgnoreCase.Equals(platform, "x86");
var isPlatformX64 = StringComparer.OrdinalIgnoreCase.Equals(platform, "x64");
var isLocalBuild = BuildSystem.IsLocalBuild;
var isRunningOnUnix = IsRunningOnUnix();
var isRunningOnWindows = IsRunningOnWindows();
var isRunningOnAppVeyor = BuildSystem.AppVeyor.IsRunningOnAppVeyor;
var isPullRequest = BuildSystem.AppVeyor.Environment.PullRequest.IsPullRequest;
var isMainRepo = StringComparer.OrdinalIgnoreCase.Equals(MainRepo, BuildSystem.AppVeyor.Environment.Repository.Name);
var isMasterBranch = StringComparer.OrdinalIgnoreCase.Equals(MasterBranch, BuildSystem.AppVeyor.Environment.Repository.Branch);
var isTagged = BuildSystem.AppVeyor.Environment.Repository.Tag.IsTag 
               && !string.IsNullOrWhiteSpace(BuildSystem.AppVeyor.Environment.Repository.Tag.Name);
var isReleasable = StringComparer.OrdinalIgnoreCase.Equals(ReleasePlatform, platform) 
                   && StringComparer.OrdinalIgnoreCase.Equals(ReleaseConfiguration, configuration);
var isMyGetRelease = !isTagged && isReleasable;
var isNuGetRelease = isTagged && isReleasable;

///////////////////////////////////////////////////////////////////////////////
// VERSION
///////////////////////////////////////////////////////////////////////////////

var version = ParseAssemblyInfo(AssemblyInfoPath).AssemblyVersion;

if (isRunningOnAppVeyor)
{
    if (isTagged)
    {
        // Use Tag Name as version
        version = BuildSystem.AppVeyor.Environment.Repository.Tag.Name;
    }
    else
    {
        // Use AssemblyVersion with Build as version
        version += "-build" + EnvironmentVariable("APPVEYOR_BUILD_NUMBER") + "-alpha";
    }
}

///////////////////////////////////////////////////////////////////////////////
// DIRECTORIES
///////////////////////////////////////////////////////////////////////////////

var artifactsDir = (DirectoryPath)Directory("./artifacts");
var testResultsDir = artifactsDir.Combine("test-results");
var nugetRoot = artifactsDir.Combine("nuget");
var dirSuffix = isPlatformAnyCPU ? configuration : platform + "/" + configuration;
var buildDirs = 
    GetDirectories("./src/**/bin/" + dirSuffix) + 
    GetDirectories("./src/**/obj/" + dirSuffix) + 
    GetDirectories("./samples/**/bin/" + dirSuffix) + 
    GetDirectories("./samples/**/obj/" + dirSuffix) +
    GetDirectories("./tests/**/bin/" + dirSuffix) + 
    GetDirectories("./tests/**/obj/" + dirSuffix);

///////////////////////////////////////////////////////////////////////////////
// NUGET NUSPECS
///////////////////////////////////////////////////////////////////////////////

// Key: Package Id
// Value is Tuple where Item1: Package Version, Item2: The *.csproj/*.props file path.
var packageVersions = new Dictionary<string, IList<Tuple<string,string>>>();

System.IO.Directory.EnumerateFiles(((DirectoryPath)Directory("./build")).FullPath, "*.props", SearchOption.AllDirectories)
    .ToList()
    .ForEach(fileName => {
    var xdoc = XDocument.Load(fileName);
    foreach (var reference in xdoc.Descendants().Where(x => x.Name.LocalName == "PackageReference"))
    {
        var name = reference.Attribute("Include").Value;
        var versionAttribute = reference.Attribute("Version");
        var packageVersion = versionAttribute != null 
            ? versionAttribute.Value 
            : reference.Elements().First(x=>x.Name.LocalName == "Version").Value;
        IList<Tuple<string, string>> versions;
        packageVersions.TryGetValue(name, out versions);
        if (versions == null)
        {
            versions = new List<Tuple<string, string>>();
            packageVersions[name] = versions;
        }
        versions.Add(Tuple.Create(packageVersion, fileName));
    }
});

Information("Checking installed NuGet package dependencies versions:");

packageVersions.ToList().ForEach(package =>
{
    var packageVersion = package.Value.First().Item1;
    bool isValidVersion = package.Value.All(x => x.Item1 == packageVersion);
    if (!isValidVersion)
    {
        Information("Error: package {0} has multiple versions installed:", package.Key);
        foreach (var v in package.Value)
        {
            Information("{0}, file: {1}", v.Item1, v.Item2);
        }
        throw new Exception("Detected multiple NuGet package version installed for different projects.");
    }
});

Information("Setting NuGet package dependencies versions:");

var AvaloniaVersion = packageVersions["Avalonia"].FirstOrDefault().Item1;

Information("Package: Avalonia, version: {0}", AvaloniaVersion);

var nuspecNuGet = new NuGetPackSettings()
{
    Id = "PanAndZoom",
    Version = version,
    Authors = new [] { "wieslawsoltes" },
    Owners = new [] { "wieslawsoltes" },
    LicenseUrl = new Uri("http://opensource.org/licenses/MIT"),
    ProjectUrl = new Uri("https://github.com/wieslawsoltes/PanAndZoom/"),
    RequireLicenseAcceptance = false,
    Symbols = false,
    NoPackageAnalysis = true,
    Description = "Easily add pan and zoom functionality to your Avalonia apps using PanAndZoom control.",
    Copyright = "Copyright 2017",
    Tags = new [] { "Pan", "Zoom", "Control", "Xaml", "Managed", "C#" },
    Files = new []
    {
        // netstandard2.0
        new NuSpecContent { Source = "src/PanAndZoom/bin/" + dirSuffix + "/netstandard2.0/" + "PanAndZoom.dll", Target = "lib/netstandard2.0" },
        new NuSpecContent { Source = "src/PanAndZoom/bin/" + dirSuffix + "/netstandard2.0/" + "PanAndZoom.xml", Target = "lib/netstandard2.0" },
        // net45
        new NuSpecContent { Source = "src/PanAndZoom/bin/" + dirSuffix + "/net45/" + "PanAndZoom.dll", Target = "lib/net45" },
        new NuSpecContent { Source = "src/PanAndZoom/bin/" + dirSuffix + "/net45/" + "PanAndZoom.xml", Target = "lib/net45" }
    },
    BasePath = Directory("./"),
    OutputDirectory = nugetRoot
};

var nuspecNuGetAvalonia = new NuGetPackSettings()
{
    Id = "Avalonia.Controls.PanAndZoom",
    Version = version,
    Authors = new [] { "wieslawsoltes" },
    Owners = new [] { "wieslawsoltes" },
    LicenseUrl = new Uri("http://opensource.org/licenses/MIT"),
    ProjectUrl = new Uri("https://github.com/wieslawsoltes/PanAndZoom/"),
    RequireLicenseAcceptance = false,
    Symbols = false,
    NoPackageAnalysis = true,
    Description = "Easily add pan and zoom functionality to your Avalonia apps using PanAndZoom control.",
    Copyright = "Copyright 2017",
    Tags = new [] { "Avalonia", "Pan", "Zoom", "Control", "Xaml", "Managed", "C#" },
    Dependencies = new []
    {
        new NuSpecDependency { Id = "PanAndZoom", Version = version },
        new NuSpecDependency { Id = "Avalonia", Version = AvaloniaVersion }
    },
    Files = new []
    {
        // netstandard2.0
        new NuSpecContent { Source = "src/Avalonia.Controls.PanAndZoom/bin/" + dirSuffix + "/netstandard2.0/" + "Avalonia.Controls.PanAndZoom.dll", Target = "lib/netstandard2.0" },
        new NuSpecContent { Source = "src/Avalonia.Controls.PanAndZoom/bin/" + dirSuffix + "/netstandard2.0/" + "Avalonia.Controls.PanAndZoom.xml", Target = "lib/netstandard2.0" },
        // net461
        new NuSpecContent { Source = "src/Avalonia.Controls.PanAndZoom/bin/" + dirSuffix + "/net461/" + "Avalonia.Controls.PanAndZoom.dll", Target = "lib/net461" },
        new NuSpecContent { Source = "src/Avalonia.Controls.PanAndZoom/bin/" + dirSuffix + "/net461/" + "Avalonia.Controls.PanAndZoom.xml", Target = "lib/net461" }
    },
    BasePath = Directory("./"),
    OutputDirectory = nugetRoot
};

var nuspecNuGetWpf = new NuGetPackSettings()
{
    Id = "Wpf.Controls.PanAndZoom",
    Version = version,
    Authors = new [] { "wieslawsoltes" },
    Owners = new [] { "wieslawsoltes" },
    LicenseUrl = new Uri("http://opensource.org/licenses/MIT"),
    ProjectUrl = new Uri("https://github.com/wieslawsoltes/PanAndZoom/"),
    RequireLicenseAcceptance = false,
    Symbols = false,
    NoPackageAnalysis = true,
    Description = "Easily add pan and zoom functionality to your WPF apps using PanAndZoom control.",
    Copyright = "Copyright 2017",
    Tags = new [] { "WPF", "Pan", "Zoom", "Control", "Xaml", "Managed", "C#" },
    Dependencies = new []
    {
        new NuSpecDependency { Id = "PanAndZoom", Version = version }
    },
    Files = new []
    {
        // net45
        new NuSpecContent { Source = "src/Wpf.Controls.PanAndZoom/bin/" + dirSuffix + "/net45/" + "Wpf.Controls.PanAndZoom.dll", Target = "lib/net45" },
        new NuSpecContent { Source = "src/Wpf.Controls.PanAndZoom/bin/" + dirSuffix + "/net45/" + "Wpf.Controls.PanAndZoom.xml", Target = "lib/net45" }
    },
    BasePath = Directory("./"),
    OutputDirectory = nugetRoot
};

var nuspecNuGetSettings = new List<NuGetPackSettings>();

nuspecNuGetSettings.Add(nuspecNuGet);
nuspecNuGetSettings.Add(nuspecNuGetAvalonia);
nuspecNuGetSettings.Add(nuspecNuGetWpf);

var nugetPackages = nuspecNuGetSettings.Select(nuspec => {
    return nuspec.OutputDirectory.CombineWithFilePath(string.Concat(nuspec.Id, ".", nuspec.Version, ".nupkg"));
}).ToArray();

///////////////////////////////////////////////////////////////////////////////
// INFORMATION
///////////////////////////////////////////////////////////////////////////////

Information("Building version {0} of PanAndZoom ({1}, {2}, {3}) using version {4} of Cake.", 
    version,
    platform,
    configuration,
    target,
    typeof(ICakeContext).Assembly.GetName().Version.ToString());

if (isRunningOnAppVeyor)
{
    Information("Repository Name: " + BuildSystem.AppVeyor.Environment.Repository.Name);
    Information("Repository Branch: " + BuildSystem.AppVeyor.Environment.Repository.Branch);
}

Information("Target: " + target);
Information("Platform: " + platform);
Information("Configuration: " + configuration);
Information("IsLocalBuild: " + isLocalBuild);
Information("IsRunningOnUnix: " + isRunningOnUnix);
Information("IsRunningOnWindows: " + isRunningOnWindows);
Information("IsRunningOnAppVeyor: " + isRunningOnAppVeyor);
Information("IsPullRequest: " + isPullRequest);
Information("IsMainRepo: " + isMainRepo);
Information("IsMasterBranch: " + isMasterBranch);
Information("IsTagged: " + isTagged);
Information("IsReleasable: " + isReleasable);
Information("IsMyGetRelease: " + isMyGetRelease);
Information("IsNuGetRelease: " + isNuGetRelease);

///////////////////////////////////////////////////////////////////////////////
// TASKS: VISUAL STUDIO
///////////////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectories(buildDirs);
    CleanDirectory(artifactsDir);
    CleanDirectory(testResultsDir);
    CleanDirectory(nugetRoot);
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    var maxRetryCount = 5;
    var toolTimeout = 1d;
    Policy
        .Handle<Exception>()
        .Retry(maxRetryCount, (exception, retryCount, context) => {
            if (retryCount == maxRetryCount)
            {
                throw exception;
            }
            else
            {
                Verbose("{0}", exception);
                toolTimeout+=0.5;
            }})
        .Execute(()=> {
            if(isRunningOnWindows)
            {
                NuGetRestore(MSBuildSolution, new NuGetRestoreSettings {
                    ToolTimeout = TimeSpan.FromMinutes(toolTimeout)
                });
            }
        });
});

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    if(isRunningOnWindows)
    {
        MSBuild(MSBuildSolution, settings => {
            settings.SetConfiguration(configuration);
            settings.UseToolVersion(MSBuildToolVersion.VS2017);
            settings.WithProperty("Platform", "\"" + platform + "\"");
            settings.SetVerbosity(Verbosity.Minimal);
            settings.SetMaxCpuCount(0);
        });
    }
});

Task("Run-Unit-Tests")
    .IsDependentOn("Build")
    .Does(() =>
{
    if(!isRunningOnWindows)
       return;
    var assemblies = GetFiles("./tests/**/bin/" + dirSuffix + "/" + UnitTestsFramework + "/*.UnitTests.dll");
    var settings = new XUnit2Settings { 
        ToolPath = (isPlatformAnyCPU || isPlatformX86) ? 
            Context.Tools.Resolve("xunit.console.x86.exe") :
            Context.Tools.Resolve("xunit.console.exe"),
        OutputDirectory = testResultsDir,
        XmlReportV1 = true,
        NoAppDomain = true,
        Parallelism = ParallelismOption.None,
        ShadowCopy = false
    };
    foreach (var assembly in assemblies)
    {
        XUnit2(assembly.FullPath, settings);
    }
});

Task("Create-NuGet-Packages")
    .IsDependentOn("Build")
    .Does(() =>
{
    foreach(var nuspec in nuspecNuGetSettings)
    {
        NuGetPack(nuspec);
    }
});

Task("Publish-MyGet")
    .IsDependentOn("Create-NuGet-Packages")
    .WithCriteria(() => !isLocalBuild)
    .WithCriteria(() => !isPullRequest)
    .WithCriteria(() => isMainRepo)
    .WithCriteria(() => isMasterBranch)
    .WithCriteria(() => isMyGetRelease)
    .Does(() =>
{
    var apiKey = EnvironmentVariable("MYGET_API_KEY");
    if(string.IsNullOrEmpty(apiKey)) 
    {
        throw new InvalidOperationException("Could not resolve MyGet API key.");
    }

    var apiUrl = EnvironmentVariable("MYGET_API_URL");
    if(string.IsNullOrEmpty(apiUrl)) 
    {
        throw new InvalidOperationException("Could not resolve MyGet API url.");
    }

    foreach(var nupkg in nugetPackages)
    {
        NuGetPush(nupkg, new NuGetPushSettings {
            Source = apiUrl,
            ApiKey = apiKey
        });
    }
})
.OnError(exception =>
{
    Information("Publish-MyGet Task failed, but continuing with next Task...");
});

Task("Publish-NuGet")
    .IsDependentOn("Create-NuGet-Packages")
    .WithCriteria(() => !isLocalBuild)
    .WithCriteria(() => !isPullRequest)
    .WithCriteria(() => isMainRepo)
    .WithCriteria(() => isMasterBranch)
    .WithCriteria(() => isNuGetRelease)
    .Does(() =>
{
    var apiKey = EnvironmentVariable("NUGET_API_KEY");
    if(string.IsNullOrEmpty(apiKey)) 
    {
        throw new InvalidOperationException("Could not resolve NuGet API key.");
    }

    var apiUrl = EnvironmentVariable("NUGET_API_URL");
    if(string.IsNullOrEmpty(apiUrl)) 
    {
        throw new InvalidOperationException("Could not resolve NuGet API url.");
    }

    foreach(var nupkg in nugetPackages)
    {
        NuGetPush(nupkg, new NuGetPushSettings {
            ApiKey = apiKey,
            Source = apiUrl
        });
    }
})
.OnError(exception =>
{
    Information("Publish-NuGet Task failed, but continuing with next Task...");
});

///////////////////////////////////////////////////////////////////////////////
// TASKS: .NET Core
///////////////////////////////////////////////////////////////////////////////

Task("Restore-NetCore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    foreach (var project in netCoreProjects)
    {
        DotNetCoreRestore(project.Path);
    }
});

Task("Run-Unit-Tests-NetCore")
    .IsDependentOn("Clean")
    .Does(() => 
{
    foreach (var project in netCoreUnitTestsProjects)
    {
        DotNetCoreRestore(project.Path);
        foreach(var framework in netCoreUnitTestsFrameworks)
        {
            Information("Running tests for: {0}, framework: {1}", project.Name, framework);
            DotNetCoreTest(project.File, new DotNetCoreTestSettings {
                Configuration = configuration,
                Framework = framework
            });
        }
    }
});

Task("Build-NetCore")
    .IsDependentOn("Restore-NetCore")
    .Does(() => 
{
    foreach (var project in netCoreProjects)
    {
        Information("Building: {0}", project.Name);
        DotNetCoreBuild(project.Path, new DotNetCoreBuildSettings {
            Configuration = configuration,
            MSBuildSettings = new DotNetCoreMSBuildSettings() {
                MaxCpuCount = 0
            }
        });
    }
});

///////////////////////////////////////////////////////////////////////////////
// TARGETS
///////////////////////////////////////////////////////////////////////////////

Task("Package")
  .IsDependentOn("Create-NuGet-Packages");

Task("Default")
  .IsDependentOn("Run-Unit-Tests");

Task("AppVeyor")
  .IsDependentOn("Run-Unit-Tests-NetCore")
  .IsDependentOn("Build-NetCore")
  .IsDependentOn("Publish-MyGet")
  .IsDependentOn("Publish-NuGet");

Task("Travis")
  .IsDependentOn("Run-Unit-Tests-NetCore")
  .IsDependentOn("Build-NetCore");

///////////////////////////////////////////////////////////////////////////////
// EXECUTE
///////////////////////////////////////////////////////////////////////////////

RunTarget(target);
