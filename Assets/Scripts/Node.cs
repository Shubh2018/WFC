using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Node", menuName = "WFC/Node")]
public class Node : ScriptableObject
{
    [SerializeField] private GameObject Prefab;

    [SerializeField] public Neighbor Up;
    [SerializeField] public Neighbor Down;
    [SerializeField] public Neighbor Left;
    [SerializeField] public Neighbor Right;
}

[System.Serializable]
public class Neighbor
{
    [SerializeField] private List<Node> compatibleNeighbors = new List<Node>();

    public List<Node> CompatibleNeighbors => compatibleNeighbors;
}