public enum Blocks : byte
{
    Air = 0,
    Grass = 1,
    Dirt = 2,
    Stone = 3,
    Sand = 4,
    Log = 5,
    Leaves = 6,
    Water = 7,
    Cobblestone = 8,
    Planks = 9,
    Clay = 10,
    Bricks = 11,
    FancyLeaves = 12,
    Glass = 13,
    CoalOre = 14,
    IronOre = 15,
    Workbench = 16,
    Furnace = 17,
}
public enum Bioms : byte
{
    RockyHills = 1,
    Grass = 2,
    Desert = 3,
    Dirt = 4,
}
public class Enums
{
    public static string GetBlockName(int id) //Function to translate number block ID to readable string names
    {
        switch (id)
        {
            case 0:
                return "Air";
            case 1:
                return "Grass";
            case 2:
                return "Dirt";
            case 3:
                return "Stone";
            case 4:
                return "Sand";
            case 5:
                return "Log";
            case 6:
                return "Leaves";
            case 7:
                return "Water";
            case 8:
                return "Cobblestone";
            case 9:
                return "Planks";
            case 10:
                return "Clay";
            case 11:
                return "Bricks";
            case 12:
                return "Fancy Leaves";
            case 13:
                return "Glass";
            case 14:
                return "Coal Ore";
            case 15:
                return "Iron Ore";
            case 16:
                return "Workbench";
            case 17:
                return "Furnace";
            default:
                return "Null";
        }
    }
    public static string GetBiomName(int id) //Translating id of biom to readable string name
    {
        switch (id)
        {
            case 1:
                return "Rocky Hills";
            case 2:
                return "Grass";
            case 3:
                return "Desert";
            case 4:
                return "Dirt";
            default:
                return "Null";
        }
                                        
    }
}
