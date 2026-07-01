using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;
using System;

[CustomEditor(typeof(NodeData))]
public class NodeDataEditor : Editor
{
    public VisualTreeAsset editorVisualTree;
    private VisualElement rootTree;
    private NodeData nodeData;
    
    public override VisualElement CreateInspectorGUI()
    {
        nodeData = (NodeData) target;
        
        rootTree = new VisualElement();
        editorVisualTree.CloneTree(rootTree);

        return rootTree;
    }
}
