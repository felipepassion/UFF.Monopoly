using Microsoft.EntityFrameworkCore;
using UFF.Monopoly.Data;
using UFF.Monopoly.Data.Entities;
using UFF.Monopoly.Entities;

namespace UFF.Monopoly.Repositories;

public interface IGameRepository
{
    Task<(Guid gameId, Game game)> CreateNewGameAsync(Guid boardDefinitionId, IEnumerable<string> playerNames, IEnumerable<int>? pawnIndices = null, CancellationToken ct = default);
    Task<Game?> GetGameAsync(Guid gameId, CancellationToken ct = default);
    Task SaveGameAsync(Guid gameId, Game game, CancellationToken ct = default);
}

public class EfGameRepository : IGameRepository
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public EfGameRepository(IDbContextFactory<ApplicationDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<(Guid gameId, Game game)> CreateNewGameAsync(Guid boardDefinitionId, IEnumerable<string> playerNames, IEnumerable<int>? pawnIndices = null, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var id = Guid.NewGuid();
        var names = playerNames.ToList();
        var pawnsList = pawnIndices?.ToList() ?? new List<int>();

        var players = names.Select((n, idx) => new Player { Name = n, PawnIndex = (pawnsList.ElementAtOrDefault(idx) >= 1 && pawnsList.ElementAtOrDefault(idx) <= 6) ? pawnsList.ElementAtOrDefault(idx) : 1 }).ToList();

        var templates = await db.BlockTemplates.AsNoTracking()
            .Where(t => t.BoardDefinitionId == boardDefinitionId)
            .OrderBy(t => t.Position)
            .ToListAsync(ct);

        var board = templates.Select(t =>
        {
            if (t.Type == BlockType.Property)
            {
                var pb = new PropertyBlock
                {
                    Position = t.Position,
                    Name = t.Name,
                    Description = t.Description,
                    ImageUrl = t.ImageUrl,
                    Color = t.Color,
                    Price = t.Price,
                    Rent = t.Rent,
                    Type = t.Type,
                    Level = t.Level ?? PropertyLevel.Barata,
                    BuildingType = t.BuildingType,
                    BuildingLevel = 0
                };
                // parse building prices csv (incremental costs level1..4)
                if (!string.IsNullOrWhiteSpace(t.BuildingPricesCsv))
                {
                    var parts = t.BuildingPricesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    for (int i = 0; i < Math.Min(parts.Length, pb.BuildingPrices.Length); i++)
                        if (int.TryParse(parts[i], out var v)) pb.BuildingPrices[i] = v;
                }
                else
                {
                    // default incremental costs based on building type
                    pb.BuildingPrices = pb.BuildingType switch
                    {
                        BuildingType.House => new[] { 100, 200, 350, 500 },
                        BuildingType.Hotel => new[] { 300, 600, 1000, 1500 },
                        BuildingType.Company => new[] { 400, 800, 1300, 1900 },
                        BuildingType.Special => new[] { 500, 1000, 1600, 2300 },
                        _ => new[] { 0, 0, 0, 0 }
                    };
                }
                return (Block)pb;
            }
            else
            {
                return new Block
                {
                    Position = t.Position,
                    Name = t.Name,
                    Description = t.Description,
                    ImageUrl = t.ImageUrl,
                    Color = t.Color,
                    Price = t.Price,
                    Rent = t.Rent,
                    Type = t.Type
                };
            }
        }).ToList();

        // Ensure players start on the 'Go' block if present
        var goIndex = board.FindIndex(b => b.Type == BlockType.Go);
        if (goIndex < 0) goIndex = 0;
        foreach (var p in players) p.CurrentPosition = goIndex;

        var game = new Game(players, board);

        var gameEntity = new GameStateEntity
        {
            Id = id,
            CurrentPlayerIndex = game.CurrentPlayerIndex,
            IsFinished = false,
            Players = players.Select(p => new PlayerStateEntity
            {
                Id = p.Id,
                Name = p.Name,
                CurrentPosition = p.CurrentPosition,
                Money = p.Money,
                InJail = p.InJail,
                GetOutOfJailFreeCards = p.GetOutOfJailFreeCards,
                JailTurns = p.JailTurns,
                IsBankrupt = p.IsBankrupt,
                PawnIndex = p.PawnIndex
            }).ToList(),
            Board = board.Select(b => {
                var bs = new BlockStateEntity
                {
                    Id = Guid.NewGuid(),
                    Position = b.Position,
                    Name = b.Name,
                    Description = b.Description,
                    ImageUrl = b.ImageUrl,
                    Color = b.Color,
                    Price = b.Price,
                    Rent = b.Rent,
                    OwnerId = b.Owner?.Id,
                    IsMortgaged = b.IsMortgaged,
                    Type = b.Type
                };
                if (b is PropertyBlock pbx)
                {
                    bs.Level = pbx.Level;
                    bs.BuildingType = pbx.BuildingType;
                    bs.BuildingLevel = pbx.BuildingLevel;
                    // store building prices as CSV in RentsCsv for backward compatibility if needed
                    bs.RentsCsv = string.Join(',', pbx.BuildingPrices);
                }
                return bs;
            }).ToList()
        };

        db.Games.Add(gameEntity);
        await db.SaveChangesAsync(ct);
        return (id, game);
    }

    public async Task<Game?> GetGameAsync(Guid gameId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var gameEntity = await db.Games
            .Include(g => g.Players)
            .Include(g => g.Board)
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == gameId, ct);
        if (gameEntity == null) return null;

        var players = gameEntity.Players
            .Select(p => new Player
            {
                Id = p.Id,
                Name = p.Name,
                CurrentPosition = p.CurrentPosition,
                Money = p.Money,
                InJail = p.InJail,
                GetOutOfJailFreeCards = p.GetOutOfJailFreeCards,
                JailTurns = p.JailTurns,
                IsBankrupt = p.IsBankrupt,
                PawnIndex = p.PawnIndex
            }).ToList();
        var idMap = players.ToDictionary(p => p.Id, p => p);

        var blocks = gameEntity.Board
            .OrderBy(b => b.Position)
            .Select(b =>
            {
                if (b.Type == BlockType.Property)
                {
                    var pb = new PropertyBlock
                    {
                        Position = b.Position,
                        Name = b.Name,
                        Description = b.Description,
                        ImageUrl = b.ImageUrl,
                        Color = b.Color,
                        Price = b.Price,
                        Rent = b.Rent,
                        IsMortgaged = b.IsMortgaged,
                        Type = b.Type,
                        Level = b.Level ?? PropertyLevel.Barata,
                        BuildingType = b.BuildingType,
                        BuildingLevel = b.BuildingLevel
                    };
                    // reconstruct building prices from RentsCsv if stored
                    if (!string.IsNullOrWhiteSpace(b.RentsCsv))
                    {
                        var parts = b.RentsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        for (int i = 0; i < Math.Min(parts.Length, pb.BuildingPrices.Length); i++)
                            if (int.TryParse(parts[i], out var v)) pb.BuildingPrices[i] = v;
                    }
                    else
                    {
                        pb.BuildingPrices = pb.BuildingType switch
                        {
                            BuildingType.House => new[] { 100, 200, 350, 500 },
                            BuildingType.Hotel => new[] { 300, 600, 1000, 1500 },
                            BuildingType.Company => new[] { 400, 800, 1300, 1900 },
                            BuildingType.Special => new[] { 500, 1000, 1600, 2300 },
                            _ => new[] { 0, 0, 0, 0 }
                        };
                    }
                    if (b.OwnerId.HasValue && idMap.TryGetValue(b.OwnerId.Value, out var ownerP))
                    {
                        pb.Owner = ownerP;
                        ownerP.OwnedProperties.Add(pb);
                    }
                    return (Block)pb;
                }
                else
                {
                    var block = new Block
                    {
                        Position = b.Position,
                        Name = b.Name,
                        Description = b.Description,
                        ImageUrl = b.ImageUrl,
                        Color = b.Color,
                        Price = b.Price,
                        Rent = b.Rent,
                        IsMortgaged = b.IsMortgaged,
                        Type = b.Type
                    };
                    if (b.OwnerId.HasValue && idMap.TryGetValue(b.OwnerId.Value, out var owner))
                    {
                        block.Owner = owner;
                        owner.OwnedProperties.Add(block);
                    }
                    return block;
                }
            }).ToList();

        var game = new Game(players, blocks);
        return game;
    }

    public async Task SaveGameAsync(Guid gameId, Game game, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var gameEntity = await db.Games
            .Include(g => g.Players)
            .Include(g => g.Board)
            .FirstOrDefaultAsync(g => g.Id == gameId, ct);
        if (gameEntity == null)
        {
            await CreateNewGameAsync(Guid.Empty, game.Players.Select(p => p.Name), null, ct);
            return;
        }

        gameEntity.CurrentPlayerIndex = game.CurrentPlayerIndex;
        gameEntity.IsFinished = game.IsFinished;

        var playerMap = game.Players.ToDictionary(p => p.Id, p => p);
        foreach (var p in gameEntity.Players)
        {
            if (!playerMap.TryGetValue(p.Id, out var model)) continue;
            p.Name = model.Name;
            p.CurrentPosition = model.CurrentPosition;
            p.Money = model.Money;
            p.InJail = model.InJail;
            p.GetOutOfJailFreeCards = model.GetOutOfJailFreeCards;
            p.JailTurns = model.JailTurns;
            p.IsBankrupt = model.IsBankrupt;
            p.PawnIndex = model.PawnIndex;
        }

        foreach (var b in gameEntity.Board)
        {
            var model = game.Board.First(x => x.Position == b.Position);
            b.Name = model.Name;
            b.Description = model.Description;
            b.ImageUrl = model.ImageUrl;
            b.Color = model.Color;
            b.Price = model.Price;
            b.Rent = model.Rent;
            b.OwnerId = model.Owner?.Id;
            b.IsMortgaged = model.IsMortgaged;
            b.Type = model.Type;
            if (model is PropertyBlock pb)
            {
                b.Level = pb.Level;
                b.BuildingType = pb.BuildingType;
                b.BuildingLevel = pb.BuildingLevel;
                b.RentsCsv = string.Join(',', pb.BuildingPrices); // store building prices
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
