using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;

[CustomEditor(typeof(MeshPoint))]
public class MeshPointEditor : Editor
{
    public VisualTreeAsset editorVisualTree;
    private VisualElement rootTree;
    private MeshPoint point;
    static Object[] scriptProps;
    
    public override VisualElement CreateInspectorGUI()
    {
        point = (MeshPoint) target;
        
        rootTree = new VisualElement();
        editorVisualTree.CloneTree(rootTree);

        Button spawnButton = rootTree.Q<Button>("_spawnObjectButton");
        spawnButton.RegisterCallback((ClickEvent evt) => {
            Prop.Props.Clear();
            point.Init();
        });

        Toggle spawnToggle = rootTree.Q<Toggle>("_spawnToggle");
        spawnToggle.RegisterCallback((ChangeEvent<bool> evt) => {
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
