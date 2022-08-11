using System.Collections.Generic;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    //TOTAL CUBES IN ONE CHUNK = 32768
    //CHUNK HEIGHT = 128
    private static readonly int heightmapSize = 128;
    private static readonly int chunkWidth = 16;
    private static readonly int chunkHeight = 128;

    public byte[] Cubes = new byte[chunkHeight * chunkWidth * chunkWidth];
    public byte this[int x, int y, int z]
    {
        get { return Cubes[x * chunkHeight * chunkWidth + y * chunkWidth + z]; }
        set { Cubes[x * chunkHeight * chunkWidth + y * chunkWidth + z] = value; }
    }
    public bool isDirty = false;
    private Heightmap heightmap;
    //COMPONENTS
    private Material[] materials = new Material[3];
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private MeshCollider _meshCollider;
    private VoxelEngine _voxelEngine;
    //GENERATING CHUNKS
    private static readonly float cubeSize = 1f;
    //TRIANGLES
    private Vector3[] _c;
    private Vector3[] _w;
    private Vector3[] _cubeVertices;
    private Vector3[] _waterVertices;
    private static readonly int[] BottomQuad =
        { 3, 1, 0, 3, 2, 1 };
    private static readonly int[] LeftQuad =
    {   7, 5, 4, 7, 6, 5  };
    private static readonly int[] FrontQuad =
    {  11, 9, 8, 11, 10, 9   };
    private static readonly int[] BackQuad =
    {      15, 13, 12, 15, 14, 13   };
    private static readonly int[] RightQuad =
    {19, 17, 16, 19, 18, 17    };
    private static readonly int[] TopQuad =
    {     23, 21, 20, 23, 22, 21    };
    //NORMALS
    private static readonly Vector3 up = Vector3.up;
    private static readonly Vector3 down = Vector3.down;
    private static readonly Vector3 forward = Vector3.forward;
    private static readonly Vector3 back = Vector3.back;
    private static readonly Vector3 left = Vector3.left;
    private static readonly Vector3 right = Vector3.right;
    private Vector3[] _cubeNormals;

    void Awake()
    {
        _voxelEngine = GetComponentInParent<VoxelEngine>();
        heightmap = _voxelEngine.world.Heightmaps[HeightmapId.FromWorldPos((int)transform.position.x, (int)transform.position.z)];
        _c = new Vector3[]
        {
          new Vector3 (0, 0, cubeSize),
          new Vector3 (cubeSize, 0, cubeSize),
          new Vector3 (cubeSize, 0, 0),
          new Vector3 (0, 0, 0),
          new Vector3 (0, cubeSize, cubeSize),
          new Vector3 (cubeSize, cubeSize, cubeSize),
          new Vector3 (cubeSize, cubeSize, 0),
          new Vector3 (0, cubeSize, 0),
        };
        _w = new Vector3[]
        {
          new Vector3 (0, 0, cubeSize),
          new Vector3 (cubeSize, 0, cubeSize),
          new Vector3 (cubeSize, 0, 0),
          new Vector3 (0, 0, 0),
          new Vector3 (0, cubeSize - 0.1f, cubeSize),
          new Vector3 (cubeSize, cubeSize - 0.1f, cubeSize),
          new Vector3 (cubeSize, cubeSize - 0.1f, 0),
          new Vector3 (0, cubeSize - 0.1f, 0),
        };
        _waterVertices = new Vector3[]
        {
             _w[0], _w[1], _w[2], _w[3], // Bottom
             _w[7], _w[4], _w[0], _w[3], // Left
             _w[4], _w[5], _w[1], _w[0], // Front
             _w[6], _w[7], _w[3], _w[2], // Back
             _w[5], _w[6], _w[2], _w[1], // Right
             _w[7], _w[6], _w[5], _w[4]  // Top
        };
        _cubeVertices = new Vector3[]
        {
             _c[0], _c[1], _c[2], _c[3], // Bottom
             _c[7], _c[4], _c[0], _c[3], // Left
             _c[4], _c[5], _c[1], _c[0], // Front
             _c[6], _c[7], _c[3], _c[2], // Back
             _c[5], _c[6], _c[2], _c[1], // Right
             _c[7], _c[6], _c[5], _c[4]  // Top
        };
        _cubeNormals = new Vector3[]
       {
            down, down, down, down,             // Bottom
	        left, left, left, left,             // Left
	        forward, forward, forward, forward,	// Front
	        back, back, back, back,             // Back
	        right, right, right, right,         // Right
	        up, up, up, up                      // Top
       };
    }
    private void Start()
    {
        materials[0] = _voxelEngine.opaqueMat;
        materials[1] = _voxelEngine.transparentMat;
        materials[2] = _voxelEngine.waterMat;
        _meshFilter = GetComponent<MeshFilter>();
        _meshFilter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        _meshFilter.mesh.MarkDynamic();
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshCollider = GetComponent<MeshCollider>();
        _meshRenderer.materials = materials;
        _meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        GetHeight();
    }
    private void Update()
    {
        if (isDirty)
            RenderToMesh();
    }
    private void GetHeight()
    {
        int posX = (int)transform.position.x;
        int _posX = posX;
        int posZ = (int)transform.position.z;
        int _posZ = posZ;
        if (posX < 0)
            posX = (int)RoundUp(Mathf.Abs(posX), heightmapSize) + posX;
        if (posZ < 0)
            posZ = (int)RoundUp(Mathf.Abs(posZ), heightmapSize) + posZ;
        Cubes = heightmap.ReturnCubes(posX % heightmapSize, posZ % heightmapSize);
        _voxelEngine.world.SetChunkDirty(_posX, _posZ, true);
    }
    /*
    private IEnumerator WaitForHeight()
    {
        int posX = (int)transform.position.x;
        int _posX = posX;
        int posZ = (int)transform.position.z;
        int _posZ = posZ;
        if (_voxelEngine.world.Heightmap.ContainsKey(HeightmapId.FromWorldPos(posX, posZ)))
        {
            Heightmap hm = _voxelEngine.world.Heightmap[HeightmapId.FromWorldPos(posX, posZ)];
            if (hm.IsDone)
            {
                if (posX < 0)
                    posX = (int)RoundUp(Mathf.Abs(posX), 128) + posX;
                if (posZ < 0)
                    posZ = (int)RoundUp(Mathf.Abs(posZ), 128) + posZ;
                Cubes = hm.ReturnCubes(posX % 128, posZ % 128);
                _voxelEngine.world.SetChunkDirty(_posX, _posZ, true);
                //isDirty = true;
            }
            else
            {
                yield return new WaitForSeconds(1f);
                StartCoroutine(WaitForHeight());
            }

        }
        else
        {
            yield return new WaitForSeconds(1f);
            StartCoroutine(WaitForHeight());
        }
    }*/
    private void RenderToMesh()
    {
        isDirty = false;
        var vertices = new List<Vector3>();
        var opaqueTriangles = new List<int>();
        var waterTriangles = new List<int>();
        var transparentTriangles = new List<int>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        for (var x = 0; x < chunkWidth; x++)
        {
            for (var y = 0; y < chunkHeight; y++)
            {
                for (var z = 0; z < chunkWidth; z++)
                {
                    var voxelType = this[x, y, z];
                    if (voxelType == (byte)Blocks.Air)
                        continue;
                    var _cubeTriangles = CalcTriangleFaces(x, y, z);
                    if (_cubeTriangles.Count == 0)
                        continue;
                    var pos = new Vector3(x, y, z);
                    var verticesPos = vertices.Count;
                    if (voxelType == (byte)Blocks.Water && this[x, y + 1, z] == (byte)Blocks.Air)
                    {
                        foreach (var vert in _waterVertices)
                            vertices.Add(pos + vert);
                    }
                    else
                    {
                        foreach (var vert in _cubeVertices)
                            vertices.Add(pos + vert);
                    }
                    foreach (var tri in _cubeTriangles)
                    {
                        if (voxelType == (byte)Blocks.Water)
                        { waterTriangles.Add(tri + verticesPos); }
                        else if (voxelType == (byte)Blocks.FancyLeaves || voxelType == (byte)Blocks.Glass)
                        { transparentTriangles.Add(tri + verticesPos); }
                        else
                        { opaqueTriangles.Add(tri + verticesPos); }
                    }
                    foreach (var normal in _cubeNormals)
                        normals.Add(normal);
                    switch (voxelType)
                    {
                        //Grass
                        case 1:
                            foreach (var uv in Atlas.GetCustomUV(0, 0, 1))
                                uvs.Add(uv);
                            break;
                        //Dirt 
                        case 2:
                            foreach (var uv in Atlas.GetUV(32, 32))
                                uvs.Add(uv);
                            break;
                        //Stone 
                        case 3:
                            foreach (var uv in Atlas.GetUV(0, 16))
                                uvs.Add(uv);
                            break;
                        //Sand 
                        case 4:
                            foreach (var uv in Atlas.GetUV(16, 16))
                                uvs.Add(uv);
                            break;
                        //WoodenLog 
                        case 5:
                            foreach (var uv in Atlas.GetCustomUV(32, 16, 2))
                                uvs.Add(uv);
                            break;
                        //Leaves 
                        case 6:
                            foreach (var uv in Atlas.GetUV(32, 0))
                                uvs.Add(uv);
                            break;
                        //Water 
                        case 7:
                            foreach (var uv in Atlas.GetUV(0, 64))
                                uvs.Add(uv);
                            break;
                        //Cobblestone
                        case 8:
                            foreach (var uv in Atlas.GetUV(48, 0))
                                uvs.Add(uv);
                            break;
                        //Planks
                        case 9:
                            foreach (var uv in Atlas.GetUV(16, 32))
                                uvs.Add(uv);
                            break;
                        //Clay
                        case 10:
                            foreach (var uv in Atlas.GetUV(48, 16))
                                uvs.Add(uv);
                            break;
                        //Bricks
                        case 11:
                            foreach (var uv in Atlas.GetUV(48, 32))
                                uvs.Add(uv);
                            break;
                        //FancyLeaves
                        case 12:
                            foreach (var uv in Atlas.GetUV(64, 64))
                                uvs.Add(uv);
                            break;
                        //Glass
                        case 13:
                            foreach (var uv in Atlas.GetUV(32, 64))
                                uvs.Add(uv);
                            break;
                        //Coal ore
                        case 14:
                            foreach (var uv in Atlas.GetUV(64, 0))
                                uvs.Add(uv);
                            break;
                        //Iron ore
                        case 15:
                            foreach (var uv in Atlas.GetUV(64, 16))
                                uvs.Add(uv);
                            break;
                        //Workbench
                        case 16:
                            foreach (var uv in Atlas.GetCustomUV(80, 16, 3))
                                uvs.Add(uv);
                            break;
                        //Furnace
                        case 17:
                            foreach (var uv in Atlas.GetCustomUV(80, 32, 4))
                                uvs.Add(uv);
                            break;
                        //Dirt
                        default:
                            foreach (var uv in Atlas.GetUV(32, 32))
                                uvs.Add(uv);
                            break;
                    }
                }
            }
        }
        Mesh mesh = _meshFilter.mesh;
        Mesh collisionMesh = new();
        collisionMesh.MarkDynamic();
        collisionMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.subMeshCount = 3;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(opaqueTriangles, 0);
        collisionMesh.subMeshCount = 2;
        collisionMesh.SetVertices(vertices);
        collisionMesh.SetTriangles(opaqueTriangles, 0);
        if (transparentTriangles.Count > 0)
        {
            mesh.SetTriangles(transparentTriangles, 1);
            collisionMesh.SetTriangles(transparentTriangles, 1);
        }
        if (waterTriangles.Count > 0)
        {
            mesh.SetTriangles(waterTriangles, 2);
        }
        mesh.RecalculateNormals();
        mesh.SetUVs(0, uvs);
        mesh.SetNormals(normals);
        mesh.Optimize();
        collisionMesh.Optimize();
        _meshCollider.sharedMesh = collisionMesh;
    }
    private void AddRange(ref List<int> triangles, byte _this, byte other, string quad)
    {
        if (other == 0 || other == 7 || other == 12 || other == 13)
        {
            //water
            if (_this == 7)
            {
                if (other == 0 || other == 12 || other == 13)
                {
                    switch (quad)
                    {
                        case "Bottom":
                            triangles.AddRange(BottomQuad);
                            break;
                        case "Top":
                            triangles.AddRange(TopQuad);
                            break;
                        case "Left":
                            triangles.AddRange(LeftQuad);
                            break;
                        case "Right":
                            triangles.AddRange(RightQuad);
                            break;
                        case "Back":
                            triangles.AddRange(BackQuad);
                            break;
                        case "Front":
                            triangles.AddRange(FrontQuad);
                            break;
                    }
                }
            }
            //leaves
            else if (_this == 12)
            {
                if (other == 0 || other == 7 || other == 12 || other == 13)
                {
                    switch (quad)
                    {
                        case "Bottom":
                            triangles.AddRange(BottomQuad);
                            break;
                        case "Top":
                            triangles.AddRange(TopQuad);
                            break;
                        case "Left":
                            triangles.AddRange(LeftQuad);
                            break;
                        case "Right":
                            triangles.AddRange(RightQuad);
                            break;
                        case "Back":
                            triangles.AddRange(BackQuad);
                            break;
                        case "Front":
                            triangles.AddRange(FrontQuad);
                            break;
                    }
                }
            }
            //glass
            else if (_this == 13)
            {
                if (other == 0 || other == 7 || other == 12)
                {
                    switch (quad)
                    {
                        case "Bottom":
                            triangles.AddRange(BottomQuad);
                            break;
                        case "Top":
                            triangles.AddRange(TopQuad);
                            break;
                        case "Left":
                            triangles.AddRange(LeftQuad);
                            break;
                        case "Right":
                            triangles.AddRange(RightQuad);
                            break;
                        case "Back":
                            triangles.AddRange(BackQuad);
                            break;
                        case "Front":
                            triangles.AddRange(FrontQuad);
                            break;
                    }
                }
            }
            else
            {
                switch (quad)
                {
                    case "Bottom":
                        triangles.AddRange(BottomQuad);
                        break;
                    case "Top":
                        triangles.AddRange(TopQuad);
                        break;
                    case "Left":
                        triangles.AddRange(LeftQuad);
                        break;
                    case "Right":
                        triangles.AddRange(RightQuad);
                        break;
                    case "Back":
                        triangles.AddRange(BackQuad);
                        break;
                    case "Front":
                        triangles.AddRange(FrontQuad);
                        break;
                }
            }
        }
    }
    private List<int> CalcTriangleFaces(int x, int y, int z)
    {
        var triangles = new List<int>();
        //Bottom
        if (y > 0)
        {
            byte below = this[x, y - 1, z];
            AddRange(ref triangles, this[x, y, z], below, "Bottom");
        }
        else
        { triangles.AddRange(BottomQuad); }

        //Top
        if (y < chunkHeight - 1)
        {
            byte above = this[x, y + 1, z];
            AddRange(ref triangles, this[x, y, z], above, "Top");
        }
        else
        { triangles.AddRange(TopQuad); }

        //Left
        if (x > 0)
        {
            byte left = this[x - 1, y, z];
            AddRange(ref triangles, this[x, y, z], left, "Left");
        }
        else
        {
            byte value = _voxelEngine.world[(int)transform.position.x - 1, y, (int)transform.position.z + z];
            AddRange(ref triangles, this[x, y, z], value, "Left");
        }

        //Right
        if (x < chunkWidth - 1)
        {
            byte right = this[x + 1, y, z];
            AddRange(ref triangles, this[x, y, z], right, "Right");
        }
        else
        {
            byte value = _voxelEngine.world[(int)transform.position.x + chunkWidth, y, (int)transform.position.z + z];
            AddRange(ref triangles, this[x, y, z], value, "Right");
        }

        //Back
        if (z > 0)
        {
            byte back = this[x, y, z - 1];
            AddRange(ref triangles, this[x, y, z], back, "Back");
        }
        else
        {
            byte value = _voxelEngine.world[(int)transform.position.x + x, y, (int)transform.position.z - 1];
            AddRange(ref triangles, this[x, y, z], value, "Back");
        }

        //Front
        if (z < chunkWidth - 1)
        {
            byte front = this[x, y, z + 1];
            AddRange(ref triangles, this[x, y, z], front, "Front");
        }
        else
        {
            byte value = _voxelEngine.world[(int)transform.position.x + x, y, (int)transform.position.z + chunkWidth];
            AddRange(ref triangles, this[x, y, z], value, "Front");
        }

        return triangles;
    }

    long RoundUp(long n, long m)
    {
        return n >= 0 ? ((n + m - 1) / m) * m : (n / m) * m;
    }
}
