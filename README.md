# PictureTransformer

PictureTransformer 是一个基于 .NET 10 和 WPF 开发的离线图片格式转换工具，支持图形界面和命令行两种使用方式。

## 功能

- 支持 JPG、PNG、WebP、AVIF、TIFF、GIF、BMP、HEIC 等常见输入格式。
- HEIC/HEIF 支持作为输入格式读取。
- 支持批量选择、文件夹导入和拖放图片。
- 支持 0% 到 60% 的压缩率设置。
- 默认输出到源文件旁边，也可以指定统一输出目录。
- 文件重名时自动添加后缀，不覆盖已有文件。
- 提供 `pictureTransformer` 命令行程序。
- 内置可供 Codex 使用的 `$picture-transformer` 图片转换 Skill。
- 每次启动时静默检查 GitHub Releases，发现新版本后展示 `update.md` 并询问是否更新。
- 使用 WiX 构建 MSI 和 Setup 安装程序。

## 构建

需要 .NET 10 SDK：

```powershell
dotnet build .\PictureTransformer.sln
```

## 发布程序

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\publish-bin.ps1
```

## 构建安装包

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build-setup.ps1
```

安装包会生成到本地 `dist` 目录。

## 命令行

```text
pictureTransformer <路径> [<路径> ...] [-d <目录>] [-o <文件>] [-f <格式>] [-c <0-60>] [--overwrite]
```

例如：

```powershell
pictureTransformer -s "D:\Images\photo.heic" -f jpg
pictureTransformer -s "D:\Images" -d "D:\Output" -f webp -c 30
pictureTransformer "D:\Images\photo.heic" -o "D:\Output\photo.png" --overwrite
```

## Codex Skill

仓库内置 [PictureTransformer Skill](skills/picture-transformer/SKILL.md)，用于让 Codex 调用本机安装的 `pictureTransformer` 完成单张、批量或文件夹图片转换。Skill 声明了全部 CLI 参数、输入/输出格式、压缩规则和输出命名行为，可通过 `$picture-transformer` 调用。

将 `skills/picture-transformer` 文件夹复制到 Codex 的个人 Skills 目录后即可使用；Windows 默认目录为 `%USERPROFILE%\.codex\skills\picture-transformer`。

## 软件更新

软件启动后会异步检查本仓库的最新正式 Release，不会阻塞主界面。发现更高版本时，会读取对应版本标签根目录下的 `update.md`，展示更新内容并询问用户是否更新；用户确认后，安装程序会下载到系统“下载”目录并自动启动。

发布新版本前需要更新仓库根目录的 [update.md](update.md)，并确保它与即将创建的版本标签处于同一个提交。网络不可用、GitHub 请求失败、`update.md` 缺失或内容无效时，软件不会显示任何提示。

## 隐私

PictureTransformer 的图片读取、转换和写入全部在本地完成，不会上传用户图片，也不收集遥测数据。软件每次启动仅访问 GitHub Releases 和对应版本的 `update.md` 以检查更新；检查失败时保持静默。

## 许可证

本项目使用 [MIT License](LICENSE) 开源。

## Code signing policy

Free code signing provided by [SignPath.io](https://signpath.io/), certificate by [SignPath Foundation](https://signpath.org/).

- 正式发布文件必须由 GitHub Actions 从本仓库的公开源代码构建。
- 每次正式签名请求都必须由项目维护者人工批准。
- 签名服务仅用于签署由本项目源代码和构建脚本生成的发布文件。
- 不使用本项目的签名权限签署其他项目或第三方二进制文件。
