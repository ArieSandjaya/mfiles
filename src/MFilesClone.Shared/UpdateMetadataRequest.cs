namespace MFilesClone.Shared;

public class UpdateMetadataRequest
{
    public List<DocumentMetadataDto> Metadata { get; set; } = new();
}
