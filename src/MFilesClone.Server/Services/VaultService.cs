using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace MFilesClone.Server.Services;

// Files are encrypted at rest with AES-256-GCM. The key is generated once and stored
// DPAPI-protected under the account running the server process, so the vault is
// unreadable outside this server process without any password to remember (but also
// non-portable to another machine/account).
public class VaultService
{
    private const int KeySizeBytes = 32;
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    private readonly string _vaultRoot;
    private readonly string _vaultKeyPath;
    private readonly byte[] _key;

    [SupportedOSPlatform("windows")]
    public VaultService(IConfiguration configuration)
    {
        _vaultRoot = configuration["VaultPath"] ?? throw new InvalidOperationException("VaultPath is not configured.");
        _vaultKeyPath = Path.Combine(Path.GetDirectoryName(_vaultRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) ?? _vaultRoot, "vault.key");
        _key = LoadOrCreateKey();
    }

    public void EnsureVaultExists()
    {
        Directory.CreateDirectory(_vaultRoot);
    }

    public string StoreFile(Stream sourceStream, string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var vaultFileName = $"{Guid.NewGuid()}{extension}";
        var destinationPath = Path.Combine(_vaultRoot, vaultFileName);

        using var memoryStream = new MemoryStream();
        sourceStream.CopyTo(memoryStream);

        EncryptToFile(memoryStream.ToArray(), destinationPath);

        return vaultFileName;
    }

    public byte[] ReadFileBytes(string vaultFileName) => DecryptFromFile(GetFullPath(vaultFileName));

    public string GetFullPath(string vaultFileName) => Path.Combine(_vaultRoot, vaultFileName);

    public void DeleteFile(string vaultFileName)
    {
        var path = GetFullPath(vaultFileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
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

    // DPAPI (DataProtectionScope.CurrentUser) is Windows-only by design: the brief calls
    // for the vault key to be protected under the server process's own running account,
    // which on non-Windows hosts has no equivalent and is out of scope for this pass.
    [SupportedOSPlatform("windows")]
    private byte[] LoadOrCreateKey()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_vaultKeyPath) ?? _vaultRoot);

        if (File.Exists(_vaultKeyPath))
        {
            var protectedKey = File.ReadAllBytes(_vaultKeyPath);
            return ProtectedData.Unprotect(protectedKey, optionalEntropy: null, DataProtectionScope.CurrentUser);
        }

        var key = RandomNumberGenerator.GetBytes(KeySizeBytes);
        var protectedNewKey = ProtectedData.Protect(key, optionalEntropy: null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(_vaultKeyPath, protectedNewKey);
        return key;
    }
}
