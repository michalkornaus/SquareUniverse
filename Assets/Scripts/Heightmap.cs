using UnityEngine;
using TinkerWorX.AccidentalNoiseLibrary;
using System.IO;

public class Heightmap
{
    private static readonly ImplicitFractal HeightNoise = new ImplicitFractal(FractalType.RidgedMulti, BasisType.Simplex, InterpolationType.Quintic)
    { Octaves = 4, Frequency = 0.2 };
    private static readonly ImplicitFractal BaseHNoise = new ImplicitFractal(FractalType.HybridMulti, BasisType.GradientValue, InterpolationType.Linear)
    { Octaves = 2, Frequency = 0.3 };
    private static readonly ImplicitFractal CaveNoise = new ImplicitFractal(FractalType.RidgedMulti, BasisType.Gradient, InterpolationType.Quintic)
    { Octaves = 1, Frequency = 2 };
    private static readonly ImplicitFractal TreeNoise = new ImplicitFractal(FractalType.Billow, BasisType.Value, InterpolationType.Linear)
    { Frequency = 4 };
    private static readonly ImplicitFractal BiomNoise = new ImplicitFractal(FractalType.FractionalBrownianMotion, BasisType.GradientValue, InterpolationType.Cubic)
    { Octaves = 8, Frequency = 0.25 };

    private static readonly ImplicitFractal GrassLandsNoise = new ImplicitFractal(FractalType.HybridMulti, BasisType.Gradient, InterpolationType.Cubic)
    { Octaves = 4, Frequency = 0.12 };
    private static readonly ImplicitFractal DesertNoise = new ImplicitFractal(FractalType.Billow, BasisType.Gradient, InterpolationType.Cubic)
    { Octaves = 3, Frequency = 0.16 };
    private static readonly ImplicitFractal HillsNoise = new ImplicitFractal(FractalType.FractionalBrownianMotion, BasisType.Gradient, InterpolationType.Cubic)
    { Octaves = 4, Frequency = 0.20 };
    private static readonly ImplicitFractal WaterNoise = new ImplicitFractal(FractalType.RidgedMulti, BasisType.Gradient, InterpolationType.Cubic)
    { Octaves = 2, Frequency = 0.08 };
    private static readonly int chunkSize = 16;
    private static readonly int heightmapSize = chunkSize * 8;
    private static readonly int chunkHeight = chunkSize * 8;
    private Vector2 position;

    //Cubes - Max 2 097 152
    public byte[] Cubes = new byte[128 * 16 * 16 * 64];

    //CubesBioms - Max 16384
    public byte[] CubesBioms = new byte[16 * 16 * 64];
    public Heightmap(int x, int z)
    {
        //x - 0, 128, -128... : z - 0, 128, -128...
        SetSeeds();
        if (!LoadData(x, z))
        {
            GenerateBioms(x, z);
            GenerateHeight(x, z);
            _SaveData(x, z);
        }
        position = new Vector2(x, z);
    }
    private bool LoadData(int x, int z)
    {
        string name = x.ToString() + "_" + z.ToString();
        //string dest = Application.persistentDataPath + "_" + "save_" + name + ".dat";
        string dest = Application.persistentDataPath + "/" + (int)VoxelEngine.WorldSeed + "/" + "region_" + name + ".dat";
        if (File.Exists(dest))
        {
            var br = new BinaryReader(File.OpenRead(dest));
            Cubes = br.ReadBytes(2097152);
            br.Close();
            return true;
        }
        else
            return false;
    }
    private void _SaveData(int x, int z)
    {
        string name = x.ToString() + "_" + z.ToString();
        //string dest = Application.persistentDataPath + "_" + "save_" + name + ".dat";
        string dest = Application.persistentDataPath + "/" + (int)VoxelEngine.WorldSeed + "/" + "region_" + name + ".dat";
        string dir = Application.persistentDataPath + "/" + (int)VoxelEngine.WorldSeed;
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        var bw = new BinaryWriter(File.Open(dest, FileMode.OpenOrCreate));
        bw.Write(Cubes);
        bw.Flush();
        bw.Close();
    }
    public void SaveData()
    {
        string name = position.x.ToString() + "_" + position.y.ToString();
        string dest = Application.persistentDataPath + "/" + (int)VoxelEngine.WorldSeed + "/" + "region_" + name + ".dat";
        string dir = Application.persistentDataPath + "/" + (int)VoxelEngine.WorldSeed;
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        var bw = new BinaryWriter(File.Open(dest, FileMode.OpenOrCreate));
        bw.Write(Cubes);
        bw.Flush();
        bw.Close();
    }
    public Vector2 GetPosition()
    {
        return position;
    }
    public byte this[int x, int y, int z]
    {
        //x - 2 080 768   y -     z - 127
        get { return Cubes[x * 128 * 128 + y * 128 + z]; }
        //x 2 080 768 + y 16256 z + 127 = 2 097 151
        set { Cubes[x * 128 * 128 + y * 128 + z] = value; }
    }
    public byte this[int x, int z]
    {
        get { return CubesBioms[x * heightmapSize + z]; }
        set { CubesBioms[x * heightmapSize + z] = value; }
    }

    public byte[] ReturnCubes(int posX, int posZ)
    {
        byte[] p = new byte[128 * 16 * 16];
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                for (int y = 0; y < chunkHeight; y++)
                {
                    p[x * 128 * 16 + y * 16 + z] = this[x + posX, y, z + posZ];
                }
            }
        }
        return p;
    }
    private void GenerateBioms(int _x, int _z)
    {
        int posX = _x / 128;
        int posZ = _z / 128;
        for (int x = 0; x < heightmapSize; x++)
        {
            for (int z = 0; z < heightmapSize; z++)
            {
                double x1 = x / (double)heightmapSize - 1;
                double z1 = z / (double)heightmapSize - 1;
                this[x, z] = CalcBiom(x1 + posX, z1 + posZ);
            }
        }
    }
    private byte CalcBiom(double x, double z)
    {
        double value = BiomNoise.Get(x, z);
        byte biom = (byte)((byte)(Remap((float)value, -1, 1, 1, 4) % 4) + 1);
        return biom;
    }
    private void GenerateHeight(int _x, int _z)
    {
        /*
         Grass id:1     Dirt id:2    Stone id:3    Sand id:4   
         WoodenLog id:5    Leaves id:6      Water id:7
        */
        //_x - 0, 128, -128... : _z - 0, 128, -128...
        double posX = _x / 128;
        double posZ = _z / 128;
        for (int x = 0; x < heightmapSize; x++)
        {
            for (int z = 0; z < heightmapSize; z++)
            {
                double x1 = x / (double)heightmapSize - 1;
                double z1 = z / (double)heightmapSize - 1;
                byte biom = this[x, z];
                double value = (HeightNoise.Get(x1 + posX, z1 + posZ) + BaseHNoise.Get(x1 + posX, z1 + posZ)) / 2f;
                /*double value = HeightNoise.Get(x1 + posX, z1 + posZ) + BaseHNoise.Get(x1 + posX, z1 + posZ);
                switch (biom)
                {
                    //biom mountains
                    case 1:
                        value += HillsNoise.Get(x1 + posX, z1 + posZ);
                        break;
                    //biom grassy lands
                    case 2:
                        value += GrassLandsNoise.Get(x1 + posX, z1 + posZ);
                        break;
                    //biom desert
                    case 3:
                        value += DesertNoise.Get(x1 + posX, z1 + posZ);
                        break;
                    //biom dirt
                    case 4:
                        value += WaterNoise.Get(x1 + posX, z1 + posZ);
                        break;
                    //biom grass
                    default:
                        value += GrassLandsNoise.Get(x1 + posX, z1 + posZ);
                        break;
                }
                value /= 3f;*/
                value = Remap((float)value, -1, 1, 0, 0.98f);
                value *= 128;
                int stone = (int)value + 15;
                int ground = 3;

                for (int y = 0; y < chunkHeight; y++)
                {
                    if (y < stone)
                    {
                        this[x, y, z] = 3;
                    }
                    else
                    {
                        if (y > stone + ground + 1 && y <= 35)
                        {
                            this[x, y, z] = 7;
                        }
                        else
                        {
                            if (y <= ground + stone)
                            {
                                /*
                                 Grass id:1     Dirt id:2    Stone id:3    Sand id:4   
                                 WoodenLog id:5    Leaves id:6      Water id:7
                                */
                                if (y <= 36)
                                    this[x, y, z] = 4;
                                else if (y <= 128 && y > 36)
                                {
                                    switch (biom)
                                    {
                                        case 3:
                                            this[x, y, z] = 4;
                                            break;
                                        default:
                                            this[x, y, z] = 2;
                                            break;
                                    }
                                }
                                /*else if (y <= 128 && y > 100)
                                    this[x, y, z] = 3;*/
                            }
                            else if (y <= ground + stone + 1)
                            {
                                if (y <= 36)
                                    this[x, y, z] = 4;
                                else if (y <= 128 && y > 36)
                                {
                                    switch (biom)
                                    {
                                        //biom hills
                                        case 1:
                                            this[x, y, z] = 3;
                                            break;
                                        //biom grass
                                        case 2:
                                            this[x, y, z] = 1;
                                            break;
                                        //biom desert
                                        case 3:
                                            this[x, y, z] = 4;
                                            break;
                                        //biom dirt
                                        case 4:
                                            this[x, y, z] = 2;
                                            break;
                                        //biom grass
                                        default:
                                            this[x, y, z] = 1;
                                            break;
                                    }
                                }
                                /* else if (y <= 128 && y > 100)
                                     this[x, y, z] = 1;*/
                            }
                            else if (biom == 2 && y > 38 && y < 90 && y <= ground + stone + 2)
                            {
                                if (CanSpawnTree(x, y, z, x1 + posX, z1 + posZ))
                                {
                                    SpawnTree(x, y, z);
                                }
                            }
                        }
                    }
                    /*if (y > 10 && y < 75 && y <= ground + stone + 2)
                    {
                        GenerateCaves(x, y, z, (int)(x1 + posX), (int)(z1 + posZ));
                    }*/
                }
            }
        }
    }
    private float Remap(float value, float sourceMin, float sourceMax, float destMin, float destMax)
    {
        return destMin + ((value - sourceMin) / (sourceMax - sourceMin)) * (destMax - destMin);
    }
    private void GenerateCaves(int x, int y, int z, int posX, int posZ)
    {
        /*double value = CaveNoise.Get(x1, y, z1) * 128;
        double value2 = Noise.Perlin3DNoise(x + (int)x1, y, z + (int)z1, 8, 9, 1.85f);
        double caveNoise = value + value2;
        caveNoise /= 2f;

        if (caveNoise < 5f)
            this[x, y, z] = 0;*/
        //int cave1 = (int)CaveNoise.Get(x + posX, y, z + posZ) * 128;
        int cave1 = Noise.PerlinNoise(x + posX, y, z + posZ, 15f, 13f, 1.3f);
        int cave2 = Noise.Perlin3DNoise(x + posX, y, z + posZ, 12f, 10f, 1.25f);
        float caveNoise = (cave1 + cave2) / 2f;
        if (caveNoise < 3.5f)
        {
            this[x, y, z] = 0;
        }
    }
    private bool CanSpawnTree(int x, int y, int z, double x1, double z1)
    {
        if (x < 126 && x >= 2 && z < 126 && z >= 2 && this[x, y - 1, z] != 0)
        {
            for (int i = 0; i <= 4; i++)
            {
                if (this[x - 2, y + i, z - 2] != 0 || this[x + 2, y + i, z + 2] != 0 || this[x + 2, y + i, z - 2] != 0 || this[x - 2, y + i, z + 2] != 0)
                {
                    return false;
                }
            }
            double value = (TreeNoise.Get(x1, z1));
            value = Remap((float)value, -1, 1, 0, 1f);
            if (value > 0.6f)
                return true;
            else
                return false;
        }
        else
        {
            return false;
        }
    }
    private void SpawnTree(int x, int y, int z)
    {
        int numberOfLogs;
        int value = (x + y + z + (int)VoxelEngine.WorldSeed) % 2;
        switch (value)
        {
            case 0:
                numberOfLogs = 5;
                break;
            case 1:
                numberOfLogs = 4;
                break;
            default:
                numberOfLogs = 5;
                break;
        }
        for (int i = 0; i < numberOfLogs; i++)
        {
            if (numberOfLogs == 4)
            {
                if (i >= 2)
                {
                    for (int m = -2; m <= 2; m++)
                    {
                        for (int n = -2; n <= 2; n++)
                        {
                            this[x + m, y + i, z + n] = 6;
                        }
                    }
                }
                this[x, y + i, z] = 5;
            }
            else
            {
                if (i > 2)
                {
                    for (int m = -2; m <= 2; m++)
                    {
                        for (int n = -2; n <= 2; n++)
                        {
                            this[x + m, y + i, z + n] = 6;
                        }
                    }
                }
                this[x, y + i, z] = 5;
            }

        }
        this[x + 1, y + numberOfLogs, z] = 6;
        this[x + 1, y + numberOfLogs, z + 1] = 6;
        this[x + 1, y + numberOfLogs, z - 1] = 6;
        this[x, y + numberOfLogs, z + 1] = 6;
        this[x, y + numberOfLogs, z - 1] = 6;
        this[x - 1, y + numberOfLogs, z] = 6;
        this[x - 1, y + numberOfLogs, z + 1] = 6;
        this[x - 1, y + numberOfLogs, z - 1] = 6;
        this[x, y + numberOfLogs, z] = 6;
        this[x, y + numberOfLogs + 1, z] = 6;
        this[x, y + numberOfLogs + 1, z - 1] = 6;
        this[x - 1, y + numberOfLogs + 1, z] = 6;
        this[x, y + numberOfLogs + 1, z + 1] = 6;
        this[x + 1, y + numberOfLogs + 1, z] = 6;
    }
    private void SetSeeds()
    {
        HeightNoise.Seed = (int)VoxelEngine.WorldSeed;
        BaseHNoise.Seed = (int)VoxelEngine.WorldSeed;
        TreeNoise.Seed = (int)VoxelEngine.WorldSeed;
        CaveNoise.Seed = (int)VoxelEngine.WorldSeed;
        BiomNoise.Seed = (int)VoxelEngine.WorldSeed;
        GrassLandsNoise.Seed = (int)VoxelEngine.WorldSeed;
        DesertNoise.Seed = (int)VoxelEngine.WorldSeed;
        HillsNoise.Seed = (int)VoxelEngine.WorldSeed;
        WaterNoise.Seed = (int)VoxelEngine.WorldSeed;
    }
}
