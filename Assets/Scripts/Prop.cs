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
    [SerializeField] private PropObject _prop;
    [SerializeField] [Range(0.0f, 1.0f)] private float _spawnChance = 0.0f;
    [SerializeField] private PropPlacementType _propPlacement;
    [SerializeField] private PropType _propType;
    [SerializeField] private PropLimitTypeEnum _limitType;
    [SerializeField] private PropSpawnTagEnum _spawnTag;
    [SerializeField] private SpawnPosition _spawnPositions;
    [SerializeField] private List<string> _keywords;
    [SerializeField] private bool _useStaticPositions;
    [SerializeField] private bool _spawnInCorners;
    [SerializeField] private List<PropNeighborProperty> _neighbors;
    [SerializeField] private int _lowerLimitCount;
    [SerializeField] private int _higherLimitCount;
    [SerializeField] private PropRotationTypeEnum _spawnRotationType;
    [SerializeField] private float _spawnRotation;

    public static PropHierarchy Props = new PropHierarchy(null, Guid.Empty);

    public PropObject PropObject => _prop;
    public PropType PropType => _propType;
    public SpawnPosition SpawnPosition => _spawnPositions;
    public List<PropNeighborProperty> Neighbors => _neighbors;
    public PropPlacementType Placement => _propPlacement;
    public float SpawnChance {set { _spawnChance = value; } get {return _spawnChance;}}
    public bool UseStaticPositions => _useStaticPositions;
    public bool SpawnInCorners => _spawnInCorners;
    public PropLimitTypeEnum LimitType => _limitType;
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

    public PropObject SpawnFloor(List<Sample> samples, GameObject parent, PropHierarchy.PropHierachyInfo parentHierarchy, Func<Vector3, Prop, bool> spawnFilterFunc)
    {
        foreach (Sample sample in samples)
        {
            Vector3 pos = sample.sample + sample.triangleNormal * 0.1f;
            Vector3 rot = Vector3.one;

            Func<List<Collider>, IEnumerable<Collider>> filterFunc = (List<Collider> cols) =>
            {
                if (parent.GetComponent<MeshSurface>() != null)
                    return cols.Where(c => c.transform.parent == parent.transform);
                else return cols.AsEnumerable();
            };

            if (!spawnFilterFunc(sample.sample, this)) continue;
            if ((rot = _prop.CheckOverlapBoxCircumference(pos, filterFunc)) == Vector3.one) continue;

            PropObject propObj = Instantiate(_prop, sample.sample, Quaternion.Euler(rot));
            propObj.transform.SetParent(parent.transform);
            propObj.RotateTo(Placement, _spawnRotationType, _spawnRotation);

            Props.Increase(parentHierarchy.id, _prop.name);
            propObj.UpdateChildren(parentHierarchy);

            return propObj;   
        }

        return null;
    }

    public PropObject SpawnWall(List<Sample> samples, GameObject parent, PropHierarchy.PropHierachyInfo parentHierarchy, Func<Vector3, Prop, bool> spawnFilterFunc)
    {
        if (!Props.CanSpawnProp(parentHierarchy.id, this)) return null;

        foreach(Sample sample in samples)
        {
            BoxCollider col = _prop.GetComponent<BoxCollider>();
            Quaternion rotation = sample.triangleNormal != Vector3.zero ? Quaternion.LookRotation(sample.triangleNormal) : Quaternion.identity;
            Vector3 pos = sample.sample + sample.triangleNormal * 0.1f + rotation * col.center;

            Func<List<Collider>, IEnumerable<Collider>> filterFunc = (List<Collider> cols) => cols.AsEnumerable();

            if (!spawnFilterFunc(sample.sample, this)) continue;
            if (_prop.CheckOverlapBox(pos, rotation, filterFunc)) continue;

            PropObject propObj = Instantiate(_prop, sample.sample, rotation);
            propObj.transform.SetParent(parent.transform);
            propObj.transform.position = sample.sample;
            propObj.transform.forward = sample.triangleNormal;
            propObj.RotateTo(Placement, _spawnRotationType, _spawnRotation);

            Props.Increase(parentHierarchy.id, _prop.name);
            propObj.UpdateChildren(parentHierarchy);

            return propObj;
        }

        return null;
    }

    public PropNeighborProperty GetRandomProp()
    {
        List<PropNeighborProperty> neighbors = new List<PropNeighborProperty>(_neighbors);
        neighbors.RemoveAll((n) => n.SpawnChance == 0.0f);
        neighbors.Sort((a, b) => a.SpawnChance.CompareTo(b.SpawnChance));

        if(neighbors.Count == 0) return null; 

        (int low, int high) = Misc.GetRandomProp(neighbors);

        return neighbors[low];
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