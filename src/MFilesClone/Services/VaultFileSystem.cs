using System.IO;
using System.Security.AccessControl;
using DokanNet;
using Microsoft.Extensions.DependencyInjection;
using MFilesClone.Models;
using FileAccess = DokanNet.FileAccess;

namespace MFilesClone.Services;

// Read-only view of the vault: "\<Category>\<Title><ext>" maps to a document's
// current version. Writing a new file directly under a category folder is the one
// supported "create" path — it buffers to a temp file and, on Cleanup, hands off to
// the same metadata dialog used by drag-and-drop, with the folder as the preselected
// category. IDocumentImportPrompt is resolved lazily via IServiceProvider (rather than
// injected directly) to avoid a circular dependency with MainWindow, which itself
// depends on VfsMountService -> VaultFileSystem.
public class VaultFileSystem : IDokanOperations
{
    private const string UncategorizedFolderName = "Tanpa Kategori";

    private readonly DocumentService _documentService;
    private readonly VaultService _vaultService;
    private readonly IServiceProvider _serviceProvider;

    private readonly Dictionary<string, Document> _pathToDocument = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _folderNames = new();
    private readonly Dictionary<string, MemoryStream> _pendingWrites = new(StringComparer.OrdinalIgnoreCase);

    public VaultFileSystem(DocumentService documentService, VaultService vaultService, IServiceProvider serviceProvider)
    {
        _documentService = documentService;
        _vaultService = vaultService;
        _serviceProvider = serviceProvider;
    }

    private void Refresh()
    {
        var documents = _documentService.GetAllAsync().GetAwaiter().GetResult();

        _pathToDocument.Clear();
        _folderNames.Clear();

        foreach (var group in documents.GroupBy(d => d.Category?.Name ?? UncategorizedFolderName))
        {
            _folderNames.Add(group.Key);
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var document in group)
            {
                if (document.CurrentVersion is null)
                {
                    continue;
                }

                var fileName = MakeUniqueFileName(document.Title + document.CurrentVersion.FileExtension, usedNames);
                _pathToDocument[$"\\{group.Key}\\{fileName}"] = document;
            }
        }
    }

    private static string MakeUniqueFileName(string fileName, HashSet<string> usedNames)
    {
        var candidate = fileName;
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var counter = 1;

        while (!usedNames.Add(candidate))
        {
            candidate = $"{nameWithoutExtension} ({counter}){extension}";
            counter++;
        }

        return candidate;
    }

    public NtStatus CreateFile(string fileName, FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, IDokanFileInfo info)
    {
        Refresh();

        if (fileName == "\\")
        {
            info.IsDirectory = true;
            return DokanResult.Success;
        }

        var segments = fileName.TrimStart('\\').Split('\\');

        if (segments.Length == 1 && _folderNames.Contains(segments[0]))
        {
            info.IsDirectory = true;
            return DokanResult.Success;
        }

        if (_pathToDocument.ContainsKey(fileName))
        {
            return mode == FileMode.Open ? DokanResult.Success : DokanResult.AccessDenied;
        }

        if (segments.Length == 2 && _folderNames.Contains(segments[0]) &&
            mode is FileMode.Create or FileMode.CreateNew or FileMode.OpenOrCreate)
        {
            _pendingWrites[fileName] = new MemoryStream();
            return DokanResult.Success;
        }

        if (_pendingWrites.ContainsKey(fileName))
        {
            return DokanResult.Success;
        }

        return DokanResult.FileNotFound;
    }

    public void Cleanup(string fileName, IDokanFileInfo info)
    {
        if (info.DeleteOnClose)
        {
            _pendingWrites.Remove(fileName);
            return;
        }

        if (!_pendingWrites.TryGetValue(fileName, out var buffer))
        {
            return;
        }

        _pendingWrites.Remove(fileName);

        if (buffer.Length == 0)
        {
            return;
        }

        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}");
        File.WriteAllBytes(tempPath, buffer.ToArray());

        var folder = fileName.TrimStart('\\').Split('\\')[0];
        var categoryId = _pathToDocument.Values.FirstOrDefault(d => d.Category?.Name == folder)?.CategoryId;

        try
        {
            var importPrompt = _serviceProvider.GetRequiredService<IDocumentImportPrompt>();
            importPrompt.PromptAndImportAsync(tempPath, categoryId).GetAwaiter().GetResult();
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    public void CloseFile(string fileName, IDokanFileInfo info)
    {
    }

    public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, IDokanFileInfo info)
    {
        if (!_pathToDocument.TryGetValue(fileName, out var document) || document.CurrentVersion is null)
        {
            bytesRead = 0;
            return DokanResult.FileNotFound;
        }

        using var stream = File.OpenRead(_vaultService.GetFullPath(document.CurrentVersion.VaultFileName));

        if (offset >= stream.Length)
        {
            bytesRead = 0;
            return DokanResult.Success;
        }

        stream.Position = offset;
        bytesRead = stream.Read(buffer, 0, buffer.Length);
        return DokanResult.Success;
    }

    public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, IDokanFileInfo info)
    {
        if (!_pendingWrites.TryGetValue(fileName, out var stream))
        {
            bytesWritten = 0;
            return DokanResult.FileNotFound;
        }

        stream.Position = offset;
        stream.Write(buffer, 0, buffer.Length);
        bytesWritten = buffer.Length;
        return DokanResult.Success;
    }

    public NtStatus FlushFileBuffers(string fileName, IDokanFileInfo info) => DokanResult.Success;

    public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, IDokanFileInfo info)
    {
        Refresh();

        if (fileName == "\\")
        {
            fileInfo = MakeDirectoryInfo("\\");
            return DokanResult.Success;
        }

        var segments = fileName.TrimStart('\\').Split('\\');

        if (segments.Length == 1 && _folderNames.Contains(segments[0]))
        {
            fileInfo = MakeDirectoryInfo(segments[0]);
            return DokanResult.Success;
        }

        if (_pathToDocument.TryGetValue(fileName, out var document) && document.CurrentVersion is not null)
        {
            fileInfo = new FileInformation
            {
                FileName = segments[^1],
                Attributes = FileAttributes.Normal | FileAttributes.ReadOnly,
                Length = document.CurrentVersion.FileSizeBytes,
                CreationTime = document.CreatedAt,
                LastWriteTime = document.ModifiedAt,
                LastAccessTime = document.ModifiedAt,
            };
            return DokanResult.Success;
        }

        if (_pendingWrites.TryGetValue(fileName, out var buffer))
        {
            fileInfo = new FileInformation
            {
                FileName = segments[^1],
                Attributes = FileAttributes.Normal,
                Length = buffer.Length,
                CreationTime = DateTime.Now,
                LastWriteTime = DateTime.Now,
                LastAccessTime = DateTime.Now,
            };
            return DokanResult.Success;
        }

        fileInfo = default;
        return DokanResult.FileNotFound;
    }

    private static FileInformation MakeDirectoryInfo(string name) => new()
    {
        FileName = name,
        Attributes = FileAttributes.Directory,
        CreationTime = DateTime.Now,
        LastWriteTime = DateTime.Now,
        LastAccessTime = DateTime.Now,
    };

    public NtStatus FindFiles(string fileName, out IList<FileInformation> files, IDokanFileInfo info)
    {
        Refresh();

        var result = new List<FileInformation>();

        if (fileName == "\\")
        {
            foreach (var folder in _folderNames)
            {
                result.Add(MakeDirectoryInfo(folder));
            }
        }
        else
        {
            var folder = fileName.TrimStart('\\');

            foreach (var (path, document) in _pathToDocument)
            {
                if (document.CurrentVersion is null)
                {
                    continue;
                }

                var segments = path.TrimStart('\\').Split('\\');
                if (segments.Length == 2 && segments[0] == folder)
                {
                    result.Add(new FileInformation
                    {
                        FileName = segments[1],
                        Attributes = FileAttributes.Normal | FileAttributes.ReadOnly,
                        Length = document.CurrentVersion.FileSizeBytes,
                        CreationTime = document.CreatedAt,
                        LastWriteTime = document.ModifiedAt,
                        LastAccessTime = document.ModifiedAt,
                    });
                }
            }
        }

        files = result;
        return DokanResult.Success;
    }

    public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files, IDokanFileInfo info)
    {
        FindFiles(fileName, out var all, info);

        files = searchPattern == "*"
            ? all
            : all.Where(f => f.FileName.Contains(searchPattern.Replace("*", string.Empty), StringComparison.OrdinalIgnoreCase)).ToList();

        return DokanResult.Success;
    }

    public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, IDokanFileInfo info) => DokanResult.Success;

    public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, IDokanFileInfo info) => DokanResult.Success;

    public NtStatus DeleteFile(string fileName, IDokanFileInfo info) => DokanResult.AccessDenied;

    public NtStatus DeleteDirectory(string fileName, IDokanFileInfo info) => DokanResult.AccessDenied;

    public NtStatus MoveFile(string oldName, string newName, bool replace, IDokanFileInfo info) => DokanResult.AccessDenied;

    public NtStatus SetEndOfFile(string fileName, long length, IDokanFileInfo info) => DokanResult.Success;

    public NtStatus SetAllocationSize(string fileName, long length, IDokanFileInfo info) => DokanResult.Success;

    public NtStatus LockFile(string fileName, long offset, long length, IDokanFileInfo info) => DokanResult.Success;

    public NtStatus UnlockFile(string fileName, long offset, long length, IDokanFileInfo info) => DokanResult.Success;

    public NtStatus GetDiskFreeSpace(out long freeBytesAvailable, out long totalBytes, out long totalFreeBytes, IDokanFileInfo info)
    {
        freeBytesAvailable = 0;
        totalFreeBytes = 0;
        totalBytes = _pathToDocument.Values.Sum(d => d.CurrentVersion?.FileSizeBytes ?? 0);
        return DokanResult.Success;
    }

    public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features, out string fileSystemName, out uint maximumComponentLength, IDokanFileInfo info)
    {
        volumeLabel = "MFilesClone";
        features = FileSystemFeatures.CasePreservedNames | FileSystemFeatures.UnicodeOnDisk;
        fileSystemName = "MFilesVFS";
        maximumComponentLength = 256;
        return DokanResult.Success;
    }

    public NtStatus Mounted(string mountPoint, IDokanFileInfo info) => DokanResult.Success;

    public NtStatus Unmounted(IDokanFileInfo info) => DokanResult.Success;

    public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity? security, AccessControlSections sections, IDokanFileInfo info)
    {
        security = null;
        return DokanResult.Success;
    }

    public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections, IDokanFileInfo info) => DokanResult.Success;

    public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, IDokanFileInfo info)
    {
        streams = new List<FileInformation>();
        return DokanResult.Success;
    }
}
