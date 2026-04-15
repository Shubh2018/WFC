using UnityEngine;

public enum PropType
{
    Floor,
    Wall,
    Ceiling
};

public enum PropPlacement
{
    NoPreference,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    NearWall
}

public enum Prop
{
    Decoration,
    Objective,
    Enemy
};

[CreateAssetMenu(fileName = "PropData", menuName = "Props/PropData")]
public class PropData : ScriptableObject
{
    [SerializeField] private GameObject _prop;

    [SerializeField] private PropType _type;
    [SerializeField] private int _maxCount;

    [SerializeField] private bool _checkOrentation;
    [SerializeField] private bool _limitOnePerRoom;
    [SerializeField] private PropPlacement _propPlacement;

    [SerializeField] private Prop _propType;

    public GameObject Prop => _prop;
    public PropType Type => _type;
    public int MaxCount => _maxCount;

    public bool CheckOrientation => _checkOrentation;
    public bool LimitOnePerRoom => _limitOnePerRoom;
    public PropPlacement Placement => _propPlacement;
    
    public Prop PropType => _propType;
}