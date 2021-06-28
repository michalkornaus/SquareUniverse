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

    public int[] Cubes = new int[chunkHeight * chunkSize * chunkSize];
    public int this[int x, int y, int z]
    {
        get { return Cubes[x * chunkHeight * chunkSize + y * chunkSize + z]; }
        set { Cubes[x * chunkHeight * chunkSize + y * chunkSize + z] = value; }
    }

    public bool isDirty = false;
    public bool isUpdatable = false;
    public bool saveable = false;

    //COMPONENTS
    private System.Random _random = new System.Random();
    private Material[] materials = new Material[2];
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
        StartCoroutine(WaitForHeight());
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
    void Start()
    {
        materials[0] = _voxelEngine.opaqueMat;
        materials[1] = _voxelEngine.transparentMat;
        _meshFilter = GetComponent<MeshFilter>();
        _meshFilter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshCollider = GetComponent<MeshCollider>();
        _meshRenderer.materials = materials;
        _meshRenderer.receiveGI = ReceiveGI.Lightmaps;
        _meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
    }

    void Update()
    {
        //if (isUpdatable)
        //    StartCoroutine(WaitForUpdateBlocks());
        if (isDirty)
            RenderToMesh();
    }
    private IEnumerator WaitForUpdateBlocks()
    {
        isUpdatable = false;
        for (var x = 0; x < chunkSize; x++)
        {
            for (var y = 0; y < chunkHeight; y++)
            {
                for (var z = 0; z < chunkSize; z++)
                {
                    if (this[x, y, z] != 7)
                        continue;
                    if (this[x, y - 1, z] == 0)
                        this[x, y - 1, z] = 7;

                }
            }
        }
        yield return new WaitForSecondsRealtime(1f);
        isUpdatable = true;
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
            isUpdatable = true;
            /*for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    for (int y = 0; y < chunkHeight; y++)
                    {
                        //x 0:15 z 0:15 y 0:127
                        //.world[15+posX, 127, 15+posZ]
                        //this[x, y, z] = hm.ReturnCubes();
                    }
                }
            }*/
        }
        else
        {
            yield return new WaitForEndOfFrame();
            StartCoroutine(WaitForHeight());
        }

    }

    private void RenderToMesh()
    {
        isDirty = false;
        var vertices = new List<Vector3>();
        var opaqueTriangles = new List<int>();
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
                        { transparentTriangles.Add(tri + verticesPos); }
                        else
                        { opaqueTriangles.Add(tri + verticesPos); }
                    }
                    foreach (var normal in _cubeNormals)
                        normals.Add(normal);
                    switch (voxelType)
                    {
                        //Grass id:1
                        case 1:
                            foreach (var uv in Atlas.GetCustomUV(0, 0, 1))
                                uvs.Add(uv);
                            break;
                        //Dirt id:2
                        case 2:
                            foreach (var uv in Atlas.GetUV(400, 0))
                                uvs.Add(uv);
                            break;
                        //Stone id:3
                        case 3:
                            foreach (var uv in Atlas.GetUV(0, 200))
                                uvs.Add(uv);
                            break;
                        //Sand id:4
                        case 4:
                            foreach (var uv in Atlas.GetUV(200, 200))
                                uvs.Add(uv);
                            break;
                        //WoodenLog id:5
                        case 5:
                            foreach (var uv in Atlas.GetCustomUV(400, 200, 2))
                                uvs.Add(uv);
                            break;
                        //Leaves id:6
                        case 6:
                            foreach (var uv in Atlas.GetUV(400, 400))
                                uvs.Add(uv);
                            break;
                        //Water id:7
                        case 7:
                            foreach (var uv in Atlas.GetUV(200, 400))
                                uvs.Add(uv);
                            break;
                        //Dirt
                        default:
                            foreach (var uv in Atlas.GetUV(400, 0))
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
        mesh.Clear();
        mesh.ClearBlendShapes();
        mesh.subMeshCount = 2;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(opaqueTriangles, 0);
        collisionMesh.subMeshCount = 1;
        collisionMesh.SetVertices(vertices);
        collisionMesh.SetTriangles(opaqueTriangles, 0);
        if (transparentTriangles.Count > 0)
        {
            mesh.SetTriangles(transparentTriangles, 1);
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
            if (this[x, y - 1, z] == 0 || this[x, y - 1, z] == 7)
            {
                if (this[x, y, z] == 7)
                {
                    if (this[x, y - 1, z] != 7)
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
        {
            if (this[x, y, z] != 0)
                triangles.AddRange(BottomQuad);
        }
        //Top
        if (y < chunkHeight - 1)
        {
            if (this[x, y + 1, z] == 0 || this[x, y + 1, z] == 7)
            {
                if (this[x, y, z] == 7)
                {
                    if (this[x, y + 1, z] != 7)
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
        else if (y == chunkHeight - 1)
        {
            triangles.AddRange(TopQuad);
        }
        //Left
        if (x > 0)
        {
            if (this[x - 1, y, z] == 0 || this[x - 1, y, z] == 7)
            {
                if (this[x, y, z] == 7)
                {
                    if (this[x - 1, y, z] != 7)
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
            if (value == 0 || value == 7)
            {
                if (this[x, y, z] == 7)
                {
                    if (value != 7)
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
            if (this[x + 1, y, z] == 0 || this[x + 1, y, z] == 7)
            {
                if (this[x, y, z] == 7)
                {
                    if (this[x + 1, y, z] != 7)
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
            if (value == 0 || value == 7)
            {
                if (this[x, y, z] == 7)
                {
                    if (value != 7)
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
            if (this[x, y, z - 1] == 0 || this[x, y, z - 1] == 7)
            {
                if (this[x, y, z] == 7)
                {
                    if (this[x, y, z - 1] != 7)
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
            if (value == 0 || value == 7)
            {
                if (this[x, y, z] == 7)
                {
                    if (value != 7)
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
            if (this[x, y, z + 1] == 0 || this[x, y, z + 1] == 7)
            {
                if (this[x, y, z] == 7)
                {
                    if (this[x, y, z + 1] != 7)
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
            if (value == 0 || value == 7)
            {
                if (this[x, y, z] == 7)
                {
                    if (value != 7)
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
