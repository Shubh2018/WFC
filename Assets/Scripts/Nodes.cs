using UnityEngine;

public class Nodes : MonoBehaviour
{
    [SerializeField] private EnvironmentType _nodeType;
    public EnvironmentType EnvironmentType => _nodeType;
}
