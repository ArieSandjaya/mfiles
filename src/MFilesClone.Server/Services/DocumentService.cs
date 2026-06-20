using Microsoft.EntityFrameworkCore;
using MFilesClone.Server.Data;
using MFilesClone.Server.Models;

namespace MFilesClone.Server.Services;

public class DocumentService
{
    private readonly AppDbContext _context;
    private readonly VaultService _vaultService;

    public DocumentService(AppDbContext context, VaultService vaultService)
    {
        _context = context;
        _vaultService = vaultService;
    }

    public async Task<Document> CreateDocumentAsync(
        Stream sourceStream,
        string originalFileName,
        string title,
        int? categoryId,
        IEnumerable<(string Key, string Value)> metadata)
    {
        var vaultFileName = _vaultService.StoreFile(sourceStream, originalFileName);

        try
        {
            var now = DateTime.UtcNow;

            var version = new DocumentVersion
            {
                VersionNumber = 1,
                VaultFileName = vaultFileName,
                OriginalFileName = originalFileName,
                FileExtension = Path.GetExtension(originalFileName),
                FileSizeBytes = sourceStream.Length,
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

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            await _context.Entry(document).Reference(d => d.Category).LoadAsync();

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
        return await _context.Documents
            .Where(d => !d.IsDeleted)
            .Include(d => d.Category)
            .Include(d => d.CurrentVersion)
            .Include(d => d.Metadata)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<(List<Document> Items, int TotalCount)> SearchPageAsync(string? keyword, int? categoryId, int skip, int take)
    {
        var query = _context.Documents
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
            .Include(d => d.Metadata)
            .OrderByDescending(d => d.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Document?> GetByIdAsync(int documentId)
    {
        return await _context.Documents
            .Include(d => d.Category)
            .Include(d => d.CurrentVersion)
            .Include(d => d.Metadata)
            .FirstOrDefaultAsync(d => d.Id == documentId);
    }

    public async Task UpdateMetadataAsync(int documentId, IEnumerable<(string Key, string Value)> metadata)
    {
        var existing = _context.DocumentMetadata.Where(m => m.DocumentId == documentId);
        _context.DocumentMetadata.RemoveRange(existing);

        foreach (var (key, value) in metadata)
        {
            _context.DocumentMetadata.Add(new DocumentMetadata { DocumentId = documentId, Key = key, Value = value });
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int documentId)
    {
        var document = await _context.Documents.FindAsync(documentId);
        if (document is null)
        {
            return;
        }

        document.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    public async Task CheckOutAsync(int documentId, string performedBy)
    {
        var document = await _context.Documents.FindAsync(documentId)
            ?? throw new InvalidOperationException("Dokumen tidak ditemukan.");

        if (document.IsCheckedOut)
        {
            throw new InvalidOperationException($"Dokumen sudah di-check-out oleh {document.CheckedOutBy}.");
        }

        document.CheckedOutAt = DateTime.UtcNow;
        document.CheckedOutBy = performedBy;
        await _context.SaveChangesAsync();
    }

    public async Task CancelCheckOutAsync(int documentId)
    {
        var document = await _context.Documents.FindAsync(documentId);
        if (document is null)
        {
            return;
        }

        document.CheckedOutAt = null;
        document.CheckedOutBy = null;
        await _context.SaveChangesAsync();
    }

    public async Task<Document> CheckInAsync(int documentId, Stream sourceStream, string originalFileName, string? comment, string performedBy)
    {
        var vaultFileName = _vaultService.StoreFile(sourceStream, originalFileName);

        try
        {
            var now = DateTime.UtcNow;

            var document = await _context.Documents
                .Include(d => d.Versions)
                .Include(d => d.Category)
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
                OriginalFileName = originalFileName,
                FileExtension = Path.GetExtension(originalFileName),
                FileSizeBytes = sourceStream.Length,
                StoredAt = now,
                Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            };

            document.Versions.Add(version);
            document.CurrentVersion = version;
            document.ModifiedAt = now;
            document.CheckedOutAt = null;
            document.CheckedOutBy = null;

            await _context.SaveChangesAsync();

            return document;
        }
        catch
        {
            _vaultService.DeleteFile(vaultFileName);
            throw;
        }
    }

    public async Task<(byte[] Content, string FileName)> GetContentAsync(int documentId, int? versionId = null)
    {
        var document = await _context.Documents
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

        var content = _vaultService.ReadFileBytes(version.VaultFileName);
        return (content, version.OriginalFileName);
    }

    public async Task<List<DocumentVersion>> GetVersionsAsync(int documentId)
    {
        return await _context.DocumentVersions
            .Where(v => v.DocumentId == documentId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync();
    }
}
