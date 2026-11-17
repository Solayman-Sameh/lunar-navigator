#define MyAppName "LunarNavigator"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "LunarNavigator Team"
#define MyAppURL "https://github.com/Solayman-Sameh/lunar-navigator"
#define MyAppExeName "LunarNavigator.exe"

#ifndef StagingDir
  #define StagingDir "..\Builds\Packaging\Staging"
#endif

[Setup]
AppId={{9B54A6F1-6E3C-4C67-9A1F-3D28E73FA441}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={localappdata}\Programs\{#MyAppName}
; Always offer the destination page, including upgrades/reinstalls.
DisableDirPage=no
AppendDefaultDirName=yes
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\Builds\Dist
OutputBaseFilename=LunarNavigator_Setup
SetupIconFile=..\Assets\LunarNavigator\Branding\LunarNavigator.ico
WizardImageFile=..\Assets\LunarNavigator\Branding\InstallerWelcome.bmp
WizardSmallImageFile=..\Assets\LunarNavigator\Branding\InstallerHeader.bmp
DisableWelcomePage=no
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
DiskSpanning=no
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.18362
CloseApplications=yes
RestartApplications=no
AppMutex=Local\LunarNavigator_SingleInstance_{#MyAppName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Root Launcher
Source: "{#StagingDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

; Unity 6 HDRP Standalone Player and supporting files
Source: "{#StagingDir}\bin\*"; DestDir: "{app}\bin"; Flags: ignoreversion recursesubdirs createallsubdirs

; Private CPython 3.11 Embedded Runtime, Scientific Backend & Vendor Libraries
Source: "{#StagingDir}\backend\*"; DestDir: "{app}\backend"; Flags: ignoreversion recursesubdirs createallsubdirs

; Third-Party Notices
Source: "{#StagingDir}\THIRD_PARTY_NOTICES.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
