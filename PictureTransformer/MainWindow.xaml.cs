using System.Windows;
using PictureTransformer.Core;
using PictureTransformer.Services;
using PictureTransformer.ViewModels;

namespace PictureTransformer;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel(new FileDialogService(), new ImageConversionService());
        _viewModel.MessageRequested += (title, message) =>
            MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        DataContext = _viewModel;
    }

    private void DropZone_OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void DropZone_OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
            _viewModel.AddDroppedFiles(paths);
        e.Handled = true;
    }

    private void DropZone_OnMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_viewModel.AddImagesCommand.CanExecute(null))
            _viewModel.AddImagesCommand.Execute(null);
    }
}
