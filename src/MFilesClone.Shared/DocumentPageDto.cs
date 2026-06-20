namespace MFilesClone.Shared;

public class DocumentPageDto
{
    public List<DocumentDto> Items { get; set; } = new();
    public int TotalCount { get; set; }

    public DocumentPageDto()
    {
    }

    public DocumentPageDto(List<DocumentDto> items, int totalCount)
    {
        Items = items;
        TotalCount = totalCount;
    }
}
