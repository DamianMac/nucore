#tool "nuget:?package=xunit.runner.console&version=2.2.0"

var target = Argument("Target", "Default");  
var configuration = Argument("Configuration", "Release");

var buildNumber = Argument("buildNumber", "0");
var version = string.Format("1.0.0.{0}", buildNumber);

Information($"Running target {target} in configuration {configuration}");

var packageId = "CHANGEME";
var distDirectory = Directory("./dist");
var packageDirectory = Directory("./dist_package");


// Deletes the contents of the Artifacts folder if it contains anything from a previous build.
Task("Clean")  
    .Does(() =>
    {
        CleanDirectory(distDirectory);
        CleanDirectory(packageDirectory);
    });

// Run dotnet restore to restore all package references.
Task("Restore")  
    .Does(() =>
    {
        var settings = new DotNetCoreRestoreSettings{
        };
        DotNetCoreRestore("./", settings);
    });

Task("GenerateVersionFile")
    .Does(() =>
    {
        using(var process = StartAndReturnProcess("./version.sh"))
        {
            process.WaitForExit();
            // This should output 0 as valid arguments supplied
            Information("Exit code: {0}", process.GetExitCode());
        }
    });

// Build using the build configuration specified as an argument.
 Task("Build")
    .Does(() =>
    {
        DotNetCoreBuild(".",
            new DotNetCoreBuildSettings()
            {
                Configuration = configuration,
                ArgumentCustomization = args => args.Append("--no-restore"),
            });
    });

// Look under a 'Tests' folder and run dotnet test against all of those projects.
// Then drop the XML test results file in the Artifacts folder at the root.
Task("Test")  
    .Does(() =>
    {
        var projects = GetFiles("./tests/tests/tests.csproj");
        foreach(var project in projects)
        {
            Information("Testing project " + project);
            DotNetCoreTest(project.FullPath);
        }
    });

// Publish the app to the /dist folder
Task("PublishWeb")  
    .Does(() =>
    {
        DotNetCorePublish(
            "./src/web/web.csproj",
            new DotNetCorePublishSettings()
            {
                Configuration = configuration,
                OutputDirectory = distDirectory,
                ArgumentCustomization = args => args.Append("--no-restore"),
            });
    });

// A meta-task that runs all the steps to Build and Test the app
Task("BuildAndTest")  
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    //.IsDependentOn("GenerateVersionFile")
    .IsDependentOn("Build")
    .IsDependentOn("Test");

// The default task to run if none is explicitly specified. In this case, we want
// to run everything starting from Clean, all the way up to Publish.
Task("Default")  
    .IsDependentOn("BuildAndTest")
    .IsDependentOn("PublishWeb");

Task("Package")
    .Does(() => 
    {
        

        StartProcess("dotnet", new ProcessSettings {
            Arguments = new ProcessArgumentBuilder()
                .Append("octo")
                .Append("pack")
                .Append($"--id={packageId}")
                .Append($"--version={version}")
                .Append($"--basePath=\"{distDirectory}\"")
                .Append($"--outFolder=\"{packageDirectory}\"")
            }
        );
    });

Task("PushPackages")
	.IsDependentOn("Package")
	.Does(() => {
		if (HasEnvironmentVariable("octopusurl"))
		{
			var server = EnvironmentVariable("octopusurl");
			var apikey = EnvironmentVariable("octopusapikey");
            var package = $"{packageDirectory}/{packageId}.{version}.nupkg";
			
            Information($"The Octopus variable was present. {server}");

            StartProcess("dotnet", new ProcessSettings {
                        Arguments = new ProcessArgumentBuilder()
                            .Append("octo")
                            .Append("push")
                            .Append($"--package={package}")
                            .Append($"--server=\"{server}\"")
                            .Append($"--apiKey=\"{apikey}\"")
                        }
                    );
		}
        else
        {
            Information("No Octopus variables present.");
        }

	});

Task("CI")
    .IsDependentOn("BuildAndTest")
    .IsDependentOn("PublishWeb")
    .IsDependentOn("Package")
    .IsDependentOn("PushPackages");

// Executes the task specified in the target argument.
RunTarget(target); 
