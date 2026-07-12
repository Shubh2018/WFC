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

        Toggle toggleSpawnInCorners = rootTree.Q<Toggle>("_spawnInCornersToggle");
        toggleSpawnInCorners.RegisterCallback((ChangeEvent<bool> evt) =>
        {
            rootTree.Q<Toggle>("_staticToggle").SetEnabled(!evt.newValue);
            rootTree.Q<GroupBox>("_spawningRotationGroup").SetEnabled(!evt.newValue);
        });

        EnumField enumSpawnType = rootTree.Q<EnumField>("_rotationEnumField");
        enumSpawnType.RegisterCallback<ChangeEvent<Enum>>((evt) =>
        {
            bool defaultVal = ((PropRotationTypeEnum) evt.newValue) == PropRotationTypeEnum.Default;
            FloatField fieldRotationAmount = rootTree.Q<FloatField>("_rotationAmountField");

            fieldRotationAmount.SetEnabled(!defaultVal);
            fieldRotationAmount.value = defaultVal ? 0 : fieldRotationAmount.value;
        });

        return rootTree;
    }
}
