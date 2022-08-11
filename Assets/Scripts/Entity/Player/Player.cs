using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : Entity
{
    //public List<Item> equipment = new List<Item>();
    [Header("UI Components")]
    public GameObject mainPanel;
    public GameObject eqPanel;
    public GameObject craftPanel;
    public GameObject item;

    public Button eqButton;
    public Button craftButton;
    
    [Header("Player variables")]

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
                    _item.GetComponentInChildren<Text>().text = Enums.GetBlockName(i) + " " + "x" + blocks[i];
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

}
