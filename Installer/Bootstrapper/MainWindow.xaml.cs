using System;
using System.IO;
using System.Windows;
using Forms = System.Windows.Forms;

namespace PictureTransformer.SetupUI;

public partial class MainWindow : Window
{
    private readonly PictureTransformerBootstrapper bootstrapper;

    public MainWindow(PictureTransformerBootstrapper bootstrapper, string installPath, bool addToPath)
    {
        InitializeComponent();
        this.bootstrapper = bootstrapper;
        InstallPathTextBox.Text = installPath;
        AddToPathCheckBox.IsChecked = addToPath;
    }

    public string InstallPath => InstallPathTextBox.Text.Trim();

    public bool AddToPath => AddToPathCheckBox.IsChecked == true;

    public void ShowReady(bool installed)
    {
        InstallPanel.Visibility = installed ? Visibility.Collapsed : Visibility.Visible;
        MaintenancePanel.Visibility = installed ? Visibility.Visible : Visibility.Collapsed;
        ProgressPanel.Visibility = Visibility.Collapsed;
        ResultPanel.Visibility = Visibility.Collapsed;
    }

    public void ShowProgress(string header)
    {
        InstallPanel.Visibility = Visibility.Collapsed;
        MaintenancePanel.Visibility = Visibility.Collapsed;
        ResultPanel.Visibility = Visibility.Collapsed;
        ProgressPanel.Visibility = Visibility.Visible;
        ProgressHeader.Text = header;
        ProgressText.Text = "正在准备…";
        InstallProgressBar.Value = 0;
    }

    public void SetProgress(int percentage)
    {
        InstallProgressBar.Value = Math.Clamp(percentage, 0, 100);
        ProgressText.Text = $"已完成 {InstallProgressBar.Value:0}%";
    }

    public void ShowResult(bool succeeded, string message)
    {
        InstallPanel.Visibility = Visibility.Collapsed;
        MaintenancePanel.Visibility = Visibility.Collapsed;
        ProgressPanel.Visibility = Visibility.Collapsed;
        ResultPanel.Visibility = Visibility.Visible;
        ResultHeader.Text = succeeded ? "操作成功完成" : "安装程序失败";
        ResultText.Text = message;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = "请选择安装 PictureTransformer 的父文件夹",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true,
            SelectedPath = GetParentFolder(InstallPath)
        };

        if (dialog.ShowDialog() == Forms.DialogResult.OK)
        {
            InstallPathTextBox.Text = AppendProductFolder(dialog.SelectedPath);
            InstallPathTextBox.CaretIndex = InstallPathTextBox.Text.Length;
            InstallPathTextBox.Focus();
        }
    }

    private void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(InstallPath))
        {
            System.Windows.MessageBox.Show(this, "请输入完整安装目录。", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            if (!Path.IsPathFullyQualified(InstallPath))
            {
                System.Windows.MessageBox.Show(this, "请输入包含盘符的完整安装目录。", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bootstrapper.BeginInstall(Path.GetFullPath(InstallPath), AddToPath);
        }
        catch (Exception)
        {
            System.Windows.MessageBox.Show(this, "安装目录无效，请重新选择或输入。", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RepairButton_Click(object sender, RoutedEventArgs e) => bootstrapper.BeginRepair();

    private void UninstallButton_Click(object sender, RoutedEventArgs e) => bootstrapper.BeginUninstall();

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private static string AppendProductFolder(string selectedPath)
    {
        var normalized = Path.TrimEndingDirectorySeparator(selectedPath.Trim());
        return string.Equals(Path.GetFileName(normalized), "PictureTransformer", StringComparison.OrdinalIgnoreCase)
            ? normalized
            : Path.Combine(normalized, "PictureTransformer");
    }

    private static string GetParentFolder(string installPath)
    {
        try
        {
            var normalized = Path.TrimEndingDirectorySeparator(installPath);
            if (string.Equals(Path.GetFileName(normalized), "PictureTransformer", StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetDirectoryName(normalized) ?? normalized;
            }

            return Directory.Exists(normalized) ? normalized : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        }
        catch
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        }
    }
}
