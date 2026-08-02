using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities.UniversalDelegates;
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

public enum DecorationType
{
    None,
    Table,
    Chair,
    Lamp,
    Barrel,
    Book,
    Potion,
    Cauldron,
    Shelf,
    Chest,
    Flag,
    Pot
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

[CreateAssetMenu(fileName = "Prop", menuName = "WFC/Prop")]
public class Prop : ScriptableObject
{
    // Private variables
    [SerializeField] private PropObject _prop;
    [SerializeField] [Range(0.0f, 1.0f)] private float _spawnChance = 0.0f;
    [SerializeField] private PropPlacementType _propPlacement;
    [SerializeField] private PropType _propType;
    [SerializeField] private DecorationType _decorationType;
    [SerializeField] private PropLimitTypeEnum _limitType;
    [Environment] [SerializeField] private string _environmentType;
    [SerializeField] private Node.NodeType _nodeType;
    [SerializeField] private bool _spacingEnabled;
    [SerializeField] private float _spacingAmount;
    [SerializeField] private PropSpawnTagEnum _spawnTag;
    [SerializeField] private SpawnPosition _spawnPositions;
    [SerializeField] private List<string> _keywords;
    [SerializeField] private bool _useStaticPositions;
    [SerializeField] private bool _spawnInCorners;
    [SerializeField] private List<PropNeighborProperty> _neighbors;
    [SerializeField] private int _neighborCount;
    [SerializeField] private int _lowerLimitCount;
    [SerializeField] private int _higherLimitCount;
    [SerializeField] private PropRotationTypeEnum _spawnRotationType;
    [SerializeField] private float _spawnRotation;

    // Public variables
    public static PropHierarchy Props = new(null, Guid.Empty);

    // Getters and Setters
    public PropObject PropObject => _prop;
    public PropType PropType => _propType;
    public DecorationType DecorationType => _decorationType;
    public SpawnPosition SpawnPosition => _spawnPositions;
    public List<PropNeighborProperty> Neighbors => _neighbors;
    public int MaxNeighborCount => _neighborCount;
    public PropPlacementType Placement => _propPlacement;
    public float SpawnChance {set { _spawnChance = value; } get {return _spawnChance;}}
    public bool UseStaticPositions => _useStaticPositions;
    public bool SpawnInCorners => _spawnInCorners;
    public PropLimitTypeEnum LimitType => _limitType;
    public string EnvironmentType => _environmentType;
    public Node.NodeType NodeType => _nodeType;
    public bool SpacingEnabled => _spacingEnabled;
    public float SpacingAmount => _spacingAmount;
    public PropSpawnTagEnum SpawnTag => _spawnTag;
    public int LowerLimitCount => _lowerLimitCount;
    public int HigherLimitCount => _higherLimitCount;
    public PropRotationTypeEnum SpawnRotationType => _spawnRotationType;
    public float SpawnRotationAmount => _spawnRotation;
    public List<string> KeyWords => _keywords;
    public Vector3 GetSize => _prop.GetComponent<BoxCollider>().size;

    public Prop(Prop p)
    {
        _prop = p._prop;
        _spawnChance = p._spawnChance;
        _propType = p._propType;
        _propPlacement = p._propPlacement;
        _neighbors = new List<PropNeighborProperty>(p._neighbors);
        _neighborCount = p._neighborCount;
        _spawnPositions = p._spawnPositions;
        _useStaticPositions = p._useStaticPositions;
        _spawnInCorners = p.SpawnInCorners;
        _limitType = p._limitType;
        _spawnTag = p._spawnTag;
        _lowerLimitCount = p._lowerLimitCount;
        _higherLimitCount = p._higherLimitCount;
        _spawnRotationType = p._spawnRotationType;
        _spawnRotation = p._spawnRotation;
    }

    // used for gizmos only
    public Prop(SpawnPosition spawnPosition)
    {
        _spawnPositions = spawnPosition;
        _useStaticPositions = true;
    }

    public bool IsSpecialised()
    {
        return _propType != PropType.Decoration;
    }

    public PropObject SpawnFloor(List<Sample> samples, List<PropObject> propObjs, GameObject parent, PropHierarchy.PropHierachyInfo parentHierarchy, Func<Vector3, Prop, bool> spawnFilterFunc)
    {
        foreach (Sample sample in samples)
        {
            Vector3 pos = sample.sample + sample.triangleNormal * 0.1f;
            Quaternion rot = _prop.GetRotation(parent, Placement, _spawnRotationType, _spawnRotation);

            IEnumerable<Collider> filterFunc(List<Collider> cols)
            {
                if (parent.GetComponent<MeshSurface>() || parent.GetComponent<MeshPoint>())
                    return cols.Where(c => c.gameObject.GetInstanceID() != parent.gameObject.GetInstanceID() 
                        && c.gameObject.GetInstanceID() != parent.transform.parent.gameObject.GetInstanceID());
                else return cols.AsEnumerable();
            }

            if (propObjs.Any(p => !p.OutsideSpacing(this, sample.sample))) continue;
            if (!spawnFilterFunc(sample.sample, this)) continue;
            if (_spawnRotationType != PropRotationTypeEnum.Default && _prop.CheckOverlapBox(pos, rot, filterFunc)) continue;
            else if ((rot = _prop.CheckOverlapBoxCircumference(pos, filterFunc)).Equals(Misc.quatEmpty)) continue;

            PropObject propObj = Instantiate(_prop, sample.sample, rot);
            propObj.transform.SetParent(parent.transform);
            propObj.Prop = this;
            propObj.RotateTo(Placement, _spawnRotationType, _spawnRotation);

            Props.Increase(parentHierarchy.id, _prop.name);
            propObj.UpdateChildren(parentHierarchy);

            return propObj;   
        }

        return null;
    }

    public PropObject SpawnWall(List<Sample> samples, List<PropObject> propObjs, GameObject parent, PropHierarchy.PropHierachyInfo parentHierarchy, Func<Vector3, Prop, bool> spawnFilterFunc)
    {
        foreach(Sample sample in samples)
        {
            BoxCollider col = _prop.GetComponent<BoxCollider>();
            Quaternion rotation = sample.triangleNormal != Vector3.zero ? Quaternion.LookRotation(sample.triangleNormal) : Quaternion.identity;
            Quaternion rot2 = _prop.GetRotation(parent, Placement, _spawnRotationType, _spawnRotation);
            Vector3 pos = sample.sample + sample.triangleNormal * 0.1f + rotation * col.center;

            static IEnumerable<Collider> filterFunc(List<Collider> cols) => cols.AsEnumerable();

            if (propObjs.Any(p => p.Prop.SpacingEnabled && Vector3.Distance(p.transform.position, sample.sample) < p.Prop.SpacingAmount)) continue;
            if (!spawnFilterFunc(sample.sample, this)) continue;
            if (_spawnRotationType != PropRotationTypeEnum.Default && _prop.CheckOverlapBox(pos, rot2, filterFunc)) continue;
            else if (_prop.CheckOverlapBox(pos, rotation, filterFunc)) continue;

            PropObject propObj = Instantiate(_prop, sample.sample, rotation);
            propObj.transform.SetParent(parent.transform);
            propObj.transform.position = sample.sample;
            propObj.transform.forward = sample.triangleNormal;
            propObj.Prop = this;
            propObj.RotateTo(Placement, _spawnRotationType, _spawnRotation);

            Props.Increase(parentHierarchy.id, _prop.name);
            propObj.UpdateChildren(parentHierarchy);

            return propObj;
        }

        return null;
    }
}

[Serializable]
public class PropNeighborProperty
{
    public Prop prop;
    public float maxDistance;
    [Range(0.0f, 1.0f)] private float _spawnChance;

    public float SpawnChance {set { _spawnChance = value; } get {return _spawnChance;}}

    public PropNeighborProperty() {}

    public PropNeighborProperty(PropNeighborProperty other)
    {
        prop = other.prop;
        maxDistance = other.maxDistance;
        SpawnChance = other.SpawnChance;
    }
}