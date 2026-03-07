using UnityEditor;
using UnityEngine.UIElements;

[CustomEditor(typeof(WFC))]
public class WaveFunctionCollapseEditor : Editor
{
    public VisualTreeAsset editorVisualTree;
    private VisualElement rootTree;
    private WFC WaveFunctionCollapse;
    
    public override VisualElement CreateInspectorGUI()
    {
        WaveFunctionCollapse = (WFC) target;
        
        rootTree = new VisualElement();
        editorVisualTree.CloneTree(rootTree);
        
        Button generateButton = rootTree.Q<Button>("_generateTiles");
        generateButton.RegisterCallback<ClickEvent>(GenerateTiles);

        Button collapseButton = rootTree.Q<Button>("_collapseTiles");
        collapseButton.RegisterCallback<ClickEvent>(CollapseTiles);
        
        Button clearButton = rootTree.Q<Button>("_clearTiles");
        clearButton.RegisterCallback<ClickEvent>(ClearTiles);

        SetGenLabels(0, 0.0);
        
        return rootTree;
    }

    private void SetGenLabels(int tiles, double time)
    {
        Label tilesLabel = rootTree.Q<Label>("_tilesGeneratedLabel");
        tilesLabel.text = $"Tiles Gen.: {tiles}";

        Label collapseLabel = rootTree.Q<Label>("_generationTimeLabel");
        collapseLabel.text = $"Gen. Time (ms): {time}";
    }

    private void GenerateTiles(ClickEvent evt)
    {   
        WaveFunctionCollapse.GenerateTiles();
    }

    private void CollapseTiles(ClickEvent evt)
    {
        WaveFunctionCollapse.CollapseTiles();
        SetGenLabels(WaveFunctionCollapse.getTiles, WaveFunctionCollapse.getCollapseTime);
    }

    private void ClearTiles(ClickEvent evt)
    {
        WaveFunctionCollapse.ClearTiles(true);
        SetGenLabels(0, 0.0);
    }
}
