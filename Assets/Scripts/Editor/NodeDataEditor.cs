using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;
using System;

[CustomEditor(typeof(Node))]
public class NodeDataEditor : Editor
{
    public VisualTreeAsset editorVisualTree;
    private VisualElement rootTree;
    private Node nodeData;
    
    public override VisualElement CreateInspectorGUI()
    {
        nodeData = (Node) target;
        
        rootTree = new VisualElement();
        editorVisualTree.CloneTree(rootTree);

        return rootTree;
    }
}
