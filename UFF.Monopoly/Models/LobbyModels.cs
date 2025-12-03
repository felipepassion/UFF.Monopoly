using System.Collections.Concurrent;

namespace UFF.Monopoly.Models;

public class LobbyRoom
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string HostConnectionId { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public int MaxPlayers { get; set; } = 4;
    public List<LobbyPlayer> Players { get; set; } = new();
    public List<RoomChatMessage> Chat { get; set; } = new();

    public bool IsFull => Players.Count >= MaxPlayers;
}

public class LobbyPlayer
{
    public string ConnectionId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsBot { get; set; }
    public bool IsReady { get; set; }
}

public class RoomChatMessage
{
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string Sender { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public static class LobbyState
{
    public static readonly ConcurrentDictionary<string, LobbyRoom> Rooms = new();
    public static readonly ConcurrentDictionary<string, string> ConnectionRoomMap = new(); // connId -> roomId

    public static IEnumerable<LobbyRoom> Snapshot() => Rooms.Values.OrderBy(r => r.Name).ToArray();
}
