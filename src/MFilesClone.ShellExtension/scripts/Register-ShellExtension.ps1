#Requires -RunAsAdministrator
<#
Registers the "Save to MFiles Clone" Explorer context menu command.
Run this manually, with an elevated PowerShell prompt, after building the
MFilesClone.ShellExtension project in Release mode.
#>
param(
    [string]$ComHostPath = (Join-Path $PSScriptRoot "..\bin\Release\net8.0-windows\MFilesClone.ShellExtension.comhost.dll"),
    [string]$CommandClsid = "d3e4a1c2-9b8a-4f7e-8b3a-2c1d5e6f7a8b"
)

if (-not (Test-Path $ComHostPath)) {
    throw "ComHost DLL not found at '$ComHostPath'. Build the project in Release mode first."
}

regsvr32.exe /s $ComHostPath

$verbKey = "Registry::HKEY_CLASSES_ROOT\*\shell\MFilesCloneSave"
New-Item -Path $verbKey -Force | Out-Null
Set-ItemProperty -Path $verbKey -Name "(Default)" -Value "Save to MFiles Clone"
Set-ItemProperty -Path $verbKey -Name "ExplorerCommandHandler" -Value "{$CommandClsid}"

Write-Host "Shell extension registered. Restart Explorer (or sign out/in) to see the new context menu entry."
