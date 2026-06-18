using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MFilesClone.Models;
using MFilesClone.Services;

namespace MFilesClone.ViewModels;

public partial class MetadataEntryViewModel : ViewModelBase
{
    private readonly CategoryService _categoryService;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string tags = string.Empty;

    [ObservableProperty]
    private Category? selectedCategory;

    [ObservableProperty]
    private string newCategoryName = string.Empty;

    public ObservableCollection<Category> Categories { get; } = new();

    public string SourceFilePath { get; }
    public string OriginalFileName { get; }
    public long FileSizeBytes { get; }

    public MetadataEntryViewModel(CategoryService categoryService, string sourceFilePath)
    {
        _categoryService = categoryService;
        SourceFilePath = sourceFilePath;
        OriginalFileName = Path.GetFileName(sourceFilePath);
        FileSizeBytes = new FileInfo(sourceFilePath).Length;
        Title = Path.GetFileNameWithoutExtension(sourceFilePath);
    }

    public async Task LoadCategoriesAsync()
    {
        Categories.Clear();
        foreach (var category in await _categoryService.GetAllAsync())
        {
            Categories.Add(category);
        }
    }

    [RelayCommand]
    private async Task AddCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCategoryName))
        {
            return;
        }

        try
        {
            var category = await _categoryService.CreateAsync(NewCategoryName.Trim());
            Categories.Add(category);
            SelectedCategory = category;
            NewCategoryName = string.Empty;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Gagal menambah kategori:\n{ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    public IEnumerable<(string Key, string Value)> BuildMetadata()
    {
        if (!string.IsNullOrWhiteSpace(Description))
        {
            yield return ("Description", Description.Trim());
        }

        foreach (var tag in Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            yield return ("Tag", tag);
        }
    }
}
