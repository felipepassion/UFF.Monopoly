using UFF.Monopoly.Entities;

namespace UFF.Monopoly.Setup;

public static class BoardFactory
{
    public static List<Block> CreateBasicBoard()
    {
        // Minimal, generic board (no copyrighted names), 40 spaces
        var list = new List<Block>();

        list.Add(new Block { Position = 0, Name = "GO", Type = BlockType.Go, Description = "Collect salary when passing." });
        list.Add(new Block { Position = 1, Name = "Brown 1", Type = BlockType.Property, Color = "#8B4513", Price = 60, Rent = 2 });
        list.Add(new Block { Position = 2, Name = "Community", Type = BlockType.CommunityChest });
        list.Add(new Block { Position = 3, Name = "Brown 2", Type = BlockType.Property, Color = "#8B4513", Price = 60, Rent = 4 });
        list.Add(new Block { Position = 4, Name = "Income Tax", Type = BlockType.Tax, Rent = 200 });
        list.Add(new Block { Position = 5, Name = "Station A", Type = BlockType.Property, Color = "#000000", Price = 200, Rent = 25 });
        list.Add(new Block { Position = 6, Name = "Light Blue 1", Type = BlockType.Property, Color = "#ADD8E6", Price = 100, Rent = 6 });
        list.Add(new Block { Position = 7, Name = "Chance", Type = BlockType.Chance });
        list.Add(new Block { Position = 8, Name = "Light Blue 2", Type = BlockType.Property, Color = "#ADD8E6", Price = 100, Rent = 6 });
        list.Add(new Block { Position = 9, Name = "Light Blue 3", Type = BlockType.Property, Color = "#ADD8E6", Price = 120, Rent = 8 });
        list.Add(new Block { Position = 10, Name = "Jail / Just Visiting", Type = BlockType.Jail });
        list.Add(new Block { Position = 11, Name = "Pink 1", Type = BlockType.Property, Color = "#FFC0CB", Price = 140, Rent = 10 });
        list.Add(new Block { Position = 12, Name = "Utility A", Type = BlockType.Property, Color = "#888888", Price = 150, Rent = 10 });
        list.Add(new Block { Position = 13, Name = "Pink 2", Type = BlockType.Property, Color = "#FFC0CB", Price = 140, Rent = 10 });
        list.Add(new Block { Position = 14, Name = "Pink 3", Type = BlockType.Property, Color = "#FFC0CB", Price = 160, Rent = 12 });
        list.Add(new Block { Position = 15, Name = "Station B", Type = BlockType.Property, Color = "#000000", Price = 200, Rent = 25 });
        list.Add(new Block { Position = 16, Name = "Orange 1", Type = BlockType.Property, Color = "#FFA500", Price = 180, Rent = 14 });
        list.Add(new Block { Position = 17, Name = "Community", Type = BlockType.CommunityChest });
        list.Add(new Block { Position = 18, Name = "Orange 2", Type = BlockType.Property, Color = "#FFA500", Price = 180, Rent = 14 });
        list.Add(new Block { Position = 19, Name = "Orange 3", Type = BlockType.Property, Color = "#FFA500", Price = 200, Rent = 16 });
        list.Add(new Block { Position = 20, Name = "Free Parking", Type = BlockType.FreeParking });
        list.Add(new Block { Position = 21, Name = "Red 1", Type = BlockType.Property, Color = "#FF0000", Price = 220, Rent = 18 });
        list.Add(new Block { Position = 22, Name = "Chance", Type = BlockType.Chance });
        list.Add(new Block { Position = 23, Name = "Red 2", Type = BlockType.Property, Color = "#FF0000", Price = 220, Rent = 18 });
        list.Add(new Block { Position = 24, Name = "Red 3", Type = BlockType.Property, Color = "#FF0000", Price = 240, Rent = 20 });
        list.Add(new Block { Position = 25, Name = "Station C", Type = BlockType.Property, Color = "#000000", Price = 200, Rent = 25 });
        list.Add(new Block { Position = 26, Name = "Yellow 1", Type = BlockType.Property, Color = "#FFFF00", Price = 260, Rent = 22 });
        list.Add(new Block { Position = 27, Name = "Yellow 2", Type = BlockType.Property, Color = "#FFFF00", Price = 260, Rent = 22 });
        list.Add(new Block { Position = 28, Name = "Utility B", Type = BlockType.Property, Color = "#888888", Price = 150, Rent = 10 });
        list.Add(new Block { Position = 29, Name = "Yellow 3", Type = BlockType.Property, Color = "#FFFF00", Price = 280, Rent = 24 });
        list.Add(new Block { Position = 30, Name = "Go To Jail", Type = BlockType.GoToJail });
        list.Add(new Block { Position = 31, Name = "Green 1", Type = BlockType.Property, Color = "#008000", Price = 300, Rent = 26 });
        list.Add(new Block { Position = 32, Name = "Green 2", Type = BlockType.Property, Color = "#008000", Price = 300, Rent = 26 });
        list.Add(new Block { Position = 33, Name = "Community", Type = BlockType.CommunityChest });
        list.Add(new Block { Position = 34, Name = "Green 3", Type = BlockType.Property, Color = "#008000", Price = 320, Rent = 28 });
        list.Add(new Block { Position = 35, Name = "Station D", Type = BlockType.Property, Color = "#000000", Price = 200, Rent = 25 });
        list.Add(new Block { Position = 36, Name = "Chance", Type = BlockType.Chance });
        list.Add(new Block { Position = 37, Name = "Dark Blue 1", Type = BlockType.Property, Color = "#00008B", Price = 350, Rent = 35 });
        list.Add(new Block { Position = 38, Name = "Luxury Tax", Type = BlockType.Tax, Rent = 100 });
        list.Add(new Block { Position = 39, Name = "Dark Blue 2", Type = BlockType.Property, Color = "#00008B", Price = 400, Rent = 50 });

        return list;
    }
}
