using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UFF.Monopoly.Data.Entities;
using UFF.Monopoly.Entities;

namespace UFF.Monopoly.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<IdentityUser>(options)
    {
        public DbSet<GameStateEntity> Games => Set<GameStateEntity>();
        public DbSet<PlayerStateEntity> Players => Set<PlayerStateEntity>();
        public DbSet<BlockStateEntity> Blocks => Set<BlockStateEntity>();
        public DbSet<BlockTemplateEntity> BlockTemplates => Set<BlockTemplateEntity>();
        public DbSet<BoardDefinitionEntity> Boards => Set<BoardDefinitionEntity>();
        public DbSet<UserProfileEntity> UserProfiles => Set<UserProfileEntity>();

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
                // store nullable enum as integer in DB
                e.Property(x => x.Level).HasConversion<int?>();
                e.Property(x => x.BuildingType).HasConversion<int>();
            });

            builder.Entity<BlockTemplateEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Type).HasConversion<int>();
                e.Property(x => x.BuildingType).HasConversion<int>();
                e.HasOne<BoardDefinitionEntity>()
                 .WithMany(b => b.Blocks)
                 .HasForeignKey(x => x.BoardDefinitionId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => new { x.BoardDefinitionId, x.Position }).IsUnique();
                // Seed data: example board and 20 property templates (neighborhoods of Rio)
                // Board seed
                var boardId = new Guid("11111111-1111-1111-1111-111111111111");
                builder.Entity<BoardDefinitionEntity>().HasData(new BoardDefinitionEntity
                {
                    Id = boardId,
                    Name = "Rio Sample Board",
                    // use a fixed UTC datetime so the model is deterministic for EF Core migrations
                    CreatedAt = new DateTime(2025, 10, 27, 18, 30, 24, DateTimeKind.Utc),
                    Rows = 5,
                    Cols = 5,
                    CellSizePx = 64
                });

                // Block templates seed (IDs kept small/simple)
                e.HasData(
                    // MuitoRica (Level = 3)
                    new BlockTemplateEntity { Id = new Guid("00000000-0000-0000-0000-000000000101"), Position = 0, Name = "Leblon", Description = "Leblon (Muito Rica)", ImageUrl = "/images/blocks/property_basic.svg", Color = "#d4af37", Price = 10000, Rent = 100, Type = BlockType.Property, Level = PropertyLevel.MuitoRica, HousePrice = 1000, HotelPrice = 2000, RentsCsv = "100,500,1000,1800,2200,2400,2500", BoardDefinitionId = boardId },
                    new BlockTemplateEntity { Id = new Guid("00000000-0000-0000-0000-000000000102"), Position = 1, Name = "Ipanema", Description = "Ipanema (Muito Rica)", ImageUrl = "/images/blocks/property_basic.svg", Color = "#d4af37", Price = 9200, Rent = 92, Type = BlockType.Property, Level = PropertyLevel.MuitoRica, HousePrice = 920, HotelPrice = 1840, RentsCsv = "92,460,920,1656,2024,2208,2300", BoardDefinitionId = boardId },
                    new BlockTemplateEntity { Id = new Guid("00000000-0000-0000-0000-000000000103"), Position = 2, Name = "Jardim Botânico", Description = "Jardim Botânico (Muito Rica)", ImageUrl = "/images/blocks/property_basic.svg", Color = "#d4af37", Price = 8800, Rent = 88, Type = BlockType.Property, Level = PropertyLevel.MuitoRica, HousePrice = 880, HotelPrice = 1760, RentsCsv = "88,440,880,1584,1936,2112,2200", BoardDefinitionId = boardId },
                    new BlockTemplateEntity { Id = new Guid("00000000-0000-0000-0000-000000000104"), Position = 3, Name = "São Conrado", Description = "São Conrado (Muito Rica)", ImageUrl = "/images/blocks/property_basic.svg", Color = "#d4af37", Price = 8400, Rent = 84, Type = BlockType.Property, Level = PropertyLevel.MuitoRica, HousePrice = 840, HotelPrice = 1680, RentsCsv = "84,420,840,1512,1848,2016,2100", BoardDefinitionId = boardId },
                    new BlockTemplateEntity { Id = new Guid("00000000-0000-0000-0000-000000000105"), Position = 4, Name = "Lagoa", Description = "Lagoa (Muito Rica)", ImageUrl = "/images/blocks/property_basic.svg", Color = "#d4af37", Price = 8000, Rent = 80, Type = BlockType.Property, Level = PropertyLevel.MuitoRica, HousePrice = 800, HotelPrice = 1600, RentsCsv = "80,400,800,1440,1760,1920,2000", BoardDefinitionId = boardId },

                    // Rica (Level = 2)
                    new BlockTemplateEntity { Id = new Guid("00000000-0000-0000-0000-000000000106"), Position = 5, Name = "Copacabana", Description = "Copacabana (Rica)", ImageUrl = "/images/blocks/property_basic.svg", Color = "#3498db", Price = 7000, Rent = 70, Type = BlockType.Property, Level = PropertyLevel.Rica, HousePrice = 700, HotelPrice = 1400, RentsCsv = "70,350,700,1260,1540,1680,1750", BoardDefinitionId = boardId },
                    new BlockTemplateEntity { Id = new Guid("00000000-0000-0000-0000-000000000107"), Position = 6, Name = "Flamengo", Description = "Flamengo (Rica)", ImageUrl = "/images/blocks/property_basic.svg", Color = "#3498db", Price = 6500, Rent = 65, Type = BlockType.Property, Level = PropertyLevel.Rica, HousePrice = 650, HotelPrice = 1300, RentsCsv = "65,325,650,1170,1430,1560,1625", BoardDefinitionId = boardId },
                    new BlockTemplateEntity { Id = new Guid("00000000-0000-0000-0000-000000000108"), Position = 7, Name = "Botafogo", Description = "Botafogo (Rica)", ImageUrl = "/images/blocks/property_basic.svg", Color = "#3498db", Price = 6000, Rent = 60, Type = BlockType.Property, Level = PropertyLevel.Rica, HousePrice = 600, HotelPrice = 1200, RentsCsv = "60,300,600,1080,1320,1440,1500", BoardDefinitionId = boardId },
                    new BlockTemplateEntity { Id = new Guid("00000000-0000-0000-0000-000000000109"), Position = 8, Name = "Gávea", Description = "Gávea (Rica)", ImageUrl = "/images/blocks/property_basic.svg", Color = "#3498db", Price = 5500, Rent = 55, Type = BlockType.Property, Level = PropertyLevel.Rica, HousePrice = 550, HotelPrice = 1100, RentsCsv = "55,275,550,990,1210,1320,1375", BoardDefinitionId = boardId },
                    new BlockTemplateEntity { Id = new Guid("00000000-0000-0000-0000-000000000110"), Position = 9, Name = "Laranjeiras", Description = "Laranjeiras (Rica)", ImageUrl = "/images/blocks/property_basic.svg", Color = "#3498db", Price = 5000, Rent = 50, Type = BlockType.Property, Level = PropertyLevel.Rica, HousePrice = 500, HotelPrice = 1000, RentsCsv = "50,250,500,900,1100,1200,1250", BoardDefinitionId = boardId },

                    // Mediana (Level = 1)
                    new BlockTemplateEntity { Id = new Guid("00000000-0000-0000-0000-000000000111"), Position = 10, Name = "Humaitá", Description = "Humaitá (Mediana)", ImageUrl = "/images/blocks/property_basic.svg", Color = "#27ae60", Price = 3500, Rent = 35, Type = BlockType.Property, Level = PropertyLevel.Mediana, HousePrice = 350, HotelPrice = 700, RentsCsv = "35,175,350,630,770,840,875", BoardDefinitionId = boardId },
                    new BlockTemplateEntity { Id = new Guid("00000000-0000-0000-0000-000000000112"), Position = 11, Name = "Leme", Description = "Leme (Mediana)", ImageUrl = "/images/blocks/property_basic.svg", Color = "#27ae60", Price = 3200, Rent = 32, Type = BlockType.Property, Level = PropertyLevel.Mediana, HousePrice = 320, HotelPrice = 640, RentsCsv = "32,160,320,576,704,768,800", BoardDefinitionId = boardId },
                    new BlockTemplateEntity { Id = new Guid("00000000-0000-0000-0000-000000000113"), Position = 12, Name = "Maracanã", Description = "Maracanã (Mediana)", ImageUrl = "/images/blocks/property_basic.svg", Color = "#27ae60", Price = 3000, Rent = 30, Type = BlockType.Property, Level = PropertyLevel.Mediana, HousePrice = 300, HotelPrice = 600, RentsCsv = "30,150,300,540,660,720,750", BoardDefinitionId = boardId },
                    new BlockTemplateEntity { Id = new Guid("00000000-0000-0000-0000-000000000114"), Position = 13, Name = "Tijuca", Description = "Tijuca (Mediana)", ImageUrl = "/images/blocks/property_basic.svg", Color = "#27ae60", Price = 2800, Rent = 28, Type = BlockType.Property, Level = PropertyLevel.Mediana, HousePrice = 280, HotelPrice = 560, RentsCsv = "28,140,280,504,616,672,700", BoardDefinitionId = boardId },
                    new BlockTemplateEntity { Id = new Guid("00000000-0000-0000-0000-000000000115"), Position = 14, Name = "Andaraí", Description = "Andaraí (Mediana)", ImageUrl = "/images/blocks/property_basic.svg", Color = "#27ae60", Price = 2500, Rent = 25, Type = BlockType.Property, Level = PropertyLevel.Mediana, HousePrice = 250, HotelPrice = 500, RentsCsv = "25,125,250,450,550,600,625", BoardDefinitionId = boardId },

                    // Barata (Level = 0)
                    new BlockTemplateEntity { Id = new Guid("00000000-0000-0000-0000-000000000116"), Position = 15, Name = "Madureira", Description = "Madureira (Barata)", ImageUrl = "/images/blocks/property_basic.svg", Color = "#8b4513", Price = 1200, Rent = 12, Type = BlockType.Property, Level = PropertyLevel.Barata, HousePrice = 120, HotelPrice = 240, RentsCsv = "12,60,120,216,264,288,300", BoardDefinitionId = boardId },
                    new BlockTemplateEntity { Id = new Guid("00000000-0000-0000-0000-000000000117"), Position = 16, Name = "Bonsucesso", Description = "Bonsucesso (Barata)", ImageUrl = "/images/blocks/property_basic.svg", Color = "#8b4513", Price = 1000, Rent = 10, Type = BlockType.Property, Level = PropertyLevel.Barata, HousePrice = 100, HotelPrice = 200, RentsCsv = "10,50,100,180,220,240,250", BoardDefinitionId = boardId },
                    new BlockTemplateEntity { Id = new Guid("00000000-0000-0000-0000-000000000118"), Position = 17, Name = "Campo Grande", Description = "Campo Grande (Barata)", ImageUrl = "/images/blocks/property_basic.svg", Color = "#8b4513", Price = 900, Rent = 9, Type = BlockType.Property, Level = PropertyLevel.Barata, HousePrice = 90, HotelPrice = 180, RentsCsv = "9,45,90,162,198,216,225", BoardDefinitionId = boardId },
                    new BlockTemplateEntity { Id = new Guid("00000000-0000-0000-0000-000000000119"), Position = 18, Name = "Realengo", Description = "Realengo (Barata)", ImageUrl = "/images/blocks/property_basic.svg", Color = "#8b4513", Price = 800, Rent = 8, Type = BlockType.Property, Level = PropertyLevel.Barata, HousePrice = 80, HotelPrice = 160, RentsCsv = "8,40,80,144,176,192,200", BoardDefinitionId = boardId },
                    new BlockTemplateEntity { Id = new Guid("00000000-0000-0000-0000-000000000120"), Position = 19, Name = "Paciência", Description = "Paciência (Barata)", ImageUrl = "/images/blocks/property_basic.svg", Color = "#8b4513", Price = 600, Rent = 6, Type = BlockType.Property, Level = PropertyLevel.Barata, HousePrice = 60, HotelPrice = 120, RentsCsv = "6,30,60,108,132,144,150", BoardDefinitionId = boardId }
                 );
             });
        }
    }
}
