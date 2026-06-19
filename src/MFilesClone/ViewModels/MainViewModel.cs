using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MFilesClone.Models;
using MFilesClone.Services;

namespace MFilesClone.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly DocumentService _documentService;
    private readonly CategoryService _categoryService;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private Category? selectedCategoryFilter;

    public ObservableCollection<Document> Documents { get; } = new();
    public ObservableCollection<Category> Categories { get; } = new();

    public MainViewModel(DocumentService documentService, CategoryService categoryService)
    {
        _documentService = documentService;
        _categoryService = categoryService;
    }

    private int _refreshSequence;

    public async Task InitializeAsync()
    {
        await RefreshCategoriesAsync();
        await RefreshAsync();
    }

    public async Task RefreshCategoriesAsync()
    {
        Categories.ReplaceAll(await _categoryService.GetAllAsync());
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        var sequence = ++_refreshSequence;
        var results = await _documentService.SearchAsync(SearchText, SelectedCategoryFilter?.Id);

        if (sequence != _refreshSequence)
        {
            // A newer refresh has already started; its results take precedence.
            return;
        }

        Documents.ReplaceAll(results);
    }

    partial void OnSearchTextChanged(string value) => _ = RefreshAsync();

    partial void OnSelectedCategoryFilterChanged(Category? value) => _ = RefreshAsync();

    public async Task AddDocumentFromFileAsync(string sourceFilePath, string title, int? categoryId, IEnumerable<(string Key, string Value)> metadata)
    {
        await _documentService.CreateDocumentAsync(sourceFilePath, title, categoryId, metadata);
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync(Document? document)
    {
        if (document is null)
        {
            return;
        }

        await _documentService.DeleteAsync(document.Id);
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task CheckOutSelectedAsync(Document? document)
    {
        if (document is null)
        {
            return;
        }

        try
        {
            await _documentService.CheckOutAsync(document.Id);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ErrorPresenter.Show("Gagal check-out dokumen", ex);
        }
    }

    [RelayCommand]
    private async Task CancelCheckOutSelectedAsync(Document? document)
    {
        if (document is null)
        {
            return;
        }

        await _documentService.CancelCheckOutAsync(document.Id);
        await RefreshAsync();
    }
}
