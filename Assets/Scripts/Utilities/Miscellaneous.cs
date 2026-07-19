using UnityEngine;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;
using Unity.Hierarchy;

public static class Misc
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
        return points.Exists(point => Misc.VecCmp(point, pos, distance));
    }

    public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles) {
        return Quaternion.Euler(angles) * (point - pivot) + pivot;
    }

    // Dynamically creates a mesh for the sampler to create points on for prop spawning on surfaces
    // This is here since standard meshes (box, cylinder, plane etc.) cannot be loaded like an ordinary asset with a path
    public static Mesh CreatePlaneMesh(Vector3 size)
    {
        Mesh mesh = new Mesh
        {
            name = "Plane",
            vertices = new Vector3[] { new(size.x, 0, size.z), new(size.x, 0, -size.z), new(-size.x, 0, size.z), new(-size.x, 0, -size.z) },
            uv = new Vector2[] { new(1, 1), new(1, 0), new(0, 1), new(0, 0) },
            triangles = new int[] { 0, 1, 2, 2, 1, 3 }
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    public static List<T> ShuffleList<T>(List<T> list)
    {
        System.Random rng = new();
        return list.OrderBy(_ => rng.Next()).ToList();
    }

    private static bool IsOfType<T>(System.Type type)
    {
        return typeof(T).GetType() == type;
    }

    private static void SetVal<T, V>(T p, string var, V val)
    {
        Type t = p.GetType();
        PropertyInfo prop = t.GetProperty(var);
        prop.SetValue(p, val);
    }

    private static V GetVal<T, V>(T p, string var)
    {
        Type t = p.GetType();
        PropertyInfo prop = t.GetProperty(var);
        return (V) prop.GetValue(p);
    }

    public static (int, int) GetRandomProp<T>(List<T> props)
    {
        string var = "SpawnChance";
        int count = props.Count;
        float totalProbability = 0;

        T[] cdf = new T[count];

        for (int i = 0; i < count; i++)
        {
            totalProbability += GetVal<T, float>(props[i], var);
            cdf[i] = (T) Activator.CreateInstance(typeof(T), props[i]);
        }

        for (int i = 0; i < count; i++)
            SetVal(cdf[i], var, GetVal<T, float>(cdf[i], var) / totalProbability);

        float rand = UnityEngine.Random.value;

        int low = 0;
        int high = cdf.Length - 1;

        while (low < high)
        {
            int mid = (low + high) / 2;

            if (GetVal<T, float>(props[mid], var) >= rand)
                high = mid;
            else 
                low = mid + 1;
        }

        return (low, high);
    }

    public static (Prop, int) GetRandomPropCDF(List<Prop> props, PropHierarchy.PropHierachyInfo hierachyInfo)
    {
        if(props.Count == 0) return (null, 0);

        MeshNode mesh = Prop.Props?.GetParentRoomNode(hierachyInfo.id);
        Environment env = mesh?.GetEnvironment;
        bool ignoreSub = env && env.IgnoreSubElements && !hierachyInfo.IsNode();

        props.RemoveAll((p) => p.SpawnChance == 0.0f || !ignoreSub && env && env.GetEntry(p.KeyWords) == null || !Prop.Props.CanSpawnProp(hierachyInfo.id, p));
        props.Sort((a, b) => a.SpawnChance.CompareTo(b.SpawnChance));

        int count = props.Count;

        if(count == 0) return (null, 0);

        if (env)
        {
            // Get a list of all required props, sorted by largest to smallest size
            List<Prop> propsRequired = new(props);

            // Remove all props that do not have a limit, that has already reached their minimum spawned amount, or has already been spawned at least once if required to do so
            propsRequired.RemoveAll(p =>
            {
                bool hasNotEntry = env.GetEntry(p.KeyWords) == null;
                bool hasReachedMin = Prop.Props.GetSpawnedPropAmount(hierachyInfo.id, p) >= p.LowerLimitCount;
                bool hasAppeared = !hasNotEntry && env.GetEntry(p.KeyWords).Required && Prop.Props.HasPropSubAppeared(hierachyInfo.id, p.PropObject.name);

                return hasReachedMin || hasAppeared;
            });

            // Sort by the amount to spawn and then by its size
            propsRequired.Sort((a, b) =>
            {
                var ret = a.LowerLimitCount.CompareTo(b.LowerLimitCount);
                if (ret == 0) ret = a.GetSize.sqrMagnitude.CompareTo(b.GetSize.sqrMagnitude);
                return ret;
            });
            propsRequired.Reverse();
            
            // If the prop is required then return it until all of them has been spawned
            if (propsRequired.Count > 0) return (propsRequired[0], count);
        }

        (int low, int high) = GetRandomProp(props);

        return (props[low], count);
    }
}