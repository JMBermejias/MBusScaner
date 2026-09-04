; HVAC Bus Scanner - Instalador Inno Setup
; Desarrollador: Jose Manuel Bernabeu Mejias
; Licencia: GPL v3

#define MyAppName "HVAC Bus Scanner"
#define MyAppVersion "1.0.3"
#define MyAppPublisher "Jose Manuel Bernabeu Mejias"
#define MyAppURL "https://github.com/jmbernabeu/MBusScaner"
#define MyAppExeName "MBusScaner.exe"

[Setup]
AppId={{8E5C8D2E-7A3B-4C1A-B2D4-9F3E1A5C7B21}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={localappdata}\Programs\HVACBusScanner
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputBaseFilename=MBusScaner-Setup
OutputDir=Instalador
SetupIconFile=..\MBusScaner\MBusScaner.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
Uninstallable=yes
CreateAppDir=yes
ChangesAssociations=no
CloseApplications=yes

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear acceso directo en el escritorio"; GroupDescription: "Accesos directos:"
Name: "startmenuicon"; Description: "Crear carpeta en el menú Inicio"; GroupDescription: "Accesos directos:"

[Files]
Source: "..\publish\MBusScaner.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion isreadme
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startmenuicon
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\MBusScaner"