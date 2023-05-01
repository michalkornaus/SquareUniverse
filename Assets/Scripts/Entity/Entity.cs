using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class Entity : MonoBehaviour
{
    private System.Random random = new System.Random();
    public string Name;
    [Range(1, 100)]
    public int Level;
    [Range(0, 100)]
    public int HealthPoints;
    [HideInInspector]
    public int _MaxHealthPoints;
    [Range(1, 10)]
    public int HealthMultiplier;

    public bool isHostile;

    [HideInInspector]
    public int PrefabID;

    [Range(0, 50)]
    public int DamagePoints;
    public float timeToAttack;
    public float pushForce;

    private float _timeToAttack = 0f;

    [Range(5, 30)]
    public int distanceToFollow;

    [Range(1, 20)]
    public int maxDistanceToWalk;

    private float distanceToPlayer;
    private bool isMoving = false;

    private WorldController _worldController;

    private NavMeshAgent navAgent;
    private MeshRenderer meshRenderer;

    private Player player;
    private Vector3 destinationVector;
    private Vector3 pushVector = Vector3.zero;
    private Rigidbody _rigidbody;

    private GameObject entityCanvas;

    public bool isOnNavMesh = false;

    private void Awake()
    {
        _MaxHealthPoints = HealthPoints;
        navAgent = GetComponent<NavMeshAgent>();
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        entityCanvas = GetComponentInChildren<EntityUI>().gameObject;
        _rigidbody = GetComponent<Rigidbody>();
        player = GameObject.FindWithTag("Player").GetComponent<Player>();
    }
    private void Start()
    {
        _worldController = GameObject.FindGameObjectWithTag("GameController").GetComponent<WorldController>();
        navAgent.updatePosition = false;
        InvokeRepeating(nameof(DisableEntity), 3f, 6f);
    }
    private void Update()
    {
        if (!isMoving && isOnNavMesh)
            StartCoroutine(WaitForMovement());

        if (_timeToAttack >= 0f)
            _timeToAttack -= Time.deltaTime;
    }
    private void DisableEntity()
    {
        //isOnNavMesh = navAgent.isOnNavMesh;
        //Check if the ground is under the entity
        LayerMask mask = LayerMask.GetMask("Chunks");
        if (!Physics.Raycast(transform.position + (Vector3.up * 2), Vector3.down, out RaycastHit hit, 10.0f, mask))
        {
            //The ground is not under the entity
            meshRenderer.enabled = false;
            entityCanvas.SetActive(false);

            navAgent.updatePosition = false;

            isOnNavMesh = false;
        }
        else
        {
            //The ground is under the entity
            meshRenderer.enabled = true;
            entityCanvas.SetActive(true);
            if (hit.collider.gameObject.GetComponentInParent<NavMeshSurface>() != null)
            {
                isOnNavMesh = true;
                navAgent.Warp(transform.position);
                navAgent.updatePosition = true;
            }
            else
            {
                isOnNavMesh = false;
                navAgent.updatePosition = false;
            }
        }
    }
    private void FixedUpdate()
    {
        if (navAgent == null)
            return;
        if (pushVector != Vector3.zero)
        {
            navAgent.updatePosition = false;
            navAgent.enabled = false;

            _rigidbody.isKinematic = false;
            _rigidbody.AddForce(pushVector, ForceMode.Impulse);
            pushVector = Vector3.zero;
        }
        if (!_rigidbody.isKinematic)
        {
            if (!_rigidbody.IsSleeping())
            {
                return;
            }
            else
            {
                _rigidbody.isKinematic = true;
                navAgent.updatePosition = true;
                navAgent.enabled = true;
            }
        }
        if (navAgent.enabled == false || !isOnNavMesh || !navAgent.isOnNavMesh)
            return;
        distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        if (isHostile && distanceToFollow > distanceToPlayer)
        {
            float angle = Vector3.Angle(player.transform.position - transform.position, transform.forward);
            if (angle <= 60f)
            {
                navAgent.SetDestination(player.transform.position);
                if (navAgent.remainingDistance <= 2.5f)
                {
                    var targetRotation = Quaternion.LookRotation(player.transform.position - transform.position);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.fixedDeltaTime);
                }
            }
            else
            {
                navAgent.SetDestination(destinationVector);
            }
        }
        else
        {
            navAgent.SetDestination(destinationVector);
        }
    }
    public void SetPushVector(Vector3 vector)
    {
        pushVector = vector;
    }
    public void OnTakenDamage(int damage)
    {
        HealthPoints -= damage;
        if (HealthPoints <= 0)
        {
            //ON DEAD GIVE PLAYER MEAT - FROM 10-30 KILLS DROP ELITE JUICY MEAT
            _timeToAttack = timeToAttack;
            navAgent.enabled = false;
            Destroy(gameObject, 0.1f);
        }
    }
    private IEnumerator WaitForMovement()
    {
        isMoving = true;
        yield return new WaitForSeconds(random.Next(1, 10));
        destinationVector = FindNewWaypoint();
        yield return new WaitForSeconds(random.Next(1, 10));
        isMoving = false;
    }
    private Vector3 FindNewWaypoint()
    {
        int valueX = random.Next(-maxDistanceToWalk, maxDistanceToWalk);
        int valueZ = random.Next(-maxDistanceToWalk, maxDistanceToWalk);
        Vector3 position = new(transform.position.x + valueX, 200, transform.position.z + valueZ);
        if (Physics.Raycast(position, Vector3.down, out RaycastHit _hit))
        {
            var p = _hit.point - (_hit.normal / 2f);
            int x = Mathf.FloorToInt(p.x);
            int y = Mathf.FloorToInt(p.y);
            int z = Mathf.FloorToInt(p.z);
            if (_worldController.world[x, y, z] == (ushort)Blocks.Grass && _worldController.world[x, y + 1, z] != (ushort)Blocks.WaterSource)
            {
                p = _hit.point + (_hit.normal / 2f);
                x = Mathf.FloorToInt(p.x);
                y = Mathf.FloorToInt(p.y);
                z = Mathf.FloorToInt(p.z);
                position = new Vector3(x, y, z);
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
    private void OnTriggerStay(Collider other)
    {
        if (!isHostile || !isOnNavMesh)
            return;
        if (other.CompareTag("Player"))
        {
            var pm = player.GetComponent<PlayerMovement>();
            if (pm.playmode == Playmodes.Survival)
            {
                if (_timeToAttack < 0f)
                {
                    Debug.Log("Hit the Player");
                    player.OnTakenDamage(DamagePoints);

                    //Push the player
                    pm.SetPushVector((transform.forward + transform.up) * pushForce);

                    _timeToAttack = timeToAttack;
                }
            }
        }
    }
}
