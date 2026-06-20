using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MFilesClone.Server.Models;
using MFilesClone.Server.Services;
using MFilesClone.Shared;

namespace MFilesClone.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
    private readonly DocumentService _documentService;

    public DocumentsController(DocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpGet]
    public async Task<ActionResult<DocumentPageDto>> Search(
        [FromQuery] string? keyword,
        [FromQuery] int? categoryId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100)
    {
        var (items, totalCount) = await _documentService.SearchPageAsync(keyword, categoryId, skip, take);
        return Ok(new DocumentPageDto(items.Select(MapDocument).ToList(), totalCount));
    }

    [HttpGet("{id:int}/versions")]
    public async Task<ActionResult<List<DocumentVersionDto>>> GetVersions(int id)
    {
        var versions = await _documentService.GetVersionsAsync(id);
        return Ok(versions.Select(MapVersion).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<DocumentDto>> Create(
        [FromForm] IFormFile file,
        [FromForm] string title,
        [FromForm] int? categoryId,
        [FromForm] string? metadata)
    {
        var metadataList = DeserializeMetadata(metadata);

        await using var stream = file.OpenReadStream();
        var document = await _documentService.CreateDocumentAsync(
            stream,
            file.FileName,
            title,
            categoryId,
            metadataList.Select(m => (m.Key, m.Value)));

        return CreatedAtAction(nameof(Search), MapDocument(document));
    }

    [HttpPut("{id:int}/metadata")]
    public async Task<IActionResult> UpdateMetadata(int id, [FromBody] UpdateMetadataRequest request)
    {
        await _documentService.UpdateMetadataAsync(id, request.Metadata.Select(m => (m.Key, m.Value)));
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _documentService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id:int}/checkout")]
    public async Task<IActionResult> CheckOut(int id)
    {
        try
        {
            await _documentService.CheckOutAsync(id, User.Identity?.Name ?? "Unknown");
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPost("{id:int}/checkout/cancel")]
    public async Task<IActionResult> CancelCheckOut(int id)
    {
        await _documentService.CancelCheckOutAsync(id);
        return NoContent();
    }

    [HttpPost("{id:int}/checkin")]
    public async Task<ActionResult<DocumentDto>> CheckIn(int id, [FromForm] IFormFile file, [FromForm] string? comment)
    {
        try
        {
            await using var stream = file.OpenReadStream();
            var document = await _documentService.CheckInAsync(id, stream, file.FileName, comment, User.Identity?.Name ?? "Unknown");
            return Ok(MapDocument(document));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpGet("{id:int}/content")]
    public async Task<IActionResult> GetContent(int id, [FromQuery] int? versionId)
    {
        try
        {
            var (content, fileName) = await _documentService.GetContentAsync(id, versionId);
            return File(content, "application/octet-stream", fileDownloadName: fileName);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    private static List<DocumentMetadataDto> DeserializeMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return new List<DocumentMetadataDto>();
        }

        return JsonSerializer.Deserialize<List<DocumentMetadataDto>>(metadataJson) ?? new List<DocumentMetadataDto>();
    }

    private static DocumentDto MapDocument(Document document) => new()
    {
        Id = document.Id,
        Title = document.Title,
        CategoryId = document.CategoryId,
        CategoryName = document.Category?.Name,
        CreatedAt = document.CreatedAt,
        ModifiedAt = document.ModifiedAt,
        IsDeleted = document.IsDeleted,
        CheckedOutAt = document.CheckedOutAt,
        CheckedOutBy = document.CheckedOutBy,
        CurrentVersion = document.CurrentVersion is null ? null : MapVersion(document.CurrentVersion),
        Metadata = document.Metadata.Select(m => new DocumentMetadataDto { Key = m.Key, Value = m.Value }).ToList(),
    };

    private static DocumentVersionDto MapVersion(DocumentVersion version) => new()
    {
        Id = version.Id,
        DocumentId = version.DocumentId,
        VersionNumber = version.VersionNumber,
        OriginalFileName = version.OriginalFileName,
        FileExtension = version.FileExtension,
        FileSizeBytes = version.FileSizeBytes,
        StoredAt = version.StoredAt,
        Comment = version.Comment,
    };
}
