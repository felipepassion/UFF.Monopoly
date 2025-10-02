namespace UFF.Monopoly.Entities;

public class Player
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int CurrentPosition { get; set; } = 0;
    public int Money { get; set; } = 1500;
    public string Name { get; set; } = string.Empty;
    public bool InJail { get; set; } = false;
    public int GetOutOfJailFreeCards { get; set; } = 0;
    public int JailTurns { get; set; } = 0;
    public bool IsBankrupt { get; set; } = false;
    
    public List<Block> OwnedProperties { get; set; } = new();
}
