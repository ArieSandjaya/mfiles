using System.Windows;
using MFilesClone.ViewModels;

namespace MFilesClone.Views;

public partial class CheckInDialog : Window
{
    private readonly CheckInViewModel _viewModel;

    public CheckInDialog(CheckInViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    public string NewFilePath => _viewModel.NewFilePath;
    public string Comment => _viewModel.Comment;

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_viewModel.NewFilePath))
        {
            MessageBox.Show("Pilih file pengganti terlebih dahulu.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }
}
