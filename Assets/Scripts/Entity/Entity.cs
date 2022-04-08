using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity : MonoBehaviour
{
    [Range(1, 100)]
    public int Level;
    [Range(0, 100)]
    public int HealthPoints;
    [Range(1, 10)]
    public int HealthMultiplier;

    public bool isHostile;
    public bool isPlayer;

    [Range(0, 10)]
    public int walkSpeed;
    public float jumpForce;

    private CharacterController cc;
    private Vector3 entityVelocity = Vector3.zero;
    private Vector3 pushVelocity = Vector3.zero;
    private readonly float gravityForce = -9.81f;
    private bool groundedEntity;
    private bool requireJump = false;

    private Vector3 pushVector;
    private Vector3 moveVector;
    private Vector3 destinationVector;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
    }
    private void FixedUpdate()
    {
        if (isPlayer)
            return;
        groundedEntity = cc.isGrounded;
        if (groundedEntity && entityVelocity.y < 0)
            entityVelocity.y = 0f;
        if (destinationVector != Vector3.zero)
            MoveTowardsDest();
        cc.Move((moveVector + pushVector).normalized * Time.deltaTime * walkSpeed);
        if (pushVector.magnitude > .1f)
            pushVector = Vector3.SmoothDamp(pushVector, Vector3.zero, ref pushVelocity, 0.2f);
        else
            pushVector = Vector3.zero;
        if (groundedEntity && requireJump)
        {
            if(Physics.Raycast(transform.position, transform.forward, 2f))
            {
                entityVelocity.y += Mathf.Sqrt(jumpForce * -3.0f * gravityForce);
                requireJump = false;
            }
        }
        entityVelocity.y += gravityForce * Time.deltaTime * 2f;
        cc.Move(entityVelocity * Time.deltaTime);
    }
    private void MoveTowardsDest()
    {
        moveVector = destinationVector - transform.position;
        if (moveVector.magnitude <= 1f)
        {
            requireJump = false;
            moveVector = Vector3.zero;
            destinationVector = Vector3.zero;
        }
        else
        {
            requireJump = true;
        }
    }
    public void SetPushVector(Vector3 vector)
    {
        pushVector = vector;
    }

    public void SetDestination(Vector3 vector)
    {
        destinationVector = vector;
    }
    public void RotateEntity(Vector3 vector)
    {
        Quaternion LookAtRotation = Quaternion.LookRotation(vector);
        Quaternion LookAtRotationY = Quaternion.Euler(transform.rotation.eulerAngles.x, LookAtRotation.eulerAngles.y, transform.rotation.eulerAngles.z);
        transform.rotation = LookAtRotationY;
    }
}
