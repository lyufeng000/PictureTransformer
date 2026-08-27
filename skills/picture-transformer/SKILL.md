---
name: picture-transformer
description: 使用已安装的 PictureTransformer 1.0.3 在 Windows 本地转换单张、多张或目录中的图片，并设置输出格式、目录和压缩率。适用于用户要求使用 PictureTransformer 处理本地图片格式转换；不用于图片内容编辑或输出 HEIC/HEIF。
---

# PictureTransformer

通过本机 `pictureTransformer` 命令行程序完成图片格式转换。实际执行转换前，读取 [references/cli-reference.md](references/cli-reference.md) 以核对参数、格式和行为。

## 工作流程

1. 从用户请求中确定输入文件或目录、输出格式、输出目录和压缩率。没有指定输出格式时使用 PNG；没有指定输出目录时保留软件默认行为，输出到每个源文件旁边；没有指定压缩率时不压缩。
2. 输入路径不明确或用户没有提供本地文件时，先询问路径。不要猜测文件位置。
3. 使用 `Get-Command pictureTransformer` 确认命令可用。若 PATH 中不存在，再检查默认安装位置 `C:\Program Files\PictureTransformer\pictureTransformer.exe`。两处都找不到时，说明需要安装 PictureTransformer 1.0.3 或提供可执行文件路径；不要自行下载安装。
4. 每个文件或目录分别使用一个 `-s` 参数；路径始终加引号。按需添加 `-d`、`-f` 和 `-c`。
5. 只有 JPG、WebP、AVIF 和 JPEG 2000 支持非零压缩率。其他输出格式必须省略 `-c` 或使用 `-c 0`。
6. 执行后根据标准输出报告生成文件路径，并明确列出失败项目。不要删除源文件；软件会为重名输出自动添加编号，不覆盖已有文件。

## 边界

- 文件夹输入只扫描该文件夹第一层，不递归子文件夹。
- HEIC/HEIF 仅支持读取，不能作为输出格式。
- `-c` 表示压缩率，范围为整数 0–60；数值越大，输出质量越低。`0` 是默认值，表示不主动压缩。
- 多帧图片转换到不支持多帧的格式时，仅输出第一帧。
- 不带参数运行 `pictureTransformer` 会打开图形界面；自动化转换必须提供至少一个 `-s`。
