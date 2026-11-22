; Inno Setup 脚本：florr_teach（C# WPF 自包含单文件版，免 .NET 运行时）
; 打包产物：FlorrTeach\publish\florr_teach.exe
; 功能：固定路径安装，安装完成直接启动，开机自启；不附带名单/值日表（首次运行后在设置里自选）

[Setup]
AppName=florr_teach
AppVersion=1.0.0
AppPublisher=yjb999
DefaultDirName=C:\Program Files\florr_teach
DisableProgramGroupPage=yes
DisableWelcomePage=yes
DisableFinishedPage=no
DisableDirPage=yes
Uninstallable=no
OutputBaseFilename=florr_teach_setup
OutputDir=.
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=ball.ico

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

[Files]
; 自包含主程序；WPF 的 native 运行时 dll 按 .NET 默认布局放在 exe 旁边，必须一并安装到 {app}
Source: "FlorrTeach\publish\florr_teach.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "FlorrTeach\publish\*.dll"; DestDir: "{app}"; Flags: ignoreversion

[Registry]
; 开机自启：写入当前用户的 Run 注册表
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "florr_teach"; ValueData: """{app}\florr_teach.exe"""; Flags: uninsdeletevalue

[Run]
; 安装完成页显示「立即启动」复选框，默认勾选；postinstall 标志用于在完成页生成复选框
Filename: "{app}\florr_teach.exe"; Description: "立即启动 florr_teach"; Flags: nowait postinstall
