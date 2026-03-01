using UnityEditor;
using UnityEngine.UIElements;

[CustomEditor(typeof(PoissonDiskSampling))]
public class PoissonDiskSamplingEditor : Editor
{
    public VisualTreeAsset editorVisualTree;
    private PoissonDiskSampling poissonDiskSampling;
    
    public override VisualElement CreateInspectorGUI()
    {
        poissonDiskSampling = (PoissonDiskSampling)target;
        
        VisualElement root = new VisualElement();

        editorVisualTree.CloneTree(root);
        
        Button generateButton = root.Q<Button>("GenerateSampleButtons");
        generateButton.RegisterCallback<ClickEvent>(GenerateSample);
        
        Button clearSamples = root.Q<Button>("ClearSamples");
        clearSamples.RegisterCallback<ClickEvent>(ClearSamples);
        
        return root;
    }

    private void GenerateSample(ClickEvent evt)
    {   
        poissonDiskSampling.GenerateSamplingPoints();
    }

    private void ClearSamples(ClickEvent evt)
    {
        poissonDiskSampling.ClearSamples();
    }
}
