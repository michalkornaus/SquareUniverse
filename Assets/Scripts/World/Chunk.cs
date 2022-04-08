using System.Collections.Generic;
using System.Collections;
using UnityEngine;
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    //TOTAL CUBES IN ONE CHUNK = 32768
    //WORLD SIZE = 128
    public static int chunkSize = 16;
    public static int chunkHeight = chunkSize * 8;

    public byte[] Cubes = new byte[chunkHeight * chunkSize * chunkSize];
    public byte this[int x, int y, int z]
    {
        get { return Cubes[x * chunkHeight * chunkSize + y * chunkSize + z]; }
        set { Cubes[x * chunkHeight * chunkSize + y * chunkSize + z] = value; }
    }
    public bool isDirty = false;

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
        gameObject.isStatic = true;
        _voxelEngine = GetComponentInParent<VoxelEngine>();
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
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshCollider = GetComponent<MeshCollider>();
        _meshRenderer.materials = materials;
        _meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        StartCoroutine(WaitForHeight());
    }
    private void Update()
    {
        if (isDirty)
            RenderToMesh();
    }
    private IEnumerator WaitForHeight()
    {
        int posX = (int)transform.position.x;
        int posZ = (int)transform.position.z;
        if (_voxelEngine.world.Heightmap.ContainsKey(HeightmapId.FromWorldPos(posX, posZ)))
        {
            Heightmap hm = _voxelEngine.world.Heightmap[HeightmapId.FromWorldPos(posX, posZ)];
            if (posX < 0)
                posX = (int)RoundUp(Mathf.Abs(posX), 128) + posX;
            if (posZ < 0)
                posZ = (int)RoundUp(Mathf.Abs(posZ), 128) + posZ;
            Cubes = hm.ReturnCubes(posX % 128, posZ % 128);
        }
        else
        {
            yield return new WaitForSeconds(0.1f);
            StartCoroutine(WaitForHeight());
        }
    }
    private void RenderToMesh()
    {
        isDirty = false;
        var vertices = new List<Vector3>();
        var opaqueTriangles = new List<int>();
        var waterTriangles = new List<int>();
        var transparentTriangles = new List<int>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        for (var x = 0; x < chunkSize; x++)
        {
            for (var y = 0; y < chunkHeight; y++)
            {
                for (var z = 0; z < chunkSize; z++)
                {
                    var voxelType = this[x, y, z];
                    if (voxelType == 0)
                        continue;
                    var _cubeTriangles = CalcTriangleFaces(x, y, z);
                    if (_cubeTriangles.Count == 0)
                        continue;
                    var pos = new Vector3(x, y, z);
                    var verticesPos = vertices.Count;
                    if (voxelType == 7 && this[x, y + 1, z] == 0)
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
                        if (voxelType == 7)
                        { waterTriangles.Add(tri + verticesPos); }
                        else if (voxelType == 12 || voxelType == 13)
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
        Mesh collisionMesh = new Mesh();
        mesh.MarkDynamic();
        collisionMesh.MarkDynamic();
        collisionMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.Clear();
        mesh.ClearBlendShapes();
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
        mesh.SetUVs(0, uvs);
        mesh.SetNormals(normals);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        collisionMesh.Optimize();
        _meshCollider.sharedMesh = collisionMesh;
    }

    private List<int> CalcTriangleFaces(int x, int y, int z)
    {
        var triangles = new List<int>();
        //Bottom
        if (y > 0)
        {
            byte _below = this[x, y - 1, z];
            byte _this = this[x, y, z];
            if (_below == 0 || _below == 7 || _below == 12 || _below == 13)
            {
                //water
                if (_this == 7)
                {
                    if (_below == 0 || _below == 12 || _below == 13)
                    {
                        triangles.AddRange(BottomQuad);
                    }
                }
                //leaves
                else if (_this == 12)
                {
                    if (_below == 0 || _below == 7 || _below == 12 || _below == 13)
                    {
                        triangles.AddRange(BottomQuad);
                    }
                }
                //glass
                else if (_this == 13)
                {
                    if (_below == 0 || _below == 7 || _below == 12)
                    {
                        triangles.AddRange(BottomQuad);
                    }
                }
                else
                {
                    triangles.AddRange(BottomQuad);
                }
            }
        }
        else
        { triangles.AddRange(BottomQuad); }

        //Top
        if (y < chunkHeight - 1)
        {
            byte _above = this[x, y + 1, z];
            byte _this = this[x, y, z];
            if (_above == 0 || _above == 7 || _above == 12 || _above == 13)
            {
                //water
                if (_this == 7)
                {
                    if (_above == 0 || _above == 12 || _above == 13)
                    {
                        triangles.AddRange(TopQuad);
                    }
                }
                //leaves
                else if (_this == 12)
                {
                    if (_above == 0 || _above == 7 || _above == 12 || _above == 13)
                    {
                        triangles.AddRange(TopQuad);
                    }
                }
                //glass
                else if (_this == 13)
                {
                    if (_above == 0 || _above == 7 || _above == 12)
                    {
                        triangles.AddRange(TopQuad);
                    }
                }
                else
                {
                    triangles.AddRange(TopQuad);
                }
            }
        }
        else
        { triangles.AddRange(TopQuad); }

        //Left
        if (x > 0)
        {
            byte _left = this[x - 1, y, z];
            byte _this = this[x, y, z];
            if (_left == 0 || _left == 7 || _left == 12 || _left == 13)
            {
                //water
                if (_this == 7)
                {
                    if (_left == 0 || _left == 12 || _left == 13)
                    {
                        triangles.AddRange(LeftQuad);
                    }
                }
                //leaves
                else if (_this == 12)
                {
                    if (_left == 0 || _left == 7 || _left == 12 || _left == 13)
                    {
                        triangles.AddRange(LeftQuad);
                    }
                }
                //glass
                else if (_this == 13)
                {
                    if (_left == 0 || _left == 7 || _left == 12)
                    {
                        triangles.AddRange(LeftQuad);
                    }
                }
                else
                {
                    triangles.AddRange(LeftQuad);
                }
            }
        }
        else
        {
            int value = _voxelEngine.world[(int)transform.position.x - 1, y, (int)transform.position.z + z];
            byte _this = this[x, y, z];
            if (value == 0 || value == 7 || value == 12 || value == 13)
            {
                //water
                if (_this == 7)
                {
                    if (value == 0 || value == 12 || value == 13)
                    {
                        triangles.AddRange(LeftQuad);
                    }
                }
                //leaves
                else if (_this == 12)
                {
                    if (value == 0 || value == 7 || value == 12 || value == 13)
                    {
                        triangles.AddRange(LeftQuad);
                    }
                }
                //glass
                else if (_this == 13)
                {
                    if (value == 0 || value == 7 || value == 12)
                    {
                        triangles.AddRange(LeftQuad);
                    }
                }
                else
                {
                    triangles.AddRange(LeftQuad);
                }
            }
        }

        //Right
        if (x < chunkSize - 1)
        {
            byte _right = this[x + 1, y, z];
            byte _this = this[x, y, z];
            if (_right == 0 || _right == 7 || _right == 12 || _right == 13)
            {
                //water
                if (_this == 7)
                {
                    if (_right == 0 || _right == 12 || _right == 13)
                    {
                        triangles.AddRange(RightQuad);
                    }
                }
                //leaves
                else if (_this == 12)
                {
                    if (_right == 0 || _right == 7 || _right == 12 || _right == 13)
                    {
                        triangles.AddRange(RightQuad);
                    }
                }
                //glass
                else if (_this == 13)
                {
                    if (_right == 0 || _right == 7 || _right == 12)
                    {
                        triangles.AddRange(RightQuad);
                    }
                }
                else
                {
                    triangles.AddRange(RightQuad);
                }
            }
        }
        else
        {
            int value = _voxelEngine.world[(int)transform.position.x + chunkSize, y, (int)transform.position.z + z];
            byte _this = this[x, y, z];
            if (value == 0 || value == 7 || value == 12 || value == 13)
            {
                //water
                if (_this == 7)
                {
                    if (value == 0 || value == 12 || value == 13)
                    {
                        triangles.AddRange(RightQuad);
                    }
                }
                //leaves
                else if (_this == 12)
                {
                    if (value == 0 || value == 7 || value == 12 || value == 13)
                    {
                        triangles.AddRange(RightQuad);
                    }
                }
                //glass
                else if (_this == 13)
                {
                    if (value == 0 || value == 7 || value == 12)
                    {
                        triangles.AddRange(RightQuad);
                    }
                }
                else
                {
                    triangles.AddRange(RightQuad);
                }
            }
        }

        //Back
        if (z > 0)
        {
            byte _back = this[x, y, z - 1];
            byte _this = this[x, y, z];
            if (_back == 0 || _back == 7 || _back == 12 || _back == 13)
            {
                //water
                if (_this == 7)
                {
                    if (_back == 0 || _back == 12 || _back == 13)
                    {
                        triangles.AddRange(BackQuad);
                    }
                }
                //leaves
                else if (_this == 12)
                {
                    if (_back == 0 || _back == 7 || _back == 12 || _back == 13)
                    {
                        triangles.AddRange(BackQuad);
                    }
                }
                //glass
                else if (_this == 13)
                {
                    if (_back == 0 || _back == 7 || _back == 12)
                    {
                        triangles.AddRange(BackQuad);
                    }
                }
                else
                {
                    triangles.AddRange(BackQuad);
                }
            }
        }
        else
        {
            int value = _voxelEngine.world[(int)transform.position.x + x, y, (int)transform.position.z - 1];
            byte _this = this[x, y, z];
            if (value == 0 || value == 7 || value == 12 || value == 13)
            {
                //water
                if (_this == 7)
                {
                    if (value == 0 || value == 12 || value == 13)
                    {
                        triangles.AddRange(BackQuad);
                    }
                }
                //leaves
                else if (_this == 12)
                {
                    if (value == 0 || value == 7 || value == 12 || value == 13)
                    {
                        triangles.AddRange(BackQuad);
                    }
                }
                //glass
                else if (_this == 13)
                {
                    if (value == 0 || value == 7 || value == 12)
                    {
                        triangles.AddRange(BackQuad);
                    }
                }
                else
                {
                    triangles.AddRange(BackQuad);
                }
            }
        }

        //Front
        if (z < chunkSize - 1)
        {
            byte _front = this[x, y, z + 1];
            byte _this = this[x, y, z];
            if (_front == 0 || _front == 7 || _front == 12 || _front == 13)
            {
                //water
                if (_this == 7)
                {
                    if (_front == 0 || _front == 12 || _front == 13)
                    {
                        triangles.AddRange(FrontQuad);
                    }
                }
                //leaves
                else if (_this == 12)
                {
                    if (_front == 0 || _front == 7 || _front == 12 || _front == 13)
                    {
                        triangles.AddRange(FrontQuad);
                    }
                }
                //glass
                else if (_this == 13)
                {
                    if (_front == 0 || _front == 7 || _front == 12)
                    {
                        triangles.AddRange(FrontQuad);
                    }
                }
                else
                {
                    triangles.AddRange(FrontQuad);
                }
            }
        }
        else
        {
            int value = _voxelEngine.world[(int)transform.position.x + x, y, (int)transform.position.z + chunkSize];
            byte _this = this[x, y, z];
            if (value == 0 || value == 7 || value == 12 || value == 13)
            {
                //water
                if (_this == 7)
                {
                    if (value == 0 || value == 12 || value == 13)
                    {
                        triangles.AddRange(FrontQuad);
                    }
                }
                //leaves
                else if (_this == 12)
                {
                    if (value == 0 || value == 7 || value == 12 || value == 13)
                    {
                        triangles.AddRange(FrontQuad);
                    }
                }
                //glass
                else if (_this == 13)
                {
                    if (value == 0 || value == 7 || value == 12)
                    {
                        triangles.AddRange(FrontQuad);
                    }
                }
                else
                {
                    triangles.AddRange(FrontQuad);
                }
            }
        }
        
        return triangles;
    }

    long RoundUp(long n, long m)
    {
        return n >= 0 ? ((n + m - 1) / m) * m : (n / m) * m;
    }
}
