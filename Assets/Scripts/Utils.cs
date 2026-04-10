using UnityEngine;
using System;
using System.Linq;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

public static class Utils
{
    public static Vector3Int[] offsets = new Vector3Int[]
    {
        Vector3Int.forward,
        Vector3Int.back,
        Vector3Int.right,
        Vector3Int.left,
        Vector3Int.up,
        Vector3Int.down
    };

    public static Vector3Int[] offsets2 = new Vector3Int[]
    {
        Vector3Int.forward,
        Vector3Int.back,
        Vector3Int.right,
        Vector3Int.left
    };

    public static Vector3Int[] offsets3 = new Vector3Int[]
    {
        Vector3Int.forward,
        Vector3Int.back,
        Vector3Int.right,
        Vector3Int.left,
        Vector3Int.up,
        Vector3Int.down,
        Vector3Int.forward + Vector3Int.up,
        Vector3Int.back + Vector3Int.up,
        Vector3Int.right + Vector3Int.up,
        Vector3Int.left + Vector3Int.up,
        Vector3Int.forward + Vector3Int.down,
        Vector3Int.back + Vector3Int.down,
        Vector3Int.right + Vector3Int.down,
        Vector3Int.left + Vector3Int.down,
    };

    public static bool VecCmp(Vector3Int a, Vector3Int b, float distance = 1.0f)
    {
        return Vector3Int.Distance(a, b) <= distance;
    }

    public static bool CheckPosValid(Vector3Int pos, int width, int height, int length)
    {
        return (pos.x < width 
             && pos.x > -1
             && pos.y < height
             && pos.y > -1
             && pos.z < length
             && pos.z > -1);
    }

    public static bool CheckVectorOverlap(List<Vector3Int> points, Vector3Int pos, float distance = 1.0f)
    {
        return points.Exists(point => Utils.VecCmp(point, pos, distance));
    }
}