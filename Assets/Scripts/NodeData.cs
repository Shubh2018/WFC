using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class NodeFace
{
    public enum Name 
    {
        Grass,
        Road
    }

    public enum Type
    {
        None,
        Flipped,
        Original
    }

    public Name name;
    public bool symmetry;
    public Type type;
}

[CreateAssetMenu(fileName = "Node", menuName = "WFC/Node")]
public class NodeData : ScriptableObject
{
    public GameObject Prefab;

    [HideInInspector] public int ClockwiseRotationSteps;

    public NodeFace Up;
    public NodeFace Down;
    public NodeFace Left;
    public NodeFace Right;
    public NodeFace Front;
    public NodeFace Back;
}