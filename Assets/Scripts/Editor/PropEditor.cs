using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;
using System;
using UnityEditor.UIElements;

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

        EnumField enumPropTypeField = rootTree.Q<EnumField>("_typeEnumField");
        Toggle spacingToggle = rootTree.Q<Toggle>("_spacingEnabledToggle");

        enumPropTypeField.RegisterCallback<ChangeEvent<Enum>>((evt) =>
        {
            PropType propType = (PropType) evt.newValue;
            bool notDefault = propType != PropType.Decoration;

            PropertyField environmentTypeField = rootTree.Q<PropertyField>("_environmentTypeField");
            EnumFlagsField nodeTypeEnumField = rootTree.Q<EnumFlagsField>("_nodeTypeEnumField");

            environmentTypeField.SetEnabled(notDefault);
            nodeTypeEnumField.SetEnabled(notDefault);
            spacingToggle.SetEnabled(notDefault);

            if (!notDefault)
            {
                spacingToggle.value = false;
                nodeTypeEnumField.value = null;
            }
        });

        spacingToggle.RegisterCallback<ChangeEvent<bool>>((evt) =>
        {
            FloatField spacingAmountField = rootTree.Q<FloatField>("_spacingAmountField");

            spacingAmountField.SetEnabled(evt.newValue);
            if (!evt.newValue) spacingAmountField.value = 0;
        });

        EnumField enumPlacementType = rootTree.Q<EnumField>("_placementEnumField");
        Toggle toggleSpawnInCorners = rootTree.Q<Toggle>("_spawnInCornersToggle");
        toggleSpawnInCorners.SetEnabled(((PropPlacementType) enumPlacementType.value) != PropPlacementType.Wall);
        
        enumPlacementType.RegisterCallback<ChangeEvent<Enum>>((evt) =>
        {
            bool IsWallType = ((PropPlacementType) evt.newValue) == PropPlacementType.Wall;

            toggleSpawnInCorners.SetEnabled(!IsWallType);
            toggleSpawnInCorners.value = false;
        });

        toggleSpawnInCorners.RegisterCallback((ChangeEvent<bool> evt) =>
        {
            rootTree.Q<Toggle>("_staticToggle").SetEnabled(!evt.newValue);
            rootTree.Q<GroupBox>("_spawningRotationGroup").SetEnabled(!evt.newValue);
            
            toggleSpawnInCorners.value = evt.newValue;
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
