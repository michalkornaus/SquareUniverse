using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Atlas
{
    //UVS TEXTURES
    private static readonly float UvOffset = 0f;
    private static readonly int xsize = 128;
    private static readonly int ysize = 128;
    private static readonly int xlength = 16;
    private static readonly int ylength = 16;
    //Generating UV for all 6 sides
    public static Vector2[] GetUV(int x, int y)
    {
        Vector2[] p = GetUVSide(x, y);
        Vector2[] uv = {
        //Bottom
        p[1], p[3], p[2], p[0],
        //Left
        p[1], p[3], p[2], p[0],
        // Front 
        p[1], p[3], p[2], p[0],
        // Back
        p[1], p[3], p[2], p[0],
        // Right
        p[1], p[3], p[2], p[0],
        // Top
        p[1], p[3], p[2], p[0],
        };
        return uv;
    }

    private static Vector2[] GetUVSide(int x, int y)
    {
        float xmin = Mathf.InverseLerp(0f, xsize, x);
        float xmax = Mathf.InverseLerp(0f, xsize, x + xlength);
        float ymin = Mathf.InverseLerp(0f, ysize, y);
        float ymax = Mathf.InverseLerp(0f, ysize, y + ylength);
        Vector2[] uv = {
        new Vector2(xmin + UvOffset, ymin + UvOffset),
        new Vector2(xmin + UvOffset, ymax - UvOffset),
        new Vector2(xmax - UvOffset, ymin + UvOffset),
        new Vector2(xmax - UvOffset, ymax - UvOffset),
        };
        return uv;
    }
    //Generating custom UV for block
    public static Vector2[] GetCustomUV(int x, int y, int type)
    {
        switch (type)
        {
            //Grass id:1
            case 1:
                Vector2[] s = GetUVSide(x, y);
                Vector2[] t = GetUVSide(x, y);
                Vector2[] b = GetUVSide(x + 32, y + 32);
                Vector2[] uv = {
                //Bottom
                b[1], b[3], b[2], b[0],
                //Left
                s[1], s[3], s[2], s[0],
                // Front 
                s[1], s[3], s[2], s[0],
                // Back
                s[1], s[3], s[2], s[0],
                // Right
                s[1], s[3], s[2], s[0],
                // Top
                t[1], t[3], t[2], t[0],
                };
                return uv;
            //WoodenLog id:5
            case 2:
                s = GetUVSide(x, y);
                Vector2[] tb = GetUVSide(0, 32);
                uv = new Vector2[] {
                //Bottom
                tb[1], tb[3], tb[2], tb[0],
                //Left
                s[1], s[3], s[2], s[0],
                // Front 
                s[1], s[3], s[2], s[0],
                // Back
                s[1], s[3], s[2], s[0],
                // Right
                s[1], s[3], s[2], s[0],
                // Top
                tb[1], tb[3], tb[2], tb[0],
                };
                return uv;
            //Workbench id:16
            case 3:
                s = GetUVSide(x, y - 16);
                tb = GetUVSide(x, y);
                uv = new Vector2[] {
                //Bottom
                tb[1], tb[3], tb[2], tb[0],
                //Left
                s[1], s[3], s[2], s[0],
                // Front 
                s[1], s[3], s[2], s[0],
                // Back
                s[1], s[3], s[2], s[0],
                // Right
                s[1], s[3], s[2], s[0],
                // Top
                tb[1], tb[3], tb[2], tb[0],
                };
                return uv;
            //Furnace id:17
            case 4:
                Vector2[] f = GetUVSide(x, y);
                s = GetUVSide(x - 16, y);
                uv = new Vector2[] {
                //Bottom
                s[1], s[3], s[2], s[0],
                //Left
                s[1], s[3], s[2], s[0],
                // Front 
                f[1], f[3], f[2], f[0],
                // Back
                s[1], s[3], s[2], s[0],
                // Right
                s[1], s[3], s[2], s[0],
                // Top
                s[1], s[3], s[2], s[0],
                };
                return uv;
            //Grass
            default:
                return GetCustomUV(x, y, 1);
        }
    }
}

