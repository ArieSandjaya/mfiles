using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace MFilesClone.ViewModels;

public partial class CheckInViewModel : ViewModelBase
{
    [ObservableProperty]
    private string newFilePath = string.Empty;

    [ObservableProperty]
    private string comment = string.Empty;

    public string OriginalFileName { get; }

    public CheckInViewModel(string originalFileName)
    {
        OriginalFileName = originalFileName;
    }

    [RelayCommand]
    private void BrowseFile()
    {
        var dialog = new OpenFileDialog { Title = "Pilih file pengganti" };
        if (dialog.ShowDialog() == true)
        {
            NewFilePath = dialog.FileName;
        }
    }
}
