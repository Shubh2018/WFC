using UnityEditor;
using UnityEngine.UIElements;

[CustomEditor(typeof(WFC))]
public class WaveFunctionCollapseEditor : Editor
{
    public VisualTreeAsset editorVisualTree;
    private WFC WaveFunctionCollapse;
    
    public override VisualElement CreateInspectorGUI()
    {
        WaveFunctionCollapse = (WFC) target;
        
        VisualElement root = new VisualElement();

        editorVisualTree.CloneTree(root);
        
        Button generateButton = root.Q<Button>("_generateTiles");
        generateButton.RegisterCallback<ClickEvent>(GenerateTiles);
        
        Button clearButton = root.Q<Button>("_clearTiles");
        clearButton.RegisterCallback<ClickEvent>(ClearTiles);
        
        return root;
    }

    private void GenerateTiles(ClickEvent evt)
    {   
        WaveFunctionCollapse.GenerateTiles();
    }

    private void CollapseTiles(ClickEvent evt)
    {
        WaveFunctionCollapse.CollapseWorld();
    }

    private void ClearTiles(ClickEvent evt)
    {
        WaveFunctionCollapse.ClearTiles();
    }
}
