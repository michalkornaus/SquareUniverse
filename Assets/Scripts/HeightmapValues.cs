using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightmapValues
{
    private double[] HeightValues = new double[16 * 16 * 64];
    public double Min { get; set; }
    public double Max { get; set; }
    public double this[int x, int z]
    {
        get { return HeightValues[x * 128 + z]; }
        set { HeightValues[x * 128 + z] = value; }
    }
}
