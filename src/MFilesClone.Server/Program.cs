using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.EntityFrameworkCore;
using MFilesClone.Server.Data;
using MFilesClone.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddSingleton<VaultService>();

builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

builder.Services.AddControllers();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();

    var vaultService = scope.ServiceProvider.GetRequiredService<VaultService>();
    vaultService.EnsureVaultExists();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
