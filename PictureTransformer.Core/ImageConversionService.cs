using ImageMagick;

namespace PictureTransformer.Core;

public sealed class ImageConversionService : IImageConversionService
{
    public Task<ConversionResult> ConvertAsync(ConversionOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Compression is < 0 or > 60)
            throw new ArgumentOutOfRangeException(nameof(options.Compression), "压缩率必须在 0 到 60 之间。");
        if (!options.OutputFormat.SupportsCompression && options.Compression != 0)
            throw new ArgumentException($"{options.OutputFormat.Name} 不支持压缩率参数。", nameof(options));
        if (!ImageFormatCatalog.IsSupportedInput(options.SourcePath))
            throw new FileNotFoundException("输入文件不存在或格式不受支持。", options.SourcePath);

        return Task.Run(() => Convert(options, cancellationToken), cancellationToken);
    }

    private static ConversionResult Convert(ConversionOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string outputPath = OutputPathService.CreateOutputPath(
            options.SourcePath,
            options.OutputFormat.Extension,
            options.DestinationDirectory,
            options.OutputPath,
            options.Overwrite);
        string tempPath = outputPath + $".{Guid.NewGuid():N}.tmp";

        try
        {
            string sourceExtension = Path.GetExtension(options.SourcePath).TrimStart('.');
            bool sameFormat = sourceExtension.Equals(options.OutputFormat.Extension, StringComparison.OrdinalIgnoreCase) ||
                              sourceExtension.Equals("jpeg", StringComparison.OrdinalIgnoreCase) && options.OutputFormat.Extension == "jpg" ||
                              sourceExtension.Equals("tif", StringComparison.OrdinalIgnoreCase) && options.OutputFormat.Extension == "tiff";

            if (sameFormat && options.Compression == 0)
            {
                File.Copy(options.SourcePath, tempPath, false);
            }
            else
            {
                using var images = new MagickImageCollection(options.SourcePath);
                cancellationToken.ThrowIfCancellationRequested();

                foreach (IMagickImage<byte> image in images)
                {
                    image.AutoOrient();
                    PrepareTransparency(image, options.OutputFormat);
                    image.Format = options.OutputFormat.Format;
                    if (options.OutputFormat.SupportsCompression)
                    {
                        int quality = 100 - options.Compression;
                        // libheif/AOM can reject its implicit lossless path at quality 100.
                        if (options.OutputFormat.Format == MagickFormat.Avif && quality == 100)
                            quality = 99;
                        image.Quality = (uint)quality;
                    }
                }

                if (images.Count > 1 && options.OutputFormat.SupportsMultipleFrames)
                {
                    images.Write(tempPath, options.OutputFormat.Format);
                }
                else
                {
                    images[0].Write(tempPath, options.OutputFormat.Format);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(tempPath, outputPath, options.Overwrite);
            return new ConversionResult(options.SourcePath, outputPath);
        }
        catch
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            throw;
        }
    }

    private static void PrepareTransparency(IMagickImage<byte> image, OutputFormatOption outputFormat)
    {
        if (outputFormat.SupportsAlpha || !image.HasAlpha)
            return;

        image.BackgroundColor = MagickColors.White;
        image.Alpha(AlphaOption.Remove);
    }
}
