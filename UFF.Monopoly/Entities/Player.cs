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

    // Pawn selection index (1..6) - persisted to DB
    public int PawnIndex { get; set; } = 1;

    public List<Block> OwnedProperties { get; set; } = new();

    // New bookkeeping fields
    // Number of turns to skip (e.g., when sent to jail)
    public int SkipTurns { get; set; } = 0;

    // Turn number when player last bought a property (used to prevent immediate building)
    public int LastPurchaseTurn { get; set; } = -1;

    // Turn number when player last built something (to restrict to 1 build per turn)
    public int LastBuildTurn { get; set; } = -1;

    // Pontuação de patrimônio (dinheiro + valor base dos imóveis + custo investido em construções)
    // Não persistido em banco; calculado sob demanda.
    public int AssetScore
    {
        get
        {
            int propertiesValue = 0;
            foreach (var b in OwnedProperties)
            {
                propertiesValue += b.Price;
                if (b is PropertyBlock pb && pb.BuildingLevel > 0 && pb.BuildingPrices is not null)
                {
                    for (int i = 0; i < pb.BuildingLevel && i < pb.BuildingPrices.Length; i++)
                    {
                        propertiesValue += pb.BuildingPrices[i];
                    }
                }
            }
            return Money + propertiesValue;
        }
    }
}
