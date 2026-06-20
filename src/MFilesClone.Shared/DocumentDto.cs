namespace MFilesClone.Shared;

public class DocumentDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public string? CheckedOutBy { get; set; }

    public bool IsCheckedOut => CheckedOutAt.HasValue;

    public DocumentVersionDto? CurrentVersion { get; set; }
    public List<DocumentMetadataDto> Metadata { get; set; } = new();
}
