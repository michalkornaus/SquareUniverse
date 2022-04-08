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
        if (Input.GetKeyDown(KeyCode.Mouse2))
        {
            StopLooking();
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            StartLooking();
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

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
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
        //Exploud bomb on touch
        if (hit.collider.tag == "DesertBomb")
        {
            hit.collider.GetComponent<EnemyBomb>().Exploud();
        }
        if (hit.moveDirection.y < -0.3)
        {
            return;
        }
        //Push rigidbody
        Rigidbody body = hit.collider.attachedRigidbody;
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
        body.velocity = pushDir * pushForce;
    }

    public void StartLooking()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        rotationSpeed = 3f;
    }

    public void StopLooking()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        rotationSpeed = 0f;
    }

}
