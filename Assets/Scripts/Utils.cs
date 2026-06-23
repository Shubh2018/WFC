using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;

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

    public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles) {
        return Quaternion.Euler(angles) * (point - pivot) + pivot;
    }

    public static Mesh CreatePlaneMesh(Vector3 size)
    {
        Mesh mesh = new Mesh();

        mesh.name = "Plane";
        mesh.vertices =  new Vector3[] { new Vector3(size.x, 0, size.z), new Vector3(size.x, 0, -size.z), new Vector3(-size.x, 0, size.z), new Vector3(-size.x, 0, -size.z) };
        mesh.uv = new Vector2[] { new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 1), new Vector2(0, 0) };
        mesh.triangles = new int[] { 0, 1, 2, 2, 1, 3 };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    public static Spawner LoadProps(string path, Func<PropData, bool> filterFunc)
    {
        string[] props = Directory.GetFiles(path, "*.asset");
        Spawner spawner = new Spawner(true);

        foreach(string prop in props)
        {
            PropData propData = (PropData) AssetDatabase.LoadAssetAtPath(prop, typeof(PropData));
            if (filterFunc(propData)) spawner.AddProp(propData); 
        }

        return spawner;
    }
}