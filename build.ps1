param(
    [string]$packageVersion = $null,
    [string]$configuration = "Release"
)

. ".\build-common.ps1"

$solutionName = "NLog.Targets.Loggly"
$sourceUrl = "https://github.com/neutmute/nlog-targets-loggly"

function init {
    # Initialization
    $global:rootFolder = Split-Path -parent $script:MyInvocation.MyCommand.Path
    $global:rootFolder = Join-Path $rootFolder .
    $global:packagesFolder = Join-Path $rootFolder packages
    $global:outputFolder = Join-Path $rootFolder _output

    # Test for AppVeyor config
    if(!(Test-Path Env:\PackageVersion )){
        $env:PackageVersion = $env:APPVEYOR_BUILD_VERSION
    }
    
    # Default when no env vars
    if(!(Test-Path Env:\PackageVersion )){
        $env:PackageVersion = "1.0.0.0"
    }
    
    _WriteOut -ForegroundColor $ColorScheme.Banner "-= $solutionName Build =-"
    _WriteConfig "rootFolder" $rootFolder
    _WriteConfig "version" $env:PackageVersion
}

function restorePackages{
    _WriteOut -ForegroundColor $ColorScheme.Banner "nuget restore"
    
    New-Item -Force -ItemType directory -Path $packagesFolder
    _DownloadNuget $packagesFolder
    nuget restore
    #nuget install gitlink -SolutionDir "$rootFolder" -ExcludeVersion

	dotnet restore "$rootFolder\src\$solutionName"

	checkExitCode
}

function nugetPack{
    _WriteOut -ForegroundColor $ColorScheme.Banner "Nuget pack"
    

    if(!(Test-Path Env:\nuget )){
        $env:nuget = nuget
    }

	dotnet pack "$rootFolder\src\$solutionName" --configuration $configuration --include-symbols --no-build --no-restore --output $outputFolder

	checkExitCode
}

function nugetPublish{

    if($env:APPVEYOR_PULL_REQUEST_NUMBER){
		_WriteOut -ForegroundColor Yellow "Pull Request Build Detected. Skipping Publish"
	}
	else{
		if(Test-Path Env:\nugetapikey ){
	        _WriteOut -ForegroundColor $ColorScheme.Banner "Nuget publish..."
		    &nuget push $outputFolder\* -ApiKey "$env:nugetapikey" -source https://www.nuget.org
		}
		else{
			_WriteOut -ForegroundColor Yellow "nugetapikey environment variable not detected. Skipping nuget publish"
		}
	}
}

function buildSolution{
    _WriteOut -ForegroundColor $ColorScheme.Banner "Build Solution"
	    
	New-Item -Force -ItemType directory -Path $outputFolder

    dotnet build "$rootFolder\$solutionName.sln" --configuration $configuration -p:PackageVersion=$env:PackageVersion

	checkExitCode
}


function executeTests{

	_WriteOut -ForegroundColor $ColorScheme.Banner "Execute Tests"

	dotnet test $rootFolder\test\NLog.Targets.Loggly.Tests

    checkExitCode
}

init

restorePackages

buildSolution

executeTests

nugetPack

nugetPublish

Write-Host "Build $env:PackageVersion complete"