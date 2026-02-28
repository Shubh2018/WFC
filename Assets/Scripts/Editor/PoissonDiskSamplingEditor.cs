using UnityEditor;
using UnityEngine.UIElements;

[CustomEditor(typeof(PoissonDiskSampling))]
public class PoissonDiskSamplingEditor : Editor
{
    public VisualTreeAsset editorVisualTree;
    
    public override VisualElement CreateInspectorGUI()
    {
        VisualElement root = new VisualElement();

        editorVisualTree.CloneTree(root);
        
        return root;
    }
}
