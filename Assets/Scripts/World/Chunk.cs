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
    private static readonly int chunkHeight = 256;

    public ushort[] Cubes = new ushort[chunkHeight * chunkWidth * chunkWidth];
    public ushort this[int x, int y, int z]
    {
        get { return Cubes[x * chunkHeight * chunkWidth + y * chunkWidth + z]; }
        set { Cubes[x * chunkHeight * chunkWidth + y * chunkWidth + z] = value; }
    }
    public bool isDirty = false;
    private Heightmap _heightmap;
    //COMPONENTS
    private Material[] materials = new Material[4];
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private MeshCollider _meshCollider;
    private WorldController _worldController;
    private ChunkUtilities chunkUtils;
    public void SetVariables(Heightmap heightmap, WorldController worldController)
    {
        _worldController = worldController;
        chunkUtils = _worldController.GetComponent<ChunkUtilities>();
        _heightmap = heightmap;
    }
    private void Start()
    {
        materials[0] = _worldController.opaqueMaterial;
        materials[1] = _worldController.transparentMaterial;
        materials[2] = _worldController.waterMaterial;
        materials[3] = _worldController.noCollisionMaterial;
        _meshFilter = GetComponent<MeshFilter>();
        _meshFilter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        _meshFilter.mesh.MarkDynamic();
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshRenderer.materials = materials;
        _meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        _meshCollider = GetComponent<MeshCollider>();
        GetHeight();
    }
    private void Update()
    {
        if (isDirty)
        {
            isDirty = false;
            RenderToMesh();
        }
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
        Cubes = _heightmap.ReturnCubes(posX % heightmapSize, posZ % heightmapSize);
        _worldController.world.SetChunkDirty(_posX, _posZ, true);
    }
    private void RenderToMesh()
    {
        var vertices = new List<Vector3>();
        var opaqueTriangles = new List<int>();
        var waterTriangles = new List<int>();
        var transparentTriangles = new List<int>();
        var noCollisionTriangles = new List<int>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        for (var x = 0; x < chunkWidth; x++)
        {
            for (var z = 0; z < chunkWidth; z++)
            {
                for (var y = 0; y < chunkHeight; y++)
                {
                    var voxelType = this[x, y, z];
                    if (voxelType == (ushort)Blocks.Air)
                        continue;
                    if (voxelType < 500)
                    {
                        //Cube blocks logic
                        var _cubeTriangles = CalcTriangleFaces(x, y, z);
                        if (_cubeTriangles.Count == 0)
                            continue;
                        bool waterVoxel = voxelType / 10 == (ushort)Blocks.WaterSource / 10;
                        var pos = new Vector3(x, y, z);
                        var verticesPos = vertices.Count;
                        if (waterVoxel)
                        {
                            ushort end = (ushort)(voxelType % 10);
                            foreach (var vert in ChunkUtilities.waterStates[end])
                                vertices.Add(pos + vert);
                        }
                        else
                        {
                            foreach (var vert in ChunkUtilities._cubeVertices)
                                vertices.Add(pos + vert);
                        }
                        foreach (var tri in _cubeTriangles)
                        {
                            if (waterVoxel)
                            { waterTriangles.Add(tri + verticesPos); }
                            else if (voxelType == (ushort)Blocks.Glass)
                            { transparentTriangles.Add(tri + verticesPos); }
                            else
                            { opaqueTriangles.Add(tri + verticesPos); }
                        }
                        foreach (var normal in ChunkUtilities._cubeNormals)
                            normals.Add(normal);
                        Atlas.SetUV(voxelType, ref uvs);
                    }
                    else if (voxelType >= 500)
                    {
                        //custom mesh blocks (stairs, bushes etc.) 
                        Mesh _mesh;
                        Vector3 pos;
                        Quaternion qAngle;
                        var verticesPos = vertices.Count;        
                        switch (voxelType / 10)
                        {
                            case 50: //Stone Stairs
                            case 51: //Cobble Stairs
                                float angle = 90f * (voxelType % 10);
                                pos = angle switch
                                {
                                    90f => new Vector3(x + 1, y, z),
                                    180f => new Vector3(x, y, z),
                                    270f => new Vector3(x, y, z + 1),
                                    _ => new Vector3(x + 1, y, z + 1),
                                };
                                qAngle = Quaternion.AngleAxis(angle, Vector3.up);
                                _mesh = chunkUtils.customBlocks[(voxelType / 10) - 50].GetComponent<MeshFilter>().sharedMesh;
                                for (int i = 0; i < _mesh.vertices.Length; i++)
                                {
                                    vertices.Add(pos + qAngle * _mesh.vertices[i]);
                                }
                                foreach (var tri in _mesh.triangles)
                                {
                                    opaqueTriangles.Add(tri + verticesPos);
                                }
                                normals.AddRange(_mesh.normals);
                                uvs.AddRange(_mesh.uv);
                                break;
                            case 100: //grass bush
                            case 101: //short bush
                            case 102: //fruit bush
                            case 103: //flower1
                            case 104: //flower2
                            case 105: //flower3
                                pos = new Vector3(x + 1, y, z);
                                qAngle = Quaternion.AngleAxis(-90f, Vector3.right);
                                _mesh = chunkUtils.customBlocks[(voxelType / 10) - 98].GetComponent<MeshFilter>().sharedMesh;
                                for (int i = 0; i < _mesh.vertices.Length; i++)
                                {
                                    vertices.Add(pos + qAngle * _mesh.vertices[i]);
                                }
                                foreach (var tri in _mesh.triangles)
                                {
                                    noCollisionTriangles.Add(tri + verticesPos);
                                }
                                normals.AddRange(_mesh.normals);
                                uvs.AddRange(_mesh.uv);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }
        Mesh mesh = _meshFilter.mesh;
        mesh.Clear();
        Mesh collisionMesh = new();
        collisionMesh.MarkDynamic();
        collisionMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.subMeshCount = 4;
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
        if (noCollisionTriangles.Count > 0)
        {
            mesh.SetTriangles(noCollisionTriangles, 3);
        }
        mesh.SetUVs(0, uvs);
        mesh.SetNormals(normals);
        mesh.Optimize();
        mesh.RecalculateNormals();
        collisionMesh.Optimize();
        _meshCollider.sharedMesh = collisionMesh;
    }
    private void AddRange(ref List<int> triangles, ushort _this, ushort other, string quad)
    {
        var endThis = _this % 10;
        var endOther = other % 10;

        _this /= 10;
        other /= 10;

        //0 - air, 7 water, 12 glass, 17/18 stairs, 19-24 bushes
        if (other == 0 || other == 7 || other == 12 || other >= 50)
        {
            //water
            if (_this == 7)
            {
                switch (quad)
                {
                    case "Bottom":
                        if (other != 7)
                            triangles.AddRange(ChunkUtilities.BottomQuad);
                        break;
                    case "Top":
                        if (other != 7)
                        {
                            triangles.AddRange(ChunkUtilities.TopQuad);
                            if (other == 0)
                            {
                                triangles.AddRange(ChunkUtilities.BottomQuad);
                            }
                        }

                        break;
                    case "Left":
                        if (other != 7 || (other == 7 && endOther < endThis && endThis != 0) || (other == 7 && endThis == 0 && endThis != endOther))
                            triangles.AddRange(ChunkUtilities.LeftQuad);
                        break;
                    case "Right":
                        if (other != 7 || (other == 7 && endOther < endThis && endThis != 0) || (other == 7 && endThis == 0 && endThis != endOther))
                            triangles.AddRange(ChunkUtilities.RightQuad);
                        break;
                    case "Back":
                        if (other != 7 || (other == 7 && endOther < endThis && endThis != 0) || (other == 7 && endThis == 0 && endThis != endOther))
                            triangles.AddRange(ChunkUtilities.BackQuad);
                        break;
                    case "Front":
                        if (other != 7 || (other == 7 && endOther < endThis && endThis != 0) || (other == 7 && endThis == 0 && endThis != endOther))
                            triangles.AddRange(ChunkUtilities.FrontQuad);
                        break;
                }
            }
            //glass
            else if (_this == 12)
            {
                if (other == 0 || other == 7 || other >= 50)
                {
                    switch (quad)
                    {
                        case "Bottom":
                            triangles.AddRange(ChunkUtilities.BottomQuad);
                            break;
                        case "Top":
                            triangles.AddRange(ChunkUtilities.TopQuad);
                            break;
                        case "Left":
                            triangles.AddRange(ChunkUtilities.LeftQuad);
                            break;
                        case "Right":
                            triangles.AddRange(ChunkUtilities.RightQuad);
                            break;
                        case "Back":
                            triangles.AddRange(ChunkUtilities.BackQuad);
                            break;
                        case "Front":
                            triangles.AddRange(ChunkUtilities.FrontQuad);
                            break;
                    }
                }
            }
            else
            {
                switch (quad)
                {
                    case "Bottom":
                        triangles.AddRange(ChunkUtilities.BottomQuad);
                        break;
                    case "Top":
                        triangles.AddRange(ChunkUtilities.TopQuad);
                        break;
                    case "Left":
                        triangles.AddRange(ChunkUtilities.LeftQuad);
                        break;
                    case "Right":
                        triangles.AddRange(ChunkUtilities.RightQuad);
                        break;
                    case "Back":
                        triangles.AddRange(ChunkUtilities.BackQuad);
                        break;
                    case "Front":
                        triangles.AddRange(ChunkUtilities.FrontQuad);
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
            ushort below = this[x, y - 1, z];
            AddRange(ref triangles, this[x, y, z], below, "Bottom");
        }
        /*
        else
        {  triangles.AddRange(ChunkUtilities.BottomQuad); }*/

        //Top
        if (y < chunkHeight - 1)
        {
            ushort above = this[x, y + 1, z];
            AddRange(ref triangles, this[x, y, z], above, "Top");
        }
        else
        { triangles.AddRange(ChunkUtilities.TopQuad); }

        //Left
        if (x > 0)
        {
            ushort left = this[x - 1, y, z];
            AddRange(ref triangles, this[x, y, z], left, "Left");
        }
        else
        {
            ushort value = _worldController.world[(int)transform.position.x - 1, y, (int)transform.position.z + z];
            AddRange(ref triangles, this[x, y, z], value, "Left");
        }

        //Right
        if (x < chunkWidth - 1)
        {
            ushort right = this[x + 1, y, z];
            AddRange(ref triangles, this[x, y, z], right, "Right");
        }
        else
        {
            ushort value = _worldController.world[(int)transform.position.x + chunkWidth, y, (int)transform.position.z + z];
            AddRange(ref triangles, this[x, y, z], value, "Right");
        }

        //Back
        if (z > 0)
        {
            ushort back = this[x, y, z - 1];
            AddRange(ref triangles, this[x, y, z], back, "Back");
        }
        else
        {
            ushort value = _worldController.world[(int)transform.position.x + x, y, (int)transform.position.z - 1];
            AddRange(ref triangles, this[x, y, z], value, "Back");
        }

        //Front
        if (z < chunkWidth - 1)
        {
            ushort front = this[x, y, z + 1];
            AddRange(ref triangles, this[x, y, z], front, "Front");
        }
        else
        {
            ushort value = _worldController.world[(int)transform.position.x + x, y, (int)transform.position.z + chunkWidth];
            AddRange(ref triangles, this[x, y, z], value, "Front");
        }

        return triangles;
    }

    long RoundUp(long n, long m)
    {
        return n >= 0 ? ((n + m - 1) / m) * m : (n / m) * m;
    }
}
