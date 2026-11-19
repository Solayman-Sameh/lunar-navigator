#define MyAppName "LunarNavigator"
#define MyAppVersion "1.1.1"
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
#ifdef IsolatedTest
; Test-only build: do not alter Windows registration or user shortcuts.
CreateUninstallRegKey=no
Compression=none
SolidCompression=no
#else
Compression=lzma2/ultra64
SolidCompression=yes
#endif
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

; Ownership marker checked before generated-file cleanup during uninstall
Source: "installation-marker.txt"; DestDir: "{app}"; Flags: ignoreversion

; Third-Party Notices
Source: "{#StagingDir}\THIRD_PARTY_NOTICES.txt"; DestDir: "{app}"; Flags: ignoreversion

#ifndef IsolatedTest
[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

#endif

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; These two folders are reserved for application files and generated runtime caches.
; Never recurse over {app} itself: it may contain unrelated user documents.
Type: dirifempty; Name: "{app}"

[Code]
var
  OwnedInstall: Boolean;

procedure CaptureInstallOwnership;
var
  Marker: AnsiString;
  InstallRoot: String;
begin
  InstallRoot := RemoveBackslashUnlessRoot(ExpandConstant('{app}'));
  OwnedInstall := False;
  if LoadStringFromFile(InstallRoot + '\installation-marker.txt', Marker) then
    OwnedInstall := (Trim(String(Marker)) = 'LunarNavigator-owned-installation-v1') and
      (Length(InstallRoot) > 3) and
      FileExists(InstallRoot + '\backend\run_backend.py') and
      FileExists(InstallRoot + '\backend\python.exe');
  Log(Format('Owned installation cleanup enabled: %d', [Ord(OwnedInstall)]));
end;

function InitializeUninstall: Boolean;
begin
  Result := not CheckForMutexes('Local\LunarNavigator_SingleInstance_LunarNavigator');
  if not Result then
    MsgBox('Close LunarNavigator before uninstalling. Its launcher will stop the scientific backend.', mbError, MB_OK);
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  { Verify ownership and clean in the same callback, before installed files and }
  { the marker are removed. Never recurse over the user-selected app root. }
  if CurUninstallStep = usUninstall then
  begin
    CaptureInstallOwnership;
    if OwnedInstall then
    begin
      Log('Removing owned backend/player folders, including runtime leftovers.');
      if not DelTree(ExpandConstant('{app}\backend'), True, True, True) then
        Log('Some owned backend files could not be removed.');
      if not DelTree(ExpandConstant('{app}\bin'), True, True, True) then
        Log('Some owned player files could not be removed.');
    end;
  end;
end;
