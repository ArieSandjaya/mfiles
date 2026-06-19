#Requires -RunAsAdministrator
<#
Removes the "Save to MFiles Clone" Explorer context menu command.
#>
param(
    [string]$ComHostPath = (Join-Path $PSScriptRoot "..\bin\Release\net8.0-windows\MFilesClone.ShellExtension.comhost.dll")
)

Remove-Item -Path "Registry::HKEY_CLASSES_ROOT\*\shell\MFilesCloneSave" -Recurse -Force -ErrorAction SilentlyContinue
regsvr32.exe /u /s $ComHostPath

Write-Host "Shell extension unregistered."
