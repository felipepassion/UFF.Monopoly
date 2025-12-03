using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UFF.Monopoly.Components;
using UFF.Monopoly.Data;
using UFF.Monopoly.Data.Entities;
using UFF.Monopoly.Infrastructure;
using UFF.Monopoly.Repositories;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using UFF.Monopoly.Hubs;
using UFF.Monopoly.Infrastructure.Bot;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.AddIdentityCore<IdentityUser>(options =>
{
    options.User.RequireUniqueEmail = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 1;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
// Postgres provider
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddScoped<ProtectedLocalStorage>();

builder.Services.AddHttpClient();

// Repositories
builder.Services.AddSingleton<IGameRepository, EfGameRepository>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddSingleton<IBotDecisionService, BotDecisionService>();

// SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Seeder
//await ApplicationDbSeeder.SeedAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// Ensure responses that are text/*, application/javascript or application/json include charset=utf-8 when missing
app.Use(async (context, next) =>
{
    await next();
    try
    {
        var ct = context.Response.ContentType;
        if (!string.IsNullOrWhiteSpace(ct))
        {
            // If content type already has a charset, do nothing
            if (!ct.Contains("charset", StringComparison.OrdinalIgnoreCase))
            {
                if (ct.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
                    || ct.Contains("javascript", StringComparison.OrdinalIgnoreCase)
                    || ct.Contains("json", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.ContentType = ct + "; charset=utf-8";
                }
            }
        }
    }
    catch { }
});

app.MapGroup("/auth").MapGet("/quick", async (string name, string cid, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, ApplicationDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(cid)) return Results.BadRequest();
    var user = await userManager.FindByNameAsync(cid);
    if (user is null)
    {
        user = new IdentityUser { UserName = cid };
        var createResult = await userManager.CreateAsync(user);
        if (!createResult.Succeeded) return Results.BadRequest(createResult.Errors);
    }

    // Update display name claim
    var claims = await userManager.GetClaimsAsync(user);
    var nameClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
    if (nameClaim is not null)
        await userManager.RemoveClaimAsync(user, nameClaim);
    await userManager.AddClaimAsync(user, new Claim(ClaimTypes.Name, name));

    await signInManager.SignInAsync(user, isPersistent: true);

    // Mirror to UserProfiles table for analytics/preferences
    var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.ClientId == cid);
    if (profile is null)
    {
        profile = new UserProfileEntity { Id = Guid.NewGuid(), ClientId = cid, DisplayName = name, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.UserProfiles.Add(profile);
    }
    else
    {
        profile.DisplayName = name;
        profile.UpdatedAt = DateTime.UtcNow;
    }
    await db.SaveChangesAsync();

    return Results.Redirect("/");
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map SignalR hubs
app.MapHub<LobbyHub>("/hubs/lobby");
app.MapHub<GameplayHub>("/hubs/gameplay");
app.MapHub<GameHub>("/gamehub");

app.Run();
