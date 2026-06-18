namespace MFilesClone.Models;

public class DocumentMetadata
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public Document Document { get; set; } = null!;
}
