using ImageMagick;

namespace PictureTransformer.Core;

public sealed record OutputFormatOption(
    string Name,
    string Extension,
    MagickFormat Format,
    bool SupportsCompression,
    bool SupportsMultipleFrames,
    bool SupportsAlpha)
{
    public override string ToString() => Name;
}

public sealed record ConversionOptions(
    string SourcePath,
    OutputFormatOption OutputFormat,
    int Compression = 0,
    string? DestinationDirectory = null,
    string? OutputPath = null,
    bool Overwrite = false);

public sealed record ConversionResult(string SourcePath, string OutputPath);

public sealed class OutputFileExistsException(string path)
    : IOException($"输出文件已经存在：{path}{Environment.NewLine}如需覆盖，请添加 --overwrite 参数。")
{
    public string Path { get; } = path;
}

public interface IImageConversionService
{
    Task<ConversionResult> ConvertAsync(ConversionOptions options, CancellationToken cancellationToken = default);
}
