using System.Net.Http;
using System.Net.Http.Json;
using MFilesClone.Shared;

namespace MFilesClone.Services;

public class CategoryService
{
    public async Task<List<CategoryDto>> GetAllAsync()
    {
        using var client = ServerConnection.CreateHttpClient();
        var categories = await client.GetFromJsonAsync<List<CategoryDto>>("api/categories");
        return categories ?? new List<CategoryDto>();
    }

    public async Task<CategoryDto> CreateAsync(string name)
    {
        using var client = ServerConnection.CreateHttpClient();
        using var response = await client.PostAsJsonAsync("api/categories", new CreateCategoryRequest { Name = name });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }
}
