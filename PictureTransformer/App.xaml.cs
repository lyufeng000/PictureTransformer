using System.Reflection;
using System.Windows;
using PictureTransformer.Services;

namespace PictureTransformer;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();

        await CheckForUpdatesAsync(mainWindow);
    }

    private static async Task CheckForUpdatesAsync(Window owner)
    {
        using var cancellation = new CancellationTokenSource();
        void CancelCheck(object? sender, EventArgs args) => cancellation.Cancel();
        owner.Closed += CancelCheck;

        try
        {
            var updateService = new GitHubUpdateService();
            Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
            UpdateInfo? update = await updateService.CheckForUpdateAsync(currentVersion, cancellation.Token);

            if (update is null || !owner.IsVisible)
                return;

            var dialog = new UpdateDialog(update, updateService) { Owner = owner };
            dialog.ShowDialog();
        }
        catch
        {
            // 更新检查失败时保持静默，不影响软件启动和图片转换。
        }
        finally
        {
            owner.Closed -= CancelCheck;
        }
    }
}
