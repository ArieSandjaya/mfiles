namespace MFilesClone.Shared;

public class DocumentVersionDto
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime StoredAt { get; set; }
    public string? Comment { get; set; }
}
