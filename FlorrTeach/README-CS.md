# florr_teach · C#（WPF）版本

Python 版的原生编译对照实现。技术栈：**C# + .NET 8 + WPF**，单文件 EXE 发布，不依赖 Python 解释器，反编译门槛高于 PyInstaller 打包。

## 一、目录结构

```
FlorrTeach/
├─ FlorrTeach.csproj          项目与单文件发布配置
├─ App.xaml / App.xaml.cs     启动入口：单实例、自启、读取名单/值日表、异常日志
├─ Models/AppConfig.cs        配置模型（字段与 Python 版 config.json 一致）
├─ Services/
│  ├─ ConfigManager.cs        配置读写（%APPDATA%\florr_teach\config.json）
│  ├─ FileParsers.cs          name.txt / clean.txt 解析（UTF-8 / GBK 容错）
│  ├─ SystemServices.cs        单实例互斥、HKCU 开机自启、结束指定进程
│  ├─ CoreAudioController.cs   Core Audio（IAudioEndpointVolume）COM 调用
│  ├─ VolumeController.cs      音量读写：Core Audio 优先，失败回退 winmm
│  ├─ GlobalMouseHook.cs       全局低级鼠标钩子，点本程序之外收回面板
│  └─ WindowExtras.cs          工具窗口样式（WS_EX_TOOLWINDOW），不进任务栏/Alt+Tab
└─ Views/
   ├─ FloatingBallWindow      悬浮球：拖拽、边缘吸附、面板联动、定时器
   ├─ ModulePanelWindow       主面板（时间 / 服务器 / 校园AI / 音量 / 点名 / 设置）
   ├─ TimeSubPanel            时间子面板（时钟 / 倒计时）
   ├─ VolumeSubPanel          音量子面板（滑块 + 静音）
   ├─ FullscreenClockWindow   全屏考试时钟（黑 / 白主题，大号字体）
   ├─ WheelPicker            苹果计时器风格纵向滚轮（拖拽 + 惯性吸附 + 淡出遮罩）
   ├─ CountdownWindow         倒计时（分 / 秒滚轮设时，开始 / 结束 / 暂停 / 继续）
   ├─ RollCallDialog         随机点名（滚动抽取，2 秒自动停，标题栏可拖）
   ├─ DutyReminderWindow       值日提醒（勾选完成，记录存桌面，标题栏可拖）
   └─ SettingsDialog          设置（主题、名单、球大小、值日、杀进程频率、开发者模式）
```

## 二、功能对齐情况

| 功能 | Python 版 | C# 版 |
|---|---|---|
| 悬浮球拖拽 / 边缘吸附 | ✅ | ✅ |
| 3×2 模块面板 | ✅ | ✅ |
| 全屏时钟（黑白主题） | ✅ | ✅ |
| 倒计时（开始/结束/暂停） | ✅ | ✅（分/秒苹果式滚轮设时） |
| 音量滑块 + 静音 | ✅ | ✅（Core Audio 优先，winmm 兜底） |
| 随机点名 | ✅ | ✅ |
| 值日提醒 + 桌面记录 | ✅ | ✅ |
| 设置（名单/值日/时间/频率） | ✅ | ✅ |
| 开机自启（HKCU Run） | ✅ | ✅ |
| 单实例互斥 | ✅ | ✅ |
| 定时结束指定进程 | ✅ | ✅ |
| 配置存 %APPDATA% | ✅ | ✅ |
| 点外部窗口收回面板 | ✅ | ✅（全局鼠标钩子，PID 比对） |
| 附属窗口不进任务栏 / Alt+Tab | ✅ | ✅（工具窗口样式） |

## 三、关键实现说明（原简化项已补齐）

1. **倒计时滚轮**：`WheelPicker` 为苹果计时器风格的纵向滚轮，分（00–99）、秒（00–59）两列。按住上下拖动可连续滚动，松手按速度惯性外推（限两格）后吸附到最近一项（CubicEase 缓动）；按下时先把吸附动画的视觉位置固化到本地，避免再次拖拽跳回首项。选中项居中加粗高亮，上下项随距离淡化、缩小，顶部与底部用卡片底色渐变遮罩淡出。未开始时滚轮设时，开始后收起滚轮显示剩余大字，结束/时间到回到滚轮。
2. **音量接口**：已接入 Core Audio（`IAudioEndpointVolume`，COM 虚表直调），与任务栏主音量、静音状态实时同步；Core Audio 初始化失败时自动回退 winmm（WAVE_MAPPER）。
3. **外部点击收回**：已安装 `WH_MOUSE_LL` 全局低级鼠标钩子。左键命中时用 `WindowFromPoint` + `GetAncestor(GA_ROOT)` + `GetWindowThreadProcessId` 比对 PID，只有点击本进程之外的窗口才收回面板；不再依赖不可靠的子窗口失活（Deactivated）事件。
4. **右键菜单位置**：设置菜单以悬浮球为 `PlacementTarget`，默认在鼠标点击位置弹出。
5. **全局主题**：`Services/ThemePalette.cs` 集中提供黑 / 白两套配色，点名、值日、倒计时等弹窗均接入同一色板；全屏时钟自带黑白主题。
6. **设置窗口任务栏可见**：其余附属窗口均为 `WS_EX_TOOLWINDOW`（不进任务栏 / Alt+Tab）；唯独设置窗口不加工具样式、`ShowInTaskbar=true`，被遮挡后可从任务栏点回。
7. **杀进程频率**：除 5s / 10s / 30s 外，支持「开机时只杀一次」与「自定义间隔秒数」（1–3600 秒，仅选中自定义时启用输入框）。
8. **开发者模式**：设置页底部双密码校验——第一框 `yjb999`、第二框 `121225`，均正确才激活。解锁后提供：①自定义悬浮球 PNG 图标（未启用或文件失效回退内置 `ball.png`）；②独立界面主题（跟随全局 / 黑底 / 白底），优先于全局时钟主题作用于各弹窗；③悬浮球大小滑块（40–100 px，越右越大，保存后记住当前像素）。
9. **全屏时钟字号**：时间 170px、日期 38px、提示 18px。

> 可选后续优化：Core Audio 可再加端点变更通知（`IAudioEndpointVolume.OnNotification` / 设备切换监听），跟随默认播放设备切换；当前每次读取都取当前默认播放端点。

## 四、发布命令

环境：已装 .NET 8 SDK（`C:\Program Files\dotnet`）。csproj 默认按**方案2（框架依赖小本体）**配置，并已关闭调试符号（`DebugSymbols=false`、`DebugType=none`），发布目录只产出 EXE、不带 PDB。

```powershell
# 方案2 · 框架依赖单文件（默认；目标机需一次性安装 .NET 8 桌面运行时，体积极小）
dotnet publish -c Release -r win-x64 --self-contained false `
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=false -o publish-frd

# 可选 · 自包含单文件（目标机无需装 .NET，体积大；命令行覆盖默认的 SelfContained）
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -o publish
```

> 分发框架依赖包时，目标机首次运行前需安装 **.NET 8 桌面运行时（Desktop Runtime，win-x64）**；未装会提示缺少运行时。框架依赖单文件不支持单文件压缩（NETSDK1176），`EnableCompressionInSingleFile` 必须为 false。

### 安装包（Inno Setup）

根目录 `florr_teach.iss` 面向**自包含**产物 `FlorrTeach\publish\florr_teach.exe`：固定安装到 `{app}`、写入 HKCU Run 开机自启、安装后自动启动，不附带名单/值日表。编译前需先发布自包含产物，再用 Inno Setup 编译器打包：

```powershell
# 1. 先出自包含单文件
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o FlorrTeach\publish

# 2. 安装 Inno Setup 7 后（ISCC.exe），编译脚本
& "D:\Inno Setup 7\ISCC.exe" "florr_teach.iss"
# 产物：florr_teach_setup.exe（输出到脚本所在目录，实测 LZMA2 压缩后约 44.5 MB）
```

> 本机 Inno Setup 7 装在 `D:\Inno Setup 7`；官方下载：https://jrsoftware.org/isdl.php

## 五、体积对比（实测）

| 版本 | 产物 | 体积 | 说明 |
|---|---|---|---|
| Python（PyInstaller） | florr_teach.exe | 35.09 MB | 打包解释器 + 依赖 |
| C# 自包含 | publish\florr_teach.exe | 68.61 MB | 内含整套 .NET 运行时 |
| C# 依赖框架（方案2） | publish-frd\florr_teach.exe | 0.45 MB | 仅应用本体，需共享运行时 |

> C# 应用本体只有约 0.45 MB；自包含包偏大是把 .NET 运行时一起打了进去，和 Python 打包解释器是同一道理。

## 六、配置与数据文件

- **配置**：`%APPDATA%\florr_teach\config.json`（与 Python 版同路径、同字段）。新增字段：`kill_interval_seconds`（自定义杀进程间隔，默认 10）、`dev_mode`（开发者模式开关）、`ball_icon`（自定义球图标 PNG 路径，空 = 默认 ball.png）、`panel_theme`（独立界面主题，空 = 跟随全局）、`ball_size_px`（自定义球像素，0 = 用三档球大小）。
- **名单 name.txt**：第一行班级名，后面每行一个名字，放 EXE 同目录。
- **值日表 clean.txt**：星期几开头，下面每行 `名字:值务`；周五与周日共用。
- **崩溃日志**：`%APPDATA%\florr_teach\crash.log`。

## 七、与 Python 版的取舍

- **Python**：开发快、语法简单、第三方库多、好改；但体积偏大、启动偏慢、字节码易被反编译、UI 较朴素。
- **C#（WPF / .NET 8）**：原生编译、反编译门槛高、启动快、UI 现代、本体体积极小、性能强；但开发与调试成本更高、打包配置更细。
