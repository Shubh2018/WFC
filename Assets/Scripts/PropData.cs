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

[CreateAssetMenu(fileName = "PropData", menuName = "Props/PropData")]
public class PropData : ScriptableObject
{
    [SerializeField] private GameObject _prop;

    [SerializeField] private PropType _type;
    [SerializeField] private int _maxCount;

    [SerializeField] private bool _checkOrentation;
    [SerializeField] private bool _limitOnePerRoom;
    [SerializeField] private bool _limitOnePerSection;
    [SerializeField] private PropPlacement _propPlacement; 

    public GameObject Prop => _prop;
    public PropType Type => _type;
    public int MaxCount => _maxCount;

    public bool CheckOrientation => _checkOrentation;
    public bool LimitOnePerRoom => _limitOnePerRoom;
    public bool LimitOnePerSection => _limitOnePerSection;
    public PropPlacement Placement => _propPlacement;
}