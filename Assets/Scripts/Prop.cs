using System;
using System.Collections.Generic;
using UnityEngine;

public enum PropPlacementType
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
    Study,
    Cellar,
    Garden,
};

public enum SpawnPosition
{
    Random,
    Center,
    North,
    South,
    East,
    West,
    NorthEast,
    NorthWest,
    SouthEast,
    SouthWest
};

// public enum NodeType
// {

// };

[CreateAssetMenu(fileName = "Prop", menuName = "Props/Prop")]
public class Prop : ScriptableObject
{
    [SerializeField] private PropObject _prop;
    [SerializeField] [Range(0.0f, 1.0f)] private float _spawnChance = 0.0f;
    [SerializeField] private Prop _parent;
    [SerializeField] private PropPlacementType _propPlacement;
    [SerializeField] private PropType _propType;
    [SerializeField] private SpawnPosition _spawnPositions;
    [SerializeField] private bool _useStaticPositions;
    [SerializeField] private NodeData.NodeType _nodeTypeToSpawnIn;
    [SerializeField] private NodeData.EnvironmentType _environmentsToSpawnIn;
    [SerializeField] private List<Structure> _structureType;
    [SerializeField] private List<PropNeighborProperty> _neighbors;

    public PropObject PropObject => _prop;
    public SpawnPosition SpawnPosition => _spawnPositions;
    public List<PropNeighborProperty> Neighbors => _neighbors;
    public NodeData.NodeType NodeTypeToSpawnIn => _nodeTypeToSpawnIn;
    public NodeData.EnvironmentType EnvironmentTypeToSpawnIn => _environmentsToSpawnIn;
    public float SpawnChance {set { _spawnChance = value; } get {return _spawnChance;}}
    public bool UseStaticPositions => _useStaticPositions;

    public Prop(Prop p)
    {
        this._prop = p._prop;
        this._spawnChance = p._spawnChance;
        this._parent = p._parent;
        this._propType = p._propType;
        this._nodeTypeToSpawnIn = p._nodeTypeToSpawnIn;
        this._structureType = new List<Structure>(p._structureType);
        this._neighbors = new List<PropNeighborProperty>(p._neighbors);
        this._spawnPositions = p._spawnPositions;
        this._useStaticPositions = p._useStaticPositions;
    }

    public PropNeighborProperty GetRandomProp(NodeData node)
    {
        if(!CompareNode(_nodeTypeToSpawnIn, node.nodeType)) return null;

        List<PropNeighborProperty> neighbors = new List<PropNeighborProperty>(_neighbors);

        neighbors.RemoveAll((n) => n.spawnChance == 0.0f);
        neighbors.Sort((x, y) => x.spawnChance.CompareTo(y.spawnChance));

        int count = neighbors.Count;

        if(count <= 0) return null; 

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

    public bool CompareNode<T>(T nodeTypeToSpawnIn, T nodeType) where T : Enum
    {
        if(!typeof(T).IsEnum)
            throw new ArgumentException($"T must be an enum");

        return nodeTypeToSpawnIn.HasFlag(nodeType) || nodeType.HasFlag(nodeTypeToSpawnIn);
    }
}

[Serializable]
public class Structure
{
    public StructureType structure;
    [Range(0.0f, 1.0f)] public float spawnChance;
};

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