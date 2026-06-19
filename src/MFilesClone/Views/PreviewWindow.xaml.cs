using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace MFilesClone.Views;

public partial class PreviewWindow : Window
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".bmp", ".gif",
    };

    public PreviewWindow(string filePath, string title)
    {
        InitializeComponent();
        Title = $"Preview - {title}";

        var extension = Path.GetExtension(filePath);

        if (ImageExtensions.Contains(extension))
        {
            ImageControl.Source = new BitmapImage(new Uri(filePath));
            ImageControl.Visibility = Visibility.Visible;
        }
        else if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            WebViewControl.Visibility = Visibility.Visible;
            WebViewControl.Source = new Uri(filePath);
        }
        else
        {
            UnsupportedText.Visibility = Visibility.Visible;
        }
    }
}
