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

public enum PropRotationTypeEnum
{
    Default,
    World,
    Parent
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
    [SerializeField] private bool _spawnInCorners;
    [SerializeField] private NodeData.NodeType _nodeTypeToSpawnIn;
    [SerializeField] private NodeData.EnvironmentType _environmentsToSpawnIn;
    [SerializeField] private List<Structure> _structureType;
    [SerializeField] private List<PropNeighborProperty> _neighbors;
    [SerializeField] private int _limitCount;
    [SerializeField] private PropRotationTypeEnum _spawnRotationType;
    [SerializeField] private float _spawnRotation;

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
    public bool SpawnInCorners => _spawnInCorners;
    public PropLimitTypeEnum LimitType => _limitType;
    public PropSpawnTagEnum SpawnTag => _spawnTag;
    public int LimitCount => _limitCount;
    public PropRotationTypeEnum SpawnRotationType => _spawnRotationType;
    public float SpawnRotationAmount => _spawnRotation;

    public Prop(Prop p)
    {
        _prop = p._prop;
        _spawnChance = p._spawnChance;
        _propType = p._propType;
        _nodeTypeToSpawnIn = p._nodeTypeToSpawnIn;
        _propPlacement = p._propPlacement;
        _structureType = new List<Structure>(p._structureType);
        _neighbors = new List<PropNeighborProperty>(p._neighbors);
        _spawnPositions = p._spawnPositions;
        _useStaticPositions = p._useStaticPositions;
        _spawnInCorners = p.SpawnInCorners;
        _limitType = p._limitType;
        _spawnTag = p._spawnTag;
        _limitCount = p._limitCount;
        _spawnRotationType = p._spawnRotationType;
        _spawnRotation = p._spawnRotation;
    }

    // used for gizmos only
    public Prop(SpawnPosition spawnPosition)
    {
        _spawnPositions = spawnPosition;
        _useStaticPositions = true;
    }

    public PropObject SpawnFloor(Sample sample, GameObject parent, PropHierarchy.PropHierachyInfo parentHierarchy, Func<Vector3, Prop, bool> spawnFilterFunc)
    {
        Vector3 pos = sample.sample + sample.triangleNormal * 0.1f;
        Vector3 rot = Vector3.one;

        Func<List<Collider>, IEnumerable<Collider>> filterFunc = (List<Collider> cols) =>
        {
            if (parent.GetComponent<MeshSurface>() != null)
                return cols.Where(c => c.transform.parent == parent.transform);
            else return cols.AsEnumerable();
        };

        if (!Props.CanSpawnProp(parentHierarchy.id, this)) return null;
        if (!spawnFilterFunc(sample.sample, this)) return null;
        if (parent.GetType() == typeof(MeshSurface))
            Debug.Log($"mesh surface overlap box check: {_prop.CheckOverlapBoxCircumference(pos, filterFunc) == Vector3.one}");
        if ((rot = _prop.CheckOverlapBoxCircumference(pos, filterFunc)) == Vector3.one) return null;

        PropObject propObj = Instantiate(_prop, sample.sample, Quaternion.Euler(rot));
        propObj.transform.SetParent(parent.transform);

        Props.Increase(parentHierarchy.id, _prop.name);
        propObj.UpdateChildren(parentHierarchy);

        return propObj;
    }

    public PropObject SpawnWall(Sample sample, Quaternion rotation, GameObject parent, PropHierarchy.PropHierachyInfo parentHierarchy, Func<Vector3, Prop, bool> spawnFilterFunc)
    {
        BoxCollider col = _prop.GetComponent<BoxCollider>();
        Vector3 pos = sample.sample + sample.triangleNormal * 0.1f + rotation * col.center;

        Func<List<Collider>, IEnumerable<Collider>> filterFunc = (List<Collider> cols) => cols.AsEnumerable();

        if (!Props.CanSpawnProp(parentHierarchy.id, this)) return null;
        if (!spawnFilterFunc(sample.sample, this)) return null;
        if (_prop.CheckOverlapBox(pos, rotation, filterFunc)) return null;

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