using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class BlocksMenu : MonoBehaviour
{
    public ItemDraggable ItemPrefab;
    public DropZone SlotPrefab;
    public GameObject blocksPanel;
    public Transform blocksTransform;
    public Transform toolsTransform;
    private WorldController worldController;
    private PlayerMovement movement;
    private float maxRow = 9f;
    private bool panelEnabled = false;

    private void Awake()
    {
        movement = GameObject.FindWithTag("Player").GetComponent<PlayerMovement>();
        worldController = GameObject.FindWithTag("GameController").GetComponent<WorldController>();
    }
    private void Start()
    {
        int columns = Mathf.CeilToInt(worldController.BlocksList.Length / maxRow);
        blocksPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(1300, 140 * columns);
        GetComponent<RectTransform>().sizeDelta = new Vector2(1300, 140 * columns);
        int index = 0;
        for (int j = 1; j <= columns; j++)
        {
            for (int i = 1; i <= maxRow; i++)
            {
                if (index >= worldController.BlocksList.Length)
                {
                    break;
                }
                else
                {
                    DropZone slot = Instantiate(SlotPrefab, new Vector2(10f + 145 * (i - 1), -10f - (140 * (j - 1))), Quaternion.identity);
                    slot.transform.SetParent(blocksTransform, false);
                    slot.creativeZone = true;
                    ItemDraggable item = Instantiate(ItemPrefab, new Vector2(0f, 0f), Quaternion.identity);
                    item.transform.SetParent(slot.transform, false);
                    item.item = worldController.BlocksList[index];
                    item.amount = 1;
                    item.creativeMenu = true;
                    item.UpdateItem();
                    slot.currentItem = item;
                }
                index++;
            }
        }
        columns = Mathf.CeilToInt(worldController.ToolsList.Length / maxRow);
        float deltaY = blocksPanel.GetComponent<RectTransform>().sizeDelta.y;
        blocksPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(1300, deltaY + 50 + (140 * columns));
        GetComponent<RectTransform>().sizeDelta = new Vector2(1300, deltaY + 50 + (140 * columns));
        index = 0;
        for (int i = 1; i <= maxRow; i++)
        {
            for (int j = 1; j <= columns; j++)
            {
                if (index >= worldController.ToolsList.Length)
                {
                    break;
                }
                else
                {
                    DropZone slot = Instantiate(SlotPrefab, new Vector2(10f + 145 * (i - 1), -15f - (140 * (j - 1)) - deltaY), Quaternion.identity);
                    slot.transform.SetParent(toolsTransform, false);
                    slot.creativeZone = true;
                    ItemDraggable item = Instantiate(ItemPrefab, new Vector2(0f, 0f), Quaternion.identity);
                    item.transform.SetParent(slot.transform, false);
                    item.item = worldController.ToolsList[index];
                    item.amount = 1;
                    item.creativeMenu = true;
                    item.UpdateItem();
                    slot.currentItem = item;
                }
                index++;
            }
        }
        ReOrder();
    }
    private void ReOrder()
    {
        DropZone[] slots = blocksTransform.GetComponentsInChildren<DropZone>();
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].transform.SetAsFirstSibling();
        }

        slots = toolsTransform.GetComponentsInChildren<DropZone>();
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].transform.SetAsFirstSibling();
        }
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B) && (movement.playmode == Playmodes.Creative || movement.playmode == Playmodes.Noclip))
        {
            panelEnabled = !panelEnabled;
        }
        if (movement.playmode == Playmodes.Survival)
            panelEnabled = false;
        blocksPanel.SetActive(panelEnabled);
    }

}