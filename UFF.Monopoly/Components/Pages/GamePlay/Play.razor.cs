using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using System.Net.Http;
using UFF.Monopoly.Data;
using UFF.Monopoly.Repositories;

namespace UFF.Monopoly.Components.Pages.GamePlay;

public partial class Play : ComponentBase, IAsyncDisposable
{
    // Injeções
    [Inject] public IDbContextFactory<ApplicationDbContext> DbFactory { get; set; } = default!;
    [Inject] public IGameRepository GameRepo { get; set; } = default!;
    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public Infrastructure.IUserProfileService Profiles { get; set; } = default!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] public HttpClient Http { get; set; } = default!;

    // Parâmetros
    [Parameter] public Guid GameId { get; set; }
    [SupplyParameterFromQuery] public Guid? boardId { get; set; }
    [SupplyParameterFromQuery(Name = "humanCount")] public int? HumanCountQuery { get; set; }
    [SupplyParameterFromQuery] public string? pawns { get; set; }
}
