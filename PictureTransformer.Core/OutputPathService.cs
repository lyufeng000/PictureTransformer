namespace PictureTransformer.Core;

public static class OutputPathService
{
    public static string CreateAvailablePath(string sourcePath, string extension, string? destinationDirectory)
        => CreateOutputPath(sourcePath, extension, destinationDirectory, null, false);

    public static string CreateOutputPath(
        string sourcePath,
        string extension,
        string? destinationDirectory,
        string? explicitOutputPath,
        bool overwrite)
    {
        extension = extension.Trim().TrimStart('.');
        if (string.IsNullOrWhiteSpace(extension))
            throw new ArgumentException("输出扩展名不能为空。", nameof(extension));

        if (!string.IsNullOrWhiteSpace(explicitOutputPath))
        {
            if (!string.IsNullOrWhiteSpace(destinationDirectory))
                throw new ArgumentException("不能同时指定输出目录和输出文件。", nameof(explicitOutputPath));

            string target = PathInputService.ResolvePath(explicitOutputPath);
            if (!Path.GetExtension(target).Equals($".{extension}", StringComparison.OrdinalIgnoreCase))
                target = Path.ChangeExtension(target, extension);

            string? parent = Path.GetDirectoryName(target);
            if (string.IsNullOrWhiteSpace(parent))
                throw new ArgumentException("输出文件路径无效。", nameof(explicitOutputPath));
            Directory.CreateDirectory(parent);

            if (Directory.Exists(target))
                throw new IOException($"输出路径是文件夹：{target}");
            if (File.Exists(target) && !overwrite)
                throw new OutputFileExistsException(target);

            return target;
        }

        string directory = string.IsNullOrWhiteSpace(destinationDirectory)
            ? Path.GetDirectoryName(Path.GetFullPath(sourcePath))!
            : PathInputService.ResolvePath(destinationDirectory);

        Directory.CreateDirectory(directory);

        string baseName = Path.GetFileNameWithoutExtension(sourcePath) + "_converted";
        string candidate = Path.Combine(directory, $"{baseName}.{extension}");
        if (overwrite)
            return candidate;

        int suffix = 2;
        while (File.Exists(candidate))
        {
            candidate = Path.Combine(directory, $"{baseName}_{suffix}.{extension}");
            suffix++;
        }

        return candidate;
    }
}
