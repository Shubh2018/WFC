using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;
using System;

[CustomEditor(typeof(Prop))]
public class PropEditor : Editor
{
    public VisualTreeAsset editorVisualTree;
    private VisualElement rootTree;
    private Prop prop;
    
    public override VisualElement CreateInspectorGUI()
    {
        prop = (Prop) target;
        
        rootTree = new VisualElement();
        editorVisualTree.CloneTree(rootTree);

        return rootTree;
    }
}
