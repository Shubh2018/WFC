using UnityEngine;
using System.Collections.Generic; 

[CreateAssetMenu(fileName = "PropData", menuName = "Props/PropData")]
public class PropData : ScriptableObject
{
    [SerializeField] private GameObject _prop;

    //Neighbor List

    [SerializeField] private Placement placement;
    [SerializeField] private int _maxCount;

    [SerializeField] private bool _checkOrentation;
    [SerializeField] private bool _limitOnePerRoom;

    [SerializeField] private PropType _propType;
    [SerializeField] [Range(0, 1)] private float _spawnChance = 0.5f;

    [SerializeField] private EnvironmentType _environment;
    [SerializeField] private List<PropData> _neighbors;

//To Add - 
    //Spawning Position
    //NodeType
    //StructureType

    public GameObject Prop => _prop;
    public Placement Placement => placement;
    public int MaxCount => _maxCount;

    public bool CheckOrientation => _checkOrentation;
    public bool LimitOnePerRoom => _limitOnePerRoom;
    public PropType PropType => _propType;
    
    public float SpawnChance => _spawnChance;
    public EnvironmentType Environment => _environment;
    public List<PropData> Neighbors => _neighbors;
}