using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MFilesClone.Data;
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

            Directory.CreateDirectory(PathProvider.AppDataRoot);

            using (var context = Services.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext())
            {
                context.Database.Migrate();
            }

            Services.GetRequiredService<VaultService>().EnsureVaultExists();

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            ErrorPresenter.Show("Gagal memulai aplikasi", ex);
            Shutdown(-1);
        }
    }

    private static void ConfigureServices(ServiceCollection services)
    {
        services.AddDbContextFactory<AppDbContext>();
        services.AddSingleton<VaultService>();
        services.AddSingleton<DocumentService>();
        services.AddSingleton<CategoryService>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
    }
}
