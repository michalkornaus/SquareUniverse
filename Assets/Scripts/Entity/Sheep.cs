using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sheep : Entity
{
    private System.Random random = new System.Random();
    [Range(1, 20)]
    public int distanceToWalk;
    private VoxelEngine _voxelEngine;
    private Entity entity;
    private bool isMoving = false;
    private void Start()
    {
        _voxelEngine = GameObject.FindGameObjectWithTag("GameController").GetComponent<VoxelEngine>();
        entity = GetComponent<Entity>();
    }
    private void Update()
    {
        if (!isMoving)
            StartCoroutine(WaitForMovement());
    }
    private IEnumerator WaitForMovement()
    {
        isMoving = true;
        yield return new WaitForSeconds(5f);
        entity.SetDestination(FindNewWaypoint());
        yield return new WaitForSeconds(random.Next(5, 10));
        isMoving = false;
    }
    private Vector3 FindNewWaypoint()
    {
        int valueX = random.Next(-distanceToWalk, distanceToWalk);
        int valueZ = random.Next(-distanceToWalk, distanceToWalk);
        Vector3 position = new Vector3(transform.position.x + valueX, 200, transform.position.z + valueZ);
        if (Physics.Raycast(position, Vector3.down, out RaycastHit _hit))
        {
            var p = _hit.point - (_hit.normal / 2f);
            int x = Mathf.FloorToInt(p.x);
            int y = Mathf.FloorToInt(p.y);
            int z = Mathf.FloorToInt(p.z);
            if (_voxelEngine.world[x, y, z] == 1 && _voxelEngine.world[x, y + 1, z] != 7)
            {
                p = _hit.point + (_hit.normal / 2f);
                x = Mathf.FloorToInt(p.x);
                y = Mathf.FloorToInt(p.y);
                z = Mathf.FloorToInt(p.z);
                position = new Vector3(x, y, z);
                Vector3 dir = position - transform.position;
                entity.RotateEntity(dir);
                return position;
            }
            else
            {
                return Vector3.zero;
            }
        }
        else
        {
            return Vector3.zero;
        }


    }
}
