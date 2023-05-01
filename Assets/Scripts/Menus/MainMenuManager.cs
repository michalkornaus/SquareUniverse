using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using TMPro;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    private SceneManager sceneManager;

    public GameObject MainMenuPanel;
    public GameObject NewGamePanel;
    public GameObject LoadGamePanel;
    public GameObject SettingsPanel;
    public TMP_Dropdown dropdown;
    public Button LoadButton;

    private string[] gameNames;

    private void Start()
    {
        StartCoroutine(SetSceneManager());
        SetMainMenuPanel();
    }
    private IEnumerator SetSceneManager()
    {
        Debug.Log("Waiting for scene manager");
        yield return new WaitForEndOfFrame();
        sceneManager = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
    }
    private bool CheckSaves()
    {
        string dir = Application.persistentDataPath;
        string[] savedGames = Directory.GetDirectories(dir);
        if (savedGames.Length == 0)
        { return false; }
        else
        { return true; }
    }
    public void SetMainMenuPanel()
    {
        if (CheckSaves()) { LoadButton.interactable = true; }
        else { LoadButton.interactable = false; }
        MainMenuPanel.SetActive(true);
        NewGamePanel.SetActive(false);
        LoadGamePanel.SetActive(false);
        SettingsPanel.SetActive(false);
    }
    public void SetNewGamePanel()
    {
        MainMenuPanel.SetActive(false);
        NewGamePanel.SetActive(true);
        LoadGamePanel.SetActive(false);
        SettingsPanel.SetActive(false);
    }
    public void SetLoadGamePanel()
    {
        MainMenuPanel.SetActive(false);
        NewGamePanel.SetActive(false);
        LoadGamePanel.SetActive(true);
        SettingsPanel.SetActive(false);

        dropdown.ClearOptions();
        string dir = Application.persistentDataPath;
        string[] savedGames = Directory.GetDirectories(dir);
        gameNames = new string[savedGames.Length];

        string search = "SquareUniverse\\";
        for (int i = 0; i < savedGames.Length; i++)
        {
            gameNames[i] = savedGames[i].Substring(savedGames[i].IndexOf(search) + search.Length);
        }
        List<TMP_Dropdown.OptionData> data = new List<TMP_Dropdown.OptionData>();
        for (int i = 0; i < gameNames.Length; i++)
        {
            data.Add(new TMP_Dropdown.OptionData(gameNames[i]));
        }
        dropdown.AddOptions(data);
    }
    public void LoadGame()
    {
        string selectedGame = gameNames[dropdown.value];
        sceneManager.SetWorldName(selectedGame);
        //Get the world seed value from the saved world file
        string dest = Application.persistentDataPath + "/" + selectedGame + "/" + "world.dat";
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Open(dest, FileMode.Open);
        WorldSave save = (WorldSave)bf.Deserialize(file);
        file.Close();
        sceneManager.SetWorldSeed(save.worldSeed);
        sceneManager.LoadGame();
    }
    public void SetSettingsPanel()
    {
        MainMenuPanel.SetActive(false);
        NewGamePanel.SetActive(false);
        LoadGamePanel.SetActive(false);
        SettingsPanel.SetActive(true);
    }
    public void ExitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif

    }
}
