using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(Environment))]
public class EnvironmentEditor : Editor
{
    public VisualTreeAsset editorVisualTree;
    private VisualElement rootTree;
    private Environment env;
    
    public override VisualElement CreateInspectorGUI()
    {
        env = (Environment) target;
        
        rootTree = new VisualElement();
        editorVisualTree.CloneTree(rootTree);

        return rootTree;
    }
}
