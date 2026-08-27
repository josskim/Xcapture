#define MyAppName "XCapture"
#define MyAppVersion "1.0.17"
#define MyAppPublisher "XCapture"
#define MyAppExeName "XCapture.exe"
#define PublishDir "..\publish-latest"
#define IconFile "..\Assets\app.ico"

[Setup]
AppId={{A4E9B623-67D5-4C24-8E1B-B39F5159FDD4}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=.
OutputBaseFilename=XCaptureSetup
SetupIconFile={#IconFile}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
CloseApplications=yes
RestartApplications=no
ChangesAssociations=yes
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"
Name: "startup"; Description: "Run XCapture when Windows starts"; GroupDescription: "Additional options:"
Name: "imageviewer"; Description: "이미지 파일 연결 프로그램과 우클릭 메뉴에 XCapture 등록"; GroupDescription: "이미지 뷰어:"; Flags: checkedonce

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startup

[Registry]
Root: HKCU; Subkey: "Software\Classes\XCapture.Image"; ValueType: string; ValueName: ""; ValueData: "XCapture 이미지"; Tasks: imageviewer; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\XCapture.Image\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"; Tasks: imageviewer
Root: HKCU; Subkey: "Software\Classes\XCapture.Image\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Tasks: imageviewer
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}"; ValueType: string; ValueName: "FriendlyAppName"; ValueData: "XCapture 이미지 뷰어"; Tasks: imageviewer; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"; Tasks: imageviewer
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Tasks: imageviewer
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".png"; ValueData: ""; Tasks: imageviewer
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".jpg"; ValueData: ""; Tasks: imageviewer
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".jpeg"; ValueData: ""; Tasks: imageviewer
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".bmp"; ValueData: ""; Tasks: imageviewer
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".gif"; ValueData: ""; Tasks: imageviewer
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".tif"; ValueData: ""; Tasks: imageviewer
Root: HKCU; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".tiff"; ValueData: ""; Tasks: imageviewer
Root: HKCU; Subkey: "Software\Classes\.png\OpenWithProgids"; ValueType: string; ValueName: "XCapture.Image"; ValueData: ""; Tasks: imageviewer; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.jpg\OpenWithProgids"; ValueType: string; ValueName: "XCapture.Image"; ValueData: ""; Tasks: imageviewer; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.jpeg\OpenWithProgids"; ValueType: string; ValueName: "XCapture.Image"; ValueData: ""; Tasks: imageviewer; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.bmp\OpenWithProgids"; ValueType: string; ValueName: "XCapture.Image"; ValueData: ""; Tasks: imageviewer; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.gif\OpenWithProgids"; ValueType: string; ValueName: "XCapture.Image"; ValueData: ""; Tasks: imageviewer; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.tif\OpenWithProgids"; ValueType: string; ValueName: "XCapture.Image"; ValueData: ""; Tasks: imageviewer; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.tiff\OpenWithProgids"; ValueType: string; ValueName: "XCapture.Image"; ValueData: ""; Tasks: imageviewer; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\image\shell\XCapture.View"; ValueType: string; ValueName: "MUIVerb"; ValueData: "XCapture로 보기"; Tasks: imageviewer; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\image\shell\XCapture.View"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\{#MyAppExeName},0"; Tasks: imageviewer
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\image\shell\XCapture.View\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Tasks: imageviewer
Root: HKCU; Subkey: "Software\XCapture\Capabilities"; ValueType: string; ValueName: "ApplicationName"; ValueData: "XCapture"; Tasks: imageviewer; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\XCapture\Capabilities"; ValueType: string; ValueName: "ApplicationDescription"; ValueData: "화면 캡처 및 이미지 뷰어"; Tasks: imageviewer
Root: HKCU; Subkey: "Software\XCapture\Capabilities\FileAssociations"; ValueType: string; ValueName: ".png"; ValueData: "XCapture.Image"; Tasks: imageviewer
Root: HKCU; Subkey: "Software\XCapture\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jpg"; ValueData: "XCapture.Image"; Tasks: imageviewer
Root: HKCU; Subkey: "Software\XCapture\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jpeg"; ValueData: "XCapture.Image"; Tasks: imageviewer
Root: HKCU; Subkey: "Software\XCapture\Capabilities\FileAssociations"; ValueType: string; ValueName: ".bmp"; ValueData: "XCapture.Image"; Tasks: imageviewer
Root: HKCU; Subkey: "Software\XCapture\Capabilities\FileAssociations"; ValueType: string; ValueName: ".gif"; ValueData: "XCapture.Image"; Tasks: imageviewer
Root: HKCU; Subkey: "Software\XCapture\Capabilities\FileAssociations"; ValueType: string; ValueName: ".tif"; ValueData: "XCapture.Image"; Tasks: imageviewer
Root: HKCU; Subkey: "Software\XCapture\Capabilities\FileAssociations"; ValueType: string; ValueName: ".tiff"; ValueData: "XCapture.Image"; Tasks: imageviewer
Root: HKCU; Subkey: "Software\RegisteredApplications"; ValueType: string; ValueName: "XCapture"; ValueData: "Software\XCapture\Capabilities"; Tasks: imageviewer; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
