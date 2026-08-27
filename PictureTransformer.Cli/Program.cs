using System.Diagnostics;
using PictureTransformer.Core;

namespace PictureTransformer.Cli;

internal static class Program
{
    private const string HelpText = """
        PictureTransformer 图片转换工具

        用法:
          pictureTransformer -s <路径> [-s <路径> ...] [-d <目录>] [-f <格式>] [-c <0-60>]

        参数:
          -h            显示帮助
          -s <路径>     输入文件或文件夹，可重复
          -d <目录>     输出目录，默认源文件旁
          -f <格式>     输出格式，默认 png
          -c <0-60>     压缩率，默认 0
        """;

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        if (args.Length == 0)
            return LaunchGui();
        if (args.Length == 1 && args[0].Equals("-h", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine(HelpText);
            return 0;
        }

        if (!TryParse(args, out CliOptions options, out string error))
        {
            Console.Error.WriteLine($"错误：{error}");
            Console.Error.WriteLine("使用 pictureTransformer -h 查看帮助。");
            return 2;
        }

        OutputFormatOption? outputFormat = ImageFormatCatalog.FindOutputFormat(options.Format);
        if (outputFormat is null)
        {
            Console.Error.WriteLine($"错误：不支持输出格式“{options.Format}”。");
            return 2;
        }
        if (!outputFormat.SupportsCompression && options.Compression != 0)
        {
            Console.Error.WriteLine($"错误：{outputFormat.Name} 不支持压缩率参数，请使用 -c 0。");
            return 2;
        }

        var sources = new List<string>();
        foreach (string source in options.Sources)
        {
            string fullPath;
            try { fullPath = Path.GetFullPath(source); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"错误：路径“{source}”无效：{ex.Message}");
                return 2;
            }

            if (File.Exists(fullPath))
                sources.Add(fullPath);
            else if (Directory.Exists(fullPath))
                sources.AddRange(ImageFormatCatalog.EnumerateImages(fullPath));
            else
            {
                Console.Error.WriteLine($"错误：找不到输入路径“{source}”。");
                return 2;
            }
        }

        sources = sources.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (sources.Count == 0)
        {
            Console.Error.WriteLine("错误：没有找到受支持的图片。");
            return 2;
        }

        var converter = new ImageConversionService();
        int failed = 0;
        foreach (string source in sources)
        {
            try
            {
                ConversionResult result = await converter.ConvertAsync(new ConversionOptions(
                    source, outputFormat, options.Compression, options.DestinationDirectory));
                Console.WriteLine($"已转换：{source} -> {result.OutputPath}");
            }
            catch (Exception ex)
            {
                failed++;
                Console.Error.WriteLine($"转换失败：{source}：{ex.Message}");
            }
        }

        Console.WriteLine($"完成：成功 {sources.Count - failed} 个，失败 {failed} 个。");
        return failed == 0 ? 0 : 1;
    }

    private static int LaunchGui()
    {
        string baseDirectory = AppContext.BaseDirectory;
        string[] candidates =
        [
            Path.Combine(baseDirectory, "app", "PictureTransformer.exe"),
            Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "..", "PictureTransformer", "bin", "Debug", "net10.0-windows", "PictureTransformer.exe"))
        ];
        string? guiPath = candidates.FirstOrDefault(File.Exists);
        if (guiPath is null)
        {
            Console.Error.WriteLine("错误：找不到 PictureTransformer 图形界面，请重新发布程序。");
            return 1;
        }

        Process.Start(new ProcessStartInfo(guiPath) { UseShellExecute = true });
        return 0;
    }

    private static bool TryParse(string[] args, out CliOptions options, out string error)
    {
        options = new CliOptions();
        error = string.Empty;

        for (int index = 0; index < args.Length; index++)
        {
            string argument = args[index].ToLowerInvariant();
            if (argument is not ("-s" or "-d" or "-f" or "-c"))
            {
                error = $"未知参数“{args[index]}”。";
                return false;
            }
            if (++index >= args.Length)
            {
                error = $"参数“{argument}”缺少值。";
                return false;
            }

            string value = args[index];
            switch (argument)
            {
                case "-s": options.Sources.Add(value); break;
                case "-d": options.DestinationDirectory = value; break;
                case "-f": options.Format = value; break;
                case "-c":
                    if (!int.TryParse(value, out int compression) || compression is < 0 or > 60)
                    {
                        error = "压缩率必须是 0 到 60 之间的整数。";
                        return false;
                    }
                    options.Compression = compression;
                    break;
            }
        }

        if (options.Sources.Count == 0)
        {
            error = "转换时必须至少提供一个 -s 输入路径。";
            return false;
        }
        return true;
    }

    private sealed class CliOptions
    {
        public List<string> Sources { get; } = [];
        public string? DestinationDirectory { get; set; }
        public string Format { get; set; } = "png";
        public int Compression { get; set; }
    }
}
