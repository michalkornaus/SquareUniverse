using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class DropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public ItemDraggable currentItem;
    public bool creativeZone = false;
    public bool isTrash = false;
    public GameObject confirmPanel;
    private void Start()
    {
        if (GetComponentInChildren<ItemDraggable>())
        {
            currentItem = GetComponentInChildren<ItemDraggable>();
        }
    }
    private void Update()
    {
        ItemDraggable item = GetComponentInChildren<ItemDraggable>();
        if (!item)
            currentItem = null;
        else
            currentItem = item;
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        //Debug.Log("OnPointerEnter");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //Debug.Log("OnPointerExit");
    }
    private IEnumerator WaitForItemDeletion()
    {
        confirmPanel.SetActive(true);
        yield return new WaitForFixedUpdate();
    }
    public void OnDrop(PointerEventData eventData)
    {
        ItemDraggable dragged = eventData.pointerDrag.GetComponent<ItemDraggable>();
        if (dragged != null)
        {
            if (isTrash)
            {
                if(confirmPanel != null)
                {
                    StartCoroutine(WaitForItemDeletion());
                    return;
                }
                else
                {
                    //return dragged to the origin
                    return;
                }
                //Destroy(dragged.gameObject);
                //return;
            }
            if (!currentItem)
            {
                if (Input.GetKey(KeyCode.LeftShift) && dragged.amount > 1 && !creativeZone)
                {
                    if (transform != dragged.parentToReturnTo)
                    {
                        ItemDraggable item = Instantiate(dragged);
                        item.name = "Item(Clone)";
                        item.parentToReturnTo = transform;
                        item.UpdatePosition();
                        item.amount /= 2;
                        item.UpdateItem();

                        if (dragged.amount % 2 != 0)
                            dragged.amount = (dragged.amount / 2) + 1;
                        else
                            dragged.amount /= 2;
                        dragged.UpdateItem();

                        currentItem = item;
                    }
                }
                else
                {
                    if (dragged.creativeMenu && !creativeZone)
                    {
                        ItemDraggable item = Instantiate(dragged);
                        item.name = "Item(Clone)";
                        item.parentToReturnTo = transform;
                        item.UpdatePosition();
                        item.creativeMenu = false;
                        item.UpdateItem();
                        currentItem = item;
                    }
                    else
                    {
                        dragged.parentToReturnTo = transform;
                        currentItem = dragged;
                        currentItem.UpdateItem();
                    }
                }
            }
            else
            {
                if (!currentItem.creativeMenu)
                {
                    if (dragged.creativeMenu)
                    {
                        currentItem.item = dragged.item;
                        currentItem.amount = dragged.amount;
                        currentItem.UpdateItem();
                    }
                    else
                    {
                        if (currentItem.item == dragged.item)
                        {
                            currentItem.amount += dragged.amount;
                            currentItem.UpdateItem();
                            Destroy(dragged.gameObject);
                        }
                        else
                        {
                            currentItem.parentToReturnTo = dragged.parentToReturnTo;
                            currentItem.UpdatePosition();
                            dragged.parentToReturnTo = transform;
                            currentItem = dragged;
                        }
                    }
                }
            }
        }
    }
}