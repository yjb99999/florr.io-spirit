# florr.io-spirit

课堂教学悬浮球工具，含两套实现：

- **`FlorrTeach/`**：C# + .NET 8 + WPF 原生实现（当前主版本）。
- **`florr_teach.py`**：早期 PyQt5 参照实现（单文件）。
- **`florr_teach.iss`**：Inno Setup 安装包脚本；`卸载悬浮球.bat`：卸载批处理。

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

## 说明

- 编译产物（`bin/`、`obj/`、`publish/`）、安装包 exe、图标素材（`ball.png`、`ball.ico`）与个人名单/配置不入库，相关规则见 `.gitignore`。
- 配置存于 `%APPDATA%\florr_teach\config.json`；名单 `name.txt`、值日表 `clean.txt` 放程序同目录。
