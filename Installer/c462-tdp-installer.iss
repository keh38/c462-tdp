; -- sync.iss --

; SEE THE DOCUMENTATION FOR DETAILS ON CREATING .ISS SCRIPT FILES!
#define SemanticVersion() \
   GetVersionComponents("..\TapDevPlatform\bin\Release\net8.0-windows\TapDevPlatform.exe", Local[0], Local[1], Local[2], Local[3]), \
   Str(Local[0]) + "." + Str(Local[1]) + ((Local[2]>0) ? "." + Str(Local[2]) : "")
    

#define verStr_ StringChange(SemanticVersion(), '.', '-')
#define EplFolderRoot GetEnv('EPL_FOLDER_ROOT')

[Setup]
AppName=Tap Pattern Development Platform
AppVerName=Game Editor V{#SemanticVersion()}
DefaultDirName={commonpf}\EPL\C462\TDP\V{#SemanticVersion()}
OutputDir=Output
DefaultGroupName=EPL
AllowNoIcons=yes
OutputBaseFilename=TDP_{#verStr_}
UsePreviousAppDir=no
UsePreviousGroup=no
UsePreviousSetupType=no
DisableProgramGroupPage=yes
PrivilegesRequired=admin

[Dirs]
Name: "{#EplFolderRoot}\EPL";

[Files]
Source: "..\TapDevPlatform\bin\Release\net8.0-windows\*.*"; DestDir: "{app}"; Flags: replacesameversion;
Source: "..\TapDevPlatform\bin\Release\net8.0-windows\*.*"; DestDir: "{app}"; Flags: replacesameversion
Source: "..\TapDevPlatform\bin\Release\net8.0-windows\runtimes\win\*.*"; DestDir: "{app}\runtimes\win"; Flags: replacesameversion recursesubdirs
Source: "..\TapDevPlatform\bin\Release\net8.0-windows\runtimes\win-arm64\*.*"; DestDir: "{app}\runtimes\win-arm64"; Flags: replacesameversion recursesubdirs
Source: "..\TapDevPlatform\bin\Release\net8.0-windows\runtimes\win-x64\*.*"; DestDir: "{app}\runtimes\win-x64"; Flags: replacesameversion recursesubdirs
Source: "..\TapDevPlatform\bin\Release\net8.0-windows\runtimes\win-x86\*.*"; DestDir: "{app}\runtimes\win-x86"; Flags: replacesameversion recursesubdirs
Source: "..\TapDevPlatform\Images\*.*"; DestDir: "{app}"; Flags: replacesameversion;
Source: "..\CHANGELOG.md"; DestDir: "{app}"; Flags: replacesameversion;
Source: "..\Context\*.*"; DestDir: "{#EplFolderRoot}\Context"; Flags: replacesameversion
Source: "..\Help\*.*"; DestDir: "{#EplFolderRoot}\Help"; Flags: replacesameversion
Source: "..\MATLAB\*.*"; DestDir: "{#EplFolderRoot}\Help"; Flags: replacesameversion recursesubdirs

;[Icons]
;Name: "{commondesktop}\Game Editor"; Filename: "{app}\game-editor.exe"; IconFilename: "{app}\note_edit.ico"; IconIndex: 0;

[Registry]
Root: HKLM64; Subkey: "Software\EPL"; Flags: uninsdeletekeyifempty
Root: HKLM64; Subkey: "Software\EPL\C462"; Flags: uninsdeletekey
Root: HKLM64; Subkey: "Software\EPL\C462\TDP"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"

