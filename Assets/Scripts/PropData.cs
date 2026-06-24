using UnityEngine;
using System.Collections.Generic;
using System;

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
    [SerializeField] private int _limitCount;
    [SerializeField] private PropLimitTypeEnum _limitType;
    [SerializeField] private PropPlacementType placement;
    [SerializeField] private int _maxCount;
    [SerializeField] private bool _checkOrentation;
    [SerializeField] private PropSpawnTagEnum _spawnTag;
    [SerializeField] private PropType _propType;
    [SerializeField] [Range(0, 1)] private float _spawnChance = 0.5f;
    [SerializeField] private NodeType _nodeTypeToSpawnIn;
    [SerializeField] private EnvironmentType _environment;
    [SerializeField] private List<PropData> _neighbors;
    public static PropHierarchy Props = new PropHierarchy(null, Guid.Empty);

    public GameObject Prop => _prop;
    public int LimitCount => _limitCount;
    public PropLimitTypeEnum LimitType => _limitType;
    public PropPlacementType Placement => placement;

    public bool CheckOrientation => _checkOrentation;
    public PropSpawnTagEnum SpawnTag => _spawnTag;

//To Add - 
    //Spawning Position
    //NodeType
    //StructureType

    public int MaxCount => _maxCount;
    public PropType PropType => _propType;
    public float SpawnChance => _spawnChance;
    public EnvironmentType Environment => _environment;
    public List<PropData> Neighbors => _neighbors;
}