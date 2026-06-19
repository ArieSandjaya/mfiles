using System.IO;
using System.Windows;
using MFilesClone.Models;
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
        var categories = await _categoryService.GetAllAsync();
        var failures = new List<string>();
        var addedAny = false;

        foreach (var filePath in filePaths)
        {
            if (!File.Exists(filePath))
            {
                continue;
            }

            try
            {
                if (await ShowMetadataDialogAsync(filePath, categories))
                {
                    addedAny = true;
                }
            }
            catch (Exception ex)
            {
                failures.Add($"{Path.GetFileName(filePath)}: {ex.Message}");
            }
        }

        if (addedAny)
        {
            await _viewModel.RefreshCategoriesAsync();
        }

        if (failures.Count > 0)
        {
            MessageBox.Show(
                $"Gagal menambahkan {failures.Count} file:\n" + string.Join("\n", failures),
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task<bool> ShowMetadataDialogAsync(string filePath, List<Category> categories)
    {
        var dialogViewModel = new MetadataEntryViewModel(_categoryService, filePath);
        dialogViewModel.SetCategories(categories);

        var dialog = new MetadataEntryDialog(dialogViewModel) { Owner = this };

        if (dialog.ShowDialog() != true)
        {
            return false;
        }

        await _viewModel.AddDocumentFromFileAsync(
            dialogViewModel.SourceFilePath,
            dialogViewModel.Title,
            dialogViewModel.SelectedCategory?.Id,
            dialogViewModel.BuildMetadata());

        if (dialogViewModel.SelectedCategory is { } selected && categories.All(c => c.Id != selected.Id))
        {
            categories.Add(selected);
        }

        return true;
    }
}
