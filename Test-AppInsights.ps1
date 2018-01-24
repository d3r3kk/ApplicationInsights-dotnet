<#
.SYNOPSIS
Run unit tests against the local build.

.DESCRIPTION
Run unit tests against the local build and report them as the build machine would. The goal is to run
the tests as nearly to how the build machine does as possible.

.EXAMPLE
PS C:\> Test-AppInsights -Config Debug -ResultDir C:\Temp\TestOutput\
Run the tests against the debug configuration, and place the results into a specific directory
#>
Param(
	# The path to the bin/ output folder from the build (without the configuration part). Defaults to ../bin/.
	[Parameter(Mandatory=$false)]
	[string]
	$BinRootDir="..\bin",
	# The path to the intermediate folder from the build. Defaults to ../obj/.
	[Parameter(Mandatory=$false)]
	[string]
	$ObjRootDir="..\obj",
	# Which Configuration do you want to test? Choices are Debug, Release. Default is Debug.
	[Parameter(Mandatory=$false)]
	[string]
	$Config="Debug",
	# Where to store the test results. Defaults to environment variable 'COMMON_TESTRESULTSDIRECTORY\[Config]' if it is present, or to [BinRootDir]/[Config]/TestResults/.
	[Parameter(Mandatory=$false)]
	[string]
	$ResultDir,
	# Path to the constructed command-file for vstest to be run with. Defaults to [ObjRoot]/Testrun/[Config]/test.command.
	[Parameter(Mandatory=$false)]
	[string]
	$CommandFilePath,
	# Path to vstest.console.exe. Default is to find the first one on the PATH (via the Get-Command cmdlet).
	[Parameter(Mandatory=$false)]
	[string]
	$VsTestPath,
	# Assembly name patterns to exclude when performing .NET Full Framework tests.
	[Parameter(Mandatory=$false)]
	[string]
	$ExcludeNamesNetFramework="*netcoreapp*,*NetCore*",
	# Assembly name patterns to exclude when performing .NET Core tests.
	[Parameter(Mandatory=$false)]
	[string]
	$ExcludeNamesNetCore,
	# Include filter for .NET Full Framework assemblies
	[Parameter(Mandatory=$false)]
	[string]
	$IncludeFilterNetFramework="*.Tests.dll",
	# Include filter for .NET Core assemblies
	[Parameter(Mandatory=$false)]
	[string]
	$IncludeFilterNetCore="*.NetCore*.Tests.dll"
)

if (!$VsTestPath)
{
	$VsTestPath = (Get-Command "vstest.console.exe").Source
}
Write-Verbose "Using specified vstest path: '$VsTestPath'"

$BinRoot = Join-Path -Path (Resolve-Path -Path $BinRootDir) -ChildPath $Config
$ObjRoot = Resolve-Path -Path $ObjRootDir
Write-Verbose "BinRoot resolved to: $BinRoot"
Write-Verbose "ObjRoot resolved to: $ObjRoot"

if (!$ResultDir)
{
	$ResultDir = Join-Path -Path $BinRoot -ChildPath "TestResults"
}
Write-Verbose "Using result folder '$ResultDir' to write test results into"

if (!$CommandFilePath)
{
	$testCmdPath = Join-Path -Path $ObjRoot -ChildPath "Testrun/$Config"
	New-Item -ItemType Directory -Path $testCmdPath -Force | Out-Null
	$CommandFilePath = Join-Path -Path $testCmdPath -ChildPath "test.command"
}
$CommandFilePathCore = "$($CommandFilePath).netcore"

Write-Verbose "Writing command file for .NET Full Framework tests to $CommandFilePath"
Write-Verbose "Writing command file for .NET Core tests to $CommandFilePathCore"

$testAssemblies = (Get-ChildItem -Recurse -Filter $IncludeFilterNetFramework -Exclude $ExcludeNamesNetFramework -Path $BinRoot -File) -join " "
$testAssembliesCore = (Get-ChildItem -Recurse -Filter $IncludeFilterNetCore -Exclude $ExcludeNamesNetCore -Path $BinRoot -File) -join " "

$testAssemblies | Out-File -PSPath $CommandFilePath -Encoding utf8 -Force
"/InIsolation" | Out-File -PSPath $CommandFilePath -Encoding utf8 -Append
"/TestCaseFilter:""Name!=ActiveInitializesSingleInstanceWhenConfigurationComponentsAccessActiveRecursively|Name!=AppInsightsDllCouldRunStandalone""" | Out-File -PSPath $CommandFilePath -Encoding utf8 -Append
"/ResultsDirectory:$ResultDir" | Out-File -PSPath $CommandFilePath -Encoding utf8 -Append

& $VsTestPath @$CommandFilePath

$testAssembliesCore | Out-File -PSPath $CommandFilePathCore -Encoding utf8 -Force
"/InIsolation" | Out-File -PSPath $CommandFilePathCore -Encoding utf8 -Append
"/TestCaseFilter:""Name!=ActiveInitializesSingleInstanceWhenConfigurationComponentsAccessActiveRecursively|Name!=AppInsightsDllCouldRunStandalone""" | Out-File -PSPath $CommandFilePathCore -Encoding utf8 -Append
"/ResultsDirectory:$ResultDir" | Out-File -PSPath $CommandFilePathCore -Encoding utf8 -Append
"/Framework:FrameworkCore10" | Out-File -PSPath $CommandFilePathCore -Encoding utf8 -Append

& $VsTestPath @$CommandFilePathCore
