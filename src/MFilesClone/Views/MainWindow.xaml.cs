using System.Windows;
using MFilesClone.Services;
using MFilesClone.ViewModels;

namespace MFilesClone.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly CategoryService _categoryService;

    public MainWindow(MainViewModel viewModel, CategoryService categoryService)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _categoryService = categoryService;
        DataContext = _viewModel;

        Loaded += async (_, _) => await _viewModel.InitializeAsync();
    }

    private void Window_DragEnter(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop)!;

        foreach (var filePath in filePaths)
        {
            if (File.Exists(filePath))
            {
                await ShowMetadataDialogAsync(filePath);
            }
        }
    }

    private async Task ShowMetadataDialogAsync(string filePath)
    {
        var dialogViewModel = new MetadataEntryViewModel(_categoryService, filePath);
        await dialogViewModel.LoadCategoriesAsync();

        var dialog = new MetadataEntryDialog(dialogViewModel) { Owner = this };

        if (dialog.ShowDialog() == true)
        {
            await _viewModel.AddDocumentFromFileAsync(
                dialogViewModel.SourceFilePath,
                dialogViewModel.Title,
                dialogViewModel.SelectedCategory?.Id,
                dialogViewModel.BuildMetadata());
        }
    }
}
