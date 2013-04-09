; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "TallyProcessor"
#define MyAppVersion "0.1 alpha 1"
#define MyAppURL "http://development.diekkamp.de/"
#define MyAppPublisher "Christopher Diekkamp"
#define MyAppExeName "TallyProcessor.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{3113F901-39D1-44E7-A7CE-F682CCDC9302}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=C:\Daten\Programmieren\TallyProcessor\SetupBuilds
LicenseFile=C:\Daten\Programmieren\TallyProcessor\WorkingDir\gpl-3.0.txt
OutputBaseFilename=setup
Compression=lzma
SolidCompression=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "deutsch"; MessagesFile: "compiler:Languages\German.isl"

[Components]
Name: "main"; Description: "TallyProcessor's core files"; Types: full compact custom;Flags: fixed
Name: "conf"; Description: "Example Configurations"; Types: full custom;Flags: checkablealone
Name: "conf\empty"; Description: "Just install examples, don't set one active";Types: full custom; Flags: exclusive
Name: "conf\net"; Description: "Start with a simpleNetwork device configured"; Flags: exclusive
Name: "conf\atem"; Description: "Start with an atem device configured"; Flags: exclusive
Name: "conf\all"; Description: "Start with a configuration containing all devices"; Flags: exclusive
Name: "bin"; Description: "LIBs and DLLs of devices";Types: full custom;
Name: "src"; Description: "TallyProcessor source code as VB2010.NET Project"; Types: full custom

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 0,6.1

[Dirs]
Name: "{app}\conf"
Name: "{app}\log"

[Files]
; BIN
Source: "C:\Daten\Programmieren\TallyProcessor\TallyProcessor\ConsoleApplication1\bin\Release\TallyProcessor.exe"; DestDir: "{app}";  Components:main;Flags: ignoreversion
Source: "C:\Daten\Programmieren\USB Interface Card\DLL_v4.0.0.0\K8055D.dll"; DestDir: "{app}";  Components:bin;Flags: ignoreversion

; CONFIG copy
Source: "C:\Daten\Programmieren\TallyProcessor\WorkingDir\config.xsd"; DestDir: "{app}"; Components:main; Flags: ignoreversion; Attribs: hidden
Source: "C:\Daten\Programmieren\TallyProcessor\WorkingDir\conf\config.empty.xml"; DestDir: "{app}\conf"; Components:main;Flags: ignoreversion
Source: "C:\Daten\Programmieren\TallyProcessor\WorkingDir\conf\config.example.xml"; DestDir: "{app}\conf";Components:conf; Flags: ignoreversion
Source: "C:\Daten\Programmieren\TallyProcessor\WorkingDir\conf\config.simpleNetwork.xml"; DestDir: "{app}\conf"; Components:conf;Flags: ignoreversion
Source: "C:\Daten\Programmieren\TallyProcessor\WorkingDir\conf\config.atem.xml"; DestDir: "{app}\conf"; Components:conf;Flags: ignoreversion

;CONFIG Set
Source: "C:\Daten\Programmieren\TallyProcessor\WorkingDir\conf\config.empty.xml"; DestDir: "{app}\conf"; DestName: "config.xml";Components:main conf\empty;  Flags: onlyifdoesntexist
Source: "C:\Daten\Programmieren\TallyProcessor\WorkingDir\conf\config.simpleNetwork.xml"; DestDir: "{app}\conf"; DestName: "config.xml";Components:conf\net
Source: "C:\Daten\Programmieren\TallyProcessor\WorkingDir\conf\config.atem.xml"; DestDir: "{app}\conf"; DestName: "config.xml";Components:conf\atem
Source: "C:\Daten\Programmieren\TallyProcessor\WorkingDir\conf\config.example.xml"; DestDir: "{app}\conf"; DestName: "config.xml";Components:conf\all

; LICENSE
Source: "C:\Daten\Programmieren\TallyProcessor\WorkingDir\gpl-3.0.txt"; DestDir: "{app}"; Components:main; 

; MANUAL
Source: "C:\Daten\Programmieren\TallyProcessor\WorkingDir\readme.txt"; DestDir: "{app}";  Components:main;Flags: isreadme
Source: "C:\Daten\Programmieren\TallyProcessor\WorkingDir\developer.txt"; DestDir: "{app}"; Components:main

;SOURCE
Source: "C:\Daten\Programmieren\TallyProcessor\TallyProcessor_src_VB2010_v0.1alpaha.zip"; destDir: "{app}\src\";Components:src

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Components:main
Name: "{group}\readme.txt"; Filename: "{app}\readme.txt"; Components:main
Name: "{group}\developer.txt"; Filename: "{app}\developer.txt" ; Components:main
Name: "{group}\licence.txt"; Filename: "{app}\gpl-3.0.txt" ; Components:main
Name: "{group}\Examples\config.example.xml"; Filename: "{app}\conf\config.example.xml"; Components:conf
Name: "{group}\Examples\config.simpleNetwork.xml"; Filename: "{app}\conf\config.simpleNetwork.xml"; Components:conf
Name: "{group}\Examples\config.atem.xml"; Filename: "{app}\conf\config.atem.xml"; Components:conf
Name: "{group}\Open config folder"; Filename: "{app}\conf"; Components:main
Name: "{group}\Remove TallyProcessor"; Filename: "{uninstallexe}"; Components:main
Name: "{group}\TallyProcessor source code"; Filename: "{app}\src\TallyProcessor_src_VB2010_v0.1alpaha.zip"; Components: src
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Components:main;Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[code]
function IsDotNetDetected(version: string; service: cardinal): boolean;
// Indicates whether the specified version and service pack of the .NET Framework is installed.
//
// version -- Specify one of these strings for the required .NET Framework version:
//    'v1.1.4322'     .NET Framework 1.1
//    'v2.0.50727'    .NET Framework 2.0
//    'v3.0'          .NET Framework 3.0
//    'v3.5'          .NET Framework 3.5
//    'v4\Client'     .NET Framework 4.0 Client Profile
//    'v4\Full'       .NET Framework 4.0 Full Installation
//
// service -- Specify any non-negative integer for the required service pack level:
//    0               No service packs required
//    1, 2, etc.      Service pack 1, 2, etc. required
var
    key: string;
    install, serviceCount: cardinal;
    success: boolean;
begin
    key := 'SOFTWARE\Microsoft\NET Framework Setup\NDP\' + version;
    // .NET 3.0 uses value InstallSuccess in subkey Setup
    if Pos('v3.0', version) = 1 then begin
        success := RegQueryDWordValue(HKLM, key + '\Setup', 'InstallSuccess', install);
    end else begin
        success := RegQueryDWordValue(HKLM, key, 'Install', install);
    end;
    // .NET 4.0 uses value Servicing instead of SP
    if Pos('v4', version) = 1 then begin
        success := success and RegQueryDWordValue(HKLM, key, 'Servicing', serviceCount);
    end else begin
        success := success and RegQueryDWordValue(HKLM, key, 'SP', serviceCount);
    end;
    result := success and (install = 1) and (serviceCount >= service);
end;

function InitializeSetup(): Boolean;
begin
    if not IsDotNetDetected('v4\Client', 0) then begin
        MsgBox('{#MyAppExeName} requires Microsoft .NET Framework 4.0 Client Profile.'#13#13
            'Please use Windows Update to install this version,'#13
            'and then re-run the {#MyAppExeName} setup program.', mbInformation, MB_OK);
        result := false;
    end else
        result := true;
end;