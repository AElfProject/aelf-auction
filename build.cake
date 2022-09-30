#tool nuget:?package=Codecov
#addin nuget:?package=Cake.Codecov&version=0.8.0
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");
var rootPath     = "./";
var srcPath      = rootPath + "chain/src/";
var contractPath = rootPath + "chain/contract/";
var testPath     = rootPath + "chain/test/";
var distPath     = rootPath + "chain/aelf-node/";
var solution     = rootPath + "chain/AElf.Contracts.Auction.sln";
var srcProjects  = GetFiles(srcPath + "**/*.csproj");
var contractProjects  = GetFiles(contractPath + "**/*.csproj");

Task("Clean")
    .Description("clean up project cache")
    .Does(() =>
{
    DeleteFiles(distPath + "*.nupkg");
    CleanDirectories(srcPath + "**/bin");
    CleanDirectories(srcPath + "**/obj");
    CleanDirectories(contractPath + "**/bin");
    CleanDirectories(contractPath + "**/obj");
    CleanDirectories(testPath + "**/bin");
    CleanDirectories(testPath + "**/obj");
});

Task("Restore")
    .Description("restore project dependencies")
    .Does(() =>
{
    DotNetCoreRestore(solution, new DotNetCoreRestoreSettings
    {
        Verbosity = DotNetCoreVerbosity.Quiet,
        Sources = new [] { "https://www.myget.org/F/aelf-project-dev/api/v3/index.json", "https://api.nuget.org/v3/index.json" }
    });
});
Task("Build")
    .Description("Compilation project")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .Does(() =>
{
    var buildSetting = new DotNetCoreBuildSettings{
        NoRestore = true,
        Configuration = configuration,
        ArgumentCustomization = args => {
            return args.Append("/clp:ErrorsOnly")
                       .Append("-v quiet");}
    };
     
    DotNetCoreBuild(solution, buildSetting);
});
Task("Build-Release")
    .Description("Compilation project")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .Does(() =>
{   var versionPrefix = EnvironmentVariable("MYGET_VERSION_PREFIX");
    var buildVersion = (DateTime.UtcNow.Ticks - 621355968000000000) / 10000000 / 86400;
    var buildSetting = new DotNetCoreBuildSettings{
        NoRestore = true,
        Configuration = configuration,
        ArgumentCustomization = args => {                   
            return args.Append("/clp:ErrorsOnly")                 
                       .Append("-v quiet")
                       .Append($"-P:Version={versionPrefix}-{buildVersion}")
                       .Append("-P:Authors=AElf")
                       .Append("-o ./nuget")
;}      
    };      
     
    DotNetCoreBuild(solution, buildSetting);
});
Task("Test-with-Codecov")
    .Description("operation test_with_codecov")
    .IsDependentOn("Build")
    .Does(() =>
{
    var testSetting = new DotNetCoreTestSettings{
        Configuration = configuration,
        NoRestore = true,
        NoBuild = true,
        ArgumentCustomization = args => {
            return args
                .Append("--logger trx")
                .Append("--settings CodeCoverage.runsettings")
                .Append("--collect:\"XPlat Code Coverage\"");
        }                
    };
    var codecovToken = "$CODECOV_TOKEN";
    var actions = new List<Action>();
    var testProjects = GetFiles("./chain/test/*.Tests/*.csproj");

    foreach(var testProject in testProjects)
    {
        var action=new Action(()=>{
            DotNetCoreTest(testProject.FullPath, testSetting);

            // if(codecovToken!=""){
            //     var dir=testProject.GetDirectory().FullPath;
            //     var reports = GetFiles(dir + "/TestResults/*/coverage.cobertura.xml");

            //     foreach(var report in reports)
            //     {
            //         Codecov(report.FullPath,"$CODECOV_TOKEN");
            //     }
            // }
        });
        actions.Add(action);
    }


    var options = new ParallelOptions {
        MaxDegreeOfParallelism = 1,
        //CancellationToken = cancellationToken
    };

    Parallel.Invoke(options, actions.ToArray());
});
Task("Run-Unit-Tests")
    .Description("operation test")
    .IsDependentOn("Build")
    .Does(() =>
{
    var testSetting = new DotNetCoreTestSettings{
        NoRestore = true,
        NoBuild = true,
        ArgumentCustomization = args => {
            return args.Append("--logger trx");
        }
};
    var testProjects = GetFiles("./chain/test/*.Tests/*.csproj");


    foreach(var testProject in testProjects)
    {
        DotNetCoreTest(testProject.FullPath, testSetting);
    }
});
Task("Upload-Coverage")
    .Does(() =>
{
    var reports = GetFiles("./chain/test/*.Tests/TestResults/*/coverage.cobertura.xml");

    foreach(var report in reports)
    {
        Codecov(report.FullPath,"$CODECOV_TOKEN");
    }
});
Task("Upload-Coverage-Azure")
    .Does(() =>
{
    Codecov("./CodeCoverage/Cobertura.xml","$CODECOV_TOKEN");
});
Task("Publish-MyGet")
    .IsDependentOn("Build-Release")
    .Does(() => {
        var apiKey = EnvironmentVariable("MYGET_API_KEY");
        var pushSettings = new DotNetCoreNuGetPushSettings 
        {
            Source = "https://www.myget.org/F/aelf-project-dev/api/v3/index.json",
            ApiKey = apiKey

        };

        var pkgs = GetFiles("./nuget/*.nupkg");
        foreach(var pkg in pkgs) 
        {
                Information($"Publishing \"{pkg}\".");
                DotNetCoreNuGetPush(pkg.FullPath, pushSettings);
        }
    });
Task("Default")
    .IsDependentOn("Run-Unit-Tests");

RunTarget(target);
