using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;
using System;

[CustomEditor(typeof(MeshSurface))]
public class MeshSurfaceEditor : Editor
{
    public VisualTreeAsset editorVisualTree;
    private VisualElement rootTree;
    private MeshSurface surface;
    static UnityEngine.Object[] scriptProps;
    
    public override VisualElement CreateInspectorGUI()
    {
        surface = (MeshSurface) target;
        
        rootTree = new VisualElement();
        editorVisualTree.CloneTree(rootTree);

        Button spawnButton = rootTree.Q<Button>("_spawnObjectsButton");
        spawnButton.RegisterCallback<ClickEvent>((ClickEvent evt) => {
            Prop.Props.Clear();
            surface.Init();
        });

        Vector3Field size = rootTree.Q<Vector3Field>("_surfaceSize");
        size.RegisterCallback<ChangeEvent<Vector3>>((ChangeEvent<Vector3> evt) => {
            MeshFilter filter = surface.GetComponent<MeshFilter>();
            MeshRenderer renderer = surface.GetComponent<MeshRenderer>();
            BoxCollider col = surface.GetComponent<BoxCollider>();

            if (filter) 
                filter.sharedMesh = Misc.CreatePlaneMesh(evt.newValue / 2);
            
            if (renderer)
                renderer.material.color = Color.grey;

            if (col) {
                col.size = evt.newValue;
                col.center = new Vector3(0.0f, evt.newValue.y / 2, 0.0f);
            }
        });

        Toggle spawnToggle = rootTree.Q<Toggle>("_spawnToggle");
        spawnToggle.RegisterCallback<ChangeEvent<bool>>((ChangeEvent<bool> evt) => {
            EnumField spawnTypeField = rootTree.Q<EnumField>("_spawnTypeField");
            IntegerField maxPropCountField = rootTree.Q<IntegerField>("_maxPropCountField");
            VisualElement objectsToSpawnField = rootTree.Q("_objectsToSpawn");

            if (evt.newValue)
            {
                objectsToSpawnField.style.display = DisplayStyle.Flex;
                spawnTypeField.style.display = DisplayStyle.None;
                maxPropCountField.style.display = DisplayStyle.None;
            } else {
                objectsToSpawnField.style.display = DisplayStyle.None;
                spawnTypeField.style.display = DisplayStyle.Flex;
                maxPropCountField.style.display = DisplayStyle.Flex;
            }
        });

        return rootTree;
    }
}
