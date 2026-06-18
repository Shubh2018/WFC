using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;
using System;

[CustomEditor(typeof(MeshNode))]
public class MeshNodeEditor : Editor
{
    public VisualTreeAsset editorVisualTree;
    private VisualElement rootTree;
    private MeshNode node;
    static UnityEngine.Object[] scriptProps;
    
    public override VisualElement CreateInspectorGUI()
    {
        node = (MeshNode) target;
        
        rootTree = new VisualElement();
        editorVisualTree.CloneTree(rootTree);

        Toggle spawnToggle = rootTree.Q<Toggle>("_spawnToggle");
        spawnToggle.RegisterCallback<ChangeEvent<bool>>((ChangeEvent<bool> evt) => {
            EnumField spawnTypeField = rootTree.Q<EnumField>("_spawnTypeField");
            VisualElement objectsToSpawnField = rootTree.Q("_objectsToSpawn");

            if (evt.newValue)
            {
                objectsToSpawnField.style.display = DisplayStyle.Flex;
                spawnTypeField.style.display = DisplayStyle.None;
            } else {
                objectsToSpawnField.style.display = DisplayStyle.None;
                spawnTypeField.style.display = DisplayStyle.Flex;
            }
        });

        return rootTree;
    }
}
