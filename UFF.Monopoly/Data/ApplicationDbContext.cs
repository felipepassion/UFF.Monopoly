using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UFF.Monopoly.Data.Entities;

namespace UFF.Monopoly.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<GameStateEntity> Games => Set<GameStateEntity>();
        public DbSet<PlayerStateEntity> Players => Set<PlayerStateEntity>();
        public DbSet<BlockStateEntity> Blocks => Set<BlockStateEntity>();
        public DbSet<BlockTemplateEntity> BlockTemplates => Set<BlockTemplateEntity>();
        public DbSet<BoardDefinitionEntity> Boards => Set<BoardDefinitionEntity>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<GameStateEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasMany(x => x.Players)
                 .WithOne(p => p.Game)
                 .HasForeignKey(p => p.GameStateId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasMany(x => x.Board)
                 .WithOne(b => b.Game)
                 .HasForeignKey(b => b.GameStateId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<PlayerStateEntity>(e =>
            {
                e.HasKey(x => x.Id);
            });

            builder.Entity<BlockStateEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Type).HasConversion<int>();
            });

            builder.Entity<BlockTemplateEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.Position).IsUnique();
                e.Property(x => x.Type).HasConversion<int>();
            });

            builder.Entity<BoardDefinitionEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasMany<BlockTemplateEntity>()
                 .WithOne()
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
