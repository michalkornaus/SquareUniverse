using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public Camera cam;
    [Header("Variables")]
    //PLAYER VARIABLES
    private CharacterController cc;
    private Vector3 playerVelocity;
    private readonly float gravityForce = -9.81f;
    private float speedMagnitude;
    private float rotationX;
    private float playerSpeed;
    public float walkSpeed;
    public float sprintSpeed;

    public float jumpForce = 1;
    private bool groundedPlayer;
    public float rotationSpeed;
    public float pushForce;

    [Header("Sprint")]
    public float staminaReg;
    public float staminaDrain;

    [Header("HeadBobbing")]
    private float time;
    public float frequency;
    public float amplitude;
    void Start()
    {
        cc = gameObject.GetComponent<CharacterController>();
        playerSpeed = walkSpeed;
        time = 0f;
    }
    void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.Confined;
            rotationSpeed = 0f;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            rotationSpeed = 3f;
        }
        groundedPlayer = cc.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0)
            playerVelocity.y = 0f;
        if (Input.GetKey(KeyCode.LeftShift))
            playerSpeed = sprintSpeed;
        else
            playerSpeed = walkSpeed;
        Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        cc.Move(transform.TransformDirection(move).normalized * Time.deltaTime * playerSpeed);
        speedMagnitude = cc.velocity.magnitude;
        if (speedMagnitude > 0.5f)
        {
            cam.transform.localPosition = new Vector3(0, (Mathf.Sin(time * frequency) * amplitude * speedMagnitude) + 0.65f, 0);
            time += Time.deltaTime;
        }
        else
        { time = 0f; }

        /* ROTATING */
        rotationX += Input.GetAxis("Mouse X") * rotationSpeed;
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, rotationX, transform.eulerAngles.z);
        if (Input.GetButtonDown("Jump") && groundedPlayer)
        {
            playerVelocity.y += Mathf.Sqrt(jumpForce * -3.0f * gravityForce);
        }
        playerVelocity.y += gravityForce * Time.deltaTime * 2f;
        cc.Move(playerVelocity * Time.deltaTime);
    }

    void OnControllerColliderHit(ControllerColliderHit other)
    {
        if (other.rigidbody == null || other.rigidbody.isKinematic)
        { return; }
        if (other.moveDirection.y < -0.3)
        {
            return;
        }
        Rigidbody body = other.collider.attachedRigidbody;
        Vector3 pushDir = new Vector3(other.moveDirection.x, 0, other.moveDirection.z);
        body.velocity = pushDir * pushForce;
    }
}
