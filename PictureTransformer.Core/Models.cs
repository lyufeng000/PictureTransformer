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
    string? DestinationDirectory = null);

public sealed record ConversionResult(string SourcePath, string OutputPath);

public interface IImageConversionService
{
    Task<ConversionResult> ConvertAsync(ConversionOptions options, CancellationToken cancellationToken = default);
}
