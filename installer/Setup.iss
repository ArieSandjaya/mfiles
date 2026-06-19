; Builds the MFiles Clone installer with Inno Setup (https://jrsoftware.org/isinfo.php).
;
; Expects MFilesClone and MFilesClone.ShellExtension to already be published (self-contained,
; win-x64) into ..\publish\MFilesClone relative to this script - see .github/workflows/build.yml
; for the exact `dotnet publish` invocations used in CI.
;
; NOTE: the Dokan kernel driver (required for the "Mount Drive M:" virtual file system feature)
; is NOT bundled here - it must be installed separately by the user from
; https://github.com/dokan-dev/dokany/releases. This installer only registers the Explorer
; "Save to MFiles Clone" context menu command and deploys the application files.

#define AppName "MFiles Clone"
#define AppVersion "1.0.0"
#define AppPublisher "MFiles Clone"
#define ShellExtensionClsid "{{d3e4a1c2-9b8a-4f7e-8b3a-2c1d5e6f7a8b}}"

[Setup]
AppId={{8F3E2B7A-4C1D-4E9B-9A52-1F2C3D4E5F60}}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\MFilesClone
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=Output
OutputBaseFilename=MFilesCloneSetup
Compression=lzma
SolidCompression=yes
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Files]
Source: "..\publish\MFilesClone\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Tasks]
Name: desktopicon; Description: "Buat shortcut di Desktop"; GroupDescription: "Shortcut tambahan:"

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\MFilesClone.exe"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\MFilesClone.exe"; Tasks: desktopicon

[Run]
Filename: "regsvr32.exe"; Parameters: "/s ""{app}\MFilesClone.ShellExtension.comhost.dll"""; Flags: runhidden; StatusMsg: "Mendaftarkan Explorer context menu..."
Filename: "reg.exe"; Parameters: "add ""HKLM\Software\Classes\*\shell\MFilesCloneSave"" /ve /d ""Save to MFiles Clone"" /f"; Flags: runhidden
Filename: "reg.exe"; Parameters: "add ""HKLM\Software\Classes\*\shell\MFilesCloneSave"" /v ExplorerCommandHandler /d ""{#ShellExtensionClsid}"" /f"; Flags: runhidden
Filename: "{app}\MFilesClone.exe"; Description: "Jalankan {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "reg.exe"; Parameters: "delete ""HKLM\Software\Classes\*\shell\MFilesCloneSave"" /f"; Flags: runhidden
Filename: "regsvr32.exe"; Parameters: "/u /s ""{app}\MFilesClone.ShellExtension.comhost.dll"""; Flags: runhidden
