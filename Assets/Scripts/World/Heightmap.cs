using UnityEngine;
using TinkerWorX.AccidentalNoiseLibrary;
using System.IO;
using System.IO.Compression;
using System;
public class Heightmap : MultiThreading.ThreadedJob
{
    private static readonly ImplicitFractal OceanNoise = new(FractalType.FractionalBrownianMotion, BasisType.Gradient, InterpolationType.Linear)
    { Octaves = 4, Frequency = 0.7 };

    private static readonly ImplicitFractal GrassFieldsNoise = new(FractalType.HybridMulti, BasisType.Gradient, InterpolationType.Quintic)
    { Octaves = 3, Frequency = 0.5 };

    private static readonly ImplicitFractal GrassHillsNoise = new(FractalType.RidgedMulti, BasisType.Gradient, InterpolationType.Linear)
    { Octaves = 4, Frequency = 1 };

    private static readonly ImplicitFractal TreeNoise = new(FractalType.FractionalBrownianMotion, BasisType.Gradient, InterpolationType.None)
    { Octaves = 2, Frequency = 7 };

    private static readonly ImplicitFractal BushNoise = new(FractalType.RidgedMulti, BasisType.Simplex, InterpolationType.Linear)
    { Frequency = 6, Octaves = 2 };

    private static readonly ImplicitFractal BiomNoise = new(FractalType.HybridMulti, BasisType.Gradient, InterpolationType.Linear)
    { Octaves = 10, Frequency = 0.25 };

    private static readonly int chunkWidth = 16;
    private static readonly int chunkCount = 64;
    private static readonly int heightmapSize = 128;
    private static readonly int chunkHeight = 256;

    private static readonly int waterLevel = 60;

    private static readonly float scale1 = 20f;
    private static readonly float scale2 = 80f;
    private static readonly float height1 = 45f;
    private static readonly float height2 = 20f;
    private static readonly float power1 = 1.06f;
    private static readonly float power2 = 1.1f;

    private readonly float threshold1;
    private readonly float threshold2;
    private readonly float threshold;
    private readonly int ground = 3;
    private string worldName;
    private int worldSeed;
    private Vector2 position = Vector3.zero;

    public Vector3 spawnPosition;

    public bool saveable = false;

    //FOR 128 HEIGHT Cubes - Max 2 097 152 cubes (one cube = 2 bytes) / size of 4 194 304 bytes
    public ushort[] Cubes = new ushort[chunkHeight * chunkWidth * chunkWidth * chunkCount];
    //FOR 128 HEIGHT _cubes - cubes / size of 4 194 304 bytes
    private byte[] _cubes = new byte[chunkHeight * chunkWidth * chunkWidth * chunkCount * sizeof(ushort)];
    private static readonly int cubesSize = chunkHeight * chunkWidth * chunkWidth * chunkCount;
    private int length;
    //CubesBioms - Max 16384
    public byte[] CubesBioms = new byte[chunkWidth * chunkWidth * chunkCount];
    private static readonly int biomsSize = chunkCount * chunkWidth * chunkWidth;

    public Heightmap(int x, int z, int worldSeed, string worldName)
    {
        threshold1 = Mathf.Pow(height1, power1) * 0.55f;
        threshold2 = Mathf.Pow(height2, power2) * 0.52f;
        threshold = (threshold1 + threshold2) / 2f;
        this.worldName = worldName;
        this.worldSeed = worldSeed;
        length = cubesSize * sizeof(ushort);
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
        string dest = Application.persistentDataPath + "/" + worldName + "/" + "worldData" + "/" + "region_" + name + ".dat";
        if (File.Exists(dest))
        {
            FileStream fs = new FileStream(dest, FileMode.Open);
            GZipStream dcmp = new GZipStream(fs, CompressionMode.Decompress);
            var br = new BinaryReader(dcmp);

            _cubes = br.ReadBytes(length);
            Buffer.BlockCopy(_cubes, 0, Cubes, 0, length);

            CubesBioms = br.ReadBytes(biomsSize);

            br.Close();
            fs.Close();
            return true;
        }
        else
            return false;
    }
    public void SaveData()
    {
        string name = ((int)position.x).ToString() + "_" + ((int)position.y).ToString();
        string dest = Application.persistentDataPath + "/" + worldName + "/" + "worldData" + "/" + "region_" + name + ".dat";
        string dir = Application.persistentDataPath + "/" + worldName + "/" + "worldData";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        FileStream fs = new FileStream(dest, FileMode.OpenOrCreate);
        GZipStream cmp = new GZipStream(fs, CompressionMode.Compress);
        var bw = new BinaryWriter(cmp);

        Buffer.BlockCopy(Cubes, 0, _cubes, 0, length);
        bw.Write(_cubes);

        bw.Write(CubesBioms);

        bw.Flush();

        bw.Close();
        fs.Close();
    }
    public ushort this[int x, int y, int z]
    {
        //FOR 128 HEIGHT x - 2 080 768   y -     z - 127
        get { return Cubes[x * chunkHeight * heightmapSize + y * heightmapSize + z]; }
        //FOR 128 HEIGHT x 2 080 768 + y 16256 z + 127 = 2 097 151
        set { Cubes[x * chunkHeight * heightmapSize + y * heightmapSize + z] = value; }
    }
    public byte this[int x, int z]
    {
        get { return CubesBioms[x * heightmapSize + z]; }
        set { CubesBioms[x * heightmapSize + z] = value; }
    }

    public ushort[] ReturnCubes(int posX, int posZ)
    {
        ushort[] p = new ushort[chunkHeight * chunkWidth * chunkWidth];
        for (int x = 0; x < chunkWidth; x++)
        {
            for (int z = 0; z < chunkWidth; z++)
            {
                for (int y = 0; y < chunkHeight; y++)
                {
                    p[x * chunkHeight * chunkWidth + y * chunkWidth + z] = this[x + posX, y, z + posZ];
                }
            }
        }
        return p;
    }
    private double GetHeight(double x, double z, int x_biom, int z_biom)
    {
        //LOCATION - HEIGHT? IMPACT - HOW MUCH BIOM DISTRIBIUTION?
        double dOcean_location = 0.15, dOcean_impact = 0.35, dOcean = 0;
        double dGrassFields_location = 0.30, dGrassFields_impact = 0.4, dGrassFields = 0;
        double dGrassHills_location = 0.40, dGrassHills_impact = 0.25, dGrassHills = 0;

        double dMask = Remap((float)BiomNoise.Get(x, z), -1, 1, 0f, 1f);
        double dOcean_strength = (dOcean_impact - Math.Abs(dMask - dOcean_location)) / dOcean_impact;
        double dGrassFields_strength = (dGrassFields_impact - Math.Abs(dMask - dGrassFields_location)) / dGrassFields_impact;
        double dGrassHills_strength = (dGrassHills_impact - Math.Abs(dMask - dGrassHills_location)) / dGrassHills_impact;

        double maxStrength = -1;
        byte maxStrength_biom = 0;

        //Calculating max strength
        if (dOcean_strength > maxStrength)
        {
            maxStrength = dOcean_strength;
            maxStrength_biom = 1;
        }
        if (dGrassFields_strength > maxStrength)
        {
            maxStrength = dGrassFields_strength;
            maxStrength_biom = 2;
        }
        if (dGrassHills_strength > maxStrength)
        {
            maxStrength_biom = 3;
        }

        //Calcuting heights 
        if (dOcean_strength > 0)
            dOcean = dOcean_strength * (Remap((float)OceanNoise.Get(x, z), -1, 1, 0, 1) * 60 - 50);
        if (dGrassFields_strength > 0)
            dGrassFields = dGrassFields_strength * (Remap((float)GrassFieldsNoise.Get(x, z), -1, 1, 0, 1) * 80 + 30);
        if (dGrassHills_strength > 0)
            dGrassHills = dGrassHills_strength * (Remap((float)GrassHillsNoise.Get(x, z), -1, 1, 0, 1) * 75 + 35);

        this[x_biom, z_biom] = maxStrength_biom;
        return (dOcean + dGrassFields + dGrassHills) / (dOcean_strength + dGrassFields_strength + dGrassHills_strength);
    }

    protected override void ThreadFunction()
    {
        GenerateHeight();
    }
    private void GenerateHeight()
    {
        double posX = position.x / heightmapSize;
        double posZ = position.y / heightmapSize;
        for (int x = 0; x < heightmapSize; x++)
        {
            for (int z = 0; z < heightmapSize; z++)
            {
                double x1 = (x / (double)heightmapSize - 1) + posX;
                double z1 = (z / (double)heightmapSize - 1) + posZ;
                double value = GetHeight(x1, z1, x, z);
                byte biom = this[x, z];
                int stone = (int)value + 30;
                int _max = Mathf.Max(stone + ground + 3, waterLevel);
                _max = Mathf.Min(_max, chunkHeight);
                if (_max + 3 > waterLevel && x % 5 == 0 && z % 5 == 0)
                    spawnPosition = new Vector3(x + (float)posX, _max + 3f, z + (float)posZ);
                for (int y = 0; y < _max; y++)
                {
                    if (y <= stone)
                    {
                        var coal = Noise.PerlinNoise(x + (int)posX, y, z + (int)posZ, 5, 125, 0.6f);
                        var iron = Noise.PerlinNoise(x + (int)posX, y, z + (int)posZ, 5, 175, 0.7f);
                        if (y > 10 && Noise.PerlinNoise(x + (int)posX, y, z + (int)posZ, 10, 5, 6f) < 5)
                        {
                            this[x, y, z] = (ushort)Blocks.Dirt;
                        }
                        else if (y > 15 && y < 70 && iron >= 6 && iron <= 9)
                        {
                            this[x, y, z] = (ushort)Blocks.IronOre;
                        }
                        else if (y > 35 && y < 100 && coal >= 0 && coal <= 5)
                        {
                            this[x, y, z] = (ushort)Blocks.CoalOre;
                        }
                        else
                        {
                            this[x, y, z] = (ushort)Blocks.Stone;
                        }
                    }
                    else
                    {
                        if (y > stone + ground + 1 && y <= waterLevel - 1)
                        {
                            this[x, y, z] = (ushort)Blocks.Water_1;
                        }
                        else
                        {
                            if (y <= ground + stone)
                            {
                                if (y <= waterLevel)
                                    this[x, y, z] = (ushort)Blocks.Sand;
                                else if (y <= _max && y > waterLevel)
                                {
                                    switch (biom)
                                    {
                                        case 1:
                                            this[x, y, z] = (ushort)Blocks.Sand;
                                            break;
                                        case 3:
                                            this[x, y, z] = (ushort)Blocks.Sand;
                                            break;
                                        case 4:
                                            this[x, y, z] = (ushort)Blocks.Stone;
                                            break;
                                        default:
                                            this[x, y, z] = (ushort)Blocks.Dirt;
                                            break;
                                    }
                                }
                            }
                            else if (y <= ground + stone + 1)
                            {
                                if (y <= waterLevel)
                                {
                                    switch (biom)
                                    {
                                        case 1:
                                            this[x, y, z] = (ushort)Blocks.Sand;
                                            break;
                                        case 2:
                                        case 5:
                                            if ((y == waterLevel - 1 || y == waterLevel) && (waterLevel - 1 == ground + stone + 1 || (waterLevel == ground + stone + 1)))
                                                this[x, y, z] = (ushort)Blocks.Grass;
                                            else
                                                this[x, y, z] = (ushort)Blocks.Sand;
                                            break;
                                        case 3:
                                            this[x, y, z] = (ushort)Blocks.Sand;
                                            break;
                                        default:
                                            this[x, y, z] = (ushort)Blocks.Dirt;
                                            break;
                                    }
                                }
                                else if (y <= _max && y > waterLevel)
                                {
                                    switch (biom)
                                    {
                                        //biom Ocean
                                        case 1:
                                            this[x, y, z] = (ushort)Blocks.Sand;
                                            break;
                                        //biom GrassFields
                                        case 2:
                                            this[x, y, z] = (ushort)Blocks.Grass;
                                            break;
                                        //biom Desert
                                        case 3:
                                            this[x, y, z] = (ushort)Blocks.Sand;
                                            break;
                                        //biom RockyMountains
                                        case 4:
                                            this[x, y, z] = (ushort)Blocks.Stone;
                                            break;
                                        //biom GrassHills
                                        case 5:
                                            this[x, y, z] = (ushort)Blocks.Grass;
                                            break;
                                        //biom grass
                                        default:
                                            this[x, y, z] = (ushort)Blocks.Grass;
                                            break;
                                    }
                                }
                            }
                            else if (y <= ground + stone + 2 && (biom == (byte)Bioms.GrassFields || biom == (byte)Bioms.GrassHills))
                            {
                                var noise = BushNoise.Get(x1, y, z1);
                                if (noise >= 0.88f && noise < 0.91f)
                                    this[x, y, z] = (ushort)Blocks.FruitBush;

                                else if (noise >= 0.70f && noise < 0.73f)
                                    this[x, y, z] = (ushort)Blocks.Flower1;

                                else if (noise >= 0.50f && noise < 0.52f)
                                    this[x, y, z] = (ushort)Blocks.Flower2;

                                else if (noise >= 0.30f && noise < 0.34f)
                                    this[x, y, z] = (ushort)Blocks.Flower3;

                                else if (noise >= 0.1f && noise < 0.2f)
                                    this[x, y, z] = (ushort)Blocks.GrassBush;

                                else if (noise >= -0.15f && noise < -0.05f)
                                    this[x, y, z] = (ushort)Blocks.GrassShortBush;

                                if (biom != (byte)Bioms.GrassHills && y > waterLevel + 2)
                                {
                                    //Spawn tree
                                    if (CanSpawnTree(x, y, z, x1, z1))
                                    {
                                        SpawnTree(x, y, z, x1, z1, false);
                                    }
                                }
                            }
                        }
                    }
                }
                for (int y = (ground + stone + 1); y > 5; y--)
                {
                    GenerateCaves(x, y, z, posX, posZ, biom, _max);
                }
            }
        }

    }
    private void GenerateCaves(int x, int y, int z, double posX, double posZ, int biom, int max)
    {
        if (this[x, y, z] / 10 == (ushort)Blocks.WaterSource / 10 || this[x, y + 1, z] / 10 == (ushort)Blocks.WaterSource / 10)
        {
            return;
        }
        else
        {
            float weight = (max - y + 10) / max;
            int cave1 = Noise.PerlinNoise(x + (int)(posX * 128), y, z + (int)(posZ * 128), scale1, height1, power1 * weight);
            int cave2 = Noise.Perlin3DNoise(x + (int)(posX * 128), y, z + (int)(posZ * 128), scale2, height2, power2 * weight);
            float caveNoise = (cave1 + cave2) / 2f;
            if (caveNoise > threshold)
            {
                this[x, y, z] = (ushort)Blocks.Air;

                if (this[x, y + 1, z] >= 190)
                    this[x, y + 1, z] = (ushort)Blocks.Air;

                if (this[x, y + 1, z] == (ushort)Blocks.Log)
                    this[x, y, z] = (ushort)Blocks.Grass;

                if (max - y <= 7 && biom == (byte)Bioms.GrassFields && this[x, y - 1, z] == (ushort)Blocks.Dirt
                    && this[x, y + 2, z] == (ushort)Blocks.Air && this[x, y + 5, z] == (ushort)Blocks.Air && this[x, y + 10, z] == (ushort)Blocks.Air && this[x, y + 20, z] == (ushort)Blocks.Air)
                {
                    this[x, y - 1, z] = (ushort)Blocks.Grass;
                }
            }
        }
    }

    private bool CanSpawnTree(int x, int y, int z, double x1, double z1)
    {
        if (this[x, y - 1, z] != 0 && x < 124 && x >= 4 && z < 124 && z >= 4)
        {
            double value = TreeNoise.Get(x1, y, z1);
            if (value > 0.998f)
            {
                for (int i = 1; i <= 10; i += 3)
                {
                    if (!CheckPositionNeg(x, y + i, z, 4, 60) || !CheckPositionCrossNeg(x, y + i, z, 3, 60) || !CheckPosition(x, y + i, z, 1, 0) || !CheckPositionCross(x, y + i, z, 2, 0))
                    {
                        return false;
                    }
                }
                return true;
            }
            else
                return false;
        }
        else
        {
            return false;
        }
    }
    private void SpawnTree(int x, int y, int z, double x1, double z1, bool isAppleTree)
    {
        int numberOfLogs = 4;
        numberOfLogs += (int)Remap(Mathf.PerlinNoise((float)x1, (float)z1), 0f, 1f, 0, 3);//(int)Remap((float)value, 0.20f, 0.70f, 0f, 4f);
        for (int i = 0; i < numberOfLogs; i++)
        {
            this[x, y + i, z] = (ushort)Blocks.Log;
            if (i > numberOfLogs - 3)
            {
                for (int m = -2; m <= 2; m++)
                {
                    for (int n = -2; n <= 2; n++)
                    {
                        if ((m == 0 && n == 0) || Mathf.Abs(m) == 2 && Mathf.Abs(n) == 2)
                            continue;
                        if (isAppleTree)
                            this[x + m, y + i, z + n] = (ushort)Blocks.Leaves;
                        else
                            this[x + m, y + i, z + n] = (ushort)Blocks.Leaves;
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
                    this[x + m, y + numberOfLogs - 3, z + n] = (ushort)Blocks.Leaves;
                }
            }
        }
        this[x + 1, y + numberOfLogs, z] = (ushort)Blocks.Leaves;
        this[x + 1, y + numberOfLogs, z + 1] = (ushort)Blocks.Leaves;
        this[x + 1, y + numberOfLogs, z - 1] = (ushort)Blocks.Leaves;
        this[x, y + numberOfLogs, z + 1] = (ushort)Blocks.Leaves;
        this[x, y + numberOfLogs, z - 1] = (ushort)Blocks.Leaves;
        this[x - 1, y + numberOfLogs, z] = (ushort)Blocks.Leaves;
        this[x - 1, y + numberOfLogs, z + 1] = (ushort)Blocks.Leaves;
        this[x - 1, y + numberOfLogs, z - 1] = (ushort)Blocks.Leaves;
        this[x, y + numberOfLogs, z] = (ushort)Blocks.Leaves;
        this[x, y + numberOfLogs + 1, z] = (ushort)Blocks.Leaves;
        this[x, y + numberOfLogs + 1, z - 1] = (ushort)Blocks.Leaves;
        this[x - 1, y + numberOfLogs + 1, z] = (ushort)Blocks.Leaves;
        this[x, y + numberOfLogs + 1, z + 1] = (ushort)Blocks.Leaves;
        this[x + 1, y + numberOfLogs + 1, z] = (ushort)Blocks.Leaves;

    }
    private void SetSeeds()
    {
        OceanNoise.Seed = worldSeed;
        GrassFieldsNoise.Seed = worldSeed;
        GrassHillsNoise.Seed = worldSeed;
        TreeNoise.Seed = worldSeed;
        BiomNoise.Seed = worldSeed;
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
