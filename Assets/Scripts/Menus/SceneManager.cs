using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using System.IO;
using TMPro;
using System.Collections;

public class SceneManager : MonoBehaviour
{
    public GameObject errorPanel;
    private static GameObject singeltonObject;
    [SerializeField]
    private string worldName = "";
    [SerializeField]
    private string worldSeed = "";
    private void Awake()
    {
        if (singeltonObject != null)
        {
            Destroy(singeltonObject);
        }
        singeltonObject = gameObject;
        DontDestroyOnLoad(gameObject);
    }
    private void Start()
    {
        errorPanel.SetActive(false);
    }
    public void OnGenerateButtonClick()
    {
        if (worldName.Length == 0)
        {
            if (worldName.Length == 0)
            {
                StartCoroutine(ShowErrorMessage());
                Debug.Log("Enter the world name!");
            }
            return;
        }
        if (worldSeed.Length == 0)
        {
            RandomSeed();
        }
        CreateGameSave();
        LoadGame();
    }
    private IEnumerator ShowErrorMessage()
    {
        errorPanel.SetActive(true);
        yield return new WaitForSeconds(3f);
        if (errorPanel != null)
            errorPanel.SetActive(false);
    }
    private void CreateGameSave()
    {
        WorldSave save = new();
        save.worldSeed = worldSeed;
        save.worldName = worldName;

        string dest = Application.persistentDataPath + "/" + worldName + "/" + "world.dat";
        string dir = Application.persistentDataPath + "/" + worldName;
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        BinaryFormatter bf = new();
        FileStream file = File.Open(dest, FileMode.OpenOrCreate);
        bf.Serialize(file, save);
        file.Close();
    }
    public void LoadGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(1, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
    public void LoadMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(0, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
    public int GetWorldSeed()
    {
        int seed = 0, index = 1;
        if (worldSeed.Length < 1)
            return 0;
        for (int i = worldSeed.Length - 1; i >= 0; i--)
        {
            seed += (worldSeed[i] % 48) * index;
            index *= 10;
        }
        return seed;
    }
    public string GetWorldSeed_String()
    {
        return worldSeed;
    }
    public string GetWorldName()
    {
        return worldName;
    }
    public void RandomizeSeedButton(TMP_InputField seedField)
    {
        RandomSeed();
        seedField.text = worldSeed;
    }
    private void RandomSeed()
    {
        Random.InitState(System.DateTime.Now.Millisecond);
        worldSeed = Random.Range(0, 2000000000).ToString();
    }
    public void SetWorldName(string name)
    {
        worldName = name;
    }
    public void SetWorldSeed(string name)
    {
        worldSeed = name;
    }
}
