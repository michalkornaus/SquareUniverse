using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Atlas
{
    //UVS TEXTURES
    private static readonly float UvOffset = 0f;
    private static readonly int xsize = 512;
    private static readonly int ysize = 512;
    private static readonly int xlength = 32;
    private static readonly int ylength = 32;
    //Generating UV for all 6 sides
    private static Vector2[] GetUV(ushort voxel, int x, int y, int rotation)
    {
        x *= 32;
        y *= 32;
        switch (voxel)
        {
            //Grass
            case 1:
                Vector2[] s = GetUVSide(416, 224);
                Vector2[] t = GetUVSide(x, y);
                Vector2[] b = GetUVSide(96, 32);
                return NewUV(b, s, s, s, s, t);
            //Wooden Log
            case 5:
                s = GetUVSide(x, y);
                Vector2[] _tb = GetUVSide(288, 32);
                switch (rotation)
                {
                    case 0:
                        return NewUV(_tb, s, s, s, s, _tb);
                    case 1:
                        return NewUV(s, _tb, s, s, _tb, s);
                    case 2:
                        return NewUV(s, s, _tb, _tb, s, s);
                    default:
                        return NewUV(_tb, s, s, s, s, _tb);
                }
            //Workbench
            case 16:
                Vector2[] tb = GetUVSide(x, y);
                s = GetUVSide(x + 64, y);
                return NewUV(tb, s, s, s, s, tb);
            //Furnace
            case 17:
                Vector2[] f = GetUVSide(x, y);
                s = GetUVSide(x + 128, y);
                tb = GetUVSide(224, y);
                switch (rotation)
                {
                    case 0:
                        return NewUV(tb, s, f, s, s, tb);
                    case 1:
                        return NewUV(tb, f, s, s, s, tb);
                    case 2:
                        return NewUV(tb, s, s, f, s, tb);
                    case 3:
                        return NewUV(tb, s, s, s, f, tb);
                    default:
                        return NewUV(tb, s, f, s, s, tb);
                }
            //Rest of blocks
            default:
                s = GetUVSide(x, y);
                return NewUV(s, s, s, s, s, s);
        }

    }
    private static Vector2[] NewUV(Vector2[] bm, Vector2[] l, Vector2[] f, Vector2[] b, Vector2[] r, Vector2[] t)
    {
        Vector2[] uv = {
                //Bottom
                bm[1], bm[3], bm[2], bm[0],
                //Left
                l[1], l[3], l[2], l[0],
                // Front 
                f[1], f[3], f[2], f[0],
                // Back
                b[1], b[3], b[2], b[0],
                // Right
                r[1], r[3], r[2], r[0],
                // Top
                t[1], t[3], t[2], t[0],
                };
        return uv;
    }
    public static void SetUV(ushort voxel, ref List<Vector2> uvs)
    {
        int rotation = voxel % 10;
        voxel /= 10;
        switch (voxel)
        {
            //x, y - 1,3,5 brackets order -> 1,2,3 blocks order
            //Grass
            case 1:
                foreach (var uv in GetUV(1, 1, 1, rotation))
                    uvs.Add(uv);
                break;
            //Dirt 
            case 2:
                foreach (var uv in GetUV(2, 3, 1, rotation))
                    uvs.Add(uv);
                break;
            //Stone 
            case 3:
                foreach (var uv in GetUV(3, 5, 1, rotation))
                    uvs.Add(uv);
                break;
            //Sand 
            case 4:
                foreach (var uv in GetUV(4, 7, 1, rotation))
                    uvs.Add(uv);
                break;
            //WoodenLog 
            case 5:
                foreach (var uv in GetUV(5, 11, 1, rotation))
                    uvs.Add(uv);
                break;
            //Leaves 
            case 6:
                foreach (var uv in GetUV(6, 13, 1, rotation))
                    uvs.Add(uv);
                break;
            //Water 
            case 7:
                foreach (var uv in GetUV(7, 13, 5, rotation))
                    uvs.Add(uv);
                break;
            //Cobblestone
            case 8:
                foreach (var uv in GetUV(8, 3, 3, rotation))
                    uvs.Add(uv);
                break;
            //Planks
            case 9:
                foreach (var uv in GetUV(9, 1, 3, rotation))
                    uvs.Add(uv);
                break;
            //Clay
            case 10:
                foreach (var uv in GetUV(10, 5, 3, rotation))
                    uvs.Add(uv);
                break;
            //Bricks
            case 11:
                foreach (var uv in GetUV(11, 7, 3, rotation))
                    uvs.Add(uv);
                break;
            //Glass
            case 12:
                foreach (var uv in GetUV(13, 9, 3, rotation))
                    uvs.Add(uv);
                break;
            //Coal ore
            case 13:
                foreach (var uv in GetUV(14, 11, 3, rotation))
                    uvs.Add(uv);
                break;
            //Iron ore
            case 14:
                foreach (var uv in GetUV(15, 13, 3, rotation))
                    uvs.Add(uv);
                break;
            //Workbench
            case 15:
                foreach (var uv in GetUV(16, 9, 5, rotation))
                    uvs.Add(uv);
                break;
            //Furnace
            case 16:
                foreach (var uv in GetUV(17, 1, 5, rotation))
                    uvs.Add(uv);
                break;
            //Dirt
            default:
                foreach (var uv in GetUV(2, 3, 1, rotation))
                    uvs.Add(uv);
                break;
        }
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
}

