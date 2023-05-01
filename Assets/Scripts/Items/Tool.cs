using UnityEngine;
public enum ToolType
{
    Pickaxe, Shovel, Axe, Sword
}
[CreateAssetMenu(fileName = "Item", menuName = "Items/Tool")]
public class Tool : Item
{
    public ToolType toolType;
    public int durability;
    [Range(0, 100)]
    public float attackDamage;
    [Range(0, 10)]
    public float attackSpeed;
    [Range(1, 30)]
    public float toolEfficiency;
}
