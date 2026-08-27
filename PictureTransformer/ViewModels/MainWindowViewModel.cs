using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using PictureTransformer.Core;
using PictureTransformer.Models;
using PictureTransformer.Services;

namespace PictureTransformer.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IFileDialogService _dialogs;
    private readonly IImageConversionService _converter;
    private readonly AsyncRelayCommand _convertCommand;
    private OutputFormatOption _selectedOutputFormat;
    private int _compression;
    private string? _outputDirectory;
    private bool _isBusy;

    public MainWindowViewModel(IFileDialogService dialogs, IImageConversionService converter)
    {
        _dialogs = dialogs;
        _converter = converter;
        OutputFormats = ImageFormatCatalog.OutputFormats;
        _selectedOutputFormat = OutputFormats.First(format => format.Extension == "png");
        AddImagesCommand = new RelayCommand(AddImages, () => !IsBusy);
        AddFolderCommand = new RelayCommand(AddFolder, () => !IsBusy);
        SelectOutputDirectoryCommand = new RelayCommand(SelectOutputDirectory, () => !IsBusy);
        _convertCommand = new AsyncRelayCommand(ConvertAsync, () => Queue.Count > 0 && !IsBusy);
        ConvertCommand = _convertCommand;
        Queue.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(QueueCountText));
            OnPropertyChanged(nameof(HasFiles));
            _convertCommand.RaiseCanExecuteChanged();
        };
    }

    public ObservableCollection<ImageQueueItem> Queue { get; } = [];
    public IReadOnlyList<OutputFormatOption> OutputFormats { get; }
    public RelayCommand AddImagesCommand { get; }
    public RelayCommand AddFolderCommand { get; }
    public RelayCommand SelectOutputDirectoryCommand { get; }
    public System.Windows.Input.ICommand ConvertCommand { get; }

    public OutputFormatOption SelectedOutputFormat
    {
        get => _selectedOutputFormat;
        set
        {
            if (SetField(ref _selectedOutputFormat, value))
            {
                if (!value.SupportsCompression) Compression = 0;
                OnPropertyChanged(nameof(IsCompressionEnabled));
            }
        }
    }

    public int Compression
    {
        get => _compression;
        set
        {
            if (SetField(ref _compression, Math.Clamp(value, 0, 60))) OnPropertyChanged(nameof(CompressionText));
        }
    }

    public bool IsCompressionEnabled => SelectedOutputFormat.SupportsCompression && !IsBusy;
    public bool CanEditSettings => !IsBusy;
    public string CompressionText => $"{Compression}%";
    public string QueueCountText => $"{Queue.Count} 个文件";
    public bool HasFiles => Queue.Count > 0;
    public string OutputDirectoryText => string.IsNullOrWhiteSpace(OutputDirectory) ? "与源文件相同目录" : OutputDirectory;

    public string? OutputDirectory
    {
        get => _outputDirectory;
        private set { if (SetField(ref _outputDirectory, value)) OnPropertyChanged(nameof(OutputDirectoryText)); }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(IsCompressionEnabled));
            OnPropertyChanged(nameof(CanEditSettings));
            AddImagesCommand.RaiseCanExecuteChanged();
            AddFolderCommand.RaiseCanExecuteChanged();
            SelectOutputDirectoryCommand.RaiseCanExecuteChanged();
            _convertCommand.RaiseCanExecuteChanged();
        }
    }

    public event Action<string, string>? MessageRequested;
    public event PropertyChangedEventHandler? PropertyChanged;

    public void AddDroppedFiles(IEnumerable<string> paths)
    {
        if (!IsBusy) AddFiles(paths);
    }
    private void AddImages() => AddFiles(_dialogs.SelectImageFiles());

    private void AddFolder()
    {
        string? folder = _dialogs.SelectInputFolder();
        if (folder is not null) AddFiles(ImageFormatCatalog.EnumerateImages(folder));
    }

    private void SelectOutputDirectory()
    {
        string? folder = _dialogs.SelectOutputFolder();
        if (folder is not null) OutputDirectory = folder;
    }

    private void AddFiles(IEnumerable<string> paths)
    {
        var existing = Queue.Select(item => item.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        int unsupported = 0;
        foreach (string path in paths)
        {
            if (!ImageFormatCatalog.IsSupportedInput(path)) { unsupported++; continue; }
            string fullPath = Path.GetFullPath(path);
            if (existing.Add(fullPath)) Queue.Add(new ImageQueueItem(fullPath));
        }
        if (unsupported > 0) MessageRequested?.Invoke("提示", $"已跳过 {unsupported} 个不受支持的文件。");
    }

    private async Task ConvertAsync()
    {
        IsBusy = true;
        int succeeded = 0;
        int failed = 0;
        try
        {
            foreach (ImageQueueItem item in Queue)
            {
                item.Status = "正在转换";
                item.Error = null;
                try
                {
                    ConversionResult result = await _converter.ConvertAsync(new ConversionOptions(item.Path, SelectedOutputFormat, Compression, OutputDirectory));
                    item.OutputPath = result.OutputPath;
                    item.Status = "转换完成";
                    succeeded++;
                }
                catch (Exception ex)
                {
                    item.Error = ex.Message;
                    item.Status = "转换失败";
                    failed++;
                }
            }
        }
        finally { IsBusy = false; }
        MessageRequested?.Invoke("转换完成", $"成功 {succeeded} 个，失败 {failed} 个。");
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
