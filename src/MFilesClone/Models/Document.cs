namespace MFilesClone.Models;

public class Document
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public int? CurrentVersionId { get; set; }
    public bool IsDeleted { get; set; }

    public Category? Category { get; set; }
    public DocumentVersion? CurrentVersion { get; set; }
    public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
    public ICollection<DocumentMetadata> Metadata { get; set; } = new List<DocumentMetadata>();
}
