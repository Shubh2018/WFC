using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(MeshSampler))]
public class MeshSampleEditor : Editor
{
    public VisualTreeAsset _treeAsset;
    private MeshSampler _meshSampler;

    Mesh mesh;

    public override VisualElement CreateInspectorGUI()
    {
        _meshSampler = (MeshSampler)target;

        VisualElement root = new VisualElement();

        _treeAsset.CloneTree(root);

        Button generateSampleButton = root.Q<Button>("Generate");
        generateSampleButton.RegisterCallback<ClickEvent>(GenerateSamples);

        Button clearSampleButton = root.Q<Button>("Clear");
        clearSampleButton.RegisterCallback<ClickEvent>(ClearSamples);

        return root;
    }

    void OnSceneGUI()
    {
        mesh = _meshSampler.Mesh;
    }

    private void GenerateSamples(ClickEvent evt)
    {
        _meshSampler.Generate();
    }

    private void ClearSamples(ClickEvent evt)
    {
        _meshSampler.Clear();
    }
}
