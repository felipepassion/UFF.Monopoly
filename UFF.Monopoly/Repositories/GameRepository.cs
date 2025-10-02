using Microsoft.EntityFrameworkCore;
using UFF.Monopoly.Data;
using UFF.Monopoly.Data.Entities;
using UFF.Monopoly.Entities;

namespace UFF.Monopoly.Repositories;

public interface IGameRepository
{
    Task<(Guid gameId, Game game)> CreateNewGameAsync(IEnumerable<string> playerNames, CancellationToken ct = default);
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

    public async Task<(Guid gameId, Game game)> CreateNewGameAsync(IEnumerable<string> playerNames, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var id = Guid.NewGuid();
        var players = playerNames.Select(n => new Player { Name = n }).ToList();
        var templates = await db.BlockTemplates.AsNoTracking().OrderBy(t => t.Position).ToListAsync(ct);
        var board = templates.Select(t => new Block
        {
            Position = t.Position,
            Name = t.Name,
            Description = t.Description,
            ImageUrl = t.ImageUrl,
            Color = t.Color,
            Price = t.Price,
            Rent = t.Rent,
            Type = t.Type
        }).ToList();

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
                IsBankrupt = p.IsBankrupt
            }).ToList(),
            Board = board.Select(b => new BlockStateEntity
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
            .OrderBy(p => p.Name)
            .Select(p => new Player
            {
                Id = p.Id,
                Name = p.Name,
                CurrentPosition = p.CurrentPosition,
                Money = p.Money,
                InJail = p.InJail,
                GetOutOfJailFreeCards = p.GetOutOfJailFreeCards,
                JailTurns = p.JailTurns,
                IsBankrupt = p.IsBankrupt
            }).ToList();
        var idMap = players.ToDictionary(p => p.Id, p => p);

        var blocks = gameEntity.Board
            .OrderBy(b => b.Position)
            .Select(b =>
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
            await CreateNewGameAsync(game.Players.Select(p => p.Name), ct);
            return;
        }

        gameEntity.CurrentPlayerIndex = game.CurrentPlayerIndex;
        gameEntity.IsFinished = game.IsFinished;

        // update players
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
        }

        // update blocks
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
        }

        await db.SaveChangesAsync(ct);
    }
}
