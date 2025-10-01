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
        // Default behavior based on type
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
            case BlockType.FreeParking:
            case BlockType.Chance:
            case BlockType.CommunityChest:
                // Not implemented in minimal version
                break;
        }
        return Task.CompletedTask;
    }
}
