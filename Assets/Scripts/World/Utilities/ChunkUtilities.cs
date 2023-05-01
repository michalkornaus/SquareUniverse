using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkUtilities : MonoBehaviour
{
    public GameObject[] customBlocks;
    public static readonly float cubeSize = 1f;
    public static readonly Vector3[] _c = GetPoints(0f);
    public static readonly Vector3[] _w = GetPoints(0.1f);
    public static readonly Vector3[] _cubeVertices = GetVertices(_c);
    public static readonly Vector3[] _waterVertices = GetVertices(_w);
    public static readonly List<Vector3[]> waterStates = GetWaterStates();
    private static List<Vector3[]> GetWaterStates()
    {
        List<Vector3[]> list = new()
        {
            GetVertices(GetPoints(0f)),     //70
            GetVertices(GetPoints(0.9f)),   //79
            GetVertices(GetPoints(0.8f)),   //78
            GetVertices(GetPoints(0.7f)),   //77
            GetVertices(GetPoints(0.6f)),   //76
            GetVertices(GetPoints(0.5f)),   //75
            GetVertices(GetPoints(0.4f)),   //74
            GetVertices(GetPoints(0.3f)),   //73
            GetVertices(GetPoints(0.2f)),   //72
            GetVertices(GetPoints(0.1f))    //71
        };
        return list;
    }
    private static Vector3[] GetVertices(Vector3[] p)
    {
        Vector3[] vec = new Vector3[]
        {
             p[0], p[1], p[2], p[3], // Bottom
             p[7], p[4], p[0], p[3], // Left
             p[4], p[5], p[1], p[0], // Front
             p[6], p[7], p[3], p[2], // Back
             p[5], p[6], p[2], p[1], // Right
             p[7], p[6], p[5], p[4]  // Top
        };
        return vec;
    }
    private static Vector3[] GetPoints(float offsetTop)
    {
        Vector3[] vec = new Vector3[]
        {
            //BOTTOM 4 points
          new Vector3 (0, 0, cubeSize),
          new Vector3 (cubeSize, 0, cubeSize),
          new Vector3 (cubeSize, 0, 0),
          new Vector3 (0, 0, 0),
            //TOP 4 points
          new Vector3 (0, cubeSize - offsetTop, cubeSize),
          new Vector3 (cubeSize, cubeSize - offsetTop, cubeSize),
          new Vector3 (cubeSize, cubeSize - offsetTop, 0),
          new Vector3 (0, cubeSize - offsetTop, 0),
        };
        return vec;
    }

    public static readonly int[] BottomQuad =
        { 3, 1, 0, 3, 2, 1 };
    public static readonly int[] LeftQuad =
    {   7, 5, 4, 7, 6, 5  };
    public static readonly int[] FrontQuad =
    {  11, 9, 8, 11, 10, 9   };
    public static readonly int[] BackQuad =
    {      15, 13, 12, 15, 14, 13   };
    public static readonly int[] RightQuad =
    {19, 17, 16, 19, 18, 17    };
    public static readonly int[] TopQuad =
    {     23, 21, 20, 23, 22, 21    };
    //NORMALS
    private static readonly Vector3 up = Vector3.up;
    private static readonly Vector3 down = Vector3.down;
    private static readonly Vector3 forward = Vector3.forward;
    private static readonly Vector3 back = Vector3.back;
    private static readonly Vector3 left = Vector3.left;
    private static readonly Vector3 right = Vector3.right;
    public static readonly Vector3[] _cubeNormals = new Vector3[]
       {
            down, down, down, down,             // Bottom
	        left, left, left, left,             // Left
	        forward, forward, forward, forward,	// Front
	        back, back, back, back,             // Back
	        right, right, right, right,         // Right
	        up, up, up, up                      // Top
       };
}
