using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D.IK;

public enum Placement
{
    Floor,
    Wall,
    Ceiling
};

public enum PropType
{
    Decoration,
    Objective,
    Enemy
};

public enum StructureType
{
    Hallway,
    Kitchen,
    Basement  
};

public enum EnvironmentType
{
    Objective,
};

// public enum NodeType
// {

// };

[CreateAssetMenu(fileName = "Prop", menuName = "Props/Prop")]
public class Prop : ScriptableObject
{
    [SerializeField] private PropObject _prop;
    [SerializeField] [Range(0.0f, 1.0f)] private float spawnChance = 0.0f;
    [SerializeField] private Prop _parent;
    [SerializeField] private PropType _propType;
    // [SerializeField] private NodeType _nodeType;
    [SerializeField] private List<SpawnChanceInStructure> _structureType;
    [SerializeField] private List<SpawnChanceInEnvironment> _environmentType;
    [SerializeField] private List<SpawnChanceNeighbors> _neighbors;

    public Prop GetRandomProp()
    {
        List<SpawnChanceNeighbors> neighbors = new List<SpawnChanceNeighbors>(_neighbors);
        neighbors.Sort((x, y) => x.spawnChance.CompareTo(y.spawnChance));

        int count = neighbors.Count;

        float totalProbability = 0;

        SpawnChanceNeighbors[] cdf = new SpawnChanceNeighbors[count];

        for (int i = 0; i < count; i++)
        {
            totalProbability +=  neighbors[i].spawnChance;
            SpawnChanceNeighbors n = new SpawnChanceNeighbors()
            {
                prop = neighbors[i].prop,
                spawnChance = totalProbability
            };

            cdf[i] = n;
        }

        for (int i = 0; i < count; i++)
            cdf[i].spawnChance /= totalProbability;

        float rand = UnityEngine.Random.value;

        int low = 0;
        int high = cdf.Length - 1;

        while (low < high)
        {
            int mid = (low + high) / 2;

            if (cdf[mid].spawnChance <= 0)
                low += mid + 1; 

            if (cdf[mid].spawnChance >= rand)
                high = mid;
            else 
                low = mid + 1;
        }

        return cdf[low].prop;
    }
}

[Serializable]
public struct SpawnChanceInStructure
{
    public StructureType structure;
    [Range(0.0f, 1.0f)] public float spawnChance;
};

[Serializable]
public struct SpawnChanceInEnvironment
{
    public EnvironmentType environment;
    [Range(0.0f, 1.0f)] public float spawnChance;
}

[Serializable]
public struct SpawnChanceNeighbors
{
    public Prop prop;
    public float maxDistance;
    [Range(0.0f, 1.0f)] public float spawnChance;
}