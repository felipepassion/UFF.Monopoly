using Microsoft.AspNetCore.SignalR;
using UFF.Monopoly.Repositories;
using UFF.Monopoly.Data;
using Microsoft.EntityFrameworkCore;

namespace UFF.Monopoly.Hubs;

public class GameHub : Hub
{
    private readonly IGameRepository _gameRepo;
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public GameHub(IGameRepository gameRepo, IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _gameRepo = gameRepo;
        _dbFactory = dbFactory;
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    public async Task<bool> SubscribeToGame(string gameId, string playerName, int pawnIndex)
    {
        if (!Guid.TryParse(gameId, out var gid)) return false;
        var groupName = GetGameGroupName(gid);
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SubscribeToRoom(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId)) return false;
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RollDice(string gameId)
    {
        if (!Guid.TryParse(gameId, out var gid)) return false;
        try
        {
            var game = await _gameRepo.GetGameAsync(gid);
            if (game is null) return false;

            var (d1, d2, total) = game.RollDice();

            // Notify group about dice values so clients can show animation
            await Clients.Group(GetGameGroupName(gid)).SendAsync("DiceRolled", gid.ToString(), d1, d2);

            // Apply move on server and persist
            await game.MoveCurrentPlayerAsync(total);
            await _gameRepo.SaveGameAsync(gid, game);

            // Notify clients that game updated (clients will fetch full state)
            await Clients.Group(GetGameGroupName(gid)).SendAsync("GameUpdated", gid.ToString());

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string GetGameGroupName(Guid gameId) => $"game_{gameId:N}";
}
