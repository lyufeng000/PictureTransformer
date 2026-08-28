namespace PictureTransformer.Core;

public static class PathInputService
{
    private static readonly char[] HiddenCharacters =
    [
        '\u202A', '\u202B', '\u202C', '\u202D', '\u202E',
        '\u2066', '\u2067', '\u2068', '\u2069', '\uFEFF'
    ];

    public static string CleanPath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        string cleaned = path;
        foreach (char character in HiddenCharacters)
            cleaned = cleaned.Replace(character.ToString(), string.Empty, StringComparison.Ordinal);
        cleaned = cleaned.Trim().Trim('"').Trim('\'').Trim();

        if (string.IsNullOrWhiteSpace(cleaned))
            throw new ArgumentException("图片路径不能为空。", nameof(path));

        return ExpandUserDirectory(cleaned);
    }

    public static string ResolvePath(string path)
    {
        string cleaned = CleanPath(path);
        try
        {
            return Path.GetFullPath(cleaned);
        }
        catch (Exception error) when (error is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new ArgumentException($"路径格式无效：{cleaned}", nameof(path), error);
        }
    }

    private static string ExpandUserDirectory(string path)
    {
        if (path == "~")
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (!path.StartsWith($"~{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
            !path.StartsWith($"~{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal))
            return path;

        string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(profile, path[2..]);
    }
}
