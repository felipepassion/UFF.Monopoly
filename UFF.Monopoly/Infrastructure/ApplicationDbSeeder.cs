using Microsoft.EntityFrameworkCore;
using UFF.Monopoly.Data;
using UFF.Monopoly.Data.Entities;
using UFF.Monopoly.Setup;

namespace UFF.Monopoly.Infrastructure;

public static class ApplicationDbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync(ct);

        if (!await db.BlockTemplates.AnyAsync(ct))
        {
            var blocks = BoardFactory.CreateBasicBoard();
            var templates = blocks.Select(b => new BlockTemplateEntity
            {
                Id = Guid.NewGuid(),
                Position = b.Position,
                Name = b.Name,
                Description = b.Description,
                ImageUrl = b.ImageUrl,
                Color = b.Color,
                Price = b.Price,
                Rent = b.Rent,
                Type = b.Type
            }).ToList();
            db.BlockTemplates.AddRange(templates);
            await db.SaveChangesAsync(ct);
        }
    }
}
