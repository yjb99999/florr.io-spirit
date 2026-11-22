# florr.io-spirit

课堂教学悬浮球工具。

## 目录

- **`FlorrTeach/`**：C# + .NET 8 + WPF 原生实现（当前主版本，源码已入库）。
- **`florr_teach.iss`**：Inno Setup 安装包脚本。
- **`卸载悬浮球.bat`**：卸载批处理。

## 构建（C# WPF）

```powershell
# 框架依赖单文件（目标机需 .NET 8 桌面运行时，体积极小）
dotnet publish -c Release -r win-x64 --self-contained false `
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=false -o publish-frd

# 自包含单文件（目标机无需装 .NET，体积大）
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -o publish
```

详见 `FlorrTeach/README-CS.md`。

## 未纳入本仓库的内容（本地保留，待补传）

- `florr_teach.py`：早期 PyQt5 参照单文件脚本（纯文本，可后续补入）。
- `ball.png` / `ball.ico`：球皮肤与图标素材（二进制）。
- `florr_teach_setup.exe`：约 47 MB 安装包（二进制，建议经 GitHub Release 附件分发）。

## 说明

- 编译产物（`bin/`、`obj/`、`publish/`）、安装包、个人名单与配置不入库，规则见 `.gitignore`。
- 配置存于 `%APPDATA%\florr_teach\config.json`；名单 `name.txt`、值日表 `clean.txt` 放程序同目录。
