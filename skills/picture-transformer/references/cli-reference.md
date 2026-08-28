# PictureTransformer 1.0.8 CLI 参考

## 调用方式

```text
pictureTransformer <路径> [<路径> ...] [选项]
pictureTransformer -s <路径> [-s <路径> ...] [选项]
```

| 参数 | 是否必需 | 说明 |
| --- | --- | --- |
| `-h`、`--help` | 否 | 显示帮助；也支持 `-help`、`/?` 和 `help`。 |
| `-s`、`--source <路径>` | 是（或位置路径） | 输入图片或文件夹，可重复，也可直接使用位置路径。文件夹仅扫描第一层。 |
| `-d`、`--destination <目录>` | 否 | 统一输出目录；省略时输出到每个源文件旁边。不存在时会自动创建。 |
| `-o`、`--output <文件>` | 否 | 单个输入的完整输出文件路径；不能与 `-d` 同时使用。扩展名自动匹配目标格式。 |
| `-f`、`--format <格式>` | 否 | 输出格式；默认 `png`。格式名不区分大小写，可带或不带开头的点。 |
| `-c`、`--compression <0-60>` | 否 | 整数压缩率；默认 `0`。仅 JPG、WebP、AVIF、JPEG 2000 支持非零值。 |
| `--overwrite` | 否 | 覆盖同名输出；也支持 `--force`。仅在用户明确要求覆盖时使用。 |

不带任何参数会启动 PictureTransformer 图形界面。

## 输入格式

支持以下文件扩展名：

```text
jpg, jpeg, jpe, png, apng, webp, avif, heic, heif,
tif, tiff, bmp, dib, gif, ico, tga, pcx,
jp2, j2k, jpf, jpx, dds, exr, hdr, psd, qoi,
ppm, pgm, pbm, pam, pnm
```

HEIC 和 HEIF 只能作为输入格式。

## 输出格式

| 格式 | `-f` 值 | 压缩率 | 多帧 | 透明通道 |
| --- | --- | --- | --- | --- |
| PNG | `png` | 否 | 否 | 是 |
| JPG/JPEG | `jpg`、`jpeg` | 是 | 否 | 否 |
| WebP | `webp` | 是 | 是 | 是 |
| AVIF | `avif` | 是 | 是 | 是 |
| TIFF | `tiff`、`tif` | 否 | 是 | 是 |
| BMP | `bmp` | 否 | 否 | 否 |
| GIF | `gif` | 否 | 是 | 是 |
| ICO | `ico` | 否 | 是 | 是 |
| TGA | `tga` | 否 | 否 | 是 |
| PCX | `pcx` | 否 | 否 | 否 |
| JPEG 2000 | `jp2`、`j2k` | 是 | 否 | 是 |
| DDS | `dds` | 否 | 是 | 是 |
| EXR | `exr` | 否 | 否 | 是 |
| HDR | `hdr` | 否 | 否 | 否 |
| PSD | `psd` | 否 | 是 | 是 |
| QOI | `qoi` | 否 | 否 | 是 |
| PPM | `ppm` | 否 | 否 | 否 |
| PGM | `pgm` | 否 | 否 | 否 |
| PBM | `pbm` | 否 | 否 | 否 |
| PAM | `pam` | 否 | 否 | 是 |

若目标格式不支持透明通道，透明区域会以白色背景合成。输出格式的实际可用性还取决于随 PictureTransformer 发布的 ImageMagick 编解码器。

## 输出规则

- 输出文件名为 `<原文件名>_converted.<扩展名>`。
- 文件已存在时依次使用 `_converted_2`、`_converted_3` 等名称。
- 使用 `--overwrite` 时，批量输出直接替换同名的 `_converted` 文件。
- 使用 `-o/--output` 时采用指定文件名；目标已存在且未使用 `--overwrite` 时安全失败。
- 输出先写入同目录临时文件，成功后再原子替换；失败时清理临时文件。
- 转换保留 ICC、EXIF 和 DPI 元数据，并根据 EXIF 自动调整方向。
- 相同格式且压缩率为 0 时直接复制源文件到新名称。
- 多个输入文件共享 `-d` 指定的输出目录。
- 输入和输出路径会清理首尾引号、Unicode 方向控制字符及 BOM，并展开开头的 `~`。

## 示例

```powershell
# 单张 HEIC 转 JPG，默认输出到源文件旁边且不压缩
pictureTransformer -s "D:\Images\photo.heic" -f jpg

# 多张图片转 WebP，压缩率 30%，输出到同一目录
pictureTransformer -s "D:\Images\a.png" -s "D:\Images\b.jpg" -d "D:\Output" -f webp -c 30

# 转换文件夹第一层中的所有受支持图片
pictureTransformer -s "D:\Images" -d "D:\Output" -f png

# 单张 HEIC 精确输出为指定 PNG；明确覆盖已有目标
pictureTransformer "D:\Images\photo.heic" -o "D:\Output\photo.png" --overwrite
```

## 退出码

| 退出码 | 含义 |
| --- | --- |
| `0` | 全部转换成功，或帮助/图形界面启动成功。 |
| `1` | 未分类转换失败，或无法启动图形界面。 |
| `2` | 参数无效，或精确输出文件已存在。 |
| `4` | 输入文件或路径不存在。 |
| `5` | 没有读取或写入权限。 |
| `6` | 路径、输入格式或图片内容无效。 |
| `7` | 内存不足。 |
| `130` | 用户按 Ctrl+C 取消。 |
