#define MyAppName "ExtrabbitCode.Inventor.Attributes"
#ifndef MyAppVersion
  #define MyAppVersion "0.0.0.1"
#endif
#define MyAppPublisher "ExtrabbitCode"
#define MyAppURL ""

[Setup]
 
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{58C9D72B-AD94-4C18-9D6E-D00892F7EB21}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName=C:\ProgramData\ExtrabbitCode\{#MyAppName}
DisableDirPage=yes
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile={#SourcePath}License.txt
; Uncomment the following line to run in non administrative install mode (install for current user only.)
;PrivilegesRequired=lowest
OutputDir={#SourcePath}
OutputBaseFilename=ExtrabbitCode.Inventor.Attributes_{#MyAppVersion}
SetupIconFile={#SourcePath}app.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\Addin\ExtrabbitCode.Inventor.Attributes.addin"; DestDir: "C:\ProgramData\Autodesk\Inventor Addins";  Flags: ignoreversion
Source: "..\bin\x64\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

