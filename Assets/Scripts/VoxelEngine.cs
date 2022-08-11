using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using System.Linq;
using System.IO;
using TMPro;

public class VoxelEngine : MonoBehaviour
{
    public World world = new();

    [Header("Materials")]
    public Material opaqueMat;
    public Material waterMat;
    public Material transparentMat;
    public SpriteAtlas spriteAtlas;

    [Header("User Interface")]
    [Header("Block UI")]
    public GameObject blockBorder;
    public Image blockImage;
    public Text blockText;
    public Text blockAmount;

    [Header("Debug menu")]
    public GameObject debugPanel;
    public TMP_Text coordsPointingText;
    public TMP_Text blockPointingText;
    public TMP_Text biomPointingText;

    public TMP_Text coordsStandingText;
    public TMP_Text blockStandingText;
    public TMP_Text biomStandingText;

    public TMP_Text chunkText;
    private bool debugEnabled = false;

    [Header("World settings")]
    public int heightmapDistance;
    public int seed;
    //private world settings variables
    private static readonly int chunkWidth = 16;
    private int distanceToUnload;
    private int distanceToLoad;
    private int entityDistToLoad;

    [Header("Player settings")]
    public int renderDistance;
    public float miningDistance;
    public GameObject selectSquare;
    public GameObject waterPanel;
    //Private player variables
    private GameObject _selectSquare;

    private Transform player;
    private Player _player;
    private Movement _movement;
    private Camera cam;

    private bool playerSet = false;
    private bool playerLoaded = false;

    [Header("Entities")]
    private int entityCount;
    public GameObject Sheep;

    //Private variables
    private int index = 1;
    private bool loadChunks = false;
    private bool loadHeightmap = true;
    private bool spawnEntity = false;

    void Awake()
    {
        //Instantiating square which shows what block player is aiming at
        _selectSquare = Instantiate(selectSquare, Vector3.zero, Quaternion.identity);
        //setting variables
        cam = Camera.main;
        player = GameObject.FindWithTag("Player").transform;
        _player = player.GetComponent<Player>();
        _movement = player.GetComponent<Movement>();
        //calculating distances to load and unload entities and chunks
        distanceToLoad = renderDistance * 16;
        entityDistToLoad = 16 * 2;
        distanceToUnload = (renderDistance * 32) + 16;
    }
    private void Start()
    {
        playerLoaded = LoadData();
        StartCoroutine(WaitForLoadChunks(2.5f));
    }
    void Update()
    {
        if (loadHeightmap)
            StartCoroutine(WaitForHeightmap());
        if (loadChunks)
            StartCoroutine(WaitForLoadChunks(0.5f));

        if (!playerSet)
            SetPlayerPosition();

        MiningPlacingBlocks();
    }
    private void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            debugEnabled = !debugEnabled;
        }
        UpdateDebug();
        UnloadChunks();
    }
    private void FixedUpdate()
    {
        CheckForWater();
    }
    private void CheckForWater()
    {
        float radius = player.GetComponent<CharacterController>().radius;
        float heightOffset = player.GetComponent<CharacterController>().height / 2f;
        Vector3[] positions = new Vector3[4];
        positions[0] = new Vector3(player.position.x + radius, player.position.y - heightOffset, player.position.z);
        positions[1] = new Vector3(player.position.x - radius, player.position.y - heightOffset, player.position.z);
        positions[2] = new Vector3(player.position.x, player.position.y - heightOffset, player.position.z + radius);
        positions[3] = new Vector3(player.position.x, player.position.y - heightOffset, player.position.z - radius);
        _movement.SetPlayerInWater(PlayerInWater(positions));

        int _x = Mathf.FloorToInt(cam.transform.position.x);
        int _y = Mathf.FloorToInt(cam.transform.position.y);
        int _z = Mathf.FloorToInt(cam.transform.position.z);
        if (world[_x, _y, _z] == (byte)Blocks.Water)
            waterPanel.SetActive(true);
        else
            waterPanel.SetActive(false);
    }
    private bool PlayerInWater(Vector3[] pos)
    {
        for (int i = 0; i < pos.Length; i++)
        {
            if (world[Mathf.FloorToInt(pos[i].x), Mathf.FloorToInt(pos[i].y), Mathf.FloorToInt(pos[i].z)] == (byte)Blocks.Water)
            {
                return true;
            }
        }
        return false;
    }
    private void UpdateDebug()
    {
        if (debugEnabled)
        {
            debugPanel.SetActive(true);
            //Getting block coords player is pointing at
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.tag == "Chunk")
                {
                    var p = hit.point - (hit.normal / 2f);
                    int _x = Mathf.FloorToInt(p.x);
                    int _y = Mathf.FloorToInt(p.y);
                    int _z = Mathf.FloorToInt(p.z);
                    coordsPointingText.text = "x: " + _x + " y: " + _y + " z: " + _z;
                    byte id = world[_x, _y, _z];
                    byte biom = world[_x, _z];
                    blockPointingText.text = Enums.GetBlockName(id) + " ID[" + id + "]";
                    biomPointingText.text = Enums.GetBiomName(biom) + " ID[" + biom + "]";
                }
            }
            else
            {
                coordsPointingText.text = "";
                biomPointingText.text = "";
                blockPointingText.text = "";
            }
            //Getting block coords player is standing on
            if (Physics.Raycast(player.position, Vector3.down, out RaycastHit _hit))
            {
                if (_hit.collider.tag == "Chunk")
                {
                    var p = _hit.point - (_hit.normal / 2f);
                    int _x = Mathf.FloorToInt(p.x);
                    int _y = Mathf.FloorToInt(p.y);
                    int _z = Mathf.FloorToInt(p.z);
                    coordsStandingText.text = "x: " + _x + " y: " + _y + " z: " + _z;
                    byte id = world[_x, _y, _z];
                    byte biom = world[_x, _z];
                    blockStandingText.text = Enums.GetBlockName(id) + " ID[" + id + "]";
                    biomStandingText.text = Enums.GetBiomName(biom) + " ID[" + biom + "]";
                }
            }
            else
            {
                coordsStandingText.text = "";
                biomStandingText.text = "";
                blockStandingText.text = "";
            }
            //Getting chunk info
            var chunk = world.Chunks[ChunkId.FromWorldPos(Mathf.FloorToInt(player.position.x), Mathf.FloorToInt(player.position.z))];
            chunkText.text = chunk.name + "\nX: " + chunk.transform.position.x + ", Z: " + chunk.transform.position.z;
        }
        else
            debugPanel.SetActive(false);
    }
    private void UpdateUI()
    {
        if (!EmptyArray(_player.blocks))
        {
            blockBorder.SetActive(true);
            blockAmount.text = "x" + _player.blocks[index].ToString();
            blockImage.sprite = spriteAtlas.GetSprite(index.ToString());
            blockText.text = Enums.GetBlockName(index);
        }
        else
        {
            blockBorder.SetActive(false);
        }
    }
    private IEnumerator SpawnEntity(GameObject gameObject)
    {
        spawnEntity = false;
        Vector3 position = new Vector3(cam.transform.position.x + Random.Range(-entityDistToLoad, entityDistToLoad), 200, cam.transform.position.z + Random.Range(-entityDistToLoad, entityDistToLoad));
        if (world[(int)position.x, (int)position.z] == 2)
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
    private void OnApplicationQuit() //Saving player data on application quit 
    {
        SaveData();
    }
    private bool LoadData()
    {
        //Creating string with destination path where files are located
        string dest = Application.persistentDataPath + "/" + seed + "/" + "player.dat";
        if (File.Exists(dest))
        {
            var br = new BinaryReader(File.OpenRead(dest));
            //reading saved values using BinaryReader
            float _x = br.ReadSingle();
            float _y = br.ReadSingle();
            float _z = br.ReadSingle();

            float _xR = br.ReadSingle();
            float _yR = br.ReadSingle();

            //assigning loaded values to player position, rotation and saved blocks
            player.position = new Vector3(_x, _y, _z);
            cam.GetComponent<PlayerCamera>().SetRotation(_xR);
            player.GetComponent<Movement>().SetRotation(_yR);
            _player.blocks = br.ReadBytes(20);
            br.Close();
            return true;
        }
        else
            return false; // returning false if file doesn't exist
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
        bw.Write(player.position.x);
        bw.Write(player.position.y);
        bw.Write(player.position.z);

        bw.Write(player.rotation.eulerAngles.x);
        bw.Write(player.rotation.eulerAngles.y);

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
    private void SetPlayerPosition()
    {
        if (Physics.Raycast(player.position, Vector3.down, out RaycastHit _hit))
        {
            if (player != null)
            {
                player.GetComponent<Movement>().enabled = true;
                if (!playerLoaded)
                {
                    player.position = _hit.point + new Vector3(0f, 0.5f, 0f);
                }
                playerSet = true;
            }
            else
                Debug.Log("No player found!");
        }
    }
    private void MiningPlacingBlocks()
    {
        var ray = cam.ScreenPointToRay(Input.mousePosition);
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
    }

    private void UnloadChunks()
    {
        foreach (KeyValuePair<ChunkId, Chunk> chunk in world.Chunks)
        {
            Vector3 pos = new Vector3(player.position.x, 0f, player.position.z);
            if (Vector3.Distance(pos, chunk.Value.gameObject.transform.position) > distanceToUnload)
            {
                world.Chunks.Remove(chunk.Key);
                Destroy(chunk.Value.gameObject);
                break;
            }
        }
    }
    private void AddChunk(int x1, int z1)
    {
        int x = (int)RoundDown(x1, 16) / 16;
        int z = (int)RoundDown(z1, 16) / 16;
        var chunkGameObject = new GameObject($"Chunk {x}, {z}")
        { tag = "Chunk" };
        chunkGameObject.transform.parent = transform;
        chunkGameObject.transform.position = new Vector3(x * chunkWidth, 0, z * chunkWidth);
        var chunk = chunkGameObject.AddComponent<Chunk>();
        world.Chunks.Add(new ChunkId(x, z), chunk);
    }
    private IEnumerator WaitForLoadChunks(float delay)
    {
        loadChunks = false;
        yield return new WaitForSeconds(delay);
        int x = (int)RoundDown((long)player.position.x, 16) + 8;
        int z = (int)RoundDown((long)player.position.z, 16) + 8;
        for (int i = 0; i <= distanceToLoad; i += 8)
        {
            if (i == 0)
            {
                if (world.Heightmaps.ContainsKey(HeightmapId.FromWorldPos(x, z)) && world.Heightmaps[HeightmapId.FromWorldPos(x, z)].IsDone && !world.Chunks.ContainsKey(ChunkId.FromWorldPos(x, z)))
                {
                    AddChunk(x, z);

                }
            }
            else
            {
                double radius = Mathf.Sqrt(i * i + i * i);
                for (int j = 0; j <= 360; j += 5)
                {
                    int x1 = (int)(x + radius * Mathf.Cos(j * Mathf.PI / 180f));
                    int z1 = (int)(z + radius * Mathf.Sin(j * Mathf.PI / 180f));
                    if (world.Heightmaps.ContainsKey(HeightmapId.FromWorldPos(x1, z1)) && world.Heightmaps[HeightmapId.FromWorldPos(x1, z1)].IsDone && !world.Chunks.ContainsKey(ChunkId.FromWorldPos(x1, z1)))
                    {
                        AddChunk(x1, z1);
                        yield return new WaitForEndOfFrame();
                    }
                }
            }

        }
        yield return new WaitForSeconds(1f);
        loadChunks = true;
    }
    private IEnumerator WaitForHeightmap()
    {
        loadHeightmap = false;
        int _x = (int)RoundDown((long)player.position.x, 128);
        int _z = (int)RoundDown((long)player.position.z, 128);
        if (heightmapDistance == 0)
        {
            if (!world.Heightmaps.ContainsKey(HeightmapId.FromWorldPos(_x, _z)))
            {
                Heightmap hm = new Heightmap(_x, _z, seed);
                world.Heightmaps.Add(HeightmapId.FromWorldPos(_x, _z), hm);
                yield return StartCoroutine(hm.WaitFor());
                hm.SaveData();
            }
        }
        else
        {
            for (int i = -heightmapDistance; i <= heightmapDistance; i++)
            {
                for (int j = -heightmapDistance; j <= heightmapDistance; j++)
                {
                    if (!world.Heightmaps.ContainsKey(HeightmapId.FromWorldPos(_x + (i * 128), _z + (j * 128))))
                    {
                        Heightmap hm = new(_x + (i * 128), _z + (j * 128), seed);
                        world.Heightmaps.Add(HeightmapId.FromWorldPos(_x + (i * 128), _z + (j * 128)), hm);
                        yield return StartCoroutine(hm.WaitFor());
                        hm.SaveData();
                    }
                }
            }
        }
        yield return new WaitForSeconds(1f);
        loadHeightmap = true;
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
