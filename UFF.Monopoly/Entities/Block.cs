namespace UFF.Monopoly.Entities;

public class Block
{
    public int Position { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int Price { get; set; } = 0;
    public int Rent { get; set; } = 0; // base rent (level 0)
    public Player? Owner { get; set; } = null;
    public bool IsMortgaged { get; set; } = false;
    public BlockType Type { get; set; } = BlockType.Property;

    public virtual Task Action(Game game, Player player)
    {
        switch (Type)
        {
            case BlockType.Go:
                break;
            case BlockType.Property:
            case BlockType.Company:
                if (Owner != null && Owner != player && !IsMortgaged)
                {
                    game.Transfer(player, Owner, Rent);
                }
                break;
            case BlockType.Tax:
                // Taxa será aplicada via lógica pendente no modal (Tax usa cálculo percentual dinâmico).
                // Aqui não paga para evitar efeitos duplicados.
                break;
            case BlockType.GoToJail:
                game.SendToJail(player);
                break;
            case BlockType.Jail:
                break;
            case BlockType.Chance:
                // Chance é tratada pela UI (pendente) para mostrar mensagem antes de aplicar.
                break;
            case BlockType.Reves:
                // Revés agora tratado somente pela lógica pendente (voltar casas ou perder dinheiro).
                break;
            case BlockType.FreeParking:
                break;
        }
        return Task.CompletedTask;
    }
}

public class CompanyBlock : Block
{
    public CompanyBlock()
    {
        Type = BlockType.Company;
        Price = 500;
        Rent = 300;
    }
}

public class PropertyBlock : Block
{
    public PropertyLevel Level { get; set; } = PropertyLevel.Barata; // mantém classificação econômica
    public BuildingType BuildingType { get; set; } = BuildingType.None;
    public int BuildingLevel { get; set; } = 0; // 0 = sem construção
    public int[] BuildingPrices { get; set; } = new int[4]; // custo incremental para níveis 1..4
    public Guid? GroupId { get; set; }

    private static string GetBaseName(BuildingType type) => type switch
    {
        BuildingType.House => "Terreno",
        BuildingType.Hotel => "Terreno",
        BuildingType.Company => "Terreno",
        BuildingType.Special => "Circo",
        _ => "Terreno"
    };

    public override Task Action(Game game, Player player)
    {
        if (Owner != null && Owner != player && !IsMortgaged)
        {
            var rentToPay = CalculateRent();
            game.Transfer(player, Owner, rentToPay);
        }
        return Task.CompletedTask;
    }

    public int CalculateRent()
    {
        if (BuildingType == BuildingType.None || BuildingLevel == 0)
            return Rent; // base
        var increments = new[] { 0.60m, 1.40m, 2.30m, 3.40m };
        var baseRent = Rent;
        var total = baseRent + (int)(baseRent * increments[BuildingLevel - 1]);
        if (BuildingType == BuildingType.Company) total = (int)(total * 1.10m);
        if (BuildingType == BuildingType.Special) total = (int)(total * 1.15m);
        return total;
    }

    public bool CanUpgrade() => BuildingType != BuildingType.None && BuildingLevel < 4;

    public bool Upgrade(Player player)
    {
        if (!CanUpgrade()) return false;
        var cost = BuildingPrices[BuildingLevel];
        if (player.Money < cost) return false;
        player.Money -= cost;
        BuildingLevel++;
        ImageUrl = BuildingType switch
        {
            BuildingType.House => $"/images/board/buildings/house{BuildingLevel}.png",
            BuildingType.Hotel => $"/images/board/buildings/hotel{BuildingLevel}.png",
            BuildingType.Company => $"/images/board/buildings/company{BuildingLevel}.png",
            BuildingType.Special => BuildingLevel switch
            {
                1 => "/images/board/buildings/special_circus.png",
                2 => "/images/board/buildings/special_shopping.png",
                3 => "/images/board/buildings/special_stadium.png",
                4 => "/images/board/buildings/special_airport.png",
                _ => ImageUrl
            },
            _ => ImageUrl
        };
        if (BuildingType != BuildingType.None)
        {
            var evo = UFF.Monopoly.Components.Pages.BoardBuilders.BuildingEvolutionDescriptions.Get(BuildingType, Math.Clamp(BuildingLevel, 1, 4));
            Name = evo.Name;
        }
        return true;
    }

    public void SetBuildingType(BuildingType type)
    {
        if (BuildingLevel > 0) return; // já iniciado
        BuildingType = type;
        ImageUrl = type switch
        {
            BuildingType.House => "/images/board/buildings/house1.png",
            BuildingType.Hotel => "/images/board/buildings/hotel1.png",
            BuildingType.Company => "/images/board/buildings/company1.png",
            BuildingType.Special => "/images/board/buildings/special_circus.png",
            _ => ImageUrl
        };
        if (BuildingType != BuildingType.None)
        {
            Name = GetBaseName(BuildingType);
        }
    }
}
