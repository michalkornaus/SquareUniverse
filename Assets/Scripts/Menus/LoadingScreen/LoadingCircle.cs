using UnityEngine;

public class LoadingCircle : MonoBehaviour
{
    public GameObject GifImageCube;
    private void Awake()
    {
        SpawnCubes();
    }
    private void SpawnCubes()
    {
        int i = 0;
        for (int j = 0; j <= 360; j += 30)
        {
            int x = (int)(transform.position.x + 100 * Mathf.Cos(j * Mathf.PI / 180f));
            int y = (int)(transform.position.y + 100 * Mathf.Sin(j * Mathf.PI / 180f));
            ColorChange cc = Instantiate(GifImageCube, new Vector2(x, y), Quaternion.identity, transform).GetComponent<ColorChange>();
            cc.Invoke("StartChange", i / 2f);
            i++;
        }
    }
}
