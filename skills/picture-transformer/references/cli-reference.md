# PictureTransformer 1.0.5 CLI 参考

## 调用方式

```text
pictureTransformer -s <路径> [-s <路径> ...] [-d <目录>] [-f <格式>] [-c <0-60>]
```

| 参数 | 是否必需 | 说明 |
| --- | --- | --- |
| `-h` | 否 | 单独使用时显示精简帮助。 |
| `-s <路径>` | 是 | 输入图片或文件夹，可重复。文件夹仅扫描第一层。 |
| `-d <目录>` | 否 | 统一输出目录；省略时输出到每个源文件旁边。不存在时会自动创建。 |
| `-f <格式>` | 否 | 输出格式；默认 `png`。格式名不区分大小写，可带或不带开头的点。 |
| `-c <0-60>` | 否 | 整数压缩率；默认 `0`。仅 JPG、WebP、AVIF、JPEG 2000 支持非零值。 |

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
- 相同格式且压缩率为 0 时直接复制源文件到新名称。
- 多个输入文件共享 `-d` 指定的输出目录。

## 示例

```powershell
# 单张 HEIC 转 JPG，默认输出到源文件旁边且不压缩
pictureTransformer -s "D:\Images\photo.heic" -f jpg

# 多张图片转 WebP，压缩率 30%，输出到同一目录
pictureTransformer -s "D:\Images\a.png" -s "D:\Images\b.jpg" -d "D:\Output" -f webp -c 30

# 转换文件夹第一层中的所有受支持图片
pictureTransformer -s "D:\Images" -d "D:\Output" -f png
```

## 退出码

| 退出码 | 含义 |
| --- | --- |
| `0` | 全部转换成功，或帮助/图形界面启动成功。 |
| `1` | 至少一个转换失败，或无法启动图形界面。 |
| `2` | 参数、路径、格式或压缩率无效。 |
