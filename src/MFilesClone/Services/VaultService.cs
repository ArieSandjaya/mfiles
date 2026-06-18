using System.IO;

namespace MFilesClone.Services;

public class VaultService
{
    public void EnsureVaultExists()
    {
        Directory.CreateDirectory(PathProvider.VaultRoot);

        if (OperatingSystem.IsWindows())
        {
            File.SetAttributes(PathProvider.VaultRoot, FileAttributes.Hidden);
        }
    }

    public string StoreFile(string sourceFilePath)
    {
        var extension = Path.GetExtension(sourceFilePath);
        var vaultFileName = $"{Guid.NewGuid()}{extension}";
        var destinationPath = Path.Combine(PathProvider.VaultRoot, vaultFileName);

        File.Copy(sourceFilePath, destinationPath, overwrite: false);

        return vaultFileName;
    }

    public string GetFullPath(string vaultFileName)
    {
        return Path.Combine(PathProvider.VaultRoot, vaultFileName);
    }
}
