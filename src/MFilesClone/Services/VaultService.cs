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

    public void DeleteFile(string vaultFileName)
    {
        File.Delete(GetFullPath(vaultFileName));
    }

    public string ExportFile(string vaultFileName, string destinationFolder, string originalFileName)
    {
        var destinationPath = GetUniqueDestinationPath(destinationFolder, originalFileName);
        File.Copy(GetFullPath(vaultFileName), destinationPath, overwrite: false);
        return destinationPath;
    }

    private static string GetUniqueDestinationPath(string folder, string originalFileName)
    {
        var candidate = Path.Combine(folder, originalFileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
        var extension = Path.GetExtension(originalFileName);
        var counter = 1;

        while (File.Exists(candidate))
        {
            candidate = Path.Combine(folder, $"{nameWithoutExtension} ({counter}){extension}");
            counter++;
        }

        return candidate;
    }
}
