using UnityEngine;
using System;

public enum PropPlacement
{
    Floor,
    Wall,
    Ceiling
};

public enum Prop
{
    Decoration,
    Objective,
    Enemy
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

[CreateAssetMenu(fileName = "PropData", menuName = "Props/PropData")]
public class PropData : ScriptableObject
{
    [SerializeField] private GameObject _prop;

    //Neighbor List

    [SerializeField] private PropPlacement placement;
    [SerializeField] private int _limitCount;
    [SerializeField] private PropLimitTypeEnum _limitType;
    [SerializeField] private bool _checkOrentation;
    [SerializeField] private PropSpawnTagEnum _spawnTag;

    [SerializeField] private Prop _propType;
    [SerializeField] [Range(0, 1)] private float _spawnChance = 0.5f;

    [SerializeField] private NodeType _nodeTypeToSpawnIn;
    public static PropHierarchy Props = new PropHierarchy(null, Guid.Empty);

    public GameObject Prop => _prop;
    public PropPlacement Placement => placement;
    public int LimitCount => _limitCount;
    public PropLimitTypeEnum LimitType => _limitType;

    public bool CheckOrientation => _checkOrentation;
    public PropSpawnTagEnum SpawnTag => _spawnTag;
    public Prop PropType => _propType;
    
    public float SpawnChance => _spawnChance;
    public NodeType NodeTypeToSpawnIn => _nodeTypeToSpawnIn;
}