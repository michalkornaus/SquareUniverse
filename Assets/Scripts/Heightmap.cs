using UnityEngine;
using TinkerWorX.AccidentalNoiseLibrary;

public class Heightmap
{
    private static ImplicitFractal HeightNoise = new ImplicitFractal(FractalType.HybridMulti, BasisType.Simplex, InterpolationType.Quintic)
    { Octaves = 6, Frequency = 0.93, Lacunarity = 0.7, Gain = 0.3 };
    private static ImplicitFractal CaveNoise = new ImplicitFractal(FractalType.RidgedMulti, BasisType.Gradient, InterpolationType.Quintic)
    { Octaves = 1, Frequency = 2 };
    private static readonly int biomsCount = 5;
    private static readonly int chunkSize = 16;
    private static readonly int heightmapSize = chunkSize * 8;
    private static readonly int chunkHeight = chunkSize * 8;
    private Vector2 position;
    private HeightmapValues heightmapValues;
    public Material NoiseShader;
    //Cubes - Max 2 097 152
    public int[] Cubes = new int[128 * 16 * 16 * 64];

    //CubesBioms - Max 16384
    public int[] CubesBioms = new int[16 * 16 * 64];
    public Heightmap(int x, int z)
    {
        //x - 0, 128, -128... : z - 0, 128, -128...
        HeightNoise.Seed = (int)VoxelEngine.WorldSeed;
        //CaveNoise.Seed = (int)VoxelEngine.WorldSeed;
        //GenerateBioms(x, z);
        GenerateHeight(x, z);
        SetHeight(x, z);
        position = new Vector2(x, z);
    }
    public Vector2 GetPosition()
    {
        return position;
    }
    public int this[int x, int y, int z]
    {
        //x - 2 080 768   y -     z - 127
        get { return Cubes[x * 128 * 128 + y * 128 + z]; }
        //x 2 080 768 + y 16256 z + 127 = 2 097 151
        set { Cubes[x * 128 * 128 + y * 128 + z] = value; }
    }
    public int this[int x, int z]
    {
        get { return CubesBioms[x * heightmapSize + z]; }
        set { CubesBioms[x * heightmapSize + z] = value; }
    }

    public int[] ReturnCubes(int posX, int posZ)
    {
        int[] p = new int[128 * 16 * 16];
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
        int posX = _x + (int)VoxelEngine.WorldSeed;
        int posZ = _z + (int)VoxelEngine.WorldSeed;
        for (int x = 0; x < heightmapSize; x++)
        {
            for (int z = 0; z < heightmapSize; z++)
            {
                this[x, z] = CalcBiom(x + posX, z + posZ);
            }
        }
    }
    private int CalcBiom(int x, int z)
    {
        int value = ((Noise.PerlinNoise(x, 45, z, 9, 1, 1.1f) + Noise.PerlinNoise(x, 10, z, 15, 3, 0.9f)) * 5) % biomsCount;
        return value;
    }
    /*
    private int CalcStoneLevel(int x, int z, int _x, int _z, int biom)
    {
        //x - 0, 128, -128... : z - 0, 128, -128...
        //_x - 0:127 _z 0:127
        int xOffset = 0, zOffset = 0;
        if (_x == 0 || _x == 127)
        {
            if (_x == 0)
            {
                if (CalcBiom(x - 1, z) != biom)
                { xOffset = -1; }
            }
            else if (_x == 127)
            {
                if (CalcBiom(x + 1, z) != biom)
                { xOffset = 1; }
            }
        }
        else
        {
            if (this[_x - 1, _z] != biom)
            { xOffset = -1; }
            else if (this[_x + 1, _z] != biom)
            { xOffset = 1; }
        }
        if (_z == 0 || _z == 127)
        {
            if (_z == 0)
            {
                if (CalcBiom(x, z - 1) != biom)
                { zOffset = -1; }
            }
            else if (_z == 127)
            {
                if (CalcBiom(x, z + 1) != biom)
                { zOffset = 1; }
            }
        }
        else
        {
            if (this[_x, _z - 1] != biom)
            { zOffset = -1; }
            else if (this[_x, _z + 1] != biom)
            { zOffset = 1; }
        }
        if (xOffset == 0 && zOffset == 0)
            return GetStoneNoise(x, z, biom);
        else
        {
            return (GetStoneNoise(x, z, biom) + GetStoneNoise(x + xOffset, z + zOffset, CalcBiom(x+xOffset, z+zOffset)))/2;
        }
    }
    private int GetStoneNoise(int x, int z, int biom)
    {
        switch (biom)
        {
            //PLAINS
            case 0:
                return Noise.PerlinNoise(x, 35, z, 10, 5, 0.6f) + 20;
            //HILLS
            case 1:
                return Noise.PerlinNoise(x, 75, z, 15, 3, 1.1f) + 35;
            //DESERT
            case 2:
                return Noise.PerlinNoise(x, 50, z, 40, 10, 1.2f) + 30;
            //OTHER BIOMS
            default:
                return Noise.PerlinNoise(x, 35, z, 10, 5, 0.6f);
        }
    }
    */
    private int CalcGroundLevel(int x, int z, int biom)
    {
        switch (biom)
        {
            //PLAINS
            case 0:
                return Noise.PerlinNoise(x, 15, z, 12, 10, 1.4f) + 2;
            //HILLS
            case 1:
                return Noise.PerlinNoise(x, 50, z, 50, 3, 0.6f) + 2;
            //DESERT
            case 2:
                return Noise.PerlinNoise(x, 15, z, 12, 5, 1.4f) + 2;
            //OTHER BIOMS
            default:
                return Noise.PerlinNoise(x, 35, z, 10, 5, 0.6f) + 2;
        }
    }

    /*
    private void CalculateHeightMap(int _x, int _z)
    {
        double posX = _x / 128;
        double posZ = _z / 128;
        // 0001 0010 0011 0100 0101 0110 0111 1000 1001 1010 1011 1100 1101 1110 1111
        ImplicitGradient gradient = new ImplicitGradient(0, 0, 0, 1)
        {
            Seed = (int)VoxelEngine.WorldSeed
        };
        ImplicitFractal fractal = new ImplicitFractal(FractalType.FractionalBrownianMotion, BasisType.Gradient, InterpolationType.Quintic)
        {
            Octaves = 6,
            Frequency = 5
        };
        ImplicitScaleOffset scaleOffset = new ImplicitScaleOffset(fractal, 1, 0);
        for (int x = 0; x < heightmapSize; x++)
        {
            for (int z = 0; z < heightmapSize; z++)
            {
                double x1 = (double)x / 127;
                double z1 = (double)z / 127;
                // ImplicitSelect select = new ImplicitSelect(gradient, 128, 0, 0, 32.0);

                //ImplicitRotateDomain rotateDomain = new ImplicitRotateDomain(translateDomain, 0, 0, 1, 90);
                //ImplicitTranslateDomain translateDomain = new ImplicitTranslateDomain(gradient, 0, 0, scaleOffset.Get(x1 + posX, z1 + posZ));//scaleOffset.Get(x1, 64, z1));
                ImplicitRotateDomain rotateDomain = new ImplicitRotateDomain(gradient, 1, 1, 1, 90);
                //translateDomain = new ImplicitTranslateDomain(rotateDomain, 0, 0, 45);

                lowlandTerrain = new ImplicitTranslateDomain(groundGradient, 0, lowlandDomain.Get(x1 + posX, 0, z1 + posZ));
                highlandTerrain = new ImplicitTranslateDomain(groundGradient, 0, highlandDomain.Get(x1 + posX, 0, z1 + posZ));
                highlowSelect = new ImplicitSelect(terrainDomain, lowlandTerrain.Get(x1 + posX, 0, z1 + posZ), highlandTerrain.Get(x1 + posX, 0, z1 + posZ), 0.15, 0.25);
                groundSelect = new ImplicitSelect(highlowSelect, 128, 0, 3, 32.0);

                //ImplicitSelect select = new ImplicitSelect(rotateDomain, 128, 0, 3, 32.0);
                for (int y = 0; y < chunkHeight; y++)
                {
                    double value = groundSelect.Get(x1 + posX, y, z1 + posZ);//groundSelect.Get(x1, y, z1);
                    //value = Remap((float)value, -1f, 1f, 0, 128);
                    /*if (value == 0)
                        this[x, y, z] = 1;
                    else if (value == 1)
                        this[x, y, z] = 2;*/
    //value *= 128;
    // if (value >= 0.5)
    /*
    this[x, y, z] = (ushort)value;

}
}
}
}
*/
    private void GenerateHeight(int _x, int _z)
    {
        double posX = _x / 128;
        double posZ = _z / 128;
        heightmapValues = new HeightmapValues();
        for (int x = 0; x < heightmapSize; x++)
        {
            for (int z = 0; z < heightmapSize; z++)
            {
                double x1 = x / (double)heightmapSize - 1;
                double z1 = z / (double)heightmapSize - 1;
                double value = HeightNoise.Get(x1 + posX, z1 + posZ);
                heightmapValues[x, z] = value;
            }
        }
    }
    private void SetHeight(int posX, int posZ)
    {
        /*
         Grass id:1     Dirt id:2    Stone id:3    Sand id:4   
         WoodenLog id:5    Leaves id:6      Water id:7
        */
        //_x - 0, 128, -128... : _z - 0, 128, -128...
        for (int x = 0; x < heightmapSize; x++)
        {
            for (int z = 0; z < heightmapSize; z++)
            {
                double value = heightmapValues[x, z];
                value = Remap((float)value, -1, 1, 0, 0.95f);
                value *= 128;
                int stone = (int)value + 15;
                //stone += CalcStoneLevel(x + posX, z + posZ, x, z, this[x, z]);
                //int ground = (CalcGroundLevel(x + posX, z + posZ, 0) + CalcGroundLevel(x + posX, z + posZ, 1))/2;
                int ground = 3;
                for (int y = 0; y < chunkHeight; y++)
                {
                    if (y > stone + ground + 1 && y <= 35)
                        this[x, y, z] = 7;
                    if (y < stone)
                    {
                        this[x, y, z] = 3;
                    }
                    else if (y <= ground + stone)
                    {
                        if (y <= 36)
                            this[x, y, z] = 4;
                        else if (y <= 100 && y > 36)
                            this[x, y, z] = 2;
                        else if (y <= 128 && y > 100)
                            this[x, y, z] = 3;
                    }
                    else if (y <= ground + stone + 1)
                    {
                        if (y <= 36)
                            this[x, y, z] = 4;
                        else if (y <= 100 && y > 36)
                            this[x, y, z] = 1;
                        else if (y <= 128 && y > 100)
                            this[x, y, z] = 3;
                    }
                    /*
                    else if (y <= ground + stone + 2 && y < 90 && y > 38)
                    {
                        
                        if (CheckPosition(x, y, z) && Noise.PerlinNoise(x + posX, y, z + posZ, 45, 5, 0.8f) > 1)
                        {
                            SpawnTree(x, y, z);
                        }
                    }
                    if (this[x, y, z] != 0 && y > 5 && y < stone && Noise.PerlinNoise(x + posX, y, z + posZ, 10, 5, 6f) < 5)
                        this[x, y, z] = 2;
                    //if (this[x, y, z] != 0 && y > 15 && y < 80 && y <= ground + stone + 2)
                    //  GenerateCaves(x, y, z, posX, posZ);
                    */
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
        /*
        double x1 = x / (double)heightmapSize - 1;
        double z1 = z / (double)heightmapSize - 1;
        double value = CaveNoise.Get(x1 + posX, y, z1 + posZ);
        value = Remap((float)value, -1f, 1f, 0.1f, 0.9f);
        if (value > 0.7f)
            this[x, y, z] = 0;*/
        float caveNoise;
        int cave1 = (int)CaveNoise.Get(x + posX, y, z + posZ) * 128;
        int cave2 = Noise.Perlin3DNoise(x + posX, y, z + posZ, 12, 10, 1.7f);
        caveNoise = cave1 + cave2;
        caveNoise /= 2;
        if (caveNoise < 5f)
            this[x, y, z] = 0;

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

    private bool CheckPosition(int x, int y, int z)
    {
        if (x < 126 && x >= 2 && z < 126 && z >= 2)
        {
            for (int i = 0; i <= 4; i++)
            {
                if (this[x - 2, y + i, z - 2] != 0 || this[x + 2, y + i, z + 2] != 0 || this[x + 2, y + i, z - 2] != 0 || this[x - 2, y + i, z + 2] != 0)
                {
                    return false;
                }
            }
            return true;
        }
        else
        { return false; }

    }
}
