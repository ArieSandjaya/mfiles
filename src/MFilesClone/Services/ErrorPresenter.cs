using System.Windows;

namespace MFilesClone.Services;

public static class ErrorPresenter
{
    public static void Show(string context, Exception ex)
    {
        MessageBox.Show($"{context}:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
