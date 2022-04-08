using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public float rotateSpeed;
    private readonly float minRotationY = -90f;
    private readonly float maxRotationY = 90f;
    private float rotationY;
    void Start()
    {
        transform.rotation = Quaternion.identity;
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse2))
        {
            rotateSpeed = 0f;
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            rotateSpeed = 3f;
        }
        rotationY += Input.GetAxis("Mouse Y") * rotateSpeed;
       rotationY = Mathf.Clamp(rotationY, minRotationY, maxRotationY);
       transform.eulerAngles = new Vector3(-rotationY, transform.eulerAngles.y, transform.eulerAngles.z);
    }
}
