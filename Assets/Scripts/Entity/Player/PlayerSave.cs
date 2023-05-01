[System.Serializable]
public class PlayerSave
{
    public int Level;
    public int HealthPoints;
    public int HealthMultiplier;

    public ItemSerializable[] itemsBar;
    public ItemSerializable[] itemsEq;

    public Playmodes playMode;

    public float playerSpawnPositionX;
    public float playerSpawnPositionY;
    public float playerSpawnPositionZ;

    public float playerPositionX;
    public float playerPositionY;
    public float playerPositionZ;

    public float rotationX;
    public float rotationY;
}
