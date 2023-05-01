using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Toolbar : MonoBehaviour
{
    public int currentIndex = 0;
    public DropZone[] Slots = new DropZone[9];
    public DropZone[] InventorySlots = new DropZone[27];
    private bool _inventoryShow = false;
    public Transform InventoryTransform;
    private WorldController worldController;
    private void Start()
    {
        worldController = GameObject.FindWithTag("GameController").GetComponent<WorldController>();
        Slots[currentIndex].GetComponent<Outline>().enabled = true;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && !Input.GetMouseButton(0))
        {
            _inventoryShow = !_inventoryShow;
            if (!_inventoryShow)
                InventoryTransform.localScale = Vector3.zero;
            else
                InventoryTransform.localScale = Vector3.one;
        }
        float axis = Input.GetAxis("Mouse ScrollWheel");
        if (axis != 0)
        {
            Slots[currentIndex].GetComponent<Outline>().enabled = false;
            if (axis > 0)
            {
                currentIndex = currentIndex + 1;
                if (currentIndex >= 9)
                    currentIndex = 0;
            }
            else
            {
                currentIndex = currentIndex - 1;
                if (currentIndex < 0)
                    currentIndex = 8;
            }
            Slots[currentIndex].GetComponent<Outline>().enabled = true;
            worldController.UpdateRotation();
        }
        bool res = int.TryParse(Input.inputString, out int num1);
        if (res)
        {
            if (new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }.Contains(num1))
            {
                Slots[currentIndex].GetComponent<Outline>().enabled = false;
                currentIndex = num1 - 1;
                Slots[currentIndex].GetComponent<Outline>().enabled = true;
                worldController.UpdateRotation();
            }
        }
    }
}
