using System.IO;
using System.Security.Cryptography;

namespace MFilesClone.Services;

// Files are encrypted at rest with AES-256-GCM. The key is generated once and stored
// DPAPI-protected under the current Windows user, so the vault is unreadable outside
// this app without any password the user has to remember (but also non-portable to
// another machine/account).
public class VaultService
{
    private const int KeySizeBytes = 32;
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    private readonly byte[] _key;

    public VaultService()
    {
        _key = LoadOrCreateKey();
    }

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

        EncryptToFile(File.ReadAllBytes(sourceFilePath), destinationPath);

        return vaultFileName;
    }

    public byte[] ReadFileBytes(string vaultFileName) => DecryptFromFile(GetFullPath(vaultFileName));

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
        File.WriteAllBytes(destinationPath, DecryptFromFile(GetFullPath(vaultFileName)));
        return destinationPath;
    }

    public string ExportToTempFile(string vaultFileName, string originalFileName)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "MFilesClonePreview");
        Directory.CreateDirectory(tempDir);

        var tempPath = Path.Combine(tempDir, $"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}");
        File.WriteAllBytes(tempPath, DecryptFromFile(GetFullPath(vaultFileName)));
        return tempPath;
    }

    private void EncryptToFile(byte[] plaintext, string destinationPath)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSizeBytes];

        using var aesGcm = new AesGcm(_key, TagSizeBytes);
        aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);

        using var fileStream = File.Create(destinationPath);
        fileStream.Write(nonce);
        fileStream.Write(tag);
        fileStream.Write(ciphertext);
    }

    private byte[] DecryptFromFile(string sourcePath)
    {
        var data = File.ReadAllBytes(sourcePath);
        var nonce = data.AsSpan(0, NonceSizeBytes);
        var tag = data.AsSpan(NonceSizeBytes, TagSizeBytes);
        var ciphertext = data.AsSpan(NonceSizeBytes + TagSizeBytes);
        var plaintext = new byte[ciphertext.Length];

        using var aesGcm = new AesGcm(_key, TagSizeBytes);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }

    private static byte[] LoadOrCreateKey()
    {
        Directory.CreateDirectory(PathProvider.AppDataRoot);

        if (File.Exists(PathProvider.VaultKeyPath))
        {
            var protectedKey = File.ReadAllBytes(PathProvider.VaultKeyPath);
            return ProtectedData.Unprotect(protectedKey, optionalEntropy: null, DataProtectionScope.CurrentUser);
        }

        var key = RandomNumberGenerator.GetBytes(KeySizeBytes);
        var protectedNewKey = ProtectedData.Protect(key, optionalEntropy: null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(PathProvider.VaultKeyPath, protectedNewKey);
        return key;
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
