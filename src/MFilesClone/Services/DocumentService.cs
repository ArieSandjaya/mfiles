using System.IO;
using Microsoft.EntityFrameworkCore;
using MFilesClone.Data;
using MFilesClone.Models;

namespace MFilesClone.Services;

public class DocumentService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly VaultService _vaultService;

    public DocumentService(IDbContextFactory<AppDbContext> contextFactory, VaultService vaultService)
    {
        _contextFactory = contextFactory;
        _vaultService = vaultService;
    }

    public async Task<Document> CreateDocumentAsync(
        string sourceFilePath,
        string title,
        int? categoryId,
        IEnumerable<(string Key, string Value)> metadata)
    {
        var vaultFileName = _vaultService.StoreFile(sourceFilePath);

        try
        {
            var fileInfo = new FileInfo(sourceFilePath);
            var now = DateTime.UtcNow;

            await using var context = await _contextFactory.CreateDbContextAsync();

            var version = new DocumentVersion
            {
                VersionNumber = 1,
                VaultFileName = vaultFileName,
                OriginalFileName = Path.GetFileName(sourceFilePath),
                FileExtension = fileInfo.Extension,
                FileSizeBytes = fileInfo.Length,
                StoredAt = now,
            };

            var document = new Document
            {
                Title = title,
                CategoryId = categoryId,
                CreatedAt = now,
                ModifiedAt = now,
                IsDeleted = false,
                CurrentVersion = version,
            };

            document.Versions.Add(version);

            foreach (var (key, value) in metadata)
            {
                document.Metadata.Add(new DocumentMetadata { Key = key, Value = value });
            }

            context.Documents.Add(document);
            await context.SaveChangesAsync();

            return document;
        }
        catch
        {
            _vaultService.DeleteFile(vaultFileName);
            throw;
        }
    }

    public async Task<List<Document>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Documents
            .Where(d => !d.IsDeleted)
            .Include(d => d.Category)
            .Include(d => d.CurrentVersion)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<DocumentPage> SearchPageAsync(string? keyword, int? categoryId, int skip, int take)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.Documents
            .Where(d => !d.IsDeleted)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(d => d.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(d =>
                d.Title.Contains(keyword) ||
                d.Metadata.Any(m => m.Value.Contains(keyword)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .Include(d => d.Category)
            .Include(d => d.CurrentVersion)
            .OrderByDescending(d => d.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return new DocumentPage(items, totalCount);
    }

    public async Task UpdateMetadataAsync(int documentId, IEnumerable<(string Key, string Value)> metadata)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var existing = context.DocumentMetadata.Where(m => m.DocumentId == documentId);
        context.DocumentMetadata.RemoveRange(existing);

        foreach (var (key, value) in metadata)
        {
            context.DocumentMetadata.Add(new DocumentMetadata { DocumentId = documentId, Key = key, Value = value });
        }

        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int documentId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var document = await context.Documents.FindAsync(documentId);
        if (document is null)
        {
            return;
        }

        document.IsDeleted = true;
        await context.SaveChangesAsync();
    }

    public async Task CheckOutAsync(int documentId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var document = await context.Documents.FindAsync(documentId)
            ?? throw new InvalidOperationException("Dokumen tidak ditemukan.");

        if (document.IsCheckedOut)
        {
            throw new InvalidOperationException($"Dokumen sudah di-check-out oleh {document.CheckedOutBy}.");
        }

        document.CheckedOutAt = DateTime.UtcNow;
        document.CheckedOutBy = Environment.UserName;
        await context.SaveChangesAsync();
    }

    public async Task CancelCheckOutAsync(int documentId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var document = await context.Documents.FindAsync(documentId);
        if (document is null)
        {
            return;
        }

        document.CheckedOutAt = null;
        document.CheckedOutBy = null;
        await context.SaveChangesAsync();
    }

    public async Task CheckInAsync(int documentId, string newFilePath, string? comment)
    {
        var vaultFileName = _vaultService.StoreFile(newFilePath);

        try
        {
            var fileInfo = new FileInfo(newFilePath);
            var now = DateTime.UtcNow;

            await using var context = await _contextFactory.CreateDbContextAsync();

            var document = await context.Documents
                .Include(d => d.Versions)
                .FirstOrDefaultAsync(d => d.Id == documentId)
                ?? throw new InvalidOperationException("Dokumen tidak ditemukan.");

            if (!document.IsCheckedOut)
            {
                throw new InvalidOperationException("Dokumen harus di-check-out sebelum check-in.");
            }

            var nextVersionNumber = document.Versions.Count == 0
                ? 1
                : document.Versions.Max(v => v.VersionNumber) + 1;

            var version = new DocumentVersion
            {
                VersionNumber = nextVersionNumber,
                VaultFileName = vaultFileName,
                OriginalFileName = Path.GetFileName(newFilePath),
                FileExtension = fileInfo.Extension,
                FileSizeBytes = fileInfo.Length,
                StoredAt = now,
                Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            };

            document.Versions.Add(version);
            document.CurrentVersion = version;
            document.ModifiedAt = now;
            document.CheckedOutAt = null;
            document.CheckedOutBy = null;

            await context.SaveChangesAsync();
        }
        catch
        {
            _vaultService.DeleteFile(vaultFileName);
            throw;
        }
    }

    public async Task<string> ExportAsync(int documentId, string destinationFolder, int? versionId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var document = await context.Documents
            .Include(d => d.CurrentVersion)
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.Id == documentId)
            ?? throw new InvalidOperationException("Dokumen tidak ditemukan.");

        var version = versionId.HasValue
            ? document.Versions.FirstOrDefault(v => v.Id == versionId)
            : document.CurrentVersion;

        if (version is null)
        {
            throw new InvalidOperationException("Versi dokumen tidak ditemukan.");
        }

        return _vaultService.ExportFile(version.VaultFileName, destinationFolder, version.OriginalFileName);
    }

    public async Task<List<DocumentVersion>> GetVersionsAsync(int documentId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.DocumentVersions
            .Where(v => v.DocumentId == documentId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync();
    }
}

public record DocumentPage(List<Document> Items, int TotalCount);
