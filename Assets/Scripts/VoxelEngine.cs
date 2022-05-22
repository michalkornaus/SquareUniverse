using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine.U2D;
using System.Linq;
using System.IO;

public class VoxelEngine : MonoBehaviour
{
    public static int WorldSeed;
    public World world = new World();

    [Header("Materials")]
    public Material opaqueMat;
    public Material waterMat;
    public Material transparentMat;
    public SpriteAtlas spriteAtlas;

    [Header("UI")]
    public GameObject selectSquare;
    public GameObject blockBorder;
    public Image blockImage;
    public Text blockText;
    public Text blockAmount;

    [Header("World settings")]
    public int heightmapDistance;
    public int worldHeight;
    public int chunkSize;
    public int seed;
    private int distanceToUnload;
    private int distanceToLoad;

    [Header("Player settings")]
    public int renderDistance;
    public float miningDistance;
    private GameObject player;
    private Player _player;
    //private List<byte> equipment;
    private Transform playerPos;
    private GameObject _selectSquare;
    private Camera _camera;

    [Header("Entities")]
    private int entityCount;
    public GameObject Sheep;
    public GameObject DesertCube;

    //Private variables
    private int index = 1;
    private bool loadChunks = false;
    private bool loadHeightmap = false;
    private bool spawnEntity = false;
    private bool setPlayer = false;
    private bool playerSet = false;

    void Awake()
    {
        FindNewSeed();
        _selectSquare = Instantiate(selectSquare, Vector3.zero, Quaternion.identity);
        _camera = Camera.main;
        playerPos = _camera.transform;
        player = GameObject.FindWithTag("Player");
        _player = player.GetComponent<Player>();

        worldHeight = 8 * chunkSize;
        distanceToLoad = 16 * 2;
        distanceToUnload = (renderDistance * 32) + 16;
    }
    private void Start()
    {
        if (LoadData())
        {
            StartCoroutine(WaitForHeightmap());
        }
        else
        {
            StartCoroutine(WaitForHeightmap());
        }
        for (int i = 0; i < _player.blocks.Length; i++)
        {
            _player.blocks[i] = 255;
        }
    }
    private void LateUpdate()
    {
        UnloadChunks();
    }
    private void UpdateUI()
    {
        if (!EmptyArray(_player.blocks))
        {
            blockBorder.SetActive(true);
            blockAmount.text = "x" + _player.blocks[index].ToString();
            blockImage.sprite = spriteAtlas.GetSprite(index.ToString());
            blockText.text = GetBlockText(index);
        }
        else
        {
            blockBorder.SetActive(false);
        }
    }
    private string GetBlockText(int id)
    {
        switch (id)
        {
            case 0:
                return "Air";
            case 1:
                return "Grass";
            case 2:
                return "Dirt";
            case 3:
                return "Stone";
            case 4:
                return "Sand";
            case 5:
                return "Log";
            case 6:
                return "Leaves";
            case 7:
                return "Water";
            case 8:
                return "Cobblestone";
            case 9:
                return "Planks";
            case 10:
                return "Clay";
            case 11:
                return "Bricks";
            case 12:
                return "Fancy Leaves";
            case 13:
                return "Glass";
            case 14:
                return "Coal Ore";
            case 15:
                return "Iron Ore";
            case 16:
                return "Workbench";
            case 17:
                return "Furnace";
            default:
                return "Null";
        }
    }
    private IEnumerator SpawnEntity(GameObject gameObject)
    {
        spawnEntity = false;
        Vector3 position = new Vector3(_camera.transform.position.x + Random.Range(-distanceToLoad, distanceToLoad), 200, _camera.transform.position.z + Random.Range(-distanceToLoad, distanceToLoad));
        if (world.GetBiom((int)position.x, (int)position.z) == 2)
        {
            if (Physics.Raycast(position, Vector3.down, out RaycastHit _hit))
            {
                position = _hit.point + new Vector3(0, 0.5f, 0);
                var rotation = Quaternion.Euler(0f, Random.Range(0.0f, 360.0f), 0f);
                Instantiate(gameObject, position, rotation);
                entityCount++;
            }
        }
        yield return new WaitForSeconds(5f);
        spawnEntity = true;
    }
    /*private IEnumerator SpawnBomb()
    {
        spawnEnemy = false;
        Vector3 position = new Vector3(_camera.transform.position.x + Random.Range(-distanceToLoad, distanceToLoad), 120, _camera.transform.position.z + Random.Range(-distanceToLoad, distanceToLoad));
        Instantiate(DesertCube, position, Quaternion.identity);
        yield return new WaitForSeconds(4f);
        spawnEnemy = true;
    }*/
    private void OnApplicationQuit()
    {
        SaveData();
    }
    private bool LoadData()
    {
        string dest = Application.persistentDataPath + "/" + seed + "/" + "player.dat";
        if (File.Exists(dest))
        {
            var br = new BinaryReader(File.OpenRead(dest));
            float _x = br.ReadSingle();
            float _y = br.ReadSingle();
            float _z = br.ReadSingle();
            player.transform.position = new Vector3(_x, _y, _z);
            _player.blocks = br.ReadBytes(20);
            br.Close();
            return true;
        }
        else
            return false;
    }
    private void SaveData()
    {
        string dest = Application.persistentDataPath + "/" + seed + "/" + "player.dat";
        string dir = Application.persistentDataPath + "/" + seed;
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        var bw = new BinaryWriter(File.Open(dest, FileMode.OpenOrCreate));
        bw.Write(player.transform.position.x);
        bw.Write(player.transform.position.y);
        bw.Write(player.transform.position.z);
        bw.Write(_player.blocks);
        bw.Flush();
        bw.Close();
    }
    private bool EmptyArray(byte[] array)
    {
        int counter = 0;
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == 0)
                counter++;
        }
        if (counter == array.Length)
            return true;
        else
            return false;
    }
    void Update()
    {
        if (loadHeightmap)
            StartCoroutine(WaitForHeightmap());
        if (loadChunks)
            StartCoroutine(WaitForLoadChunks());
        if (!playerSet && setPlayer)
        {
            /* FOR ENABLING/DISABLING PLAYER MOVEMENT */
            if ((player = GameObject.FindWithTag("Player")) != null)
                player.GetComponent<Movement>().enabled = true;
            playerSet = true;
            spawnEntity = true;
            if (Physics.Raycast(player.transform.position, Vector3.down, out RaycastHit _hit))
            {
                player.transform.position = _hit.point + new Vector3(0f, 0.5f, 0f);
            }
        }
        //SWITCHING NOCLIP/PLAYER MOVEMENT
        /*
        if (playerSet && Input.GetKeyUp(KeyCode.F))
        {
            if(player.GetComponent<Player>().isCreative)
            {
                player.transform.position = _camera.transform.position;
                player.transform.rotation = _camera.transform.rotation;
                player.GetComponent<Player>().isCreative = false;
                player.GetComponent<Movement>().enabled = true;
                player.GetComponent<CharacterController>().enabled = true;
                _camera.GetComponent<FreeCam>().enabled = false;
            }
            else
            {
                player.GetComponent<Player>().isCreative = true;
                player.GetComponent<Movement>().enabled = false;
                player.GetComponent<CharacterController>().enabled = false;
                _camera.GetComponent<FreeCam>().enabled = true;
            }
        }*/
        //SPAWNING ENTITIES
        if (playerSet && spawnEntity && entityCount <= 5)
        {
            StartCoroutine(SpawnEntity(Sheep));
        }
        var ray = _camera.ScreenPointToRay(Input.mousePosition);
        float axis = Input.GetAxis("Mouse ScrollWheel");
        if (axis != 0 && !EmptyArray(_player.blocks))
        {
            if (axis > 0)
            {
                do
                {
                    index = index + 1;
                    if (index >= _player.blocks.Length)
                        index = 0;
                } while (_player.blocks[index] == 0);
            }
            else
            {
                do
                {
                    index = index - 1;
                    if (index < 0)
                        index = _player.blocks.Length - 1;
                } while (_player.blocks[index] == 0);
            }
            UpdateUI();
        }
        bool res = int.TryParse(Input.inputString, out int num1);
        if (res)
        {
            if (new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }.Contains(num1))
            {
                index = num1;
                UpdateUI();
            }
        }
        if (Physics.Raycast(ray, out RaycastHit hit, miningDistance))
        {
            if (hit.collider.tag == "Chunk")
            {
                //HITING BLOCKS
                _selectSquare.SetActive(true);
                var p = hit.point - (hit.normal / 2f);
                float _x = Mathf.FloorToInt(p.x);
                float _y = Mathf.FloorToInt(p.y);
                float _z = Mathf.FloorToInt(p.z);
                Vector3 point = new Vector3(_x + 0.5f, _y + 0.5f, _z + 0.5f);
                _selectSquare.transform.position = point + (hit.normal / 2f);
                _selectSquare.transform.rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);
                if (Input.GetMouseButtonUp(0))
                {
                    p = hit.point - (hit.normal / 2f);
                    int x = Mathf.FloorToInt(p.x);
                    int y = Mathf.FloorToInt(p.y);
                    int z = Mathf.FloorToInt(p.z);
                    if (_player.blocks[world[x, y, z]] < 255)
                    {
                        _player.blocks[world[x, y, z]]++;
                    }
                    world[x, y, z] = 0;
                    world.SetChunkDirty(x, z, false);
                    UpdateUI();
                }
                else if (Input.GetMouseButtonUp(1))
                {
                    if (_player.blocks[index] > 0)
                    {
                        p = hit.point + (hit.normal / 2f);
                        if (Vector3.Distance(hit.point, player.transform.position) > 1f)
                        {
                            int x = Mathf.FloorToInt(p.x);
                            int y = Mathf.FloorToInt(p.y);
                            int z = Mathf.FloorToInt(p.z);
                            _player.blocks[index]--;
                            world[x, y, z] = (byte)index;
                            world.SetChunkDirty(x, z, false);
                            UpdateUI();
                        }
                    }
                }
            }
            else
            {
                //HITTING ENTITIES
                _selectSquare.SetActive(false);
            }
        }
        else
        {
            _selectSquare.SetActive(false);
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

    private void UnloadChunks()
    {
        foreach (KeyValuePair<ChunkId, Chunk> chunk in world.Chunks)
        {
            Vector3 pos = new Vector3(playerPos.position.x, 0f, playerPos.position.z);
            if (Vector3.Distance(pos, chunk.Value.gameObject.transform.position) > distanceToUnload)
            {
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
                    LoadChunks(x, z);
            }
            else
            {
                double radius = Mathf.Sqrt(i * i + i * i);
                for (int j = 0; j <= 360; j += 10)
                {
                    double x1 = x + radius * Mathf.Cos(j * Mathf.PI / 180f);
                    double z1 = z + radius * Mathf.Sin(j * Mathf.PI / 180f);
                    if (!world.Chunks.ContainsKey(ChunkId.FromWorldPos((int)x1, (int)z1)))
                    {
                        LoadChunks((int)x1, (int)z1);
                        yield return new WaitForEndOfFrame();
                    }
                }
            }
        }
        loadChunks = true;
    }

    private IEnumerator WaitForHeightmap()
    {
        loadHeightmap = false;
        int _x = (int)RoundDown((long)playerPos.position.x, 128);
        int _z = (int)RoundDown((long)playerPos.position.z, 128);
        if (heightmapDistance == 0)
        {
            if (!world.Heightmap.ContainsKey(HeightmapId.FromWorldPos(_x, _z)))
            {
                yield return new WaitForEndOfFrame();
                Heightmap hm = new Heightmap(_x, _z);
                world.Heightmap.Add(HeightmapId.FromWorldPos(_x, _z), hm);
            }
        }
        else
        {
            for (int i = -heightmapDistance; i <= heightmapDistance; i++)
            {
                for (int j = -heightmapDistance; j <= heightmapDistance; j++)
                {
                    if (!world.Heightmap.ContainsKey(HeightmapId.FromWorldPos(_x + (i * 128), _z + (j * 128))))
                    {
                        yield return new WaitForEndOfFrame();
                        Heightmap hm = new Heightmap(_x + (i * 128), _z + (j * 128));
                        world.Heightmap.Add(HeightmapId.FromWorldPos(_x + (i * 128), _z + (j * 128)), hm);
                    }
                }
            }
        }
        loadChunks = true;
        yield return new WaitForSeconds(0.5f);
        loadHeightmap = true;
        setPlayer = true;
    }

    private void LoadChunks(int x1, int z1)
    {
        int _x = (int)RoundDown(x1, 16);
        int _z = (int)RoundDown(z1, 16);
        AddChunk(_x / 16, _z / 16);
    }

    private void AddChunk(int x, int z)
    {
        var chunkGameObject = new GameObject($"Chunk {x}, {z}")
        { tag = "Chunk" };
        chunkGameObject.transform.parent = transform;
        chunkGameObject.transform.position = new Vector3(x * chunkSize, 0, z * chunkSize);
        var chunk = chunkGameObject.AddComponent<Chunk>();
        world.Chunks.Add(new ChunkId(x, z), chunk);
        world.SetChunkDirty(x * 16, z * 16, true);
    }
    private void FindNewSeed()
    {
        WorldSeed = seed;
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
}
