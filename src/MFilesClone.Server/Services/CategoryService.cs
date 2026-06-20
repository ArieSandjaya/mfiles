using Microsoft.EntityFrameworkCore;
using MFilesClone.Server.Data;
using MFilesClone.Server.Models;

namespace MFilesClone.Server.Services;

public class CategoryService
{
    private readonly AppDbContext _context;

    public CategoryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Category>> GetAllAsync()
    {
        return await _context.Categories.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<Category> CreateAsync(string name)
    {
        var category = new Category { Name = name };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return category;
    }
}
