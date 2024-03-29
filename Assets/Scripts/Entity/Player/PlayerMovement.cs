﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum Playmodes
{
    Survival, Creative, Noclip
}
public class PlayerMovement : MonoBehaviour
{
    [Header("Variables")]
    public Camera cam;
    public Playmodes playmode;
    private int playmodeIndex;

    //Player private variables
    private CharacterController cc;
    private PlayerCamera pc;
    private Vector3 playerVelocity;
    private float speedMagnitude;

    //Bools
    public bool focused = true;
    private bool inWater = false;
    private bool groundedPlayer;

    [Header("Speeds")]
    public float walkSpeed;
    public float rotationSpeed;
    private float rotationX;
    private float playerSpeed;

    [Header("Forces")]
    public float pushForce;
    private Vector3 _pushVector;
    public float jumpForce = 1;
    private readonly float gravityForce = -9.81f;
    private readonly float waterForce = -3f;

    [Header("Sprint")]
    public float sprintSpeed;
    public float staminaReg;
    public float staminaDrain;

    [Header("HeadBobbing")]
    public float frequency;
    public float amplitude;
    private float time;
    private void Awake()
    {
        pc = GetComponentInChildren<PlayerCamera>();
        cc = gameObject.GetComponent<CharacterController>();
    }
    void Start()
    {
        playerSpeed = walkSpeed;
        playmodeIndex = (int)playmode;
        StartLooking();
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            //switching playmodes of player
            ChangePlaymode();
        }
        switch (playmode)
        {
            case Playmodes.Survival:
                SurvivalMovement();
                break;
            case Playmodes.Creative:
                CreativeMovement();
                break;
            case Playmodes.Noclip:
                NoClipMovement();
                break;
        }

        //Rotating player
        rotationX += Input.GetAxis("Mouse X") * rotationSpeed;
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, rotationX, transform.eulerAngles.z);
    }
    private void SurvivalMovement()
    {
        gameObject.layer = 0;
        groundedPlayer = cc.isGrounded;
        //if player is on the ground reset up/down velocity
        if (groundedPlayer && playerVelocity.y < 0)
            playerVelocity.y = 0f;
        //check if shift key is pressed and changed player speed accordingly
        var shiftKey = Input.GetKey(KeyCode.LeftShift);
        if (inWater)
            playerSpeed = shiftKey ? sprintSpeed / 2f : walkSpeed / 2f;
        else
            playerSpeed = shiftKey ? sprintSpeed : walkSpeed;

        //Moving player
        Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        cc.Move(transform.TransformDirection(move).normalized * Time.deltaTime * playerSpeed);

        //Head bobing 
        speedMagnitude = cc.velocity.magnitude;
        if (speedMagnitude > 0.5f)
        {
            cam.transform.localPosition = new Vector3(0, (Mathf.Sin(time * frequency) * amplitude * speedMagnitude) + 0.65f, 0);
            time += Time.deltaTime;
        }
        else
        { time = 0f; }

        //Applying gravity and jumping forces - checking if player is in water
        if (Input.GetButtonDown("Jump"))
        {
            if (groundedPlayer && !inWater)
                playerVelocity.y += Mathf.Sqrt(jumpForce * -3.0f * gravityForce);
            if (inWater)
                playerVelocity.y += Mathf.Sqrt(jumpForce * -3.0f * waterForce);
        }
        if (inWater)
            playerVelocity.y += waterForce * Time.deltaTime * 1.5f;
        else
            playerVelocity.y += gravityForce * Time.deltaTime * 2f;
        cc.Move((playerVelocity + _pushVector) * Time.deltaTime);
        if (_pushVector.magnitude > 0f)
            _pushVector = Vector3.Lerp(_pushVector, Vector3.zero, Time.deltaTime * 50f);
    }
    private void CreativeMovement()
    {
        gameObject.layer = 0;
        var shiftKey = Input.GetKey(KeyCode.LeftShift);
        playerSpeed = shiftKey ? sprintSpeed * 2f : walkSpeed * 2f;
        //Moving player
        Vector3 move = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("UpDown"), Input.GetAxis("Vertical"));
        cc.Move(transform.TransformDirection(move).normalized * Time.deltaTime * playerSpeed * 2.5f);
    }
    private void NoClipMovement()
    {
        gameObject.layer = 2;
        var shiftKey = Input.GetKey(KeyCode.LeftShift);
        playerSpeed = shiftKey ? sprintSpeed * 2f : walkSpeed * 2f;
        //Moving player
        Vector3 move = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("UpDown"), Input.GetAxis("Vertical"));
        cc.Move(transform.TransformDirection(move).normalized * Time.deltaTime * playerSpeed * 2.5f);
    }
    private void ChangePlaymode()
    {
        playmodeIndex++;
        switch (playmodeIndex)
        {
            case 0:
                playerVelocity.y = 0;
                playmode = Playmodes.Survival;
                break;
            case 1:
                playmode = Playmodes.Creative;
                break;
            case 2:
                playmode = Playmodes.Noclip;
                break;
            case 3:
                playmodeIndex = 0;
                playerVelocity.y = 0;
                playmode = Playmodes.Survival;
                break;
        }
    }
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        /*
        if (hit.rigidbody == null || hit.rigidbody.isKinematic)
        {
            if (hit.collider.CompareTag("Mob"))
            {
                //Push mob
                if (hit.moveDirection.y < -0.3)
                {
                    return;
                }
                Vector3 _pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
                hit.collider.GetComponent<Entity>().SetPushVector(_pushDir * pushForce);
                return;
            }
            else
            {
                return;
            }
        }
        if (hit.moveDirection.y < -0.3)
        {
            return;
        }
        //Push rigidbody
        Rigidbody body = hit.collider.attachedRigidbody;
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
        body.velocity = pushDir * pushForce;*/
    }

    public void StartLooking()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        rotationSpeed = 3f;
        pc.rotateSpeed = 3f;
    }

    public void StopLooking()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;
        rotationSpeed = 0f;
        pc.rotateSpeed = 0f;
    }
    public void SetRotation(float rotation) // setting rotation variable when loading saved world
    {
        rotationX = rotation;
    }

    public void SetPushVector(Vector3 pushVector)
    {
        _pushVector = pushVector;
    }
    public void SetPlayerInWater(bool _inWater)
    {
        inWater = _inWater;
    }
}
