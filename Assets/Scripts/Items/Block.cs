using UnityEngine;
[CreateAssetMenu(fileName = "Item", menuName = "Items/Block")]
public class Block : Item
{
    public ToolType toolType;
    [Range(0, 100)]
    public int resistance;
    public int ID;
    public int maxAmount;
}
