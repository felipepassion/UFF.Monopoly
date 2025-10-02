using UFF.Monopoly.Entities;

namespace UFF.Monopoly.Setup;

public static class BoardFactory
{
    public static List<Block> CreateBasicBoard()
    {
        // Minimal board without Chance, Community Chest, or Free Parking
        var list = new List<Block>();

        list.Add(new Block { Position = 0, Name = "GO", Type = BlockType.Go, Description = "Collect salary when passing." });
        list.Add(new Block { Position = 1, Name = "Brown 1", Type = BlockType.Property, Color = "#8B4513", Price = 60, Rent = 2 });
        list.Add(new Block { Position = 2, Name = "Brown 2", Type = BlockType.Property, Color = "#8B4513", Price = 60, Rent = 4 });
        list.Add(new Block { Position = 3, Name = "Income Tax", Type = BlockType.Tax, Rent = 200 });
        list.Add(new Block { Position = 4, Name = "Station A", Type = BlockType.Property, Color = "#000000", Price = 200, Rent = 25 });
        list.Add(new Block { Position = 5, Name = "Light Blue 1", Type = BlockType.Property, Color = "#ADD8E6", Price = 100, Rent = 6 });
        list.Add(new Block { Position = 6, Name = "Light Blue 2", Type = BlockType.Property, Color = "#ADD8E6", Price = 100, Rent = 6 });
        list.Add(new Block { Position = 7, Name = "Light Blue 3", Type = BlockType.Property, Color = "#ADD8E6", Price = 120, Rent = 8 });
        list.Add(new Block { Position = 8, Name = "Jail / Just Visiting", Type = BlockType.Jail });
        list.Add(new Block { Position = 9, Name = "Pink 1", Type = BlockType.Property, Color = "#FFC0CB", Price = 140, Rent = 10 });
        list.Add(new Block { Position = 10, Name = "Utility A", Type = BlockType.Property, Color = "#888888", Price = 150, Rent = 10 });
        list.Add(new Block { Position = 11, Name = "Pink 2", Type = BlockType.Property, Color = "#FFC0CB", Price = 140, Rent = 10 });
        list.Add(new Block { Position = 12, Name = "Pink 3", Type = BlockType.Property, Color = "#FFC0CB", Price = 160, Rent = 12 });
        list.Add(new Block { Position = 13, Name = "Station B", Type = BlockType.Property, Color = "#000000", Price = 200, Rent = 25 });
        list.Add(new Block { Position = 14, Name = "Orange 1", Type = BlockType.Property, Color = "#FFA500", Price = 180, Rent = 14 });
        list.Add(new Block { Position = 15, Name = "Orange 2", Type = BlockType.Property, Color = "#FFA500", Price = 180, Rent = 14 });
        list.Add(new Block { Position = 16, Name = "Orange 3", Type = BlockType.Property, Color = "#FFA500", Price = 200, Rent = 16 });
        list.Add(new Block { Position = 17, Name = "Go To Jail", Type = BlockType.GoToJail });
        list.Add(new Block { Position = 18, Name = "Red 1", Type = BlockType.Property, Color = "#FF0000", Price = 220, Rent = 18 });
        list.Add(new Block { Position = 19, Name = "Red 2", Type = BlockType.Property, Color = "#FF0000", Price = 220, Rent = 18 });

        return list;
    }
}
