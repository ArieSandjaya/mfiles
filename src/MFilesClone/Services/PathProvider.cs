using System.IO;

namespace MFilesClone.Services;

public static class PathProvider
{
    public static string AppDataRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MFilesClone");

    public static string DbPath => Path.Combine(AppDataRoot, "mfiles.db");

    public static string VaultRoot => Path.Combine(AppDataRoot, "Vault");

    public static string ConnectionString => $"Data Source={DbPath}";
}
