using System.Windows;
using Microsoft.Win32;
using MFilesClone.Services;
using MFilesClone.ViewModels;

namespace MFilesClone.Views;

public partial class VersionHistoryDialog : Window
{
    private readonly DocumentService _documentService;
    private readonly int _documentId;
    private readonly VersionHistoryViewModel _viewModel = new();

    public VersionHistoryDialog(DocumentService documentService, int documentId, string documentTitle)
    {
        InitializeComponent();

        _documentService = documentService;
        _documentId = documentId;
        DataContext = _viewModel;
        Title = $"Riwayat Versi - {documentTitle}";

        Loaded += async (_, _) => _viewModel.SetVersions(await _documentService.GetVersionsAsync(_documentId));
    }

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedVersion is not { } version)
        {
            MessageBox.Show("Pilih versi yang ingin diekspor.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var folderDialog = new OpenFolderDialog { Title = "Pilih folder tujuan" };
        if (folderDialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var path = await _documentService.ExportAsync(_documentId, folderDialog.FolderName, version.Id);
            MessageBox.Show($"Berhasil diekspor ke:\n{path}", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorPresenter.Show("Gagal mengekspor file", ex);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
