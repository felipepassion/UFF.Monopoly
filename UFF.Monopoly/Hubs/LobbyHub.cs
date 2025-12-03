using Microsoft.AspNetCore.SignalR;
using UFF.Monopoly.Models;

namespace UFF.Monopoly.Hubs;

public class LobbyHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("LobbyUpdated", LobbyState.Snapshot());
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (LobbyState.ConnectionRoomMap.TryRemove(Context.ConnectionId, out var roomId))
        {
            if (LobbyState.Rooms.TryGetValue(roomId, out var room))
            {
                room.Players.RemoveAll(p => p.ConnectionId == Context.ConnectionId);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetRoomGroup(roomId));

                if (room.HostConnectionId == Context.ConnectionId)
                {
                    if (room.Players.Count > 0)
                    {
                        var newHost = room.Players.FirstOrDefault(p => !p.IsBot) ?? room.Players[0];
                        room.HostConnectionId = newHost.ConnectionId;
                        room.HostName = newHost.DisplayName;
                    }
                    else
                    {
                        LobbyState.Rooms.TryRemove(room.Id, out _);
                    }
                }

                if (LobbyState.Rooms.ContainsKey(room.Id))
                {
                    await Clients.Group(GetRoomGroup(roomId)).SendAsync("RoomUpdated", room);
                }
            }
        }
        await BroadcastLobby();
        await base.OnDisconnectedAsync(exception);
    }

    public async Task CreateRoom(string name, int maxPlayers, string displayName)
    {
        if (string.IsNullOrWhiteSpace(name)) name = "Room";
        if (maxPlayers <= 1) maxPlayers = 2;
        if (maxPlayers > 8) maxPlayers = 8;

        var room = new LobbyRoom
        {
            Name = name.Trim(),
            MaxPlayers = maxPlayers,
            HostConnectionId = Context.ConnectionId,
            HostName = string.IsNullOrWhiteSpace(displayName) ? "Host" : displayName.Trim()
        };
        room.Players.Add(new LobbyPlayer { ConnectionId = Context.ConnectionId, DisplayName = room.HostName, IsReady = false });

        LobbyState.Rooms[room.Id] = room;
        LobbyState.ConnectionRoomMap[Context.ConnectionId] = room.Id;

        await Groups.AddToGroupAsync(Context.ConnectionId, GetRoomGroup(room.Id));
        await Clients.Caller.SendAsync("NavigatedToRoom", room.Id);
        await Clients.Group(GetRoomGroup(room.Id)).SendAsync("RoomUpdated", room);
        await BroadcastLobby();
    }

    public async Task JoinRoom(string roomId, string displayName)
    {
        if (!LobbyState.Rooms.TryGetValue(roomId, out var room)) return;
        var max = Math.Min(4, room.MaxPlayers);
        if (room.Players.Count >= max) { await Clients.Caller.SendAsync("JoinFailed", roomId, "Room full"); return; }

        // If already present by connection id, no-op
        var existingByConn = room.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
        if (existingByConn is not null) return;

        // If same display name exists, treat as reconnection: update connection id
        var existingByName = room.Players.FirstOrDefault(p => !p.IsBot && string.Equals(p.DisplayName, displayName?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (existingByName is not null)
        {
            existingByName.ConnectionId = Context.ConnectionId;
            LobbyState.ConnectionRoomMap[Context.ConnectionId] = roomId;
            await Groups.AddToGroupAsync(Context.ConnectionId, GetRoomGroup(roomId));
            await Clients.Caller.SendAsync("NavigatedToRoom", roomId);
            await Clients.Group(GetRoomGroup(roomId)).SendAsync("RoomUpdated", room);
            await BroadcastLobby();
            return;
        }

        var player = new LobbyPlayer { ConnectionId = Context.ConnectionId, DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Player" : displayName.Trim(), IsReady = false };
        room.Players.Add(player);
        LobbyState.ConnectionRoomMap[Context.ConnectionId] = roomId;

        await Groups.AddToGroupAsync(Context.ConnectionId, GetRoomGroup(roomId));
        await Clients.Caller.SendAsync("NavigatedToRoom", roomId);
        await Clients.Group(GetRoomGroup(roomId)).SendAsync("RoomUpdated", room);
        await BroadcastLobby();
    }

    // For direct link visitors: subscribe to room group and optionally add to players if not present
    public async Task SubscribeRoom(string roomId, string displayName)
    {
        if (!LobbyState.Rooms.TryGetValue(roomId, out var room)) return;
        await Groups.AddToGroupAsync(Context.ConnectionId, GetRoomGroup(roomId));

        // If the same display name exists, update its connectionId to current and map, avoiding duplicates
        var existingByName = room.Players.FirstOrDefault(p => !p.IsBot && string.Equals(p.DisplayName, displayName?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (existingByName is not null)
        {
            existingByName.ConnectionId = Context.ConnectionId;
            LobbyState.ConnectionRoomMap[Context.ConnectionId] = roomId;
            await Clients.Group(GetRoomGroup(room.Id)).SendAsync("RoomUpdated", room);
            await BroadcastLobby();
            return;
        }

        if (!room.Players.Any(p => p.ConnectionId == Context.ConnectionId))
        {
            var max = Math.Min(4, room.MaxPlayers);
            if (room.Players.Count < max)
            {
                var player = new LobbyPlayer { ConnectionId = Context.ConnectionId, DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Player" : displayName.Trim(), IsReady = false };
                room.Players.Add(player);
                LobbyState.ConnectionRoomMap[Context.ConnectionId] = roomId;
                await Clients.Group(GetRoomGroup(room.Id)).SendAsync("RoomUpdated", room);
                await BroadcastLobby();
            }
            else
            {
                await Clients.Caller.SendAsync("JoinFailed", roomId, "Room full");
            }
        }
        else
        {
            await Clients.Caller.SendAsync("RoomUpdated", room);
        }
    }

    public async Task LeaveRoom(string roomId)
    {
        if (!LobbyState.Rooms.TryGetValue(roomId, out var room)) return;
        var removed = room.Players.RemoveAll(p => p.ConnectionId == Context.ConnectionId);
        if (removed > 0)
        {
            LobbyState.ConnectionRoomMap.TryRemove(Context.ConnectionId, out _);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetRoomGroup(roomId));

            if (room.HostConnectionId == Context.ConnectionId)
            {
                if (room.Players.Count > 0)
                {
                    var newHost = room.Players.FirstOrDefault(p => !p.IsBot) ?? room.Players[0];
                    room.HostConnectionId = newHost.ConnectionId;
                    room.HostName = newHost.DisplayName;
                }
                else
                {
                    LobbyState.Rooms.TryRemove(room.Id, out _);
                }
            }

            if (LobbyState.Rooms.ContainsKey(room.Id))
            {
                await Clients.Group(GetRoomGroup(roomId)).SendAsync("RoomUpdated", room);
            }
            await BroadcastLobby();
        }
    }

    public Task RequestLobby()
        => Clients.Caller.SendAsync("LobbyUpdated", LobbyState.Snapshot());

    public async Task SendRoomMessage(string roomId, string sender, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (!LobbyState.Rooms.TryGetValue(roomId, out var room)) return;
        var msg = new RoomChatMessage { Sender = string.IsNullOrWhiteSpace(sender) ? "Player" : sender.Trim(), Text = text.Trim() };
        room.Chat.Add(msg);
        await Clients.Group(GetRoomGroup(roomId)).SendAsync("RoomChatMessage", roomId, msg);
    }

    public async Task ToggleReady(string roomId)
    {
        if (!LobbyState.Rooms.TryGetValue(roomId, out var room)) return;
        var player = room.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
        if (player is null)
        {
            // Fallback: match by recent name in case of reconnection
            player = room.Players.FirstOrDefault(p => !p.IsBot && string.Equals(p.DisplayName, room.HostName, StringComparison.OrdinalIgnoreCase));
        }
        if (player is null) return;
        player.IsReady = !player.IsReady;
        await Clients.Group(GetRoomGroup(roomId)).SendAsync("RoomUpdated", room);
    }

    public async Task AddBot(string roomId, string botName)
    {
        if (!LobbyState.Rooms.TryGetValue(roomId, out var room)) return;
        if (room.HostConnectionId != Context.ConnectionId) return;
        var max = Math.Min(4, room.MaxPlayers);
        if (room.Players.Count >= max) return;
        var bot = new LobbyPlayer { ConnectionId = $"bot_{Guid.NewGuid():N}", DisplayName = string.IsNullOrWhiteSpace(botName) ? "Bot" : botName.Trim(), IsBot = true, IsReady = true };
        room.Players.Add(bot);
        await Clients.Group(GetRoomGroup(roomId)).SendAsync("RoomUpdated", room);
        await BroadcastLobby();
    }

    public async Task RemoveBot(string roomId, string botConnectionId)
    {
        if (!LobbyState.Rooms.TryGetValue(roomId, out var room)) return;
        if (room.HostConnectionId != Context.ConnectionId) return;
        var removed = room.Players.RemoveAll(p => p.ConnectionId == botConnectionId && p.IsBot);
        if (removed > 0)
        {
            await Clients.Group(GetRoomGroup(roomId)).SendAsync("RoomUpdated", room);
            await BroadcastLobby();
        }
    }

    private Task BroadcastLobby()
        => Clients.All.SendAsync("LobbyUpdated", LobbyState.Snapshot());

    private static string GetRoomGroup(string roomId) => $"lobbyroom_{roomId}";
}
