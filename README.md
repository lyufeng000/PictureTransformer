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
pictureTransformer -s <路径> [-s <路径> ...] [-d <目录>] [-f <格式>] [-c <0-60>]
```

例如：

```powershell
pictureTransformer -s "D:\Images\photo.heic" -f jpg
pictureTransformer -s "D:\Images" -d "D:\Output" -f webp -c 30
```

## 隐私

PictureTransformer 完全离线运行，不收集或上传用户数据。

## 许可证

本项目使用 [MIT License](LICENSE) 开源。
