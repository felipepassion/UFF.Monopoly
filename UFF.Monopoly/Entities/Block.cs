namespace UFF.Monopoly.Entities;

public class Block
{
    public int Position { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int Price { get; set; } = 0;
    public int Rent { get; set; } = 0;
    public Player? Owner { get; set; } = null;
    public bool IsMortgaged { get; set; } = false;
    public BlockType Type { get; set; } = BlockType.Property;

    // Action to be executed when a player lands on this block.
    public virtual Task Action(Game game, Player player)
    {
        switch (Type)
        {
            case BlockType.Go:
                // nothing special on landing beyond salary handled on pass
                break;
            case BlockType.Property:
                if (Owner != null && Owner != player && !IsMortgaged)
                {
                    game.Transfer(player, Owner, Rent);
                }
                break;
            case BlockType.Tax:
                game.PayBank(player, Rent);
                break;
            case BlockType.GoToJail:
                game.SendToJail(player);
                break;
            case BlockType.Jail:
                // just visiting when landing
                break;
            case BlockType.Chance:
                // basic chance: give or take small amount randomly
                var rng = new Random();
                var delta = rng.Next(-200, 301); // -200..300
                if (delta >= 0)
                {
                    // bank pays player
                    player.Money += delta;
                }
                else
                {
                    game.PayBank(player, -delta);
                }
                break;
            case BlockType.Reves:
                // reverse of chance
                var rng2 = new Random();
                var delta2 = rng2.Next(-100, 201);
                if (delta2 >= 0)
                {
                    player.Money += delta2;
                }
                else
                {
                    game.PayBank(player, -delta2);
                }
                break;
            case BlockType.FreeParking:
                // nothing for now
                break;
        }
        return Task.CompletedTask;
    }
}

public class PropertyBlock : Block
{
    public PropertyLevel Level { get; set; } = PropertyLevel.Barata;
    public int Houses { get; set; } = 0; // 0..4
    public int Hotels { get; set; } = 0; // 0..2

    // group id to identify monopoly sets
    public Guid? GroupId { get; set; }

    // Price details for building
    public int HousePrice { get; set; } = 0;
    public int HotelPrice { get; set; } = 0;

    // rents for 0..4 houses and 1..2 hotels (hotels index at 5 and 6)
    public int[] Rents { get; set; } = new int[7];

    public PropertyBlock()
    {
        Type = BlockType.Property;
    }

    public override Task Action(Game game, Player player)
    {
        if (Owner != null && Owner != player && !IsMortgaged)
        {
            var rent = CalculateRent();
            game.Transfer(player, Owner, rent);
        }
        // If owner is player, they can choose to build via UI elsewhere; building not automatic here.
        return Task.CompletedTask;
    }

    public int CalculateRent()
    {
        // if hotels > 0, use hotel rents; else use house rents based on Houses
        if (Hotels > 0)
        {
            var idx = 5 + Math.Min(Hotels - 1, 1); // 5 -> first hotel, 6 -> second hotel
            return Rents[idx];
        }
        return Rents[Math.Clamp(Houses, 0, 4)];
    }

    public bool CanBuildHouse() => Hotels == 0 && Houses < 4;
    public bool CanBuildHotel() => Houses == 4 && Hotels < 2;

    public bool BuildHouse(Player player)
    {
        if (!CanBuildHouse()) return false;
        if (player.Money < HousePrice) return false;
        player.Money -= HousePrice;
        Houses++;
        return true;
    }

    public bool BuildHotel(Player player)
    {
        if (!CanBuildHotel()) return false;
        if (player.Money < HotelPrice) return false;
        player.Money -= HotelPrice;
        // convert 4 houses into 1 hotel
        Houses = 0;
        Hotels++;
        return true;
    }
}
