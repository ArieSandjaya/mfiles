using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MFilesClone.Services;
using MFilesClone.ViewModels;
using MFilesClone.Views;

namespace MFilesClone;

public partial class App : Application
{
    public IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            Services = services.BuildServiceProvider();

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            if (e.Args.Length > 0)
            {
                _ = mainWindow.ImportFilesAsync(e.Args);
            }
        }
        catch (Exception ex)
        {
            ErrorPresenter.Show("Gagal memulai aplikasi", ex);
            Shutdown(-1);
        }
    }

    private static void ConfigureServices(ServiceCollection services)
    {
        services.AddSingleton<DocumentService>();
        services.AddSingleton<CategoryService>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<VaultFileSystem>();
        services.AddSingleton<VfsMountService>();
        services.AddSingleton<IDocumentImportPrompt>(sp => sp.GetRequiredService<MainWindow>());
    }
}
