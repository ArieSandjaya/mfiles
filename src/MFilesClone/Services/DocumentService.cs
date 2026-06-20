using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using MFilesClone.Shared;

namespace MFilesClone.Services;

// Talks to MFilesClone.Server over HTTP instead of touching a local database/vault.
// The current Windows logon flows through automatically via ServerConnection's
// UseDefaultCredentials handler, so no separate sign-in step is needed here.
public class DocumentService
{
    public async Task<DocumentDto> CreateDocumentAsync(
        string sourceFilePath,
        string title,
        int? categoryId,
        IEnumerable<(string Key, string Value)> metadata)
    {
        using var client = ServerConnection.CreateHttpClient();
        using var content = new MultipartFormDataContent();

        await using var fileStream = File.OpenRead(sourceFilePath);
        using var streamContent = new StreamContent(fileStream);
        content.Add(streamContent, "file", Path.GetFileName(sourceFilePath));
        content.Add(new StringContent(title), "title");

        if (categoryId.HasValue)
        {
            content.Add(new StringContent(categoryId.Value.ToString()), "categoryId");
        }

        var metadataDtos = metadata.Select(m => new DocumentMetadataDto { Key = m.Key, Value = m.Value }).ToList();
        content.Add(new StringContent(JsonSerializer.Serialize(metadataDtos)), "metadata");

        using var response = await client.PostAsync("api/documents", content);
        await EnsureSuccessAsync(response);

        return (await response.Content.ReadFromJsonAsync<DocumentDto>())!;
    }

    public async Task<List<DocumentDto>> GetAllAsync()
    {
        var page = await SearchPageAsync(null, null, 0, int.MaxValue);
        return page.Items;
    }

    public async Task<DocumentPageDto> SearchPageAsync(string? keyword, int? categoryId, int skip, int take)
    {
        using var client = ServerConnection.CreateHttpClient();

        var query = $"api/documents?skip={skip}&take={take}";

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query += $"&keyword={Uri.EscapeDataString(keyword)}";
        }

        if (categoryId.HasValue)
        {
            query += $"&categoryId={categoryId.Value}";
        }

        using var response = await client.GetAsync(query);
        await EnsureSuccessAsync(response);

        return (await response.Content.ReadFromJsonAsync<DocumentPageDto>())!;
    }

    public async Task UpdateMetadataAsync(int documentId, IEnumerable<(string Key, string Value)> metadata)
    {
        using var client = ServerConnection.CreateHttpClient();

        var request = new UpdateMetadataRequest
        {
            Metadata = metadata.Select(m => new DocumentMetadataDto { Key = m.Key, Value = m.Value }).ToList(),
        };

        using var response = await client.PutAsJsonAsync($"api/documents/{documentId}/metadata", request);
        await EnsureSuccessAsync(response);
    }

    public async Task DeleteAsync(int documentId)
    {
        using var client = ServerConnection.CreateHttpClient();
        using var response = await client.DeleteAsync($"api/documents/{documentId}");
        await EnsureSuccessAsync(response);
    }

    public async Task CheckOutAsync(int documentId)
    {
        using var client = ServerConnection.CreateHttpClient();
        using var response = await client.PostAsync($"api/documents/{documentId}/checkout", content: null);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var message = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(message);
        }

        await EnsureSuccessAsync(response);
    }

    public async Task CancelCheckOutAsync(int documentId)
    {
        using var client = ServerConnection.CreateHttpClient();
        using var response = await client.PostAsync($"api/documents/{documentId}/checkout/cancel", content: null);
        await EnsureSuccessAsync(response);
    }

    public async Task CheckInAsync(int documentId, string newFilePath, string? comment)
    {
        using var client = ServerConnection.CreateHttpClient();
        using var content = new MultipartFormDataContent();

        await using var fileStream = File.OpenRead(newFilePath);
        using var streamContent = new StreamContent(fileStream);
        content.Add(streamContent, "file", Path.GetFileName(newFilePath));

        if (!string.IsNullOrWhiteSpace(comment))
        {
            content.Add(new StringContent(comment), "comment");
        }

        using var response = await client.PostAsync($"api/documents/{documentId}/checkin", content);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var message = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(message);
        }

        await EnsureSuccessAsync(response);
    }

    public async Task<string> ExportAsync(int documentId, string destinationFolder, int? versionId = null)
    {
        var (content, fileName) = await GetContentAndFileNameAsync(documentId, versionId);

        var destinationPath = GetUniqueDestinationPath(destinationFolder, fileName);
        await File.WriteAllBytesAsync(destinationPath, content);
        return destinationPath;
    }

    public async Task<byte[]> GetFileBytesAsync(int documentId, int? versionId = null)
    {
        var (content, _) = await GetContentAndFileNameAsync(documentId, versionId);
        return content;
    }

    public async Task<string> DownloadToTempFileAsync(int documentId, int? versionId, string originalFileName)
    {
        var (content, _) = await GetContentAndFileNameAsync(documentId, versionId);

        var tempDir = Path.Combine(Path.GetTempPath(), "MFilesClonePreview");
        Directory.CreateDirectory(tempDir);

        var tempPath = Path.Combine(tempDir, $"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}");
        await File.WriteAllBytesAsync(tempPath, content);
        return tempPath;
    }

    public async Task<List<DocumentVersionDto>> GetVersionsAsync(int documentId)
    {
        using var client = ServerConnection.CreateHttpClient();
        using var response = await client.GetAsync($"api/documents/{documentId}/versions");
        await EnsureSuccessAsync(response);

        return (await response.Content.ReadFromJsonAsync<List<DocumentVersionDto>>())!;
    }

    private async Task<(byte[] Content, string FileName)> GetContentAndFileNameAsync(int documentId, int? versionId)
    {
        using var client = ServerConnection.CreateHttpClient();

        var query = $"api/documents/{documentId}/content";
        if (versionId.HasValue)
        {
            query += $"?versionId={versionId.Value}";
        }

        using var response = await client.GetAsync(query);
        await EnsureSuccessAsync(response);

        var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? "file";
        var content = await response.Content.ReadAsByteArrayAsync();
        return (content, fileName);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException(string.IsNullOrWhiteSpace(message)
            ? $"Permintaan ke server gagal ({(int)response.StatusCode})."
            : message);
    }

    private static string GetUniqueDestinationPath(string folder, string originalFileName)
    {
        var candidate = Path.Combine(folder, originalFileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
        var extension = Path.GetExtension(originalFileName);
        var counter = 1;

        while (File.Exists(candidate))
        {
            candidate = Path.Combine(folder, $"{nameWithoutExtension} ({counter}){extension}");
            counter++;
        }

        return candidate;
    }
}
