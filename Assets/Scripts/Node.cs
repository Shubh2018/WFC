using UnityEngine;

[CreateAssetMenu(fileName = "Node", menuName = "WFC/Node")]
public class Node : ScriptableObject
{
    [SerializeField] private GameObject Prefab;
}