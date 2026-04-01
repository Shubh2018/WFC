using UnityEngine;

public enum PropType
{
    Floor,
    Wall,
    Ceiling
};

[CreateAssetMenu(fileName = "PropData", menuName = "Props/PropData")]
public class PropData : ScriptableObject
{
    [SerializeField] private GameObject _prop;
    
    [SerializeField] private PropType _type;
    
    public GameObject Prop => _prop;
    public PropType Type => _type;
}
