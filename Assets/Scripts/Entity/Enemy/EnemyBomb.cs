using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBomb : Entity
{
    private VoxelEngine _voxelEngine;
    private void Awake()
    {
        _voxelEngine = GameObject.FindGameObjectWithTag("GameController").GetComponent<VoxelEngine>();
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.tag == "Chunk")
        {
            Exploud();
        }
    }

    public void Exploud()
    {
        int x = Mathf.FloorToInt(transform.position.x);
        int y = Mathf.FloorToInt(transform.position.y);
        int z = Mathf.FloorToInt(transform.position.z);
        for (int i = -3; i <= 3; i++)
        {
            for (int j = -3; j <= 3; j++)
            {
                for (int k = -3; k <= 3; k++)
                {
                    if (Mathf.Abs(i) + Mathf.Abs(k) + Mathf.Abs(j) <= 5)
                        _voxelEngine.world[x + i, y + j, z + k] = 0;
                }
            }
        }
        _voxelEngine.world.SetNearChunks(x, z);
        Destroy(gameObject);
    }
}
