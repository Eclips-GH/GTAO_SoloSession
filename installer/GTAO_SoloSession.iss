#define AppName "GTAO Solo Session"
#define AppExeName "GTAO_SoloSession.exe"
#define AppVersion "1.0.0"
#define AppPublisher "Eclips-GH"
#define AppURL "https://github.com/Eclips-GH/GTAO_SoloSession"

[Setup]
AppId={{C6E6A7C1-0E22-4C5D-9C6F-8C92B28D3A8E}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
OutputDir=output
OutputBaseFilename=GTAO_SoloSession_Setup_v{#AppVersion}
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
WizardStyle=modern
DisableProgramGroupPage=yes

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "Créer une icône sur le bureau"; GroupDescription: "Raccourcis:"; Flags: unchecked

[Files]
; --- Ton app publiée ---
Source: "..\GTAO_SoloSession\bin\Release\net8.0-windows\win-x64\publish\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\GTAO_SoloSession\bin\Release\net8.0-windows\win-x64\publish\assets\*"; DestDir: "{app}\assets"; Flags: ignoreversion recursesubdirs createallsubdirs

; --- Installer du .NET Desktop Runtime (tu mets le bon nom de fichier ici) ---
Source: "dotnet-sdk-10.0.102-win-x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{tmp}\dotnet-sdk-10.0.102-win-x64.exe"; Parameters: "/install /quiet /norestart"; StatusMsg: "Installation de .NET Desktop Runtime..."; Flags: waituntilterminated; Check: NeedsDesktopRuntime
Filename: "{app}\{#AppExeName}"; Description: "Lancer {#AppName}"; Flags: nowait postinstall skipifsilent

[Code]
function IsDesktopRuntimeInstalled(): Boolean;
var
  key: string;
  version: string;
begin
  Result := False;

  // .NET Desktop Runtime (WindowsDesktop) x64
  key := 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App';

  // La valeur "Version" existe si le runtime desktop est installé
  if RegQueryStringValue(HKLM, key, 'Version', version) then
  begin
    // Si tu veux forcer .NET 8 uniquement, tu peux vérifier StartsText('8.', version)
    Result := (Length(version) > 0);
  end;
end;

function NeedsDesktopRuntime(): Boolean;
begin
  Result := not IsDesktopRuntimeInstalled();
end;
