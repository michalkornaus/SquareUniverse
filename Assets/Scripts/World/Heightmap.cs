using UnityEngine;
using TinkerWorX.AccidentalNoiseLibrary;
using System.IO;
using System.Collections.Generic;

public class Heightmap
{
    private static readonly ImplicitFractal HeightNoise = new ImplicitFractal(FractalType.RidgedMulti, BasisType.Simplex, InterpolationType.Quintic)
    { Octaves = 4, Frequency = 0.2 };
    private static readonly ImplicitFractal BaseHNoise = new ImplicitFractal(FractalType.HybridMulti, BasisType.GradientValue, InterpolationType.Linear)
    { Octaves = 2, Frequency = 0.3 };

    private static readonly ImplicitFractal TreeNoise = new ImplicitFractal(FractalType.RidgedMulti, BasisType.GradientValue, InterpolationType.Linear);

    private static readonly ImplicitFractal BiomNoise = new ImplicitFractal(FractalType.FractionalBrownianMotion, BasisType.GradientValue, InterpolationType.Cubic)
    { Octaves = 8, Frequency = 0.25 };

    private static readonly int chunkSize = 16;
    private static readonly int heightmapSize = chunkSize * 8;
    private static readonly int chunkHeight = chunkSize * 8;

    private static readonly float scale1 = 8f;
    private static readonly float scale2 = 10f;
    private static readonly float height1 = 60f;
    private static readonly float height2 = 70f;
    private static readonly float power1 = 1.05f;
    private static readonly float power2 = 1.15f;

    private readonly float threshold1;
    private readonly float threshold2;
    private readonly float threshold;
    private readonly int ground = 3;
    private int seed;

    /*private int min1 = 1000;
    private int max1 = -1000;
    private int min2 = 1000;
    private int max2 = -1000;*/

    private Vector2 position;

    //Cubes - Max 2 097 152
    public byte[] Cubes = new byte[128 * 16 * 16 * 64];
    public List<int> _cubes = new List<int>();

    //CubesBioms - Max 16384
    public byte[] CubesBioms = new byte[16 * 16 * 64];
    public List<short> _cubesBioms = new List<short>();

    public Heightmap(int x, int z)
    {
        threshold1 = (Mathf.Pow(height1, power1) * 0.55f) - scale1;
        threshold2 = (Mathf.Pow(height2, power2) * 0.55f) + scale2;
        threshold = (threshold1 + threshold2) / 2f;
        //x - 0, 128, -128... : z - 0, 128, -128...
        seed = VoxelEngine.WorldSeed;
        position = new Vector2(x, z);
        SetSeeds();
        if (!LoadData(x, z))
        {
            GenerateBioms(x, z);
            //_GenerateHeight(x, z);
            GenerateHeight(x, z);
            SaveData();
        }
    }
    private bool LoadData(int x, int z)
    {
        string name = x.ToString() + "_" + z.ToString();
        string dest = Application.persistentDataPath + "/" + seed + "/" + "region_" + name + ".dat";
        if (File.Exists(dest))
        {
            var br = new BinaryReader(File.OpenRead(dest));
            /*int size = br.ReadInt32();
            int index = 0;
            for (int i = 0; i < (size / 2); i++)
            {
                byte block = (byte)br.ReadInt32();
                _cubes.Add(block);
                int amount = br.ReadInt32();
                _cubes.Add(amount);
                for (int j = index; j < (amount + index); j++)
                {
                    Cubes[j] = block;
                }
                index += amount;
            }*/
            Cubes = br.ReadBytes(2097152);
            /*size = br.ReadInt32();
            index = 0;
            for (int i = 0; i < (size / 2); i++)
            {
                byte biom = (byte)br.ReadInt16();
                _cubesBioms.Add(biom);
                short amount = br.ReadInt16();
                _cubesBioms.Add(amount);
                for (int j = index; j < (amount + index); j++)
                {
                    CubesBioms[j] = biom;
                }
                index += amount;
            }*/
            CubesBioms = br.ReadBytes(16384);
            br.Close();
            return true;
        }
        else
            return false;
    }
    public void SaveData()
    {
        string name = position.x.ToString() + "_" + position.y.ToString();
        string dest = Application.persistentDataPath + "/" + seed + "/" + "region_" + name + ".dat";
        string dir = Application.persistentDataPath + "/" + seed;
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        var bw = new BinaryWriter(File.Open(dest, FileMode.OpenOrCreate));
        bw.Write(Cubes);
        /*bw.Write(_cubes.Count);
        foreach (int item in _cubes)
        {
            bw.Write(item);
        }*/
        bw.Write(CubesBioms);
        /*bw.Write(_cubesBioms.Count);
        foreach (short item in _cubesBioms)
        {
            bw.Write(item);
        }*/
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
        //short amount = 0;
        //byte prevBiom = 0;
        for (int x = 0; x < heightmapSize; x++)
        {
            for (int z = 0; z < heightmapSize; z++)
            {
                double x1 = x / (double)heightmapSize - 1;
                double z1 = z / (double)heightmapSize - 1;
                byte biom = CalcBiom(x1 + posX, z1 + posZ);
                this[x, z] = biom;
                /*
                if (amount > 0)
                {
                    if (prevBiom == biom)
                    {
                        amount++;
                    }
                    else
                    {
                        _cubesBioms.Add(amount);
                        _cubesBioms.Add(biom);
                        prevBiom = biom;
                        amount = 1;
                    }
                }
                else
                {
                    _cubesBioms.Add(biom);
                    prevBiom = biom;
                    amount++;
                }*/

            }
        }
        //if (_cubesBioms.Count == 1)
        //   _cubesBioms.Add(amount);
    }
    private byte CalcBiom(double x, double z)
    {
        double value = BiomNoise.Get(x, z);
        byte biom = (byte)((byte)(Remap((float)value, -1, 1, 1, 4) % 4) + 1);
        return biom;
    }

    private void GenerateHeight(int _x, int _z)
    {
        double posX = _x / 128;
        double posZ = _z / 128;
        //int amount = 0;
        //byte prevBlock = 0;
        for (int x = 0; x < heightmapSize; x++)
        {
            for (int z = 0; z < heightmapSize; z++)
            {
                double x1 = x / (double)heightmapSize - 1;
                double z1 = z / (double)heightmapSize - 1;
                byte biom = this[x, z];
                double value = (HeightNoise.Get(x1 + posX, z1 + posZ) + BaseHNoise.Get(x1 + posX, z1 + posZ)) / 2f;
                value = Remap((float)value, -1, 1, 0, 0.98f);
                value *= 128;
                int stone = (int)value + 15;
                int _max = Mathf.Max(stone + ground + 3, 36);
                for (int y = 0; y < _max; y++)
                {
                    if (y <= stone)
                    {
                        int stoneNoise = Noise.PerlinNoise((int)(x + posX), y, (int)(z + posZ), 5, 12, 1.2f);
                        if (stoneNoise >= 15 && stoneNoise < 17)
                            this[x, y, z] = 14;
                        else if (stoneNoise >= 17)
                            this[x, y, z] = 15;
                        else
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
                            }
                            else if (biom == 2 && y > 38 && y < 90 && y <= ground + stone + 2)
                            {
                                if (CanSpawnTree(x, y, z, x1 + posX, z1 + posZ, out double _value))
                                {
                                    SpawnTree(x, y, z, _value);
                                }
                            }
                        }
                    }
                    if (y > 5 && y < 80 && y <= ground + stone + 1)
                    {
                        GenerateCaves(x, y, z, (int)(x1 + posX), (int)(z1 + posZ), biom);
                    }
                    /*byte block = this[x, y, z];
                    if (amount > 0)
                    {
                        if (prevBlock == block)
                        {
                            amount++;
                        }
                        else
                        {
                            _cubes.Add(amount);
                            _cubes.Add(block);
                            prevBlock = block;
                            amount = 1;
                        }
                    }
                    else
                    {
                        _cubes.Add(block);
                        prevBlock = block;
                        amount++;
                    }*/
                }
            }
        }
        //Debug.Log("threshold1: " + threshold1 + ", threshold2: " + threshold2);
        //Debug.Log("min cave1: " + min1 + ", max cave1: " + max1);
        //Debug.Log("min cave2: " + min2 + ", max cave2: " + max2);
    }
    private void GenerateCaves(int x, int y, int z, int posX, int posZ, int biom)
    {
        if (this[x, y, z] != 7 || this[x, y + 1, z] != 7)
        {
            int cave1 = Noise.PerlinNoise(x + posX, y, z + posZ, scale1, height1, power1);
            int cave2 = Noise.Perlin3DNoise(x + posX, y, z + posZ, scale2, height2, power2);
            float caveNoise = (cave1 + cave2) / 2f;
            if (caveNoise > threshold)
            {
                this[x, y, z] = 0;
                if (biom == 2 && this[x, y - 1, z] == 2 && this[x, y + 2, z] == 0)
                {
                    this[x, y - 1, z] = 1;
                }
            }
        }
    }

    private bool CanSpawnTree(int x, int y, int z, double x1, double z1, out double value)
    {
        if (x < 124 && x >= 4 && z < 124 && z >= 4 && this[x, y - 1, z] != 0)
        {
            for (int i = 1; i <= 10; i += 3)
            {
                if (!CheckPositionNeg(x, y + i, z, 4, 6) || !CheckPositionCrossNeg(x, y + i, z, 3, 6) || !CheckPosition(x, y + i, z, 1, 0) || !CheckPositionCross(x, y + i, z, 2, 0))
                {
                    value = 0.0;
                    return false;
                }
            }
            value = TreeNoise.Get(x1, z1);
            float remap = Remap((float)value, -1, 1, 0, 1f);
            if (remap > 0.7f)
                return true;
            else
                return false;
        }
        else
        {
            value = 0.0;
            return false;
        }
    }
    private void SpawnTree(int x, int y, int z, double value)
    {
        int numberOfLogs = 4;
        numberOfLogs += (int)Remap((float)value, 0.20f, 0.70f, 0f, 4f);
        for (int i = 0; i < numberOfLogs; i++)
        {
            this[x, y + i, z] = 5;
            if (i > numberOfLogs - 3)
            {
                for (int m = -2; m <= 2; m++)
                {
                    for (int n = -2; n <= 2; n++)
                    {
                        if ((m == 0 && n == 0) || Mathf.Abs(m) == 2 && Mathf.Abs(n) == 2)
                            continue;
                        this[x + m, y + i, z + n] = 6;
                    }
                }
            }
        }
        if (numberOfLogs >= 5)
        {
            for (int m = -1; m <= 1; m++)
            {
                for (int n = -1; n <= 1; n++)
                {
                    if (m == 0 && n == 0)
                        continue;
                    this[x + m, y + numberOfLogs - 3, z + n] = 6;
                }
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
        HeightNoise.Seed = seed;
        BaseHNoise.Seed = seed;
        TreeNoise.Seed = seed;
        BiomNoise.Seed = seed;
    }
    private bool CheckPosition(int x, int y, int z, int offset, int block)
    {
        if (this[x - offset, y, z - offset] == block && this[x + offset, y, z + offset] == block && this[x + offset, y, z - offset] == block && this[x - offset, y, z + offset] == block)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    private bool CheckPositionCross(int x, int y, int z, int offset, int block)
    {
        if (this[x, y, z - offset] == block && this[x, y, z + offset] == block && this[x + offset, y, z] == block && this[x - offset, y, z] == block)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    private bool CheckPositionNeg(int x, int y, int z, int offset, int block)
    {
        if (this[x - offset, y, z - offset] != block && this[x + offset, y, z + offset] != block && this[x + offset, y, z - offset] != block && this[x - offset, y, z + offset] != block)
        {
            return true;
        }
        else
        {
            return false;
        }

    }
    private bool CheckPositionCrossNeg(int x, int y, int z, int offset, int block)
    {
        if (this[x, y, z - offset] != block && this[x, y, z + offset] != block && this[x + offset, y, z] != block && this[x - offset, y, z] != block)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    private static float Remap(float value, float sourceMin, float sourceMax, float destMin, float destMax)
    {
        return destMin + ((value - sourceMin) / (sourceMax - sourceMin)) * (destMax - destMin);
    }
    private void _GenerateHeight(int _x, int _z)
    {
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
                value = Remap((float)value, -1, 1, 0, 0.98f);
                value *= 128;
                int stone = (int)value + 15;
                int _max = Mathf.Max(stone + ground + 1, 36);
                for (int y = 0; y <= _max; y++)
                {
                    GenerateBlock(x, y, z, (int)(x1 + posX), (int)(z1 + posZ), biom, stone);
                }
            }
        }
    }
    private void GenerateBlock(int x, int y, int z, int posX, int posZ, byte biom, int stone)
    {
        int cave1 = Noise.PerlinNoise(x + posX, y, z + posZ, scale1, height1, power1);
        int cave2 = Noise.Perlin3DNoise(x + posX, y, z + posZ, scale2, height2, power2);
        float caveNoise = (cave1 + cave2) / 2f;
        // < - everything except caves
        if (caveNoise <= threshold)
        {
            if (y < stone)
            {
                this[x, y, z] = 3;
            }
            else
            {
                if (y > (stone + ground + 1) && y <= 35)
                {
                    this[x, y, z] = 7;
                }
                else
                {
                    if (y <= ground + stone)
                    {
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
                    }
                    else if (biom == 2 && y > 38 && y < 90 && y <= ground + stone + 1)
                    {
                        double _value;
                        if (CanSpawnTree(x, y, z, posX, posZ, out _value))
                        {
                            SpawnTree(x, y, z, _value);
                        }
                    }
                }
            }
        }
    }
}
