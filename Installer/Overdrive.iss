#ifndef ReleaseDir
  #error Define ReleaseDir as the folder containing the built app and release files.
#endif
#define AppVersion "0.1.0"
[Setup]
AppId={{A1E11E98-2292-4A15-BD61-625C856B0B57}
AppName=Overdrive
AppVersion={#AppVersion}
AppPublisher=Suleiman Alnsour
AppPublisherURL=https://suleimanalnsour.me
AppSupportURL=https://optimize.suleimanalnsour.me
AppUpdatesURL=https://optimize.suleimanalnsour.me
DefaultDirName={localappdata}\Programs\Overdrive
DefaultGroupName=Overdrive
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
OutputDir={#ReleaseDir}
OutputBaseFilename=Overdrive-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
LicenseFile={#ReleaseDir}\LICENSE
UninstallDisplayName=Overdrive
CloseApplications=yes
RestartApplications=no
[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked
[Files]
Source: "{#ReleaseDir}\Overdrive.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#ReleaseDir}\Overdrive.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#ReleaseDir}\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#ReleaseDir}\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#ReleaseDir}\THIRD-PARTY-NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#ReleaseDir}\licenses\*"; DestDir: "{app}\licenses"; Flags: ignoreversion recursesubdirs
Source: "{#ReleaseDir}\Electric-Gaze.json"; DestDir: "{app}"; Flags: ignoreversion
[Icons]
Name: "{userprograms}\Overdrive"; Filename: "{app}\Overdrive.exe"
Name: "{userdesktop}\Overdrive"; Filename: "{app}\Overdrive.exe"; Tasks: desktopicon
[Run]
Filename: "{app}\Overdrive.exe"; Description: "Open Overdrive"; Flags: nowait postinstall skipifsilent
[Code]
function InitializeSetup(): Boolean;
begin
  Result := IsDotNetInstalled(net48, 0);
  if not Result then
    MsgBox('Overdrive needs Microsoft .NET Framework 4.8. Install it from Microsoft, then run setup again.', mbInformation, MB_OK);
end;
