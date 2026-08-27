using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace PictureTransformer.Models;

public sealed class ImageQueueItem : INotifyPropertyChanged
{
    private string _status = "等待转换";
    private string? _outputPath;
    private string? _error;

    public ImageQueueItem(string path)
    {
        Path = System.IO.Path.GetFullPath(path);
        Name = System.IO.Path.GetFileName(path);
        var info = new FileInfo(path);
        Details = $"{System.IO.Path.GetExtension(path).TrimStart('.').ToUpperInvariant()} · {FormatFileSize(info.Length)}";
    }

    public string Path { get; }
    public string Name { get; }
    public string Details { get; }

    public string Status { get => _status; set => SetField(ref _status, value); }
    public string? OutputPath { get => _outputPath; set => SetField(ref _outputPath, value); }
    public string? Error { get => _error; set => SetField(ref _error, value); }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static string FormatFileSize(long bytes) => bytes switch
    {
        >= 1024 * 1024 => $"{bytes / 1024d / 1024d:F1} MB",
        >= 1024 => $"{bytes / 1024d:F1} KB",
        _ => $"{bytes} B"
    };
}
