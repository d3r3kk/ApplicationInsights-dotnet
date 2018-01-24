Param(
    # Path to sn.exe for x86 platforms. Default is to look for the WindowsSDK_ExecutablePath_x86 environment variable, or hardcode it to a known path.
    [Parameter(Mandatory=$false)]
    [string]
    $StrongNamePath86,
    # Path to sn.exe for x64 platforms. Default is to look for the WindowsSDK_ExecutablePath_x64 environment variable, or hardcode it to a known path.
    [Parameter(Mandatory=$false)]
    [string]
    $StrongNamePath64,
    # If Enable is in the parameter list, enable strong name validation for our assemblies. Otherwise disable it and allow testing unsigned assemblies.
    [Parameter(Mandatory=$false)]
    [switch]
    $Enable
)

if (!$StrongNamePath86)
{
    if (Test-Path -Path (Join-Path -Path $Env:WindowsSDK_ExecutablePath_x86 -ChildPath "sn.exe" -ErrorAction SilentlyContinue) -ErrorAction SilentlyContinue)
    {
        $StrongNamePath86 = (Join-Path -Path $Env:WindowsSDK_ExecutablePath_x86 -ChildPath "sn.exe")
    }
    else {
        $StrongNamePath86 = "C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.2 Tools\sn.exe"
    }
}
Write-Verbose "x86 sn.exe being used from '$StrongNamePath86'"

if (!$StrongNamePath64)
{
    if (Test-Path -Path (Join-Path -Path $Env:WindowsSDK_ExecutablePath_x64 -ChildPath "sn.exe" -ErrorAction SilentlyContinue) -ErrorAction SilentlyContinue)
    {
        $StrongNamePath64 = (Join-Path -Path $Env:WindowsSDK_ExecutablePath_x64 -ChildPath "sn.exe")
    }
    else 
    {
        $StrongNamePath64 = "C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.2 Tools\x64\sn.exe"
    }
}
Write-Verbose "x64 sn.exe being used from '$StrongNamePath64'"

$command = "-Vr"
if ($Enable)
{
    $command = "-Vu"
    Write-Verbose "Using command $command to enable strong name validation"
}
else 
{
    Write-Verbose "Using command $command to disable strong name validation"
}

& $StrongNamePath64 $command *,31bf3856ad364e35
& $StrongNamePath86 $command *,31bf3856ad364e35
