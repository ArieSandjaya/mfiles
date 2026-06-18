using System.Windows;
using MFilesClone.ViewModels;

namespace MFilesClone.Views;

public partial class MetadataEntryDialog : Window
{
    private readonly MetadataEntryViewModel _viewModel;

    public MetadataEntryDialog(MetadataEntryViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_viewModel.Title))
        {
            MessageBox.Show("Judul tidak boleh kosong.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }
}
