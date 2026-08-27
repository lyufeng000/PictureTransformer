using ImageMagick;

namespace PictureTransformer.Core;

public static class ImageFormatCatalog
{
    private static readonly OutputFormatOption[] AllOutputFormats =
    [
        new("PNG", "png", MagickFormat.Png, false, false, true),
        new("JPG", "jpg", MagickFormat.Jpeg, true, false, false),
        new("WEBP", "webp", MagickFormat.WebP, true, true, true),
        new("AVIF", "avif", MagickFormat.Avif, true, true, true),
        new("TIFF", "tiff", MagickFormat.Tiff, false, true, true),
        new("BMP", "bmp", MagickFormat.Bmp, false, false, false),
        new("GIF", "gif", MagickFormat.Gif, false, true, true),
        new("ICO", "ico", MagickFormat.Ico, false, true, true),
        new("TGA", "tga", MagickFormat.Tga, false, false, true),
        new("PCX", "pcx", MagickFormat.Pcx, false, false, false),
        new("JPEG 2000", "jp2", MagickFormat.Jp2, true, false, true),
        new("DDS", "dds", MagickFormat.Dds, false, true, true),
        new("EXR", "exr", MagickFormat.Exr, false, false, true),
        new("HDR", "hdr", MagickFormat.Hdr, false, false, false),
        new("PSD", "psd", MagickFormat.Psd, false, true, true),
        new("QOI", "qoi", MagickFormat.Qoi, false, false, true),
        new("PPM", "ppm", MagickFormat.Ppm, false, false, false),
        new("PGM", "pgm", MagickFormat.Pgm, false, false, false),
        new("PBM", "pbm", MagickFormat.Pbm, false, false, false),
        new("PAM", "pam", MagickFormat.Pam, false, false, true)
    ];

    private static readonly HashSet<string> SupportedInputExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".jpe", ".png", ".apng", ".webp", ".avif",
        ".heic", ".heif", ".tif", ".tiff", ".bmp", ".dib", ".gif",
        ".ico", ".tga", ".pcx", ".jp2", ".j2k", ".jpf", ".jpx",
        ".dds", ".exr", ".hdr", ".psd", ".qoi", ".ppm", ".pgm",
        ".pbm", ".pam", ".pnm"
    };

    public static IReadOnlyList<OutputFormatOption> OutputFormats { get; } = GetAvailableOutputFormats();

    public static bool IsSupportedInput(string path) =>
        File.Exists(path) && SupportedInputExtensions.Contains(Path.GetExtension(path));

    public static OutputFormatOption? FindOutputFormat(string value)
    {
        string normalized = value.Trim().TrimStart('.');
        if (normalized.Equals("jpeg", StringComparison.OrdinalIgnoreCase))
            normalized = "jpg";
        if (normalized.Equals("tif", StringComparison.OrdinalIgnoreCase))
            normalized = "tiff";
        if (normalized.Equals("j2k", StringComparison.OrdinalIgnoreCase))
            normalized = "jp2";

        return OutputFormats.FirstOrDefault(format =>
            format.Extension.Equals(normalized, StringComparison.OrdinalIgnoreCase) ||
            format.Name.Equals(normalized, StringComparison.OrdinalIgnoreCase));
    }

    public static IEnumerable<string> EnumerateImages(string directory) =>
        Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
            .Where(IsSupportedInput)
            .OrderBy(path => path, StringComparer.CurrentCultureIgnoreCase);

    private static IReadOnlyList<OutputFormatOption> GetAvailableOutputFormats()
    {
        var writableFormats = MagickNET.SupportedFormats
            .Where(format => format.SupportsWriting)
            .Select(format => format.Format)
            .ToHashSet();

        return AllOutputFormats.Where(format => writableFormats.Contains(format.Format)).ToArray();
    }
}
