using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System;

public class VoxelEngine : MonoBehaviour
{
    [Header("World important variables")]
    //public variables
    public World world = new World();
    public GameObject selectCube;
    public Material opaqueMat;
    public Material transparentMat;
    public Material GenerationNoise;
    public int renderDistance;
    public int heightmapDistance;
    public int worldHeight;
    public int chunkSize;

    public static float WorldSeed;
    public float miningDistance;
    public int block = 1;

    //Private variables
    private System.Random _random = new System.Random();
    private int distanceToUnload;
    private bool loadChunks = false;
    private bool loadHeightmap = false;
    private Transform playerPos;
    private GameObject player;
    private GameObject _selectCube;
    private Camera _camera;
    void Awake()
    {
        FindNewSeed();
        _selectCube = Instantiate(selectCube, Vector3.zero, Quaternion.identity);
        _camera = Camera.main;
        playerPos = _camera.transform;
        player = GameObject.FindWithTag("Player");
        if ((player = GameObject.FindWithTag("Player")) != null)
            player.GetComponent<Movement>().enabled = false;
        //GenerateHeightmaps();
        StartCoroutine(WaitForHeightmap());
        worldHeight = 8 * chunkSize;
        distanceToUnload = (renderDistance * 32) + 16;
    }
    void LateUpdate()
    {
        UnloadChunks();
    }
    void Update()
    {
        if (loadHeightmap)
        { StartCoroutine(WaitForHeightmap()); };
        if (loadChunks)
        { StartCoroutine(WaitForLoadChunks()); }
        var ray = _camera.ScreenPointToRay(Input.mousePosition);
        bool res = int.TryParse(Input.inputString, out int num1);
        //id 1:7
        if (res)
            if (new int[] { 1, 2, 3, 4, 5, 6, 7 }.Contains(num1))
                block = GetPressedNumber();

        if (Physics.Raycast(ray, out RaycastHit hit, miningDistance))
        {
            _selectCube.SetActive(true);
            var p = hit.point - (hit.normal / 2f);
            float _x = Mathf.FloorToInt(p.x);
            float _y = Mathf.FloorToInt(p.y);
            float _z = Mathf.FloorToInt(p.z);
            Vector3 point = new Vector3(_x, _y, _z);
            _selectCube.transform.position = point;
            if (Input.GetMouseButtonUp(0))
            {
                p = hit.point - (hit.normal / 2f);
                int x = Mathf.FloorToInt(p.x);
                int y = Mathf.FloorToInt(p.y);
                int z = Mathf.FloorToInt(p.z);
                world[x, y, z] = 0;
                world.SetChunkDirty(x, z, false);
            }
            else if (Input.GetMouseButtonUp(1))
            {
                p = hit.point + (hit.normal / 2f);
                if (Vector3.Distance(hit.point, player.transform.position) > 1f)
                {
                    int x = Mathf.FloorToInt(p.x);
                    int y = Mathf.FloorToInt(p.y);
                    int z = Mathf.FloorToInt(p.z);
                    if (block > 0 && block < 8)
                    {
                        world[x, y, z] = (ushort)block;
                    }
                    world.SetChunkDirty(x, z, false);
                }
            }
        }
        else
        {
            _selectCube.SetActive(false);
        }
        if (Input.GetKeyUp(KeyCode.G))
        {
            StopAllCoroutines();
            ClearAllChunks();
            ClearAllHeightmaps();
            FindNewSeed();
            loadHeightmap = true;
        }
    }
    private int GetPressedNumber()
    {
        for (int number = 1; number <= 7; number++)
        {
            if (Input.GetKeyDown(number.ToString()))
                return number;
        }
        return -1;
    }

    private void UnloadChunks()
    {
        foreach (KeyValuePair<ChunkId, Chunk> chunk in world.Chunks)
        {
            Vector3 pos = new Vector3(playerPos.position.x, 0f, playerPos.position.z);
            if (Vector3.Distance(pos, chunk.Value.gameObject.transform.position) > distanceToUnload)
            {
                if (chunk.Value.saveable)
                {

                }
                world.Chunks.Remove(chunk.Key);
                Destroy(chunk.Value.gameObject);
                break;
            }
        }
    }
    private IEnumerator WaitForLoadChunks()
    {
        loadChunks = false;
        int x = (int)RoundDown((long)playerPos.position.x, 16) + 8;
        int z = (int)RoundDown((long)playerPos.position.z, 16) + 8;
        for (int i = 0; i <= renderDistance * 16; i += 8)
        {
            if (i == 0)
            {
                if (!world.Chunks.ContainsKey(ChunkId.FromWorldPos(x, z)))
                {
                    LoadChunks(x, z);
                    yield return new WaitForEndOfFrame();
                    continue;
                }
            }
            double radius = Math.Sqrt(i * i + i * i);
            for (int j = 0; j <= 360; j += 10)
            {
                double x1 = x + radius * Math.Cos(j * Math.PI / 180);
                double z1 = z + radius * Math.Sin(j * Math.PI / 180);
                if (!world.Chunks.ContainsKey(ChunkId.FromWorldPos((int)x1, (int)z1)))
                {
                    LoadChunks((int)x1, (int)z1);
                    yield return new WaitForEndOfFrame();
                }
            }
        }
        loadChunks = true;
    }

    private async void GenerateHeightmaps()
    {
        loadHeightmap = false;
        int _x = (int)RoundDown((long)playerPos.position.x, 128);
        int _z = (int)RoundDown((long)playerPos.position.z, 128);
        for (int i = -heightmapDistance; i <= heightmapDistance; i++)
        {
            for (int j = -heightmapDistance; j <= heightmapDistance; j++)
            {
                if (!world.Heightmap.ContainsKey(HeightmapId.FromWorldPos(_x + (i * 128), _z + (j * 128))))
                {
                    //await Task.Delay(5);
                    //await Task.Run(() => GenerateHeightmap(_x, _z, i, j));
                    await Task.Delay(5);
                    GenerateHeightmap(_x, _z, i, j);

                    //GenerateHeightmap(_x, _z, i ,j);
                    //await Task.Run(GenerateHeightmap());
                    //Heightmap hm = new Heightmap(_x + (i * 128), _z + (j * 128));
                    //world.Heightmap.Add(HeightmapId.FromWorldPos(_x + (i * 128), _z + (j * 128)), hm);
                    //yield return new WaitForEndOfFrame();
                }
            }
        }
        //loadChunks = true;
        loadHeightmap = true;
    }
    private void GenerateHeightmap(int _x, int _z, int i, int j)
    {
        Heightmap hm = new Heightmap(_x + (i * 128), _z + (j * 128));
        world.Heightmap.Add(HeightmapId.FromWorldPos(_x + (i * 128), _z + (j * 128)), hm);
        loadChunks = true;
    }
    private IEnumerator WaitForHeightmap()
    {
        loadHeightmap = false;
        int _x = (int)RoundDown((long)playerPos.position.x, 128);
        int _z = (int)RoundDown((long)playerPos.position.z, 128);
        for (int i = -heightmapDistance; i <= heightmapDistance; i++)
        {
            for (int j = -heightmapDistance; j <= heightmapDistance; j++)
            {
                if (!world.Heightmap.ContainsKey(HeightmapId.FromWorldPos(_x + (i * 128), _z + (j * 128))))
                {
                    Heightmap hm = new Heightmap(_x + (i * 128), _z + (j * 128));
                    world.Heightmap.Add(HeightmapId.FromWorldPos(_x + (i * 128), _z + (j * 128)), hm);
                    yield return new WaitForEndOfFrame();
                }
            }
        }
        loadChunks = true;
        yield return new WaitForSeconds(0.25f);
        loadHeightmap = true;
    }

    private void LoadChunks(int x1, int z1)
    {
        int _x = (int)RoundDown(x1, 16);
        int _z = (int)RoundDown(z1, 16);
        //int _x = (int)RoundDown((long)playerPos.position.x - xOffset, 16);
        //int _z = (int)RoundDown((long)playerPos.position.z - zOffset, 16);
        //if (!world.Chunks.ContainsKey(ChunkId.FromWorldPos(_x, _z)))
        //{  
        AddChunk(_x / 16, _z / 16);
        //}
    }

    /*
private void GenerateWorld()
{
    //GenerateChunks();
    //StartCoroutine(StartRender());
    //StartCoroutine(WaitForSetBlocks());
}

private IEnumerator WaitForSetBlocks()
{
    yield return new WaitForEndOfFrame();
    foreach (KeyValuePair<ChunkId, Chunk> chunk in _world.Chunks)
    {
        yield return new WaitForEndOfFrame();
        chunk.Value.SetCorrectBlocks();
    }
}
private void GenerateChunks()
{
    int dist = chunkRenderDistance;
    if (dist == 0)
        AddChunk(0, 0);
    else
    {
        for (int x = -dist; x < dist; x++)
        {
            for (int z = -dist; z < dist; z++)
            {
                AddChunk(x, z);
            }
        }
    }
}
    private IEnumerator StartRender()
    {
        yield return new WaitForEndOfFrame();
        foreach (KeyValuePair<ChunkId, Chunk> chunk in _world.Chunks)
        {
            chunk.Value.isDirty = true;
            yield return new WaitForEndOfFrame();
        }
    }
    */

    private void AddChunk(int x, int z)
    {
        var chunkGameObject = new GameObject($"Chunk {x}, {z}")
        { tag = "Chunk" };
        chunkGameObject.transform.parent = transform;
        chunkGameObject.transform.position = new Vector3(x * chunkSize, 0, z * chunkSize);
        var chunk = chunkGameObject.AddComponent<Chunk>();
        world.Chunks.Add(new ChunkId(x, z), chunk);
        world.SetChunkDirty(x * 16, z * 16, true);
        if (player != null && player.GetComponent<Movement>().enabled == false)
        {
            player.GetComponent<Movement>().enabled = true;
            player.transform.position = new Vector3(chunk.transform.position.x, 100, chunk.transform.position.z);
        }
    }
    private void FindNewSeed()
    {
        WorldSeed = _random.Next(0, 1000000);
    }
    private void ClearAllHeightmaps()
    {
        world.Heightmap.Clear();
    }
    private void ClearAllChunks()
    {
        foreach (KeyValuePair<ChunkId, Chunk> chunk in world.Chunks)
        {
            Destroy(chunk.Value.gameObject);
        }
        world.Chunks.Clear();
    }
    long RoundDown(long n, long m)
    {
        return n >= 0 ? (n / m) * m : ((n - m + 1) / m) * m;
    }
    long RoundUp(long n, long m)
    {
        return n >= 0 ? ((n + m - 1) / m) * m : (n / m) * m;
    }

}
