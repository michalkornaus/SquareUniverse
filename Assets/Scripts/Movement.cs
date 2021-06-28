using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    private GameObject gameHandler;
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

    //private float stamina;
    [Header("Audio")]
    public AudioSource audioSourc;
    public AudioClip[] defaultSteps;
    public AudioClip[] carpetSteps;
    public AudioClip[] metalSteps;
    public AudioClip[] woodSteps;
    //private string matName;
    //private bool step = true;
    //private float audioStepLengthWalk = 0.45f;
    //private float audioStepLengthRun = 0.3f;
    [Header("HeadBobbing")]
    private float time;
    public float frequency;
    public float amplitude;
    void Start()
    {
        gameHandler = GameObject.FindWithTag("GameController");
        cc = gameObject.GetComponent<CharacterController>();
        playerSpeed = walkSpeed;
        time = 0f;
    }
    void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.Confined;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
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
        /*
                if (cc.isGrounded)
                {
                    input = transform.TransformDirection(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")).normalized;
                    Moving();
                    input *= moveSpeed;
                    // * acceleration;
                    /*
                    if (speedMagnitude > 1)
                    {
                        cam.transform.localPosition = new Vector3(0, (Mathf.Sin(time * frequency) * amplitude * speedMagnitude) + 0.55f, 0);
                        time += Time.fixedDeltaTime;
                    }
                    else
                        time = 0f;
                }
                else
                {
                    input = transform.TransformDirection(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")).normalized;
                    moveSpeed = 1.2f;
                    input *= moveSpeed;
                }*/


    }
    void Moving()
    {
        //
        /*if ((Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)) && acceleration < 1f)
        {
            acceleration += 0.05f;
        }
        else if (Input.GetKey(KeyCode.LeftShift) && (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)) && acceleration < 1f)
        {
            acceleration += 0.09f;
        }
        else if (!(Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)) && speedMagnitude == 0)
        {
            acceleration = 0.01f;
        }*/

        if (Input.GetKey(KeyCode.LeftShift))
        {
            //moveSpeed = sprintSpeed;
            /*
            if (stamina > 1)
            {
                moveSpeed = SprintSpeed;
                if (speedMagnitude > 1)
                    _gameHandler.GetComponent<PlayerStats_Script>().stamina -= staminaDrain;
                else if (speedMagnitude < 1)
                    _gameHandler.GetComponent<PlayerStats_Script>().stamina += staminaReg;
            }
            else if (stamina < 1 && stamina > 0)
            {
                moveSpeed = _moveSpeed;
            }*/
        }
        else
        {
            //moveSpeed = _moveSpeed;
            /*
            if (stamina < 200)
            {
                _gameHandler.GetComponent<PlayerStats_Script>().stamina += staminaReg;
            }*/
        }
    }

    void OnControllerColliderHit(ControllerColliderHit other)
    {
        /*
        if (other.normal.x < 0 && other.normal.z < 0 && other.normal.y > 0.1 && other.collider.sharedMaterial != null)
        {
            matName = other.collider.sharedMaterial.name;
        }
        if (cc.isGrounded && step == true && speedMagnitude > 1 && speedMagnitude < 4)
        {
            switch (matName)
            {
                case "Metal":
                    WalkOnMetal();
                    break;
                case "Stone":
                    WalkDefault(); ;
                    break;
                case "Carpet":
                    WalkOnCarpet();
                    break;
                case "Wood":
                    WalkOnWood();
                    break;
                default:
                    WalkDefault();
                    break;
            }
        }
        else if (cc.isGrounded && step == true && speedMagnitude > 4 && speedMagnitude < 5)
        {
            switch (matName)
            {
                case "Metal":
                    RunOnMetal();
                    break;
                case "Stone":
                    RunDefault(); ;
                    break;
                case "Carpet":
                    RunOnCarpet();
                    break;
                case "Wood":
                    RunOnWood();
                    break;
                default:
                    RunDefault();
                    break;
            }
        }*/
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

    /* IEnumerator WaitForFootSteps(float stepsLength)
     {
         step = false;
         yield return new WaitForSeconds(stepsLength);
         step = true;
     }
     void WalkDefault()
     {
         audioSourc.clip = defaultSteps[Random.Range(0, defaultSteps.Length)];
         audioSourc.volume = 0.08f;
         audioSourc.Play();
         StartCoroutine(WaitForFootSteps(audioStepLengthWalk));
     }
     void RunDefault()
     {
         audioSourc.clip = defaultSteps[Random.Range(0, defaultSteps.Length)];
         audioSourc.volume = 0.1f;
         audioSourc.Play();
         StartCoroutine(WaitForFootSteps(audioStepLengthRun));
     }
     void WalkOnCarpet()
     {
         audioSourc.clip = carpetSteps[Random.Range(0, carpetSteps.Length)];
         audioSourc.volume = 0.08f;
         audioSourc.Play();
         StartCoroutine(WaitForFootSteps(audioStepLengthWalk));
     }
     void RunOnCarpet()
     {
         audioSourc.clip = carpetSteps[Random.Range(0, carpetSteps.Length)];
         audioSourc.volume = 0.1f;
         audioSourc.Play();
         StartCoroutine(WaitForFootSteps(audioStepLengthRun));
     }
     void WalkOnMetal()
     {
         audioSourc.clip = metalSteps[Random.Range(0, metalSteps.Length)];
         audioSourc.volume = 0.08f;
         audioSourc.Play();
         StartCoroutine(WaitForFootSteps(audioStepLengthWalk));
     }
     void RunOnMetal()
     {
         audioSourc.clip = metalSteps[Random.Range(0, metalSteps.Length)];
         audioSourc.volume = 0.1f;
         audioSourc.Play();
         StartCoroutine(WaitForFootSteps(audioStepLengthRun));
     }
     void WalkOnWood()
     {
         audioSourc.clip = woodSteps[Random.Range(0, woodSteps.Length)];
         audioSourc.volume = 0.08f;
         audioSourc.Play();
         StartCoroutine(WaitForFootSteps(audioStepLengthWalk));
     }
     void RunOnWood()
     {
         audioSourc.clip = woodSteps[Random.Range(0, woodSteps.Length)];
         audioSourc.volume = 0.1f;
         audioSourc.Play();
         StartCoroutine(WaitForFootSteps(audioStepLengthRun));
     }
     */
}
