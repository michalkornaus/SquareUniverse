using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.Rendering.PostProcessing;
using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using System.IO;
using TMPro;
public class Player : MonoBehaviour
{
    private SceneManager sceneManager;

    [HideInInspector]
    public Vector3 spawnPosition;

    [Header("Player variables")]
    [Range(1, 100)]
    public int Level;
    [Range(0, 100)]
    public int HealthPoints;
    private int maxHealthPoints;
    [Range(1, 10)]
    public int HealthMultiplier;

    [Header("Health UI")]
    public TMP_Text healthText;
    public Image FlaskMask;
    private readonly float fillAmountMax = 0.8148f;
    private bool _respawnRunning = false;
    private Color colorHealthFull = new Color(0.8679245f, 0.09416164f, 0.09416164f, 1f);
    private Color colorHealthLow = new Color(0.3773585f, 0.2830189f, 0.2830189f, 1f);

    public PostProcessVolume volume;
    private Vignette _vignette;

    public ItemDraggable[] itemsBar = new ItemDraggable[9];
    public ItemDraggable[] itemsEq = new ItemDraggable[27];
    public Toolbar toolbar;

    [HideInInspector]
    public bool isAlive = true;

    private PlayerMovement playerMovement;
    private WorldController worldController;
    private PlayerCamera playerCamera;

    private PlayerNeeds playerNeeds;
    private void Awake()
    {
        playerCamera = Camera.main.GetComponent<PlayerCamera>();
        sceneManager = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
        worldController = GameObject.FindWithTag("GameController").GetComponent<WorldController>();
        playerMovement = GetComponent<PlayerMovement>();
        playerNeeds = GetComponent<PlayerNeeds>();
        maxHealthPoints = HealthPoints;
    }
    private void Update()
    {
        if (HealthPoints <= 0)
        {
            if (!_respawnRunning)
                StartCoroutine(Respawn());
            return;
        }
        //Update health UI
        if (HealthPoints >= 0)
            healthText.text = HealthPoints.ToString();
        else
            healthText.text = "0";
        healthText.color = Color.Lerp(colorHealthLow, colorHealthFull, HealthPoints / 100f);
        FlaskMask.fillAmount = fillAmountMax * (HealthPoints / 100f);
        //Update toolbar items
        for (int i = 0; i < itemsBar.Length; i++)
        {
            itemsBar[i] = toolbar.Slots[i].currentItem;
        }
        //Update inventory items
        for (int i = 0; i < itemsEq.Length; i++)
        {
            itemsEq[i] = toolbar.InventorySlots[i].currentItem;
        }
    }
    public void OnTakenDamage(int damage)
    {
        HealthPoints -= damage;
        if (HealthPoints <= 0)
        {
            if (!_respawnRunning)
                StartCoroutine(Respawn());
            return;
        }
        StartCoroutine(VignetteControl());
    }
    public IEnumerator Respawn()
    {
        isAlive = false;
        StopCoroutine(VignetteControl());
        _respawnRunning = true;

        healthText.text = HealthPoints.ToString();
        healthText.color = Color.Lerp(colorHealthLow, colorHealthFull, HealthPoints / 100f);
        FlaskMask.fillAmount = fillAmountMax * (HealthPoints / 100f);

        playerMovement.enabled = false;
        playerCamera.enabled = false;

        volume.profile.TryGetSettings(out _vignette);
        _vignette.active = true;
        _vignette.intensity.value = 0.4f;

        Debug.Log("Player's death");

        yield return new WaitForSeconds(5f);

        Debug.Log("Spawn position: "+spawnPosition + ", Player position: " + transform.position);
        transform.position = spawnPosition;

        bool isGround = false;
        LayerMask mask = LayerMask.GetMask("Chunks");
        while (!isGround)
        {
            if (Physics.Raycast(transform.position, Vector3.down, mask))
            {
                isGround = true;
            }
            yield return new WaitForEndOfFrame();
        }
        HealthPoints = maxHealthPoints;
        playerMovement.enabled = true;
        playerCamera.enabled = true;
        _vignette.active = false;
        _respawnRunning = false;

        Debug.Log("Spawned player in!");
        isAlive = true;
    }
    private IEnumerator VignetteControl()
    {
        StopCoroutine(VignetteControl());
        volume.profile.TryGetSettings(out _vignette);
        _vignette.active = true;
        _vignette.intensity.value = 0.4f;
        yield return new WaitForSeconds(0.2f);
        while (_vignette.intensity.value > 0f)
        {
            _vignette.intensity.value -= Time.deltaTime * 2f;
            yield return new WaitForEndOfFrame();
        }
        _vignette.active = false;
    }
    //Saving data on application quit 
    private void OnApplicationQuit()
    {
        SavePlayerData();
    }
    private PlayerSave CreateSave()
    {
        PlayerSave save = new();
        save.Level = Level;
        save.HealthPoints = HealthPoints;
        save.HealthMultiplier = HealthMultiplier;
        save.itemsBar = new ItemSerializable[9];
        for (int i = 0; i < itemsBar.Length; i++)
        {
            if (itemsBar[i] != null)
            {
                save.itemsBar[i] = new ItemSerializable();
                string type = itemsBar[i].item.GetType().ToString();
                save.itemsBar[i].type = type;
                switch (type)
                {
                    case "Block":
                        save.itemsBar[i].ID = System.Array.IndexOf(worldController.BlocksList, itemsBar[i].item);
                        break;
                    case "Tool":
                        save.itemsBar[i].ID = System.Array.IndexOf(worldController.ToolsList, itemsBar[i].item);
                        break;
                }
                save.itemsBar[i].amount = itemsBar[i].amount;
            }
        }

        save.itemsEq = new ItemSerializable[27];
        for (int i = 0; i < itemsEq.Length; i++)
        {
            if (itemsEq[i] != null)
            {
                save.itemsEq[i] = new ItemSerializable();
                string type = itemsEq[i].item.GetType().ToString();
                save.itemsEq[i].type = type;
                switch (type)
                {
                    case "Block":
                        save.itemsEq[i].ID = System.Array.IndexOf(worldController.BlocksList, itemsEq[i].item);
                        break;
                    case "Tool":
                        save.itemsEq[i].ID = System.Array.IndexOf(worldController.ToolsList, itemsEq[i].item);
                        break;
                }
                save.itemsEq[i].amount = itemsEq[i].amount;
            }
        }
        save.playMode = playerMovement.playmode;
        save.playerPositionX = transform.position.x;
        save.playerPositionY = transform.position.y;
        save.playerPositionZ = transform.position.z;
        save.playerSpawnPositionX = spawnPosition.x;
        save.playerSpawnPositionY = spawnPosition.y;
        save.playerSpawnPositionZ = spawnPosition.z;
        save.rotationX = Camera.main.transform.rotation.eulerAngles.x;
        save.rotationY = transform.rotation.eulerAngles.y;
        return save;
    }
    public void SavePlayerData()
    {
        PlayerSave save = CreateSave();

        string dest = Application.persistentDataPath + "/" + sceneManager.GetWorldName() + "/" + "player.dat";
        BinaryFormatter bf = new();
        FileStream file = File.Open(dest, FileMode.OpenOrCreate);
        bf.Serialize(file, save);
        file.Close();
    }
    public bool LoadPlayerData()
    {
        string dest = Application.persistentDataPath + "/" + sceneManager.GetWorldName() + "/" + "player.dat";
        if (File.Exists(dest))
        {
            BinaryFormatter bf = new();
            FileStream file = File.Open(dest, FileMode.Open);
            PlayerSave save = (PlayerSave)bf.Deserialize(file);
            file.Close();

            Level = save.Level;
            HealthPoints = save.HealthPoints;
            HealthMultiplier = save.HealthMultiplier;
            for (int i = 0; i < save.itemsBar.Length; i++)
            {
                if (save.itemsBar[i] != null)
                {
                    ItemDraggable item = Instantiate(worldController.ItemPrefab, toolbar.Slots[i].transform);
                    item.amount = save.itemsBar[i].amount;
                    string type = save.itemsBar[i].type;
                    switch (type)
                    {
                        case "Block":
                            item.item = worldController.BlocksList[save.itemsBar[i].ID];
                            break;
                        case "Tool":
                            item.item = worldController.ToolsList[save.itemsBar[i].ID];
                            break;
                    }
                    item.UpdateItem();
                    //itemsBar[i] = item;
                }
            }
            for (int i = 0; i < save.itemsEq.Length; i++)
            {
                if (save.itemsEq[i] != null)
                {
                    
                    ItemDraggable item = Instantiate(worldController.ItemPrefab, toolbar.InventorySlots[i].transform);
                    item.amount = save.itemsEq[i].amount;
                    string type = save.itemsEq[i].type;
                    switch (type)
                    {
                        case "Block":
                            item.item = worldController.BlocksList[save.itemsEq[i].ID];
                            break;
                        case "Tool":
                            item.item = worldController.ToolsList[save.itemsEq[i].ID];
                            break;
                    }
                    item.UpdateItem();
                    //itemsEq[i] = item;
                    //Debug.Log(save.itemsEq[i] + ", " + itemsEq[i]);
                }
            }
            playerMovement.playmode = save.playMode;
            transform.position = new Vector3(save.playerPositionX, save.playerPositionY, save.playerPositionZ);
            spawnPosition = new Vector3(save.playerSpawnPositionX, save.playerSpawnPositionY, save.playerSpawnPositionZ);
            playerCamera.SetRotation(save.rotationX);
            playerMovement.SetRotation(save.rotationY);
            return true;
        }
        else
        {
            // returning false if file doesn't exist
            return false;
        }

    }
}
