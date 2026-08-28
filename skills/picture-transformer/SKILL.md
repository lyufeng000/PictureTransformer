---
name: picture-transformer
description: 使用已安装的 PictureTransformer 1.0.8 在 Windows 本地转换单张、多张或目录中的图片，并设置输出格式、精确输出文件、覆盖规则和压缩率。适用于用户要求使用 PictureTransformer 处理本地图片格式转换；不用于图片内容编辑或输出 HEIC/HEIF。
---

# PictureTransformer

通过本机 `pictureTransformer` 命令行程序完成图片格式转换。实际执行转换前，读取 [references/cli-reference.md](references/cli-reference.md) 以核对参数、格式和行为。

## 工作流程

1. 从用户请求中确定输入文件或目录、输出格式、输出目录或完整输出文件、压缩率和覆盖规则。没有指定输出格式时使用 PNG；没有指定输出位置时保留软件默认行为，输出到每个源文件旁边；没有指定压缩率时不压缩。
2. 输入路径不明确或用户没有提供本地文件时，先询问路径。不要猜测文件位置。
3. 使用 `Get-Command pictureTransformer` 确认命令可用。若 PATH 中不存在，再检查默认安装位置 `C:\Program Files\PictureTransformer\bin\pictureTransformer.exe`。两处都找不到时，说明需要安装 PictureTransformer 1.0.8 或提供可执行文件路径；不要自行下载安装。
4. 每个文件或目录分别使用一个 `-s` 参数；路径始终加引号。多个输入共享目录时使用 `-d`；仅当单个输入需要精确文件名时使用 `-o`。`-d` 与 `-o` 不能同时使用。
5. 只有 JPG、WebP、AVIF 和 JPEG 2000 支持非零压缩率。其他输出格式必须省略 `-c` 或使用 `-c 0`。
6. 默认不要添加 `--overwrite`；只有用户明确要求覆盖或替换已有输出时才使用。未覆盖时，批量输出会自动添加编号，精确输出路径已存在则会安全失败。
7. 执行后根据标准输出报告生成文件路径，并明确列出失败项目。不要删除源文件。

## 边界

- 文件夹输入只扫描该文件夹第一层，不递归子文件夹。
- HEIC/HEIF 仅支持读取，不能作为输出格式。
- `-c` 表示压缩率，范围为整数 0–60；数值越大，输出质量越低。`0` 是默认值，表示不主动压缩。
- `-o/--output` 仅支持一个解析后的输入文件；扩展名会自动替换为目标格式的扩展名。
- 路径会清理首尾引号、Unicode 方向控制字符和 BOM，并展开开头的 `~` 用户目录。
- 多帧图片转换到不支持多帧的格式时，仅输出第一帧。
- 不带参数运行 `pictureTransformer` 会打开图形界面；自动化转换必须提供至少一个位置路径或 `-s/--source`。
