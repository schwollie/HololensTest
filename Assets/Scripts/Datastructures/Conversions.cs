using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Conversions
{
    public static int FlooredIntDivision(int a, int b)
    {
        int temp = a / b;
        if (a * b < 0)
        {
            temp -= 1;
        }
        return temp;
    }

    public static Vector2Int FlooredIntDivision(Vector2Int vec, int div)
    {
        return new Vector2Int(FlooredIntDivision(vec.x, div), FlooredIntDivision(vec.y, div));
    }

    public static Vector3Int FlooredIntDivision(Vector3Int vec, int div)
    {
        return new Vector3Int(FlooredIntDivision(vec.x, div), FlooredIntDivision(vec.y, div), FlooredIntDivision(vec.z, div));
    }

    public static int FlooredDivision(float a, float b)
    {
        return Mathf.FloorToInt(a / b);
    }

    public static Vector2Int FlooredDivision(Vector2 vec, float div)
    {
        return new Vector2Int(FlooredDivision(vec.x, div), FlooredDivision(vec.y, div));
    }

    public static Vector3Int FlooredDivision(Vector3 vec, float div)
    {
        return new Vector3Int(FlooredDivision(vec.x, div), FlooredDivision(vec.y, div), FlooredDivision(vec.z, div));
    }
}
