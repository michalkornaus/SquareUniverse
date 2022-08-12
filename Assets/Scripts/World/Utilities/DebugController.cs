using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DebugController : MonoBehaviour
{
    [Header("Debug menu")]
    public GameObject debugPanel;
    public TMP_Text coordsPointingText;
    public TMP_Text blockPointingText;
    public TMP_Text biomPointingText;

    public TMP_Text coordsStandingText;
    public TMP_Text blockStandingText;
    public TMP_Text biomStandingText;

    public TMP_Text chunkText;
    private bool debugEnabled = false;

    private Transform player;
    private Camera cam;
    private WorldController worldController;
    private World world;

    // Start is called before the first frame update
    void Start()
    {
        worldController = GetComponent<WorldController>();
        world = worldController.world;
        cam = Camera.main;
        player = GameObject.FindWithTag("Player").transform;
    }
    private void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            debugEnabled = !debugEnabled;
        }
        UpdateDebug();
    }
    private void UpdateDebug()
    {
        if (debugEnabled)
        {
            debugPanel.SetActive(true);
            //Getting block coords player is pointing at
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.tag == "Chunk")
                {
                    var p = hit.point - (hit.normal / 2f);
                    int _x = Mathf.FloorToInt(p.x);
                    int _y = Mathf.FloorToInt(p.y);
                    int _z = Mathf.FloorToInt(p.z);
                    coordsPointingText.text = "x: " + _x + " y: " + _y + " z: " + _z;
                    ushort id = world[_x, _y, _z];
                    ushort biom = world[_x, _z];
                    blockPointingText.text = Enums.GetBlockName(id) + " ID[" + id + "]";
                    biomPointingText.text = Enums.GetBiomName(biom) + " ID[" + biom + "]";
                }
            }
            else
            {
                coordsPointingText.text = "";
                biomPointingText.text = "";
                blockPointingText.text = "";
            }
            //Getting block coords player is standing on
            if (Physics.Raycast(player.position, Vector3.down, out RaycastHit _hit))
            {
                if (_hit.collider.tag == "Chunk")
                {
                    var p = _hit.point - (_hit.normal / 2f);
                    int _x = Mathf.FloorToInt(p.x);
                    int _y = Mathf.FloorToInt(p.y);
                    int _z = Mathf.FloorToInt(p.z);
                    coordsStandingText.text = "x: " + _x + " y: " + _y + " z: " + _z;
                    ushort id = world[_x, _y, _z];
                    ushort biom = world[_x, _z];
                    blockStandingText.text = Enums.GetBlockName(id) + " ID[" + id + "]";
                    biomStandingText.text = Enums.GetBiomName(biom) + " ID[" + biom + "]";
                }
            }
            else
            {
                coordsStandingText.text = "";
                biomStandingText.text = "";
                blockStandingText.text = "";
            }
            //Getting chunk info
            var chunk = world.Chunks[ChunkId.FromWorldPos(Mathf.FloorToInt(player.position.x), Mathf.FloorToInt(player.position.z))];
            chunkText.text = chunk.name + "\nX: " + chunk.transform.position.x + ", Z: " + chunk.transform.position.z;
        }
        else
            debugPanel.SetActive(false);
    }
}
