using UnityEngine;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;
using Unity.Hierarchy;
using Unity.Entities.UniversalDelegates;

public static class Misc
{
    public static readonly Vector3Int[] offsets = new Vector3Int[]
    {
        Vector3Int.forward,
        Vector3Int.back,
        Vector3Int.right,
        Vector3Int.left,
        Vector3Int.up,
        Vector3Int.down
    };

    public static readonly Vector3Int[] offsets2 = new Vector3Int[]
    {
        Vector3Int.forward,
        Vector3Int.back,
        Vector3Int.right,
        Vector3Int.left
    };

    public static readonly Vector3Int[] offsets3 = new Vector3Int[]
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

    public static Quaternion quatEmpty => new(1, 0, 1, 0);

    public static bool VecCmp(Vector3Int a, Vector3Int b, float distance = 1.0f)
    {
        return Vector3Int.Distance(a, b) <= distance;
    }

    public static bool CheckPosValid(Vector3Int pos, int width, int height, int length)
    {
        return pos.x < width 
             && pos.x > -1
             && pos.y < height
             && pos.y > -1
             && pos.z < length
             && pos.z > -1;
    }

    public static bool CheckVectorOverlap(List<Vector3Int> points, Vector3Int pos, float distance = 1.0f)
    {
        return points.Exists(point => Misc.VecCmp(point, pos, distance));
    }

    public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles) {
        return Quaternion.Euler(angles) * (point - pivot) + pivot;
    }

    public static bool IsInBounds(BoxCollider a, BoxCollider b)
    {
        return a.size.x > b.size.x 
            && a.size.y > b.size.y
            && a.size.z > b.size.z;
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

    public static bool WithinDisOfCam(Vector3 pos, float maxDistance)
    {
        Camera cam = Camera.current;
        float d = Vector3.Distance(pos, cam.transform.position);
        return d <= maxDistance;
    }

    public static List<T> ShuffleList<T>(List<T> list)
    {
        System.Random rng = new();
        return list.OrderBy(_ => rng.Next()).ToList();
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

    public static (int, int) GetSpawnChance<T>(List<T> props)
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

    public static Prop GetRandomProp(List<Prop> props, PropHierarchy.PropHierachyInfo hierachyInfo, List<Prop> exceptions, bool forceSpawn)
    {
        if(props.Count == 0) return null;

        MeshNode mesh = Prop.Props?.GetParentRoomNode(hierachyInfo.id);
        Environment env = mesh?.GetEnvironment;
        BoxCollider col = Prop.Props.FindEntry(hierachyInfo.id)?.childObj?.GetComponent<BoxCollider>(); // MeshPoints do not have a collider, therefore it may be null
        bool ignoreSub = env && env.IgnoreSubElements && !hierachyInfo.IsNode();

        // Remove props that either do not have a chance of spawning, are not allowed to spawn in this environment, has reached its max amount in this hierarchy, or is larger than the area itself
        props.RemoveAll((p) =>
        {
            bool noChance = p.SpawnChance == 0.0f;
            bool notAllowedToSpawn = !ignoreSub && env && env.GetEntry(p.KeyWords) == null;
            bool amountMax = !Prop.Props.CanSpawnProp(hierachyInfo.id, p);
            bool outOfBounds = col && !IsInBounds(col, p.PropObject.GetComponent<BoxCollider>());

            return noChance || notAllowedToSpawn || amountMax || outOfBounds;
        });

        // Remove exceptions
        props = props.Except(exceptions).ToList();

        // Sort the props based on chance
        props.Sort((a, b) => a.SpawnChance.CompareTo(b.SpawnChance));

        int count = props.Count;

        if(count == 0) return null;

        if (env)
        {
            // Get a list of all props
            List<Prop> propsRequired = new(props);

            // Remove all props that has already reached their minimum spawned amount, or has already been spawned at least once if required to do so
            propsRequired.RemoveAll(p =>
            {
                bool isInEnv = env.GetEntry(p.KeyWords) != null;
                bool isRequired = env.GetEntry(p.KeyWords).Required;
                bool isSpecialised = p.PropType != PropType.Decoration;

                bool hasReachedMin = Prop.Props.GetSpawnedPropAmount(hierachyInfo.id, p) >= p.LowerLimitCount;
                bool hasAppeared = Prop.Props.HasPropSubAppeared(hierachyInfo.id, p.PropObject.name);
                bool hasSpecialEnv = env.Name == p.EnvironmentType && p.NodeType.HasFlag(mesh.NodeData.nodeType);

                return isInEnv && (!isRequired && hasReachedMin || isRequired && hasAppeared && hasReachedMin) && (!isSpecialised || isSpecialised && !hasSpecialEnv);
            });

            // Sort by specialisation, then node requirement, magnitude size and lastly lower amount limit
            // This results in specialised larger required props spawning first, then decorative non-required smaller props later
            propsRequired.Sort((a, b) =>
            {
                var ret = b.NodeType.CompareTo(a.NodeType);
                if (ret == 0) ret = env.GetEntry(a.KeyWords).Required.CompareTo(env.GetEntry(b.KeyWords).Required);
                if (ret == 0) ret = a.GetSize.sqrMagnitude.CompareTo(b.GetSize.sqrMagnitude);
                if (ret == 0) ret = a.LowerLimitCount.CompareTo(b.LowerLimitCount);
                
                return ret;
            });

            propsRequired.Reverse();
            
            // If the list in not empty at this point, return the most important one (first one)
            if (propsRequired.Count > 0) return propsRequired[0];
        }

        (int low, int high) = GetSpawnChance(props);

        //if (!forceSpawn && UnityEngine.Random.Range(0.0f, 1.0f) > props[high].SpawnChance) return null; // comment out this line to force spawning objects, making chances only relevant for choice

        return props[high];
    }

    public static PropNeighborProperty GetRandomNeighborProp(Prop prop, PropHierarchy.PropHierachyInfo hierachyInfo, List<Prop> exceptions, bool forceSpawn)
    {
        List<PropNeighborProperty> neighbors = new(prop.Neighbors);

        Prop p = GetRandomProp(neighbors.Select(p => p.prop).ToList(), hierachyInfo, exceptions, forceSpawn);

        return neighbors.Find(n => n.prop.GetInstanceID() == p.GetInstanceID());
    }
}