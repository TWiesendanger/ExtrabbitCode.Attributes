#define MyAppName "ExtrabbitCode.Inventor.Attributes"
#define MyAppVersion "0.0.0.1"
#define MyAppPublisher "Company"
#define MyAppURL "http://www.company.com"

[Setup]
 
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{GUID}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName=C:\ProgramData\{#MyAppName}
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

