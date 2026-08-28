using System.Diagnostics;
using ImageMagick;
using PictureTransformer.Core;

namespace PictureTransformer.Cli;

internal static class Program
{
    private static readonly HashSet<string> HelpArguments = new(StringComparer.OrdinalIgnoreCase)
    {
        "-h", "--help", "-help", "/?", "help"
    };

    private const string HelpText = """
        PictureTransformer 图片转换工具

        用法:
          pictureTransformer <输入路径> [<输入路径> ...] [选项]
          pictureTransformer -s <路径> [-s <路径> ...] [选项]

        参数:
          -h, --help                  显示帮助
          -s, --source <路径>         输入文件或文件夹，可重复；也可直接使用位置路径
          -d, --destination <目录>    输出目录，默认源文件旁
          -o, --output <文件>         指定完整输出文件路径，仅限单个输入文件
          -f, --format <格式>         输出格式，默认 png
          -c, --compression <0-60>    压缩率，默认 0
              --overwrite            覆盖同名输出文件；默认自动添加编号

        路径会自动清理首尾引号、Unicode 方向控制字符，并展开开头的 ~ 用户目录。
        """;

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        if (args.Length == 0)
            return LaunchGui();
        if (args.Any(argument => HelpArguments.Contains(argument)))
        {
            Console.WriteLine(HelpText);
            return 0;
        }

        if (!TryParse(args, out CliOptions options, out string error))
        {
            Console.Error.WriteLine($"错误：{error}");
            Console.Error.WriteLine("使用 pictureTransformer --help 查看帮助。");
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
            try
            {
                fullPath = PathInputService.ResolvePath(source);
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine($"错误：{ex.Message}");
                return 6;
            }

            if (File.Exists(fullPath))
            {
                if (!ImageFormatCatalog.IsSupportedInput(fullPath))
                {
                    Console.Error.WriteLine($"错误：不支持的输入图片格式“{Path.GetExtension(fullPath)}”。");
                    return 6;
                }
                sources.Add(fullPath);
            }
            else if (Directory.Exists(fullPath))
            {
                sources.AddRange(ImageFormatCatalog.EnumerateImages(fullPath));
            }
            else
            {
                Console.Error.WriteLine($"错误：找不到输入路径“{source}”。");
                return 4;
            }
        }

        sources = sources.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (sources.Count == 0)
        {
            Console.Error.WriteLine("错误：没有找到受支持的图片。");
            return 6;
        }
        if (!string.IsNullOrWhiteSpace(options.OutputPath) && sources.Count != 1)
        {
            Console.Error.WriteLine("错误：-o/--output 只能与单个输入文件一起使用。");
            return 2;
        }

        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };
        Console.CancelKeyPress += cancelHandler;

        try
        {
            var converter = new ImageConversionService();
            int failed = 0;
            int failureExitCode = 0;
            foreach (string source in sources)
            {
                try
                {
                    ConversionResult result = await converter.ConvertAsync(new ConversionOptions(
                        source,
                        outputFormat,
                        options.Compression,
                        options.DestinationDirectory,
                        options.OutputPath,
                        options.Overwrite),
                        cancellation.Token);
                    Console.WriteLine($"已转换：{source} -> {result.OutputPath}");
                }
                catch (OperationCanceledException)
                {
                    Console.Error.WriteLine("转换已由用户取消。");
                    return 130;
                }
                catch (Exception ex)
                {
                    failed++;
                    int exitCode = ClassifyExitCode(ex);
                    if (failureExitCode == 0)
                        failureExitCode = exitCode;
                    Console.Error.WriteLine($"转换失败：{source}：{ex.Message}");
                }
            }

            Console.WriteLine($"完成：成功 {sources.Count - failed} 个，失败 {failed} 个。");
            return failed == 0 ? 0 : failureExitCode;
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
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
            string original = args[index];
            string argument = original.ToLowerInvariant();
            switch (argument)
            {
                case "-s":
                case "--source":
                    if (!TryReadValue(args, ref index, argument, out string source, out error)) return false;
                    options.Sources.Add(source);
                    break;
                case "-d":
                case "--destination":
                case "--output-directory":
                    if (!TryReadValue(args, ref index, argument, out string destination, out error)) return false;
                    options.DestinationDirectory = destination;
                    break;
                case "-o":
                case "--output":
                    if (!TryReadValue(args, ref index, argument, out string output, out error)) return false;
                    options.OutputPath = output;
                    break;
                case "-f":
                case "--format":
                    if (!TryReadValue(args, ref index, argument, out string format, out error)) return false;
                    options.Format = format;
                    break;
                case "-c":
                case "--compression":
                    if (!TryReadValue(args, ref index, argument, out string compressionValue, out error)) return false;
                    if (!int.TryParse(compressionValue, out int compression) || compression is < 0 or > 60)
                    {
                        error = "压缩率必须是 0 到 60 之间的整数。";
                        return false;
                    }
                    options.Compression = compression;
                    break;
                case "--overwrite":
                case "--force":
                    options.Overwrite = true;
                    break;
                default:
                    if (original.StartsWith('-'))
                    {
                        error = $"未知参数“{original}”。";
                        return false;
                    }
                    options.Sources.Add(original);
                    break;
            }
        }

        if (options.Sources.Count == 0)
        {
            error = "转换时必须至少提供一个输入路径。";
            return false;
        }
        if (!string.IsNullOrWhiteSpace(options.DestinationDirectory) && !string.IsNullOrWhiteSpace(options.OutputPath))
        {
            error = "不能同时使用 -d/--destination 和 -o/--output。";
            return false;
        }
        return true;
    }

    private static bool TryReadValue(
        string[] args,
        ref int index,
        string argument,
        out string value,
        out string error)
    {
        if (++index >= args.Length)
        {
            value = string.Empty;
            error = $"参数“{argument}”缺少值。";
            return false;
        }

        value = args[index];
        error = string.Empty;
        return true;
    }

    private static int ClassifyExitCode(Exception exception) => exception switch
    {
        OutputFileExistsException => 2,
        FileNotFoundException or DirectoryNotFoundException => 4,
        UnauthorizedAccessException => 5,
        ArgumentException or InvalidDataException or MagickException => 6,
        OutOfMemoryException => 7,
        _ => 1
    };

    private sealed class CliOptions
    {
        public List<string> Sources { get; } = [];
        public string? DestinationDirectory { get; set; }
        public string? OutputPath { get; set; }
        public string Format { get; set; } = "png";
        public int Compression { get; set; }
        public bool Overwrite { get; set; }
    }
}
