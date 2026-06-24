using UnityEngine;

public class Nodes : MonoBehaviour
{
    [SerializeField] private NodeData.EnvironmentType _nodeType;
    public NodeData.EnvironmentType EnvironmentType => _nodeType;
}
