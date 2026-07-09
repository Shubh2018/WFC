using System;
using System.Collections.Generic;
using System.Linq;
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

public enum PropSpawnTagEnum {
    Small,
    Medium,
    Large,
    SmallToMedium,
    MediumToLarge,
    Any
};

public enum PropLimitTypeEnum {
    PerRoom,
    PerHierarchyElement,
    InTotal
};

[CreateAssetMenu(fileName = "Prop", menuName = "Props/Prop")]
public class Prop : ScriptableObject
{
    [SerializeField] private PropObject _prop;
    [SerializeField] [Range(0.0f, 1.0f)] private float _spawnChance = 0.0f;
    [SerializeField] private PropPlacementType _propPlacement;
    [SerializeField] private PropType _propType;
    [SerializeField] private PropLimitTypeEnum _limitType;
    [SerializeField] private PropSpawnTagEnum _spawnTag;
    [SerializeField] private SpawnPosition _spawnPositions;
    [SerializeField] private bool _useStaticPositions;
    [SerializeField] private NodeData.NodeType _nodeTypeToSpawnIn;
    [SerializeField] private NodeData.EnvironmentType _environmentsToSpawnIn;
    [SerializeField] private List<Structure> _structureType;
    [SerializeField] private List<PropNeighborProperty> _neighbors;
    [SerializeField] private int _limitCount;

    public static PropHierarchy Props = new PropHierarchy(null, Guid.Empty);

    public PropObject PropObject => _prop;
    public PropType PropType => _propType;
    public SpawnPosition SpawnPosition => _spawnPositions;
    public List<PropNeighborProperty> Neighbors => _neighbors;
    public NodeData.NodeType NodeTypeToSpawnIn => _nodeTypeToSpawnIn;
    public PropPlacementType Placement => _propPlacement;
    public NodeData.EnvironmentType EnvironmentTypeToSpawnIn => _environmentsToSpawnIn;
    public float SpawnChance {set { _spawnChance = value; } get {return _spawnChance;}}
    public bool UseStaticPositions => _useStaticPositions;
    public PropLimitTypeEnum LimitType => _limitType;
    public PropSpawnTagEnum SpawnTag => _spawnTag;
    public int LimitCount => _limitCount;

    public Prop(Prop p)
    {
        _prop = p._prop;
        _spawnChance = p._spawnChance;
        _propType = p._propType;
        _nodeTypeToSpawnIn = p._nodeTypeToSpawnIn;
        _structureType = new List<Structure>(p._structureType);
        _neighbors = new List<PropNeighborProperty>(p._neighbors);
        _spawnPositions = p._spawnPositions;
        _useStaticPositions = p._useStaticPositions;
        _limitType = p._limitType;
        _spawnTag = p._spawnTag;
        _limitCount = p._limitCount;
    }

    public PropObject Spawn(Sample sample, Quaternion rotation, bool wall, GameObject parent, PropHierarchy.PropHierachyInfo parentHierarchy, Func<Vector3, Prop, bool> spawnFilterFunc)
    {
        BoxCollider col = _prop.GetComponent<BoxCollider>();
        Vector3 pos = sample.sample + sample.triangleNormal * 0.1f + rotation * col.center;

        //Debug.Log($"spawn check, gameobject: {parent.name}, cannot spawn: {!Props.CanSpawnProp(parentHierarchy.id, this)}, filter func: {!spawnFilterFunc(sample.sample, this)}, overlap: {_prop.CheckOverlapBox(pos, rotation)}");

        if (!Props.CanSpawnProp(parentHierarchy.id, this)) return null;
        if (!spawnFilterFunc(sample.sample, this)) return null;
        //Debug.Log($"pos: {pos}, rot: {rotation}, sample: {sample.sample}, prop: {_prop.name}");
        //SpawnOverlapTest(pos, rotation, wall);
        if (_prop.CheckOverlapBox(pos, rotation)) return null;

        PropObject propObj = Instantiate(_prop, sample.sample, rotation);
        propObj.transform.SetParent(parent.transform);

        Props.Increase(parentHierarchy.id, _prop.name);
        propObj.UpdateChildren(parentHierarchy);

        return propObj;
    }

    public void SpawnOverlapTest(Vector3 position, Quaternion rotation, bool ignoreColCenter = false)
    {
        BoxCollider col = _prop.GetComponent<BoxCollider>();

        GameObject obj = new GameObject();
        obj.name = _prop.name;
        obj.transform.position = position;
        obj.transform.rotation = rotation;

        BoxCollider boxComponent = obj.AddComponent<BoxCollider>();
        if (!ignoreColCenter) boxComponent.center = col.center;
        boxComponent.enabled = false;
        boxComponent.size = col.size;

        ColDetectorTest colComponent = obj.AddComponent<ColDetectorTest>();
    }

    public Vector3 GetSize()
    {
        return _prop.GetComponent<BoxCollider>().size;
    }

    public PropNeighborProperty GetRandomProp(NodeData node)
    {
        if(!CompareNode(_nodeTypeToSpawnIn, node.nodeType)) return null;
        return GetRandomProp();
    }

    public PropNeighborProperty GetRandomProp()
    {
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