# PictureTransformer 安装包

安装包使用 WiX Toolset 5.0.2 构建，包括标准 Windows Installer MSI 和 Burn 引导程序。

## 构建

在项目根目录运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build-setup.ps1
```

最终结果位于 `dist`：

- `PictureTransformer-Setup.exe`：推荐提供给普通用户。
- `PictureTransformer.msi`：用于 Windows Installer 或集中部署。

## 安装行为

- 默认安装到 `%ProgramFiles%\PictureTransformer`，并允许在 Setup 的选项页修改路径。
- 按计算机安装，需要管理员权限。
- 创建桌面和开始菜单快捷方式。
- 将命令行程序所在目录加入系统 PATH。
- 支持升级、修复和卸载。

## 发布前注意

当前版本号为 `1.0.3`。发布新版本时，需要同步修改 `Directory.Build.props`、`Package.wxs` 和 `Bundle.wxs` 中的版本号。

当前产物没有数字签名，Windows 可能显示“未知发布者”。正式公开分发前建议为 MSI、Burn 引擎和最终 Setup 添加代码签名。
