using UnityEngine;

public enum NodeType
{
    Objective,
};

public class Nodes : MonoBehaviour
{
    [SerializeField] private NodeType _nodeType;
    public NodeType NodeType => _nodeType;
}
