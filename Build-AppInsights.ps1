<#
.SYNOPSIS
    Build the ApplicationInsights SDK locally

.DESCRIPTION
    Build the Application Insights SDK locally, using the closest approximation possible to how the build machines
    build the SDK.

.EXAMPLE
    PS C:\> Build-AppInsights -Config Release
    Build the release version of the SDK locally.
#>
Param(
    # Configuration to build, default is 'Debug'
    [Parameter(Mandatory=$false)]
    [string]
    $Config="Debug",
    # Platform to build. This is usually best left to the default of 'Mixed Platforms'.
    [Parameter(Mandatory=$false)]
    [string]
    $Platform="Mixed Platforms",
    # Path to the Microsoft.ApplicationInsights.sln solution file. Default is ./Microsoft.ApplicationInsights.sln
    [Parameter(Mandatory=$false)]
    [string]
    $SolutionPath="./Microsoft.ApplicationInsights.sln",
    # Path to the nuget executable. Defaults to the output of Get-Command nuget.
    [Parameter(Mandatory=$false)]
    [string]
    $NugetPath,
    # Timeout for resolving project-to-project references during Nuget restore operation. Default is 300.
    [Parameter(Mandatory=$false)]
    [int]
    $NugetProjectResolveTimeout=300,
    # Verbosity of the nuget command. Default is Detailed.
    [Parameter(Mandatory=$false)]
    [string]
    $NugetVerbosity="Detailed",
    # Other nuget command-line options not supplied here
    [Parameter(Mandatory=$false)]
    [string]
    $OtherNugetOptions,
    # Path to msbuild. Default is wherever 'Get-Command msbuild' points to.
    [Parameter(Mandatory=$false)]
    [string]
    $MsbuildPath,
    # Is this build to represent a 'stable' build release? Default is false.
    [Parameter(Mandatory=$false)]
    [bool]
    $IsStableRelease=$false,
    # Is this build to represent a 'public' build release? Default is true.
    [Parameter(Mandatory=$false)]
    [bool]
    $IsPublicRelease=$true,
    # Logfile name for the msbuild log. Default is bld-[username]-[Config].log
    [Parameter(Mandatory=$false)]
    [string]
    $BuildLogFile
)

if (!$NugetPath)
{
    $NugetPath = (Get-Command -Name nuget).Source
}
Write-Verbose "Using nuget at '$NugetPath'"

$solution = Resolve-Path -Path $SolutionPath
Write-Verbose "Using solution '$solution'"

if (!$MsbuildPath)
{
    $MsbuildPath = (Get-Command -Name msbuild).Source
}
Write-Verbose "Using msbuild  from '$MsbuildPath'"

if (!$BuildLogFile)
{
    $BuildLogFile = "bld-$($Env:USERNAME)-$Config.log"
}
Write-Verbose "Build log file is '$BuildLogFile'"

& $NugetPath restore -Project2ProjectTimeOut $NugetProjectResolveTimeout -Verbosity $NugetVerbosity $OtherNugetOptions $solution
& $MsbuildPath $solution /p:StableRelease=$IsStableRelease /p:PublicRelease=$IsPublicRelease /m /nologo "/flp:logfile=$BuildLogFile;verbosity=diagnostic"
