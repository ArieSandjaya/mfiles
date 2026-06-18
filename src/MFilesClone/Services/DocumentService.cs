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
        var fileInfo = new FileInfo(sourceFilePath);
        var now = DateTime.UtcNow;

        await using var context = await _contextFactory.CreateDbContextAsync();

        var document = new Document
        {
            Title = title,
            CategoryId = categoryId,
            CreatedAt = now,
            ModifiedAt = now,
            IsDeleted = false,
        };

        var version = new DocumentVersion
        {
            VersionNumber = 1,
            VaultFileName = vaultFileName,
            OriginalFileName = Path.GetFileName(sourceFilePath),
            FileExtension = fileInfo.Extension,
            FileSizeBytes = fileInfo.Length,
            StoredAt = now,
        };

        document.Versions.Add(version);

        foreach (var (key, value) in metadata)
        {
            document.Metadata.Add(new DocumentMetadata { Key = key, Value = value });
        }

        context.Documents.Add(document);
        await context.SaveChangesAsync();

        document.CurrentVersionId = version.Id;
        await context.SaveChangesAsync();

        return document;
    }

    public async Task<List<Document>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Documents
            .Where(d => !d.IsDeleted)
            .Include(d => d.Category)
            .Include(d => d.CurrentVersion)
            .Include(d => d.Metadata)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Document>> SearchAsync(string? keyword, int? categoryId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.Documents
            .Where(d => !d.IsDeleted)
            .Include(d => d.Category)
            .Include(d => d.CurrentVersion)
            .Include(d => d.Metadata)
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

        return await query.OrderByDescending(d => d.CreatedAt).ToListAsync();
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
}
