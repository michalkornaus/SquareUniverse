using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMenuManager : MonoBehaviour
{
    private SceneManager sceneManager;
    private WorldController worldController;

    private Player player;
    private PlayerMovement playerMovement;

    public GameObject GameMenuPanel;
    private void Start()
    {
        player = GameObject.FindWithTag("Player").GetComponent<Player>();
        playerMovement = player.GetComponent<PlayerMovement>();
        worldController = GameObject.FindWithTag("GameController").GetComponent<WorldController>();
        sceneManager = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
    }
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            player.SavePlayerData();
            worldController.SaveHeightmapData();
            worldController.SaveEntities();
            GameMenuPanel.SetActive(!GameMenuPanel.activeInHierarchy);
        }
        if(Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
        {
            if(player.toolbar.InventoryTransform.localScale == Vector3.zero && !GameMenuPanel.activeInHierarchy)
            {
                playerMovement.focused = true;
            }
            else
            {
                playerMovement.focused = false;
            }

            if (playerMovement.focused)
                playerMovement.StartLooking();
            else
                playerMovement.StopLooking();
        }
    }
    public void OnMainMenuButton()
    {
        GameMenuPanel.SetActive(false);
        sceneManager.LoadMenu();
    }
}
