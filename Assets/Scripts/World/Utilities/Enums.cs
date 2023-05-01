public enum Blocks
{
    //Cube blocks
    Air = 0,
    Grass = 10,
    Dirt = 20,
    Stone = 30,
    Sand = 40,
    Log = 50,
    Leaves = 60,
    WaterSource = 70,   //1.0f
    Water_1 = 79,       //0.9f
    Water_2 = 78,       //0.8f
    Water_3 = 77,       //0.7f
    Water_4 = 76,       //0.6f
    Water_5 = 75,       //0.5f
    Water_6 = 74,       //0.4f
    Water_7 = 73,       //0.3f
    Water_8 = 72,       //0.2f
    Water_9 = 71,       //0.1f
    Cobblestone = 80,
    Planks = 90,
    Clay = 100,
    Bricks = 110,
    Glass = 120,
    CoalOre = 130,
    IronOre = 140,
    Workbench = 150,
    Furnace = 160,
    AppleLeaves = 170,
    AppleLeavesFruit = 180,

    //Custom blocks
    StairsStone = 500,
    StairsCobble = 510,

    //Vegetation
    GrassBush = 1000,
    GrassShortBush = 1010,
    FruitBush = 1020,
    Flower1 = 1030,
    Flower2 = 1040,
    Flower3 = 1050,
}
public enum Bioms
{
    Ocean = 1,
    GrassFields = 2,
    Desert = 3,
    RockyMountains = 4,
    GrassHills = 5,
}
public class Enums
{
    public static int GetRotationsCount(int id)
    {
        id /= 10;
        switch (id)
        {
            case 5:
                return 2;
            case 16:
                return 3;
            case 50:
                return 3;
            case 51:
                return 3;
            default:
                return 0;
        }
    }
    public static string GetBlockName(int id) //Function to translate number block ID to readable string names
    {
        id /= 10;
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
                return "Glass";
            case 13:
                return "Coal Ore";
            case 14:
                return "Iron Ore";
            case 15:
                return "Workbench";
            case 16:
                return "Furnace";

            case 50:
                return "Stairs";
            case 51:
                return "Stairs";

            case 100:
                return "Grass Bush";
            case 101:
                return "Short Bush";
            case 102:
                return "Fruit Bush";
            case 103:
                return "Flower";
            case 104:
                return "Flower";
            case 105:
                return "Flower";
            default:
                return "Null";
        }
    }
    public static string GetBiomName(int id) //Translating id of biom to readable string name
    {
        switch (id)
        {
            case 1:
                return "Ocean";
            case 2:
                return "Grass Fields";
            case 3:
                return "Desert";
            case 4:
                return "Rocky Mountains";
            case 5:
                return "Grass Hills";
            default:
                return "Null";
        }

    }
}
