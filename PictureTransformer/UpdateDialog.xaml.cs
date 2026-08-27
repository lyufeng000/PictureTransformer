using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using PictureTransformer.Services;

namespace PictureTransformer;

public partial class UpdateDialog : Window
{
    private readonly UpdateInfo _update;
    private readonly IUpdateService _updateService;
    private readonly CancellationTokenSource _downloadCancellation = new();
    private bool _isDownloading;
    private bool _installerStarted;

    public UpdateDialog(UpdateInfo update, IUpdateService updateService)
    {
        InitializeComponent();
        _update = update ?? throw new ArgumentNullException(nameof(update));
        _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));

        VersionText.Text = $"可用版本：{update.TagName}";
        ReleaseNotesTextBox.Text = update.ReleaseNotes;
    }

    private async void UpdateButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_isDownloading)
            return;

        _isDownloading = true;
        UpdateButton.IsEnabled = false;
        LaterButton.IsEnabled = false;
        DownloadProgress.IsIndeterminate = true;
        DownloadProgress.Visibility = Visibility.Visible;
        StatusText.Text = "正在下载安装程序…";
        StatusText.Visibility = Visibility.Visible;

        var progress = new Progress<double>(percentage =>
        {
            DownloadProgress.IsIndeterminate = false;
            DownloadProgress.Value = Math.Clamp(percentage, 0, 100);
            StatusText.Text = $"正在下载安装程序… {percentage:0}%";
        });

        try
        {
            string installerPath = await _updateService.DownloadInstallerAsync(
                _update,
                progress: progress,
                cancellationToken: _downloadCancellation.Token);

            StatusText.Text = "下载完成，正在启动安装程序…";
            Process? installer = Process.Start(new ProcessStartInfo(installerPath)
            {
                UseShellExecute = true
            });
            if (installer is null)
                throw new InvalidOperationException("系统没有启动安装程序。");

            _installerStarted = true;
            Application.Current.Shutdown();
        }
        catch (OperationCanceledException)
        {
            if (IsVisible)
                ResetAfterFailure("更新下载已取消。");
        }
        catch (Exception ex)
        {
            if (!IsVisible)
                return;

            ResetAfterFailure("下载或启动安装程序失败，可以稍后重试。");
            MessageBox.Show(
                this,
                $"无法完成更新：{ex.Message}",
                "更新失败",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void LaterButton_OnClick(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_isDownloading && !_installerStarted)
            _downloadCancellation.Cancel();

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _downloadCancellation.Dispose();
        base.OnClosed(e);
    }

    private void ResetAfterFailure(string status)
    {
        _isDownloading = false;
        DownloadProgress.IsIndeterminate = false;
        UpdateButton.IsEnabled = true;
        LaterButton.IsEnabled = true;
        StatusText.Text = status;
    }
}
