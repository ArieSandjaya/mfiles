using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MFilesClone.Services;
using MFilesClone.Shared;

namespace MFilesClone.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private const int PageSize = 100;

    private readonly DocumentService _documentService;
    private readonly CategoryService _categoryService;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private CategoryDto? selectedCategoryFilter;

    [ObservableProperty]
    private bool hasMore;

    [ObservableProperty]
    private int totalCount;

    public ObservableCollection<DocumentDto> Documents { get; } = new();
    public ObservableCollection<CategoryDto> Categories { get; } = new();

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
        var page = await _documentService.SearchPageAsync(SearchText, SelectedCategoryFilter?.Id, skip: 0, take: PageSize);

        if (sequence != _refreshSequence)
        {
            // A newer refresh has already started; its results take precedence.
            return;
        }

        Documents.ReplaceAll(page.Items);
        TotalCount = page.TotalCount;
        HasMore = Documents.Count < page.TotalCount;
    }

    [RelayCommand]
    public async Task LoadMoreAsync()
    {
        if (!HasMore)
        {
            return;
        }

        var sequence = ++_refreshSequence;
        var page = await _documentService.SearchPageAsync(SearchText, SelectedCategoryFilter?.Id, skip: Documents.Count, take: PageSize);

        if (sequence != _refreshSequence)
        {
            return;
        }

        foreach (var document in page.Items)
        {
            Documents.Add(document);
        }

        TotalCount = page.TotalCount;
        HasMore = Documents.Count < page.TotalCount;
    }

    partial void OnSearchTextChanged(string value) => _ = RefreshAsync();

    partial void OnSelectedCategoryFilterChanged(CategoryDto? value) => _ = RefreshAsync();

    public async Task AddDocumentFromFileAsync(string sourceFilePath, string title, int? categoryId, IEnumerable<(string Key, string Value)> metadata)
    {
        await _documentService.CreateDocumentAsync(sourceFilePath, title, categoryId, metadata);
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync(DocumentDto? document)
    {
        if (document is null)
        {
            return;
        }

        await _documentService.DeleteAsync(document.Id);
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task CheckOutSelectedAsync(DocumentDto? document)
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
    private async Task CancelCheckOutSelectedAsync(DocumentDto? document)
    {
        if (document is null)
        {
            return;
        }

        await _documentService.CancelCheckOutAsync(document.Id);
        await RefreshAsync();
    }
}
