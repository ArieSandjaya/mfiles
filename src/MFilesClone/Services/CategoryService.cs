using Microsoft.EntityFrameworkCore;
using MFilesClone.Data;
using MFilesClone.Models;

namespace MFilesClone.Services;

public class CategoryService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public CategoryService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Category>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Categories.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<Category> CreateAsync(string name)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var category = new Category { Name = name };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        return category;
    }
}
