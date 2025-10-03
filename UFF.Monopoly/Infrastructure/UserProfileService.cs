using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore;
using UFF.Monopoly.Data;
using UFF.Monopoly.Data.Entities;

namespace UFF.Monopoly.Infrastructure;

public interface IUserProfileService
{
    Task<UserProfileEntity> GetOrCreateAsync(CancellationToken ct = default);
    Task UpdateAsync(string? displayName = null, Guid? lastBoardId = null, string? pawnImageUrl = null, CancellationToken ct = default);
    Task<string?> GetPawnFromSessionAsync();
    Task SetPawnInSessionAsync(string pawnUrl);
}

internal sealed class UserProfileService : IUserProfileService
{
    private readonly ProtectedLocalStorage _storage;
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    private const string ClientIdKey = "clientId";
    private const string PawnKey = "pawnImageUrl";
    private const string DefaultPawn = "/images/pawns/PawnsB1.png";

    public UserProfileService(ProtectedLocalStorage storage, IDbContextFactory<ApplicationDbContext> factory)
    {
        _storage = storage;
        _factory = factory;
    }

    public async Task<UserProfileEntity> GetOrCreateAsync(CancellationToken ct = default)
    {
        var clientId = await GetOrCreateClientIdAsync();
        await using var db = await _factory.CreateDbContextAsync(ct);
        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.ClientId == clientId, ct);
        if (profile is null)
        {
            profile = new UserProfileEntity
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                DisplayName = "Player 1",
                PawnImageUrl = DefaultPawn,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.UserProfiles.Add(profile);
            await db.SaveChangesAsync(ct);
        }
        else if (string.IsNullOrWhiteSpace(profile.PawnImageUrl))
        {
            profile.PawnImageUrl = DefaultPawn;
            profile.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        // Ensure browser storage has the pawn as well
        var pawnLocal = await _storage.GetAsync<string>(PawnKey);
        if (!pawnLocal.Success || string.IsNullOrWhiteSpace(pawnLocal.Value))
        {
            await _storage.SetAsync(PawnKey, profile.PawnImageUrl!);
        }
        return profile;
    }

    public async Task UpdateAsync(string? displayName = null, Guid? lastBoardId = null, string? pawnImageUrl = null, CancellationToken ct = default)
    {
        var clientId = await GetOrCreateClientIdAsync();
        await using var db = await _factory.CreateDbContextAsync(ct);
        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.ClientId == clientId, ct);
        if (profile is null)
        {
            profile = new UserProfileEntity { Id = Guid.NewGuid(), ClientId = clientId };
            db.UserProfiles.Add(profile);
        }
        if (!string.IsNullOrWhiteSpace(displayName)) profile.DisplayName = displayName!;
        if (lastBoardId.HasValue) profile.LastBoardId = lastBoardId;
        if (!string.IsNullOrWhiteSpace(pawnImageUrl)) profile.PawnImageUrl = pawnImageUrl;
        profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(pawnImageUrl))
            await _storage.SetAsync(PawnKey, pawnImageUrl);
    }

    public async Task<string?> GetPawnFromSessionAsync()
    {
        try
        {
            var stored = await _storage.GetAsync<string>(PawnKey);
            return stored.Success ? stored.Value : null;
        }
        catch
        {
            return null;
        }
    }

    public async Task SetPawnInSessionAsync(string pawnUrl)
    {
        try
        {
            await _storage.SetAsync(PawnKey, pawnUrl);
        }
        catch { }
    }

    private async Task<string> GetOrCreateClientIdAsync()
    {
        var stored = await _storage.GetAsync<string>(ClientIdKey);
        if (stored.Success && !string.IsNullOrWhiteSpace(stored.Value))
        {
            return stored.Value!;
        }
        var id = Guid.NewGuid().ToString("N");
        await _storage.SetAsync(ClientIdKey, id);
        return id;
    }
}
