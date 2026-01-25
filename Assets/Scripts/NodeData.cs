using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Node", menuName = "WFC/Node")]
public class NodeData : ScriptableObject
{
    public GameObject Prefab;
    public float Angle;

    public Neighbor Up;
    public Neighbor Down;
    public Neighbor Left;
    public Neighbor Right;
}

[System.Serializable]
public class Neighbor
{
    [SerializeField] private List<NodeData> compatibleNeighbors = new List<NodeData>();

    public List<NodeData> CompatibleNeighbors => compatibleNeighbors;
}