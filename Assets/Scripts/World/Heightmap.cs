using UnityEngine;
using TinkerWorX.AccidentalNoiseLibrary;
using System.IO;
using System.IO.Compression;
using System;

public class Heightmap : MultiThreading.ThreadedJob
{
    private static readonly ImplicitFractal HeightNoise = new ImplicitFractal(FractalType.RidgedMulti, BasisType.Simplex, InterpolationType.Quintic)
    { Octaves = 4, Frequency = 0.2 };
    private static readonly ImplicitFractal BaseHNoise = new ImplicitFractal(FractalType.HybridMulti, BasisType.GradientValue, InterpolationType.Linear)
    { Octaves = 2, Frequency = 0.3 };

    private static readonly ImplicitFractal TreeNoise = new ImplicitFractal(FractalType.RidgedMulti, BasisType.GradientValue, InterpolationType.Linear);

    private static readonly ImplicitFractal BiomNoise = new ImplicitFractal(FractalType.Billow, BasisType.Gradient, InterpolationType.Cubic)
    { Octaves = 3, Frequency = 0.25 };

    private static readonly int chunkWidth = 16;
    private static readonly int heightmapSize = 128;
    private static readonly int chunkHeight = 128;

    private static readonly float scale1 = 10f;
    private static readonly float scale2 = 15f;
    private static readonly float height1 = 45f;
    private static readonly float height2 = 20f;
    private static readonly float power1 = 1.06f;
    private static readonly float power2 = 1.1f;

    private readonly float threshold1;
    private readonly float threshold2;
    private readonly float threshold;
    private readonly int ground = 3;
    private int seed;

    private Vector2 position;
    //Cubes - Max 2 097 152
    public byte[] Cubes = new byte[chunkHeight * chunkWidth * chunkWidth * 64];

    //CubesBioms - Max 16384
    public byte[] CubesBioms = new byte[chunkWidth * chunkWidth * 64];

    public Heightmap(int x, int z, int seed)
    {
        threshold1 = (Mathf.Pow(height1, power1) * 0.55f);
        threshold2 = (Mathf.Pow(height2, power2) * 0.52f);
        threshold = (threshold1 + threshold2) / 2f;
        //x - 0, 128, -128... : z - 0, 128, -128...
        this.seed = seed;
        position = new Vector2(x, z);
        SetSeeds();
        if (!LoadData((int)position.x, (int)position.y))
            Start();
        else
            IsDone = true;
    }
    private bool LoadData(int x, int z)
    {
        string name = x.ToString() + "_" + z.ToString();
        string dest = Application.persistentDataPath + "/" + seed + "/" + "region_" + name + ".dat";
        if (File.Exists(dest))
        {
            FileStream fs = new FileStream(dest, FileMode.Open);
            GZipStream dcmp = new GZipStream(fs, CompressionMode.Decompress);
            var br = new BinaryReader(dcmp);
            Cubes = br.ReadBytes(2097152);
            CubesBioms = br.ReadBytes(16384);

            //Buffer.BlockCopy(packetBytes, readPosition, shortSamples, 0, payloadLength);

            br.Close();
            fs.Close();
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
        FileStream fs = new FileStream(dest, FileMode.OpenOrCreate);
        GZipStream cmp = new GZipStream(fs, CompressionMode.Compress);
        var bw = new BinaryWriter(cmp);

        bw.Write(Cubes);
        bw.Write(CubesBioms);

        //Buffer.BlockCopy(shortSamples, 0, packetBytes, 0, shortSamples.Length * sizeof(short)). 

        bw.Flush();
        bw.Close();
        fs.Close();
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
        for (int x = 0; x < chunkWidth; x++)
        {
            for (int z = 0; z < chunkWidth; z++)
            {
                for (int y = 0; y < chunkHeight; y++)
                {
                    p[x * 128 * 16 + y * 16 + z] = this[x + posX, y, z + posZ];
                }
            }
        }
        return p;
    }
    private void GenerateBioms()
    {
        double posX = position.x / 128;
        double posZ = position.y / 128;
        for (int x = 0; x < heightmapSize; x++)
        {
            for (int z = 0; z < heightmapSize; z++)
            {
                double x1 = x / (double)heightmapSize - 1;
                double z1 = z / (double)heightmapSize - 1;
                byte biom = CalcBiom(x1 + posX, z1 + posZ);
                this[x, z] = biom;
            }
        }

    }
    private byte CalcBiom(double x, double z)
    {
        float _value = (float)BiomNoise.Get(x, z);
        byte biom = (byte)((byte)(Remap(_value, -1, 1, 1, 4) % 4) + 1);
        //Debug.Log("Value: " + _value + "biom: " +biom);
        return biom;
    }
    protected override void ThreadFunction()
    {
        GenerateBioms();
        GenerateHeight();
    }
    private void GenerateHeight()
    {
        double posX = position.x / 128;
        double posZ = position.y / 128;
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
                _max = Mathf.Min(_max, chunkHeight);
                for (int y = 0; y < _max; y++)
                {
                    if (y <= stone)
                    {
                        /*int stoneNoise = Noise.PerlinNoise((int)(x + posX), y, (int)(z + posZ), 5, 12, 1.2f);
                        if (stoneNoise >= 15 && stoneNoise < 17)
                            this[x, y, z] = 14;
                        else if (stoneNoise >= 17)
                            this[x, y, z] = 15;
                        else*/
                        this[x, y, z] = (byte)Blocks.Stone;
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
                                else if (y <= _max && y > 36)
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
                                else if (y <= _max && y > 36)
                                {
                                    switch (biom)
                                    {
                                        //biom rocky hills
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
                            /*
                            else if (y <= ground + stone + 2 && biom == 2 && y > 38 && y < 90)
                            {
                                if (CanSpawnTree(x, y, z, x1 + posX, z1 + posZ, out double _value))
                                {
                                    SpawnTree(x, y, z, _value);
                                }
                            }*/
                        }
                    }
                    /*
                    if (y <= ground + stone + 1 && y > 3)
                    {
                        GenerateCaves(x, y, z, posX, posZ, biom, _max);
                    }*/
                }
            }
        }

    }
    private void GenerateCaves(int x, int y, int z, double posX, double posZ, int biom, int max)
    {
        if (this[x, y, z] != 7 || this[x, y + 1, z] != 7)
        {
            float weight = (max - y) / max;
            int cave1 = Noise.PerlinNoise(x + (int)(posX * 128), y, z + (int)(posZ * 128), scale1, height1, power1 * weight);
            int cave2 = Noise.Perlin3DNoise(x + (int)(posX * 128), y, z + (int)(posZ * 128), scale2, height2, power2 * weight);
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
        if (this[x, y - 1, z] != 0 && x < 124 && x >= 4 && z < 124 && z >= 4)
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
            //Debug.Log(value);
            float remap = Remap((float)value, -1, 1, 0f, 1f);
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
}
