using System.Text.Json;
using System.Text.Json.Serialization;
using UFF.Monopoly.Entities;

namespace UFF.Monopoly.Infrastructure;

public interface IBlockSetProvider
{
    Task<List<BlockSetInfo>> ListAsync(CancellationToken ct = default);
    Task<(BlockSetInfo info, List<Block> blocks)> LoadAsync(string key, CancellationToken ct = default);
}

public class BlockSetInfo
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public int Rows { get; init; } = 10;
    public int Cols { get; init; } = 10;
    public int CellSizePx { get; init; } = 64;
    public int Count { get; init; }
}

internal sealed class BlockSetProvider(IWebHostEnvironment env) : IBlockSetProvider
{
    private readonly string _folder = Path.Combine(env.WebRootPath, "blocks");

    public async Task<List<BlockSetInfo>> ListAsync(CancellationToken ct = default)
    {
        var result = new List<BlockSetInfo>();
        if (!Directory.Exists(_folder)) return result;
        foreach (var file in Directory.EnumerateFiles(_folder, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                await using var fs = File.OpenRead(file);
                var model = await JsonSerializer.DeserializeAsync<BlockFileModel>(fs, JsonOpts, ct) ?? new();
                var key = Path.GetFileNameWithoutExtension(file);
                result.Add(new BlockSetInfo
                {
                    Key = key,
                    Name = string.IsNullOrWhiteSpace(model.Name) ? key : model.Name,
                    Rows = model.Rows > 0 ? model.Rows : 10,
                    Cols = model.Cols > 0 ? model.Cols : 10,
                    CellSizePx = model.CellSizePx > 0 ? model.CellSizePx : 64,
                    Count = model.Blocks?.Count ?? 0
                });
            }
            catch
            {
                // ignore invalid files
            }
        }
        return result.OrderByDescending(i => i.Name).ToList();
    }

    public async Task<(BlockSetInfo info, List<Block> blocks)> LoadAsync(string key, CancellationToken ct = default)
    {
        var file = Path.Combine(_folder, key + ".json");
        if (!File.Exists(file)) return (new BlockSetInfo { Key = key, Name = key, Rows = 10, Cols = 10, CellSizePx = 64, Count = 0 }, new());
        await using var fs = File.OpenRead(file);
        var model = await JsonSerializer.DeserializeAsync<BlockFileModel>(fs, JsonOpts, ct) ?? new();
        var info = new BlockSetInfo
        {
            Key = key,
            Name = string.IsNullOrWhiteSpace(model.Name) ? key : model.Name,
            Rows = model.Rows > 0 ? model.Rows : 10,
            Cols = model.Cols > 0 ? model.Cols : 10,
            CellSizePx = model.CellSizePx > 0 ? model.CellSizePx : 64,
            Count = model.Blocks?.Count ?? 0
        };
        var blocks = (model.Blocks ?? []).OrderBy(b => b.Position).Select(m => new Block
        {
            Position = m.Position,
            Name = m.Name ?? string.Empty,
            Description = m.Description ?? string.Empty,
            ImageUrl = m.ImageUrl ?? string.Empty,
            Color = m.Color ?? string.Empty,
            Price = m.Price,
            Rent = m.Rent,
            Type = m.Type
        }).ToList();
        return (info, blocks);
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
    };

    private sealed class BlockFileModel
    {
        public string? Name { get; set; }
        public int Rows { get; set; }
        public int Cols { get; set; }
        public int CellSizePx { get; set; }
        public List<BlockFileItem>? Blocks { get; set; }
    }

    private sealed class BlockFileItem
    {
        public int Position { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? Color { get; set; }
        public int Price { get; set; }
        public int Rent { get; set; }
        public BlockType Type { get; set; }
    }
}
