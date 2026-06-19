using System.IO;
using System.Windows;
using Microsoft.Win32;
using MFilesClone.Models;
using MFilesClone.Services;
using MFilesClone.ViewModels;

namespace MFilesClone.Views;

public partial class MainWindow : Window, IDocumentImportPrompt
{
    private readonly MainViewModel _viewModel;
    private readonly CategoryService _categoryService;
    private readonly DocumentService _documentService;
    private readonly VaultService _vaultService;
    private readonly VfsMountService _vfsMountService;

    public MainWindow(
        MainViewModel viewModel,
        CategoryService categoryService,
        DocumentService documentService,
        VaultService vaultService,
        VfsMountService vfsMountService)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _categoryService = categoryService;
        _documentService = documentService;
        _vaultService = vaultService;
        _vfsMountService = vfsMountService;
        DataContext = _viewModel;

        Loaded += async (_, _) => await _viewModel.InitializeAsync();
        Closed += (_, _) => _vfsMountService.Unmount();
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
        await ImportFilesAsync(filePaths);
    }

    public async Task ImportFilesAsync(IEnumerable<string> filePaths)
    {
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

    public async Task<bool> PromptAndImportAsync(string sourceFilePath, int? categoryId)
    {
        return await Dispatcher.InvokeAsync(async () =>
        {
            var categories = await _categoryService.GetAllAsync();
            var preselected = categoryId.HasValue ? categories.FirstOrDefault(c => c.Id == categoryId) : null;
            var added = await ShowMetadataDialogAsync(sourceFilePath, categories, preselected);

            if (added)
            {
                await _viewModel.RefreshCategoriesAsync();
            }

            return added;
        }).Task.Unwrap();
    }

    private async Task<bool> ShowMetadataDialogAsync(string filePath, List<Category> categories, Category? preselectedCategory = null)
    {
        var dialogViewModel = new MetadataEntryViewModel(_categoryService, filePath);
        dialogViewModel.SetCategories(categories);

        if (preselectedCategory is not null)
        {
            dialogViewModel.SelectedCategory = preselectedCategory;
        }

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

    private async void CheckIn_Click(object sender, RoutedEventArgs e)
    {
        if (DocumentsGrid.SelectedItem is not Document document)
        {
            return;
        }

        if (!document.IsCheckedOut)
        {
            MessageBox.Show("Dokumen harus di-check-out sebelum check-in.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialogViewModel = new CheckInViewModel(document.CurrentVersion?.OriginalFileName ?? document.Title);
        var dialog = new CheckInDialog(dialogViewModel) { Owner = this };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            await _documentService.CheckInAsync(document.Id, dialog.NewFilePath, dialog.Comment);
            await _viewModel.RefreshAsync();
        }
        catch (Exception ex)
        {
            ErrorPresenter.Show("Gagal check-in dokumen", ex);
        }
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        if (DocumentsGrid.SelectedItem is not Document document)
        {
            return;
        }

        var folderDialog = new OpenFolderDialog { Title = "Pilih folder tujuan" };
        if (folderDialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var path = await _documentService.ExportAsync(document.Id, folderDialog.FolderName);
            MessageBox.Show($"Berhasil diekspor ke:\n{path}", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorPresenter.Show("Gagal mengekspor file", ex);
        }
    }

    private void VersionHistory_Click(object sender, RoutedEventArgs e)
    {
        if (DocumentsGrid.SelectedItem is not Document document)
        {
            return;
        }

        var dialog = new VersionHistoryDialog(_documentService, document.Id, document.Title) { Owner = this };
        dialog.ShowDialog();
    }

    private void Preview_Click(object sender, RoutedEventArgs e)
    {
        if (DocumentsGrid.SelectedItem is not Document document || document.CurrentVersion is null)
        {
            return;
        }

        string tempPath;

        try
        {
            tempPath = _vaultService.ExportToTempFile(document.CurrentVersion.VaultFileName, document.CurrentVersion.OriginalFileName);
        }
        catch (Exception ex)
        {
            ErrorPresenter.Show("Gagal membuka pratinjau", ex);
            return;
        }

        var preview = new PreviewWindow(tempPath, document.Title) { Owner = this };
        preview.Closed += (_, _) =>
        {
            try
            {
                File.Delete(tempPath);
            }
            catch (IOException)
            {
                // Best-effort cleanup; the OS temp folder is reclaimed eventually anyway.
            }
        };
        preview.Show();
    }

    private void MountDrive_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _vfsMountService.Mount();
        }
        catch (Exception ex)
        {
            ErrorPresenter.Show("Gagal mount virtual drive (pastikan driver Dokan terinstall)", ex);
        }
    }

    private void UnmountDrive_Click(object sender, RoutedEventArgs e)
    {
        _vfsMountService.Unmount();
    }
}
