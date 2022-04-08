using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : Entity
{
    //public List<Item> equipment = new List<Item>();
    public GameObject mainPanel;
    public Button eqButton;
    public GameObject eqPanel;
    public Button craftButton;
    public GameObject craftPanel;
    public GameObject item;
    public byte[] blocks = new byte[20];
    public bool isCreative;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && !EmptyArray(blocks))
        {
            mainPanel.SetActive(!mainPanel.activeInHierarchy);
            if (mainPanel.activeInHierarchy)
            {
                FillPanel();
            }
            else
            {

            }
        }
    }
    private void FillPanel()
    {
        if (!eqButton.interactable)
        {
            int index = 0;
            for (int i = 0; i < blocks.Length; i++)
            {
                if (blocks[i] != 0)
                {
                    GameObject _item = Instantiate(item, new Vector2(0f, 0f - (50f * index)), eqPanel.transform.rotation);
                    _item.transform.SetParent(eqPanel.transform, false);
                    _item.GetComponentInChildren<Text>().text = GetBlockText(i) + " " + "x" + blocks[i];
                    _item.SetActive(true);
                    index++;
                }
            }
        }
        else
        {

        }
    }
    public void ActivateEquipment()
    {
        eqButton.interactable = false;
        eqPanel.SetActive(true);
        craftPanel.SetActive(false);
        craftButton.interactable = true;
    }
    public void ActivateCrafting()
    {
        eqButton.interactable = true;
        eqPanel.SetActive(false);
        craftPanel.SetActive(true);
        craftButton.interactable = false;
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
}
