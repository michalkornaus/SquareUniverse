using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public float rotateSpeed = 5f;
    private readonly float minRotationY = -90f;
    private readonly float maxRotationY = 90f;
    private float rotationY;
    void Start()
    {
        transform.rotation = Quaternion.identity;
    }
    void Update()
    {
       rotationY += Input.GetAxis("Mouse Y") * rotateSpeed;
       rotationY = Mathf.Clamp(rotationY, minRotationY, maxRotationY);
       transform.eulerAngles = new Vector3(-rotationY, transform.eulerAngles.y, transform.eulerAngles.z);
    }
}
