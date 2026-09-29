; Inno Setup script for Vardiologio.
; Packs the self-contained publish folder into a single Setup .exe.
; The database lives in %LOCALAPPDATA%\Vardiologio and is NOT touched by
; install, upgrade or uninstall: user data survives every reinstall.

#define MyAppName "Βαρδιολόγιο"
#define MyAppVersion "0.6.0"                     ; bump on every build you hand out
#define MyAppExeName "Vardiologio.App.exe"
#define PublishDir "..\publish\Vardiologio"      ; output of dotnet publish (step 1)

[Setup]
; Unique id of the product. Generate once (Tools > Generate GUID) and NEVER change it,
; otherwise upgrades install side by side instead of replacing the old version.
AppId={{PUT-YOUR-GUID-HERE}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=Nikos Xaplanteris
; Per-user install: works without administrator rights on office PCs.
PrivilegesRequired=lowest
DefaultDirName={autopf}\Vardiologio
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=output
OutputBaseFilename=Vardiologio-Setup-{#MyAppVersion}
SetupIconFile=..\Vardiologio.App\Assets\Vardiologio.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
; 64-bit only, matching the win-x64 publish.
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Tasks]
; Optional desktop shortcut, offered as a checkbox during setup.
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; Whole publish folder, including wwwroot and subfolders.
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Offer to launch the app at the end of setup.
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent