using UnityEngine;
using UnityEngine.EventSystems;
public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    //[HideInInspector]
    public Transform parentToReturnTo = null;
    public bool creativeMenu = false;
    public void OnBeginDrag(PointerEventData eventData)
    {
        //Debug.Log("OnBeginDrag");
        parentToReturnTo = transform.parent;
        transform.SetParent(transform.parent.parent);
        transform.position = transform.parent.parent.position;
        GetComponent<CanvasGroup>().blocksRaycasts = false;
        GetComponent<CanvasGroup>().ignoreParentGroups = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        //Debug.Log ("OnDrag");
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        //Debug.Log("OnEndDrag");
        transform.SetParent(parentToReturnTo);
        transform.position = new Vector2(transform.parent.position.x + 50, transform.parent.position.y - 50);
        GetComponent<CanvasGroup>().blocksRaycasts = true;
        GetComponent<CanvasGroup>().ignoreParentGroups = false;
    }
    public void UpdatePosition()
    {
        transform.SetParent(parentToReturnTo);
        GetComponent<CanvasGroup>().blocksRaycasts = true;
        GetComponent<CanvasGroup>().ignoreParentGroups = false;
        transform.position = new Vector2(transform.parent.position.x + 50, transform.parent.position.y - 50);
    }
}