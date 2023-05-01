using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
public class ItemDraggable : Draggable, IPointerClickHandler, IPointerExitHandler
{
    public Item item;
    public int amount;
    public TMP_Text nameText;
    public TMP_Text amountText;
    public Image image;

    public GameObject InfoPanel;
    public TMP_Text fullName;
    public TMP_Text type;

    public void UpdateItem()
    {
        if (!creativeMenu)
        { nameText.text = item.label; }
        else
        { nameText.text = ""; }

        if (amount > 1)
        { amountText.text = amount.ToString(); }
        else
        { amountText.text = ""; }
            
        image.sprite = item.sprite;
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        UpdatePanel();
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        InfoPanel.SetActive(false);
    }
    private void UpdatePanel()
    {
        InfoPanel.SetActive(true);
        fullName.text = item.name;
        type.text = item.GetType().ToString();
    }
}
