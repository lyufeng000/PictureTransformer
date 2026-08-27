namespace PictureTransformer.Core;

public static class OutputPathService
{
    public static string CreateAvailablePath(string sourcePath, string extension, string? destinationDirectory)
    {
        string directory = string.IsNullOrWhiteSpace(destinationDirectory)
            ? Path.GetDirectoryName(Path.GetFullPath(sourcePath))!
            : Path.GetFullPath(destinationDirectory);

        Directory.CreateDirectory(directory);

        string baseName = Path.GetFileNameWithoutExtension(sourcePath) + "_converted";
        string candidate = Path.Combine(directory, $"{baseName}.{extension}");
        int suffix = 2;
        while (File.Exists(candidate))
        {
            candidate = Path.Combine(directory, $"{baseName}_{suffix}.{extension}");
            suffix++;
        }

        return candidate;
    }
}
