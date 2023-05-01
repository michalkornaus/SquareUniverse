using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.AI.Navigation;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using System.IO;
using System;

public class WorldController : MonoBehaviour
{
    private System.Random random = new System.Random();
    private SceneManager sceneManager;
    public World world = new();

    [Header("Materials")]
    public Material opaqueMaterial;
    public Material transparentMaterial;
    public Material noCollisionMaterial;
    public Material waterMaterial;

    [Header("User Interface")]
    public GameObject loadingPanel;
    public Block[] BlocksList;
    public Tool[] ToolsList;
    public ItemDraggable ItemPrefab;

    [Header("World settings")]
    public int heightmapDistance;

    //private world settings variables
    private static readonly int chunkWidth = 16;
    private int distanceToUnload;
    private int distanceToLoad;


    [Header("Player settings")]
    public int renderDistance;
    public float miningDistance;
    public GameObject ghostBlock;
    public GameObject hoverQuad;
    public GameObject waterPanel;
    public Slider slider;

    //Private player variables
    private GameObject _selectSquare;
    private GameObject _ghostBlock;
    private Transform player;
    private Player _player;
    private PlayerNeeds _playerNeeds;
    private PlayerMovement _movement;
    private CharacterController cc;
    private Camera cam;
    private bool playerSet = false;
    private bool playerLoaded = false;

    [SerializeField]
    private GameObject chunksNavMesh;
    [SerializeField]
    private GameObject chunksRest;
    private NavMeshSurface navMesh;
    private bool buildNav = false;
    private bool updateNav = false;

    [Header("Entities")]
    public GameObject[] entites;
    public List<Entity> entitiesList = new List<Entity>();
    private float pushForce = 25;
    private bool spawnEntity = false;
    private bool newEntites = true;
    private int entityCount = 15;
    private int entityDistToLoad;
    private float timerHit;


    //Private variables
    private int blockIndex = 10;
    private int blockRotation = 0;
    private bool loadChunks = false;
    private bool loadHeightmap = false;

    private const float tickTimerMax = 0.5f;
    private float tickTimer;

    //Block mining timers
    private const float timerBlockMax = 10f;
    private Vector3 prevBlock = Vector3.zero;
    private float timerBlock;

    private List<Vector3> waterBlocks = new List<Vector3>();

    //Ghost cube variables
    private ChunkUtilities chunkUtils;
    void Awake()
    {
        loadingPanel.SetActive(true);
        sceneManager = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();

        chunkUtils = GetComponent<ChunkUtilities>();
        //Instantiating square which shows what block player is aiming at
        _selectSquare = Instantiate(hoverQuad, Vector3.zero, Quaternion.identity);

        //Instantiating ghost cube which shows block where player is aiming to help see that it will look like
        _ghostBlock = Instantiate(ghostBlock, Vector3.zero, Quaternion.identity);
        SetupGhostCube();

        //setting variables

        cam = Camera.main;
        navMesh = GetComponentInChildren<NavMeshSurface>();
        player = GameObject.FindWithTag("Player").transform;
        _player = player.GetComponent<Player>();
        _movement = player.GetComponent<PlayerMovement>();
        _playerNeeds = player.GetComponent<PlayerNeeds>();
        cc = player.GetComponent<CharacterController>();

        //calculating distances to load and unload entities and chunks
        distanceToLoad = renderDistance * 16;
        entityDistToLoad = 16 * 3;
        distanceToUnload = (renderDistance * 16) + 96;
    }
    private void Start()
    {
        StartCoroutine(WaitForHeightmap(0f));
        playerLoaded = _player.LoadPlayerData();
        if (playerLoaded)
            StartCoroutine(WaitForLoadChunks(4f, 1.5f));
        else
            StartCoroutine(WaitForLoadChunks(8f, 0f));

        LoadEntities();
        InvokeRepeating("UpdateNav", 2.5f, 10f);
    }
    private void UpdateNav()
    {
        updateNav = true;
    }
    void Update()
    {
        if (loadHeightmap)
            StartCoroutine(WaitForHeightmap(2f));
        if (loadChunks)
            StartCoroutine(WaitForLoadChunks(0.5f, 1.5f));

        MiningPlacingBlocks();

        if (Input.GetKeyDown(KeyCode.R))
        {
            blockRotation = (ushort)(blockRotation == Enums.GetRotationsCount(blockIndex) ? 0 : blockRotation + 1);
            UpdateGhostBlock();
        }

        tickTimer += Time.deltaTime;
        if (tickTimer > tickTimerMax)
        {
            //One tick per 0.5 seconds
            tickTimer -= tickTimerMax;

            if (!playerSet)
                SetPlayerPosition();

            CheckForWater();

            CheckForEndOfWorld();

            if (buildNav)
            {
                navMesh.BuildNavMesh();
                buildNav = false;
            }
            if (updateNav && navMesh.navMeshData != null)
            {
                navMesh.UpdateNavMesh(navMesh.navMeshData);
                updateNav = false;
            }
            if (spawnEntity && newEntites)
            {
                StartCoroutine(SpawnEntites());
            }
            /*
            if (waterBlocks.Count > 0)
            {
                //DynamicWaterBlocks();
                //CheckWaterBlocks();
            }*/
        }
    }
    private void CheckForEndOfWorld()
    {
        if (player.position.y < -10)
        {
            _player.OnTakenDamage(20);
        }
    }
    private IEnumerator SpawnEntites()
    {
        newEntites = false;
        yield return new WaitForSeconds(5f);
        if (entitiesList.Count <= entityCount)
        {
            int valueX = random.Next(-entityDistToLoad, entityDistToLoad);
            int valueZ = random.Next(-entityDistToLoad, entityDistToLoad);
            Vector3 position = new(player.position.x + valueX, 300, player.position.z + valueZ);
            if (Physics.Raycast(position, Vector3.down, out RaycastHit _hit, 400))
            {
                if (Vector3.Distance(_hit.transform.position, player.position) > 10)
                {
                    var p = _hit.point - (_hit.normal / 2f);
                    int x = Mathf.FloorToInt(p.x);
                    int y = Mathf.FloorToInt(p.y);
                    int z = Mathf.FloorToInt(p.z);
                    if (world[x, y, z] == (ushort)Blocks.Grass && world[x, y + 1, z] != (ushort)Blocks.WaterSource)
                    {
                        p = _hit.point + (_hit.normal / 2f);
                        x = Mathf.FloorToInt(p.x);
                        y = Mathf.FloorToInt(p.y);
                        z = Mathf.FloorToInt(p.z);
                        position = new Vector3(x, y, z);
                        var randomRotation = Quaternion.Euler(0, random.Next(0, 360), 0);
                        int prefabID = random.Next(0, entites.Length);
                        Entity entity = Instantiate(entites[prefabID], position, randomRotation).GetComponent<Entity>();
                        entity.PrefabID = prefabID;
                        entitiesList.Add(entity);
                        Debug.Log("Spawned entity: " + entity.name);
                    }
                    else
                    {
                        Debug.Log("Tried to spawn entity on wrong block");
                    }
                }
                else
                {
                    Debug.Log("Tried to spawn entity too close to player");
                }
            }
            else
            {
                Debug.Log("Can't find blocks");
            }
            yield return new WaitForSeconds(10f);
            newEntites = true;
        }
    }
    private void LateUpdate()
    {
        if (playerSet)
            UnloadChunks();
    }
    private void DynamicWaterBlocks()
    {
        foreach (var item in waterBlocks)
        {
            int x = (int)item.x;
            int y = (int)item.y;
            int z = (int)item.z;

            int waterValue = world[x, y, z] % 10;

            /*WaterSource = 70, //1.0f
            Water_1 = 79,       //0.9f
            Water_2 = 78,       //0.8f
            Water_3 = 77,       //0.7f
            Water_4 = 76,       //0.6f
            Water_5 = 75,       //0.5f
            Water_6 = 74,       //0.4f
            Water_7 = 73,       //0.3f
            Water_8 = 72,       //0.2f
            Water_9 = 71,       //0.1f*/

            if (waterValue <= 0)
            {
                //x-1
                if (world[x - 1, y, z] == 0 || (world[x - 1, y, z] != (ushort)Blocks.WaterSource && world[x - 1, y, z] / 10 == 7))
                {
                    world[x - 1, y, z] = (ushort)Blocks.Water_1;
                    world.SetChunkDirty(x - 1, z, false);
                    if (!waterBlocks.Contains(new Vector3(x - 1, y, z)))
                        waterBlocks.Add(new Vector3(x - 1, y, z));
                    waterBlocks.Remove(item);
                    break;
                }
                //x+1
                if (world[x + 1, y, z] == 0 || (world[x + 1, y, z] != (ushort)Blocks.WaterSource && world[x + 1, y, z] / 10 == 7))
                {
                    world[x + 1, y, z] = (ushort)Blocks.Water_1;
                    world.SetChunkDirty(x + 1, z, false);
                    if (!waterBlocks.Contains(new Vector3(x + 1, y, z)))
                        waterBlocks.Add(new Vector3(x + 1, y, z));
                    waterBlocks.Remove(item);
                    break;
                }
                //z-1
                if (world[x, y, z - 1] == 0 || (world[x, y, z - 1] != (ushort)Blocks.WaterSource && world[x, y, z - 1] / 10 == 7))
                {
                    world[x, y, z - 1] = (ushort)Blocks.Water_1;
                    world.SetChunkDirty(x, z - 1, false);
                    if (!waterBlocks.Contains(new Vector3(x, y, z - 1)))
                        waterBlocks.Add(new Vector3(x, y, z - 1));
                    waterBlocks.Remove(item);
                    break;
                }
                //z+1
                if (world[x, y, z + 1] == 0 || (world[x, y, z + 1] != (ushort)Blocks.WaterSource && world[x, y, z + 1] / 10 == 7))
                {
                    world[x, y, z + 1] = (ushort)Blocks.Water_1;
                    world.SetChunkDirty(x, z + 1, false);
                    if (!waterBlocks.Contains(new Vector3(x, y, z + 1)))
                        waterBlocks.Add(new Vector3(x, y, z + 1));
                    waterBlocks.Remove(item);
                    break;
                }
            }
            else if (waterValue >= 1)
            {
                ushort newLevel = (ushort)(Blocks.WaterSource + Mathf.CeilToInt(waterValue / 2f));
                if (newLevel % 10 > 0)
                {
                    //x-1
                    if (world[x - 1, y, z] == 0 || (world[x - 1, y, z] != (ushort)Blocks.WaterSource && world[x - 1, y, z] / 10 == 7 && world[x - 1, y, z] % 10 != waterValue))
                    {
                        if (world[x - 1, y, z] != 0)
                            newLevel = (ushort)(Blocks.WaterSource + Mathf.CeilToInt((waterValue + world[x - 1, y, z] % 10) / 2));
                        world[x, y, z] = newLevel;
                        world[x - 1, y, z] = newLevel;
                        world.SetChunkDirty(x, z, false);
                        world.SetChunkDirty(x - 1, z, false);
                        if (newLevel % 10 >= 1)
                        {
                            if (!waterBlocks.Contains(new Vector3(x - 1, y, z)))
                            {
                                waterBlocks.Add(new Vector3(x - 1, y, z));
                                waterBlocks.Add(new Vector3(x, y, z));
                                break;
                            }
                        }
                    }
                    else if (world[x - 1, y, z] == (ushort)Blocks.WaterSource)//&& world[x, y, z] != (ushort)Blocks.Water_1)
                    {
                        world[x, y, z] = (ushort)Blocks.WaterSource;
                        world.SetChunkDirty(x, z, false);
                    }


                    //x+1
                    if (world[x + 1, y, z] == 0 || (world[x + 1, y, z] != (ushort)Blocks.WaterSource && world[x + 1, y, z] / 10 == 7 && world[x + 1, y, z] % 10 != waterValue))
                    {
                        if (world[x + 1, y, z] != 0)
                            newLevel = (ushort)(Blocks.WaterSource + Mathf.CeilToInt((waterValue + world[x + 1, y, z] % 10) / 2));
                        world[x, y, z] = newLevel;
                        world[x + 1, y, z] = newLevel;
                        world.SetChunkDirty(x, z, false);
                        world.SetChunkDirty(x + 1, z, false);
                        if (newLevel % 10 >= 1)
                            if (!waterBlocks.Contains(new Vector3(x + 1, y, z)))
                            {
                                waterBlocks.Add(new Vector3(x, y, z));
                                waterBlocks.Add(new Vector3(x + 1, y, z));
                                break;
                            }
                    }
                    else if (world[x + 1, y, z] == (ushort)Blocks.WaterSource)//&& world[x, y, z] != (ushort)Blocks.Water_1)
                    {
                        world[x, y, z] = (ushort)Blocks.WaterSource;
                        world.SetChunkDirty(x, z, false);
                    }
                }
            }
            waterBlocks.Remove(item);
            break;
        }
    }
    private void CheckWaterBlocks()
    {
        foreach (var item in waterBlocks)
        {
            //this block is water
            int x = (int)item.x;
            int y = (int)item.y;
            int z = (int)item.z;

            int waterValue = world[x, y, z] % 10;
            world.SetChunkDirty(x, z, false);
            if (waterValue < 8)
            {
                if (world[x, y - 1, z] / 10 == (ushort)Blocks.WaterSource / 10)
                {
                    waterBlocks.Remove(item);
                    break;
                }
                else
                {
                    if (world[x, y - 1, z] == 0)
                    {
                        world[x, y - 1, z] = (ushort)Blocks.Water_1;
                        waterBlocks.Add(new Vector3(x, y - 1, z));
                        waterBlocks.Remove(item);
                        break;
                    }
                    if (world[x - 1, y, z] == 0)
                    {
                        world[x - 1, y, z] = (ushort)(Blocks.Water_1 + waterValue);
                        //world.SetChunkDirty(x, z, false);
                        waterBlocks.Add(new Vector3(x - 1, y, z));
                    }
                    if (world[x, y, z - 1] == 0)
                    {
                        world[x, y, z - 1] = (ushort)(Blocks.Water_1 + waterValue);
                        //world.SetChunkDirty(x, z, false);
                        waterBlocks.Add(new Vector3(x, y, z - 1));
                    }
                    if (world[x + 1, y, z] == 0)
                    {
                        world[x + 1, y, z] = (ushort)(Blocks.Water_1 + waterValue);
                        //world.SetChunkDirty(x, z, false);
                        waterBlocks.Add(new Vector3(x + 1, y, z));
                    }
                    if (world[x, y, z + 1] == 0)
                    {
                        world[x, y, z + 1] = (ushort)(Blocks.Water_1 + waterValue);
                        //world.SetChunkDirty(x, z, false);
                        waterBlocks.Add(new Vector3(x, y, z + 1));
                    }
                }
            }
            waterBlocks.Remove(item);
            break;
        }
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
        if (player.position.y > 0 && player.position.y < 255)
        {
            _movement.SetPlayerInWater(PlayerInWater(positions));
            int _x = Mathf.FloorToInt(cam.transform.position.x);
            int _y = Mathf.FloorToInt(cam.transform.position.y);
            int _z = Mathf.FloorToInt(cam.transform.position.z);
            if (world[_x, _y, _z] / 10 == (ushort)Blocks.WaterSource / 10)
            {
                waterPanel.SetActive(true);
                cam.farClipPlane = 25;
                cam.clearFlags = CameraClearFlags.SolidColor;
                RenderSettings.fogColor = new Color(0.2085f, 0.3436f, 0.6226f);
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = 0.11f;
            }
            else
            {
                waterPanel.SetActive(false);
                cam.farClipPlane = 350;
                cam.clearFlags = CameraClearFlags.Skybox;
                RenderSettings.fogColor = new Color(0.7411f, 0.8196f, 0.8627f);
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogStartDistance = 150;
                RenderSettings.fogEndDistance = 220;
            }
        }
        else
        {
            waterPanel.SetActive(false);
            cam.farClipPlane = 350;
            cam.clearFlags = CameraClearFlags.Skybox;
            RenderSettings.fogColor = new Color(0.7411f, 0.8196f, 0.8627f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 150;
            RenderSettings.fogEndDistance = 220;
        }
    }
    private bool PlayerInWater(Vector3[] pos)
    {
        for (int i = 0; i < pos.Length; i++)
        {
            if (world[Mathf.FloorToInt(pos[i].x), Mathf.FloorToInt(pos[i].y), Mathf.FloorToInt(pos[i].z)] / 10 == (ushort)Blocks.WaterSource / 10)
            {
                return true;
            }
        }
        return false;
    }
    private void OnApplicationQuit() //Saving data on application quit 
    {
        SaveEntities();
        SaveHeightmapData();
    }
    private EntitySave[] CreateEntitiesSave()
    {
        EntitySave[] saves = new EntitySave[entitiesList.Count];
        for (int i = 0; i < entitiesList.Count; i++)
        {
            Entity entity = entitiesList[i];
            saves[i] = new();

            saves[i].EntityPrefabID = entity.PrefabID;

            saves[i].HealthPoints = entity.HealthPoints;

            saves[i].EntityPositionX = entity.transform.position.x;
            saves[i].EntityPositionY = entity.transform.position.y;
            saves[i].EntityPositionZ = entity.transform.position.z;

            saves[i].RotationY = entity.transform.rotation.eulerAngles.y;
        }
        return saves;
    }
    public void SaveEntities()
    {

        EntitySave[] savedEntities = CreateEntitiesSave();

        string dest = Application.persistentDataPath + "/" + sceneManager.GetWorldName() + "/" + "entities.dat";
        BinaryFormatter bf = new();
        FileStream file = File.Open(dest, FileMode.OpenOrCreate);
        bf.Serialize(file, savedEntities);
        file.Close();
    }
    private void LoadEntities()
    {
        string dest = Application.persistentDataPath + "/" + sceneManager.GetWorldName() + "/" + "entities.dat";
        if (File.Exists(dest))
        {
            BinaryFormatter bf = new();
            FileStream file = File.Open(dest, FileMode.Open);
            EntitySave[] savedEntities = (EntitySave[])bf.Deserialize(file);
            file.Close();
            //Load in the entities
            foreach (var entity in savedEntities)
            {
                Vector3 pos = new Vector3(entity.EntityPositionX, entity.EntityPositionY, entity.EntityPositionZ);
                Quaternion rot = Quaternion.Euler(0f, entity.RotationY, 0f);
                var _entity = Instantiate(entites[entity.EntityPrefabID], pos, rot).GetComponent<Entity>();
                _entity.HealthPoints = entity.HealthPoints;
                _entity.PrefabID = entity.EntityPrefabID;
                entitiesList.Add(_entity);
                Debug.Log("Spawned entity: " + _entity.name);
            }

        }
    }
    public void SaveHeightmapData()
    {
        foreach (var item in world.Heightmaps)
        {
            if (item.Value.saveable)
            {
                item.Value.SaveData();
            }
        }
    }
    private void AddTools()
    {
        for (int i = 0; i < 4; i++)
        {
            ItemDraggable item = Instantiate(ItemPrefab, _player.toolbar.Slots[i].transform);
            item.item = ToolsList[i]; item.amount = 1;
            item.UpdateItem();
            _player.toolbar.Slots[i].currentItem = item;
            _player.itemsBar[i] = item;
        }
    }
    private IEnumerator SpawnPlayer(RaycastHit _hit)
    {
        Debug.Log("Previous pos: " + player.position);
        if (world[Mathf.FloorToInt(_hit.point.x), Mathf.FloorToInt(_hit.point.y), Mathf.FloorToInt(_hit.point.z)] / 10 != (ushort)Blocks.WaterSource / 10)
        {
            //Spawning player when there is not a water block under him
            player.position = _hit.point + new Vector3(0f, 0.5f, 0f);
            Debug.Log("New pos: " + player.position);

            _movement.enabled = true;
            cc.enabled = true;

            loadingPanel.SetActive(false);
            spawnEntity = true;

            Debug.Log("Spawned new player in!");
            _player.spawnPosition = player.position;
        }
        else
        {   //Else find spot where there is not any water blocks
            Vector3 newPos = _hit.point + new Vector3(0f, 10f, 0f);
            bool findSpot = false;
            int count = 0;
            while (findSpot == false)
            {
                foreach (KeyValuePair<HeightmapId, Heightmap> heightmap in world.Heightmaps)
                {
                    if (heightmap.Value.spawnPosition != Vector3.zero)
                    {
                        findSpot = true;
                        newPos = heightmap.Value.spawnPosition + new Vector3(0f, 10f, 0f);
                        Debug.Log("Found new spawn position for player");
                        break;
                    }
                    else
                    {
                        count++;
                        yield return new WaitForEndOfFrame();
                    }
                }
                if (count > 500)
                {
                    Debug.Log("Too much Heightmap searches - breaking!");
                    break;
                }
            }
            bool hasGround = false;
            count = 0;
            LayerMask mask = LayerMask.GetMask("Chunks");
            yield return new WaitForSeconds(1f);
            while (hasGround == false)
            {
                if (Physics.Raycast(newPos, Vector3.down, out RaycastHit hit, 100, mask))
                {
                    player.position = hit.point + new Vector3(0f, 0.5f, 0f);
                    hasGround = true;
                }
                else
                {
                    count++;
                    yield return new WaitForEndOfFrame();
                }
                if (count > 5000)
                {
                    Debug.Log("Too much ground searches - breaking!");
                    player.position = _hit.point + new Vector3(0f, 0.5f, 0f);
                    break;
                }
            }
            Debug.Log("New pos: " + player.position);

            _movement.enabled = true;
            cc.enabled = true;

            loadingPanel.SetActive(false);
            spawnEntity = true;

            Debug.Log("Spawned new player in!");
            _player.spawnPosition = player.position;
        }
    }
    private void SetPlayerPosition()
    {
        if (player != null)
        {
            if (!playerLoaded)
            {
                LayerMask mask = LayerMask.GetMask("Chunks");
                if (Physics.Raycast(player.position, Vector3.down, out RaycastHit _hit, 300f, mask))
                {
                    playerSet = true;
                    //Add tools to new player 
                    AddTools();
                    StartCoroutine(SpawnPlayer(_hit));
                }
            }
            else
            {
                LayerMask mask = LayerMask.GetMask("Chunks");
                if (Physics.Raycast(player.position, Vector3.down, mask))
                {
                    playerSet = true;

                    _movement.enabled = true;
                    cc.enabled = true;

                    loadingPanel.SetActive(false);
                    spawnEntity = true;

                    Debug.Log("Loaded player in!");
                }
            }
        }
    }
    private void SetupGhostCube()
    {
        Mesh mesh = _ghostBlock.GetComponentInChildren<MeshFilter>().mesh;
        mesh.MarkDynamic();
    }
    private void UpdateGhostBlock()
    {
        Mesh mesh = _ghostBlock.GetComponentInChildren<MeshFilter>().mesh;
        mesh.Clear();
        if (blockIndex + blockRotation < 500)
        {
            var vertices = new List<Vector3>();
            foreach (var vert in ChunkUtilities._cubeVertices)
                vertices.Add(vert - new Vector3(0.5f, 0.5f, 0.5f));
            mesh.SetVertices(vertices);
            var triangles = new List<int>();
            triangles.AddRange(ChunkUtilities.BottomQuad);
            triangles.AddRange(ChunkUtilities.TopQuad);
            triangles.AddRange(ChunkUtilities.LeftQuad);
            triangles.AddRange(ChunkUtilities.RightQuad);
            triangles.AddRange(ChunkUtilities.BackQuad);
            triangles.AddRange(ChunkUtilities.FrontQuad);
            mesh.SetTriangles(triangles, 0);
            var normals = new List<Vector3>();
            foreach (var normal in ChunkUtilities._cubeNormals)
                normals.Add(normal);
            mesh.SetNormals(normals);

            var uvs = new List<Vector2>();
            Atlas.SetUV((ushort)(blockIndex + blockRotation), ref uvs);
            mesh.SetUVs(0, uvs);
        }
        else if (blockIndex + blockRotation >= 500)
        {
            Mesh _mesh;
            Vector3 pos;
            Quaternion qAngle;
            //custom mesh blocks (stairs, bushes etc.) 
            switch (blockIndex / 10)
            {
                case 50: //Stone Stairs
                case 51: //Cobble stairs
                    float angle = 90f * (blockRotation % 10);
                    pos = angle switch
                    {
                        90f => new Vector3(1, 0, 0),
                        180f => new Vector3(0, 0, 0),
                        270f => new Vector3(0, 0, 1),
                        _ => new Vector3(1, 0, 1),
                    };
                    qAngle = Quaternion.AngleAxis(angle, Vector3.up);
                    _mesh = chunkUtils.customBlocks[(blockIndex / 10) - 50].GetComponent<MeshFilter>().sharedMesh;
                    break;
                case 100: //grass bush
                case 101: //short bush
                case 102: //fruit bush
                case 103: //flower1
                case 104: //flower2
                case 105: //flower3
                    pos = new Vector3(1, 0, 0);
                    qAngle = Quaternion.AngleAxis(-90f, Vector3.right);
                    // -100 + 2 -> bushes start at the thirst (0,1,2) custom blocks index
                    _mesh = chunkUtils.customBlocks[(blockIndex / 10) - 98].GetComponent<MeshFilter>().sharedMesh;
                    break;
                default:
                    pos = Vector3.zero;
                    qAngle = Quaternion.AngleAxis(-90f, Vector3.right);
                    _mesh = chunkUtils.customBlocks[(blockIndex / 10) - 98].GetComponent<MeshFilter>().sharedMesh;
                    break;
            }
            var vertices = new List<Vector3>();
            for (int i = 0; i < _mesh.vertices.Length; i++)
            {
                vertices.Add(pos + qAngle * _mesh.vertices[i] - new Vector3(0.5f, 0.5f, 0.5f));
            }
            mesh.SetVertices(vertices);
            mesh.SetTriangles(_mesh.triangles, 0);
            mesh.SetNormals(_mesh.normals);
            mesh.SetUVs(0, _mesh.uv);
        }
    }
    public void UpdateRotation()
    {
        blockRotation = 0;
        timerBlock = 0f;
    }
    private void DestroyBlock(Block block, int x, int y, int z)
    {
        world[x, y, z] = 0;
        if (y < 256 && world[x, y + 1, z] / 10 >= (ushort)Blocks.GrassBush / 10)
        {
            world[x, y + 1, z] = 0;
        }
        world.SetChunkDirty(x, z, false);
        WaterNearby(x, y, z);
        updateNav = true;

        bool alreadyOwned = false;
        for (int i = 0; i < _player.itemsBar.Length; i++)
        {
            if (_player.itemsBar[i] != null && _player.itemsBar[i].item == block)
            {
                _player.itemsBar[i].amount++;
                _player.itemsBar[i].UpdateItem();
                alreadyOwned = true;
                break;
            }
        }
        if (!alreadyOwned)
        {
            for (int i = 0; i < _player.itemsBar.Length; i++)
            {
                if (_player.itemsBar[i] == null)
                {
                    ItemDraggable item = Instantiate(ItemPrefab, _player.toolbar.Slots[i].transform);
                    item.item = block; item.amount = 1;
                    item.UpdateItem();
                    _player.toolbar.Slots[i].currentItem = item;
                    _player.itemsBar[i] = item;
                    return;
                }
            }
            //if the toolbar is full, check the inventory blocks
            bool eqAlreadyOwned = false;
            for (int i = 0; i < _player.itemsEq.Length; i++)
            {
                if (_player.itemsEq[i] != null && _player.itemsEq[i].item == block)
                {
                    _player.itemsEq[i].amount++;
                    _player.itemsEq[i].UpdateItem();
                    eqAlreadyOwned = true;
                    break;
                }
            }
            if (!eqAlreadyOwned)
            {
                for (int i = 0; i < _player.itemsEq.Length; i++)
                {
                    if (_player.itemsEq[i] == null)
                    {
                        ItemDraggable item = Instantiate(ItemPrefab, _player.toolbar.InventorySlots[i].transform);
                        item.item = block; item.amount = 1;
                        item.UpdateItem();
                        _player.toolbar.InventorySlots[i].currentItem = item;
                        _player.itemsEq[i] = item;
                        break;
                    }
                }
            }
        }

    }

    private void WaterNearby(int x, int y, int z)
    {
        if (world[x, y + 1, z] / 10 == (ushort)Blocks.WaterSource / 10)
            waterBlocks.Add(new Vector3(x, y + 1, z));
        if (world[x - 1, y, z] / 10 == (ushort)Blocks.WaterSource / 10)
            waterBlocks.Add(new Vector3(x - 1, y, z));
        if (world[x, y, z - 1] / 10 == (ushort)Blocks.WaterSource / 10)
            waterBlocks.Add(new Vector3(x, y, z - 1));
        if (world[x + 1, y, z] / 10 == (ushort)Blocks.WaterSource / 10)
            waterBlocks.Add(new Vector3(x + 1, y, z));
        if (world[x, y, z + 1] / 10 == (ushort)Blocks.WaterSource / 10)
            waterBlocks.Add(new Vector3(x, y, z + 1));
    }
    private void PlaceBlock(Vector3 hitPoint, int x, int y, int z, int index)
    {
        if (Vector3.Distance(hitPoint, player.GetComponent<CharacterController>().ClosestPoint(hitPoint)) > 1f)
        {
            if (_movement.playmode == Playmodes.Survival)
            {
                _player.itemsBar[index].amount--;
                if (_player.itemsBar[index].amount <= 0)
                {
                    Destroy(_player.itemsBar[index].gameObject);
                }
                else
                {
                    _player.itemsBar[index].UpdateItem();
                }

            }
            world[x, y, z] = (ushort)(blockIndex + blockRotation);
            if (blockIndex / 10 == (ushort)Blocks.WaterSource / 10)
            {
                waterBlocks.Add(new Vector3(x, y, z));
            }
            world.SetChunkDirty(x, z, false);
            WaterNearby(x, y, z);
            updateNav = true;
        }
    }
    private int GetBlockIndex(ushort value)
    {
        if (value < 500)
            return (value / 10) - 1;
        else if (value >= 500 && value < 1000)
            return (value / 10) - 34;
        else if (value >= 1000)
            return (value / 10) - 82;
        else
            return 2;
    }
    private void MiningPlacingBlocks()
    {
        if (!_player.isAlive)
            return;
        if (_movement.focused)
        {
            var _prevBlock = Vector3.zero;
            for (float i = 0; i < (_movement.playmode == Playmodes.Survival ? miningDistance : miningDistance * 3); i += 0.20f)
            {
                var currentPoint = cam.transform.position + (cam.transform.forward * i);
                int _x = Mathf.FloorToInt(currentPoint.x);
                int _y = Mathf.FloorToInt(currentPoint.y);
                int _z = Mathf.FloorToInt(currentPoint.z);
                var currentBlock = new Vector3(_x, _y, _z);
                if (_prevBlock != Vector3.zero)
                {
                    if (_prevBlock == currentBlock)
                        continue;
                    else
                        _prevBlock = currentBlock;
                }
                else
                {
                    _prevBlock = currentBlock;
                }

                if (world[_x, _y, _z] != 0)
                {
                    if (world[_x, _y, _z] / 10 == 7)
                    {
                        int currIndex = _player.toolbar.currentIndex;
                        if (Input.GetMouseButtonDown(1) && _player.itemsBar[currIndex] == null)
                        {
                            _playerNeeds.Drink(5f);
                        }
                        //if(player current item != block/tool)
                        //Player aims at water - enable drinking with empty hand
                    }
                    else if (world[_x, _y, _z] >= 1000) //if player aims at bush blocks
                    {
                        if (Input.GetMouseButtonDown(0))
                        {
                            Block block = BlocksList[GetBlockIndex(world[_x, _y, _z])];
                            DestroyBlock(block, _x, _y, _z);
                        }
                    }
                    break;
                }
            }
        }
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, _movement.playmode == Playmodes.Survival ? miningDistance : miningDistance * 3))
        {
            if (hit.collider.CompareTag("Chunk") && _movement.focused)
            {
                //Destroying blocks
                _selectSquare.SetActive(true);
                var p = hit.point - (hit.normal / 2f);
                int _x = Mathf.FloorToInt(p.x);
                int _y = Mathf.FloorToInt(p.y);
                int _z = Mathf.FloorToInt(p.z);
                Vector3 point = new Vector3(_x + 0.5f, _y + 0.5f, _z + 0.5f);
                _selectSquare.transform.position = point + (hit.normal / 2f) + (hit.normal / 25f);
                _selectSquare.transform.rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);
                if (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0))
                {
                    if (world[_x, _y, _z] != 0 && _y > 0)
                    {
                        Block block = BlocksList[GetBlockIndex(world[_x, _y, _z])];
                        if (_movement.playmode == Playmodes.Survival)
                        {
                            timerBlock += Time.deltaTime;
                            if (timerBlock > 0.1f)
                                slider.gameObject.SetActive(true);
                            var minedBlock = point;
                            float max;
                            if (prevBlock != Vector3.zero && minedBlock != prevBlock)
                            {
                                timerBlock = 0f;
                            }
                            prevBlock = minedBlock;

                            int currIndex = _player.toolbar.currentIndex;
                            float multiplier = 10f / block.resistance;
                            if (_player.itemsBar[currIndex] != null)
                            {
                                if (_player.itemsBar[currIndex].item.GetType() == typeof(Tool))
                                {
                                    float efficiency = float.Parse(GetPropValue(_player.itemsBar[currIndex].item, "toolEfficiency").ToString());
                                    ToolType toolType = (ToolType)GetPropValue(_player.itemsBar[currIndex].item, "toolType");
                                    if (block.toolType == toolType)
                                    {
                                        multiplier *= efficiency;
                                    }
                                }
                            }
                            max = timerBlockMax / multiplier;

                            if (slider.gameObject.activeInHierarchy && max != 0f)
                            {
                                slider.value = timerBlock / max;
                            }
                            if (timerBlock > max)
                            {
                                slider.value = 0f;
                                timerBlock -= max;
                                DestroyBlock(block, _x, _y, _z);
                            }
                        }
                        else
                        {
                            if (Input.GetKey(KeyCode.CapsLock) && Input.GetMouseButton(0))
                            {
                                DestroyBlock(block, _x, _y, _z);
                            }
                            else if (!Input.GetKey(KeyCode.CapsLock) && Input.GetMouseButtonDown(0))
                            {
                                DestroyBlock(block, _x, _y, _z);
                            }
                        }
                    }
                }
                else
                {
                    SetVariables();
                    int currIndex = _player.toolbar.currentIndex;
                    if (_player.itemsBar[currIndex] != null)
                    {
                        if (_player.itemsBar[currIndex].item.GetType() == typeof(Block))
                        {
                            blockIndex = int.Parse(GetPropValue(_player.itemsBar[currIndex].item, "ID").ToString());
                            p = hit.point + (hit.normal / 2f);
                            int x = Mathf.FloorToInt(p.x);
                            int y = Mathf.FloorToInt(p.y);
                            int z = Mathf.FloorToInt(p.z);
                            if (world[x, y, z] == (ushort)Blocks.Air || world[x, y, z] / 10 == (ushort)Blocks.WaterSource / 10 || world[x, y, z] / 10 >= (ushort)Blocks.GrassBush / 10)
                            {
                                _ghostBlock.SetActive(true);
                                Vector3 point1 = new Vector3(_x, _y, _z);
                                _ghostBlock.transform.position = point1 + hit.normal;
                                UpdateGhostBlock();
                                if (!Input.GetKey(KeyCode.CapsLock) && Input.GetMouseButtonUp(1))
                                {
                                    PlaceBlock(hit.point, x, y, z, currIndex);
                                }
                                else if (Input.GetKey(KeyCode.CapsLock) && Input.GetMouseButton(1))
                                {
                                    PlaceBlock(hit.point, x, y, z, currIndex);
                                }
                            }
                        }
                        else
                            _ghostBlock.SetActive(false);
                    }
                    else
                        _ghostBlock.SetActive(false);
                }
            }
            else if (hit.collider.CompareTag("Mob") && _movement.focused)
            {
                int currIndex = _player.toolbar.currentIndex;
                if (_player.itemsBar[currIndex] != null)
                {
                    if (_player.itemsBar[currIndex].item.GetType() == typeof(Tool))
                    {
                        float attackSpeed = float.Parse(GetPropValue(_player.itemsBar[currIndex].item, "attackSpeed").ToString());
                        if (timerHit > 0f)
                            timerHit -= Time.deltaTime;
                        if (Input.GetMouseButtonDown(0) && timerHit <= 0f)
                        {
                            int attackDmg = int.Parse(GetPropValue(_player.itemsBar[currIndex].item, "attackDamage").ToString());
                            timerHit = attackSpeed;
                            Entity entity = hit.collider.GetComponent<Entity>();
                            entity.OnTakenDamage(attackDmg);
                            entity.SetPushVector((player.forward + player.up) * pushForce);
                            if (entity.HealthPoints <= 0)
                            {
                                entitiesList.Remove(entity);
                            }
                        }
                    }
                }
                SetVariables();
                _selectSquare.SetActive(false);
                _ghostBlock.SetActive(false);
            }
            else
            {
                SetVariables();
                _selectSquare.SetActive(false);
                _ghostBlock.SetActive(false);
            }
        }
        else
        {
            SetVariables();
            _selectSquare.SetActive(false);
            _ghostBlock.SetActive(false);
        }
    }
    private void SetVariables()
    {
        slider.value = 0f;
        slider.gameObject.SetActive(false);
        timerBlock = 0f;
    }
    private void UnloadChunks()
    {
        foreach (KeyValuePair<ChunkId, Chunk> chunk in world.Chunks)
        {
            Vector3 pos = new Vector3(player.position.x, 70f, player.position.z);
            if (Vector3.Distance(pos, chunk.Value.gameObject.transform.position) > distanceToUnload)
            {
                world.Chunks.Remove(chunk.Key);
                Destroy(chunk.Value.gameObject);
                break;
            }
        }
        foreach (var entity in entitiesList)
        {
            Vector3 pos = new Vector3(player.position.x, 70f, player.position.z);
            if (Vector3.Distance(pos, entity.transform.position) > distanceToUnload * 3)
            {
                entitiesList.Remove(entity);
                Destroy(entity.gameObject);
                break;
            }
        }
    }
    private void AddChunk(int x1, int z1, Heightmap heighmap, bool navMesh)
    {
        int x = (int)RoundDown(x1, 16) / 16;
        int z = (int)RoundDown(z1, 16) / 16;
        var chunkGameObject = new GameObject($"Chunk {x}, {z}")
        {
            tag = "Chunk",
            layer = 11
        };
        chunkGameObject.transform.parent = navMesh ? chunksNavMesh.transform : chunksRest.transform;
        chunkGameObject.transform.position = new Vector3(x * chunkWidth, 0, z * chunkWidth);
        var chunk = chunkGameObject.AddComponent<Chunk>();
        chunk.SetVariables(heighmap, this);
        world.Chunks.Add(new ChunkId(x, z), chunk);
    }
    private IEnumerator WaitForLoadChunks(float beginDelay, float endDelay)
    {
        loadChunks = false;
        yield return new WaitForSeconds(beginDelay);
        int x = (int)player.position.x;//RoundDown((long)player.position.x, 16) + 8;
        int z = (int)player.position.z;//RoundDown((long)player.position.z, 16) + 8;
        int playerAngle = 360 - ((int)player.rotation.eulerAngles.y);
        for (int i = 0; i <= distanceToLoad; i += 8)
        {
            if (i == 0)
            {
                if (world.Heightmaps.ContainsKey(HeightmapId.FromWorldPos(x, z)) && world.Heightmaps[HeightmapId.FromWorldPos(x, z)].IsDone)
                {
                    if (!world.Chunks.ContainsKey(ChunkId.FromWorldPos(x, z)))
                    {
                        AddChunk(x, z, world.Heightmaps[HeightmapId.FromWorldPos(x, z)], true);
                        buildNav = true;
                    }
                    else
                    {
                        world.Chunks[ChunkId.FromWorldPos(x, z)].transform.parent = chunksNavMesh.transform;
                    }
                }
            }
            else
            {
                double radius = Mathf.Sqrt(i * i + i * i);
                for (float j = playerAngle; j <= playerAngle + 180; j += 7.5f)
                {
                    int x1 = (int)(x + radius * Mathf.Cos(j * Mathf.PI / 180f));
                    int z1 = (int)(z + radius * Mathf.Sin(j * Mathf.PI / 180f));
                    if (world.Heightmaps.ContainsKey(HeightmapId.FromWorldPos(x1, z1)) && world.Heightmaps[HeightmapId.FromWorldPos(x1, z1)].IsDone)
                    {
                        if (!world.Chunks.ContainsKey(ChunkId.FromWorldPos(x1, z1)))
                        {
                            if (i <= 32)
                                AddChunk(x1, z1, world.Heightmaps[HeightmapId.FromWorldPos(x1, z1)], true);
                            else
                                AddChunk(x1, z1, world.Heightmaps[HeightmapId.FromWorldPos(x1, z1)], false);
                            yield return new WaitForEndOfFrame();
                        }
                        else
                        {
                            if (i <= 32)
                                world.Chunks[ChunkId.FromWorldPos(x1, z1)].transform.parent = chunksNavMesh.transform;
                            else
                                world.Chunks[ChunkId.FromWorldPos(x1, z1)].transform.parent = chunksRest.transform;
                        }
                    }
                }
                for (float j = playerAngle + 180; j <= playerAngle + 360; j += 7.5f)
                {
                    int x1 = (int)(x + radius * Mathf.Cos(j * Mathf.PI / 180f));
                    int z1 = (int)(z + radius * Mathf.Sin(j * Mathf.PI / 180f));
                    if (world.Heightmaps.ContainsKey(HeightmapId.FromWorldPos(x1, z1)) && world.Heightmaps[HeightmapId.FromWorldPos(x1, z1)].IsDone)
                    {
                        if (!world.Chunks.ContainsKey(ChunkId.FromWorldPos(x1, z1)))
                        {
                            if (i <= 32)
                                AddChunk(x1, z1, world.Heightmaps[HeightmapId.FromWorldPos(x1, z1)], true);
                            else
                                AddChunk(x1, z1, world.Heightmaps[HeightmapId.FromWorldPos(x1, z1)], false);
                            yield return new WaitForEndOfFrame();
                        }
                        else
                        {
                            if (i <= 32)
                                world.Chunks[ChunkId.FromWorldPos(x1, z1)].transform.parent = chunksNavMesh.transform;
                            else
                                world.Chunks[ChunkId.FromWorldPos(x1, z1)].transform.parent = chunksRest.transform;
                        }
                    }
                }
            }
        }
        yield return new WaitForSeconds(endDelay);
        loadChunks = true;
    }

    private IEnumerator WaitForHeightmap(float delay)
    {
        loadHeightmap = false;
        int _x = (int)RoundDown((long)player.position.x, 128);
        int _z = (int)RoundDown((long)player.position.z, 128);
        for (int i = -heightmapDistance; i <= heightmapDistance; i++)
        {
            for (int j = -heightmapDistance; j <= heightmapDistance; j++)
            {
                if (!world.Heightmaps.ContainsKey(HeightmapId.FromWorldPos(_x + (i * 128), _z + (j * 128))))
                {
                    Heightmap hm = new(_x + (i * 128), _z + (j * 128), sceneManager.GetWorldSeed(), sceneManager.GetWorldName());
                    world.Heightmaps.Add(HeightmapId.FromWorldPos(_x + (i * 128), _z + (j * 128)), hm);
                    yield return StartCoroutine(hm.WaitFor());
                    hm.SaveData();
                }
            }
        }
        yield return new WaitForSeconds(delay);
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
    public static object GetPropValue(object src, string fieldName)
    {
        Type type = src.GetType();
        FieldInfo info = type.GetField(fieldName);
        if (info == null)
        { return null; }
        return info.GetValue(src);
    }
}
