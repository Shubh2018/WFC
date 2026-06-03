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
    [SerializeField] private NodeData.NodeType _nodeTypeToSpawnIn;
    // [SerializeField] private NodeType _nodeType;
    [SerializeField] private List<Structure> _structureType;
    [SerializeField] private List<Environment> _environmentType;
    [SerializeField] private List<PropNeighborProperty> _neighbors;

    public PropObject PropObject => _prop;
    public List<PropNeighborProperty> Neighbors => _neighbors;

    public PropNeighborProperty GetRandomProp(NodeData node)
    {
        CompareNodeType(node);

        List<PropNeighborProperty> neighbors = new List<PropNeighborProperty>(_neighbors);

        neighbors.RemoveAll((n) => n.spawnChance == 0.0f);
        neighbors.Sort((x, y) => x.spawnChance.CompareTo(y.spawnChance));

        int count = neighbors.Count;

        float totalProbability = 0;

        PropNeighborProperty[] cdf = new PropNeighborProperty[count];

        for (int i = 0; i < count; i++)
        {
            totalProbability +=  neighbors[i].spawnChance;
            PropNeighborProperty n = new PropNeighborProperty()
            {
                prop = neighbors[i].prop,
                spawnChance = totalProbability,
                maxDistance = neighbors[i].maxDistance
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

            if (cdf[mid].spawnChance >= rand)
                high = mid;
            else 
                low = mid + 1;
        }

        return cdf[low];
    }

    public NodeData.NodeType CompareNodeType(NodeData node)
    {
        return (NodeData.NodeType)((int)node.nodeType & (int)_nodeTypeToSpawnIn);
    }
}

[Serializable]
public class Structure
{
    public StructureType structure;
    [Range(0.0f, 1.0f)] public float spawnChance;
};

[Serializable]
public class Environment
{
    public EnvironmentType environment;
    [Range(0.0f, 1.0f)] public float spawnChance;
}

[Serializable]
public class NodeProperty
{
    public NodeData.NodeType _spawnInNode;
    [Range(0.0f, 1.0f)] public float spawnChance;
}

[Serializable]
public class PropNeighborProperty
{
    public Prop prop;
    public float maxDistance;
    [Range(0.0f, 1.0f)] public float spawnChance;
}