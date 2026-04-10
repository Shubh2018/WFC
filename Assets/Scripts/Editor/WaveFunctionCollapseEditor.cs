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

        Button generatePathButton = rootTree.Q<Button>("_generatePath");
        generatePathButton.RegisterCallback<ClickEvent>(GeneratePath);

        Button stopGeneratePathButton = rootTree.Q<Button>("_stopGeneratePath");
        stopGeneratePathButton.RegisterCallback<ClickEvent>(StopGeneratePath);

        Button clearPathButton = rootTree.Q<Button>("_clearPath");
        clearPathButton.RegisterCallback<ClickEvent>(ClearPath);

        Button collapseButton = rootTree.Q<Button>("_collapseTiles");
        collapseButton.RegisterCallback<ClickEvent>(CollapseTiles);

        Button stopCollapse = rootTree.Q<Button>("_stopCollapse");
        stopCollapse.RegisterCallback<ClickEvent>(StopCollapseOfTiles);

        Button pauseCollapse = rootTree.Q<Button>("_pauseCollapse");
        pauseCollapse.RegisterCallback<ClickEvent>(PauseCollapseOfTiles);

        Button finishCollapse = rootTree.Q<Button>("_finishCollapse");
        finishCollapse.RegisterCallback<ClickEvent>(FinishCollapseOfTiles);

        Button clearCollapsedButton = rootTree.Q<Button>("_clearCollapsed");
        clearCollapsedButton.RegisterCallback<ClickEvent>((ClickEvent evt) => {
            StopCollapseOfTiles(evt);
            WaveFunctionCollapse.ClearTiles(false);
            SetGenLabels(0, 0.0, 0.0f);
        });
        
        Button clearButton = rootTree.Q<Button>("_clearTiles");
        clearButton.RegisterCallback<ClickEvent>(ClearTiles);

        Slider collapseSpeedSlider = rootTree.Q<Slider>("_collapseSpeedSlider");
        collapseSpeedSlider.RegisterCallback<ChangeEvent<float>>(UpdateCollapseTime);

        SetGenLabels(0, 0.0, 1.0f);
        
        return rootTree;
    }

    private void SetGenLabels(int tiles, double time, float delay)
    {
        SetLabelText("_collapseSpeedLabel", $"Delay (s): {delay}");
        SetLabelText("_tilesGeneratedLabel", $"Tiles Gen.: {tiles}");
        SetLabelText("_generationTimeLabel", $"Gen. Time (ms): {time}");
    }

    private void UpdateCollapseTime(ChangeEvent<float> evt)
    {
        WaveFunctionCollapse.collapseWaitTime = evt.newValue;
        SetLabelText("_collapseSpeedLabel", $"Delay (s): {WaveFunctionCollapse.collapseWaitTime}");
    }

    private void GenerateTiles(ClickEvent evt)
    {   
        WaveFunctionCollapse.GenerateTiles();
    }

    private void GeneratePath(ClickEvent evt)
    {
        WaveFunctionCollapse.StartFindPath();
    }

    private void StopGeneratePath(ClickEvent evt)
    {
        WaveFunctionCollapse.StopFindPath();
    }

    private void ClearPath(ClickEvent evt)
    {
        WaveFunctionCollapse.ClearPath();
    }

    private void CollapseTiles(ClickEvent evt)
    {
        WaveFunctionCollapse.pauseGeneration = false;
        WaveFunctionCollapse.StartCollapse(() => {
            SetLabelText("_doneLabel", "Done...");
            ResetControls();
        });

        SetGenLabels(WaveFunctionCollapse.getTiles, WaveFunctionCollapse.getCollapseTime, WaveFunctionCollapse.collapseWaitTime);
        SetButtonState("_pauseCollapse", true);
        SetButtonState("_stopCollapse", true);
        SetButtonState("_finishCollapse", true);
        SetSliderState("_collapseSpeedSlider", true);
        SetButtonState("_collapseTiles", false);
        SetLabelText("_doneLabel", "");
    }

    private void PauseCollapseOfTiles(ClickEvent evt)
    {
        WaveFunctionCollapse.pauseGeneration = !WaveFunctionCollapse.pauseGeneration;
        SetButtonText("_pauseCollapse", WaveFunctionCollapse.pauseGeneration ? "Unpause" : "Pause");
    }

    private void StopCollapseOfTiles(ClickEvent evt)
    {
        WaveFunctionCollapse.pauseGeneration = false;
        WaveFunctionCollapse.StopCollapse();

        ResetControls();
    }

    private void FinishCollapseOfTiles(ClickEvent evt)
    {
        WaveFunctionCollapse.collapseWaitTime = 0.0f;
        SetSliderValue("_collapseSpeedSlider", 0.0f);
        SetLabelText("_collapseSpeedLabel", $"Delay (s): {0}");
        ResetControls();
        SetButtonState("_collapseTiles", false);
    }

    private void ResetControls()
    {
        SetButtonText("_pauseCollapse", "Pause");
        SetButtonText("_stopCollapse", "Stop");
        SetButtonState("_collapseTiles", true);
        SetButtonState("_pauseCollapse", false);
        SetButtonState("_stopCollapse", false);
        SetButtonState("_finishCollapse", false);
        SetSliderState("_collapseSpeedSlider", false);
    }

    private void ClearTiles(ClickEvent evt)
    {
        StopCollapseOfTiles(evt);
        WaveFunctionCollapse.ClearTiles(true);
        SetGenLabels(0, 0.0, 0.0f);
    }

    private void SetLabelText(string name, string value)
    {
        Label label = rootTree.Q<Label>(name);
        label.text = value;
    }

    private void SetButtonText(string name, string value)
    {
        Button button = rootTree.Q<Button>(name);
        button.text = value;
    }

    private void SetButtonState(string name, bool state)
    {
        Button button = rootTree.Q<Button>(name);
        button.SetEnabled(state);
    }

    private void SetSliderState(string name, bool state)
    {
        Slider slider = rootTree.Q<Slider>(name);
        slider.SetEnabled(state);
    }

    private void SetSliderValue(string name, float value)
    {
        Slider slider = rootTree.Q<Slider>(name);
        slider.value = value;
    }
}
