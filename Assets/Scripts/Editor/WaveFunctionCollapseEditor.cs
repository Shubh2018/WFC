using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;

[CustomEditor(typeof(WFC))]
public class WaveFunctionCollapseEditor : Editor
{
    public VisualTreeAsset editorVisualTree;
    private VisualElement rootTree;
    private WFC WaveFunctionCollapse;

    private Label testText;
    
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

        Toggle overrideObjListToggle = rootTree.Q<Toggle>("OverrideObjList");
        overrideObjListToggle.RegisterCallback<ChangeEvent<bool>>((ChangeEvent<bool> evt) => {
            VisualElement objectsToSpawnField = rootTree.Q("Objects");
            objectsToSpawnField.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
        });

        // Debug Settings (A*)
        Toggle togglePathLine = rootTree.Q<Toggle>("_togglePath");
        Toggle togglePathPoints = rootTree.Q<Toggle>("_togglePathPoints");
        Toggle togglePathStairs = rootTree.Q<Toggle>("_togglePathStairs");
        Toggle togglePathField = rootTree.Q<Toggle>("_togglePathField");
        Toggle togglePathFinding = rootTree.Q<Toggle>("_togglePathFinding");
        Toggle togglePathDelay = rootTree.Q<Toggle>("_togglePathDelay");
        
        togglePathLine.RegisterCallback<ChangeEvent<bool>>((ChangeEvent<bool> evt) => 
            WaveFunctionCollapse.SavePathSettings(evt.newValue, togglePathPoints.value, togglePathStairs.value, togglePathField.value, togglePathFinding.value, togglePathDelay.value)
        );
        togglePathPoints.RegisterCallback<ChangeEvent<bool>>((ChangeEvent<bool> evt) => 
            WaveFunctionCollapse.SavePathSettings(togglePathLine.value, evt.newValue, togglePathStairs.value, togglePathField.value, togglePathFinding.value, togglePathDelay.value)
        );
        togglePathStairs.RegisterCallback<ChangeEvent<bool>>((ChangeEvent<bool> evt) => 
            WaveFunctionCollapse.SavePathSettings(togglePathLine.value, togglePathPoints.value, evt.newValue, togglePathField.value, togglePathFinding.value, togglePathDelay.value)
        );
        togglePathField.RegisterCallback<ChangeEvent<bool>>((ChangeEvent<bool> evt) => 
            WaveFunctionCollapse.SavePathSettings(togglePathLine.value, togglePathPoints.value, togglePathStairs.value, evt.newValue, togglePathFinding.value, togglePathDelay.value)
        );
        togglePathFinding.RegisterCallback<ChangeEvent<bool>>((ChangeEvent<bool> evt) => 
            WaveFunctionCollapse.SavePathSettings(togglePathLine.value, togglePathPoints.value, togglePathStairs.value, togglePathField.value, evt.newValue, togglePathDelay.value)
        );
        togglePathDelay.RegisterCallback<ChangeEvent<bool>>((ChangeEvent<bool> evt) => 
            WaveFunctionCollapse.SavePathSettings(togglePathLine.value, togglePathPoints.value, togglePathStairs.value, togglePathField.value, togglePathFinding.value, evt.newValue)
        );

        // Debug Settings (PDS)
        Toggle togglePDSFloorSamples = rootTree.Q<Toggle>("_togglePDSFloorSamples");
        Toggle togglePDSWallSamples = rootTree.Q<Toggle>("_togglePDSWallSamples");
        Toggle togglePDSSamplePoints = rootTree.Q<Toggle>("_togglePDSSamplePoints");
        Slider sliderPDSSamplesRenderDistance = rootTree.Q<Slider>("_sliderPDSSamplesRenderDistance");
        
        togglePDSFloorSamples.RegisterCallback<ChangeEvent<bool>>((ChangeEvent<bool> evt) => 
            WaveFunctionCollapse.SavePDSSettings(evt.newValue, togglePDSWallSamples.value, togglePDSSamplePoints.value, sliderPDSSamplesRenderDistance.value)
        );

        togglePDSWallSamples.RegisterCallback<ChangeEvent<bool>>((ChangeEvent<bool> evt) => 
            WaveFunctionCollapse.SavePDSSettings(togglePDSFloorSamples.value, evt.newValue, togglePDSSamplePoints.value, sliderPDSSamplesRenderDistance.value)
        );

        togglePDSSamplePoints.RegisterCallback<ChangeEvent<bool>>((ChangeEvent<bool> evt) => {
            if (evt.newValue) {
                WaveFunctionCollapse.SavePDSSettings(false, false, true, sliderPDSSamplesRenderDistance.value);
                togglePDSFloorSamples.value = false;
                togglePDSFloorSamples.SetEnabled(false);
                togglePDSWallSamples.value = false;
                togglePDSWallSamples.SetEnabled(false);
            }

            else {
                WaveFunctionCollapse.SavePDSSettings(togglePDSFloorSamples.value, togglePDSWallSamples.value, false, sliderPDSSamplesRenderDistance.value);
                togglePDSFloorSamples.SetEnabled(true);
                togglePDSWallSamples.SetEnabled(true);
            }
        });

        sliderPDSSamplesRenderDistance.RegisterCallback<ChangeEvent<float>>((ChangeEvent<float> evt) =>
            WaveFunctionCollapse.SavePDSSettings(togglePDSFloorSamples.value, togglePDSWallSamples.value, togglePDSSamplePoints.value, evt.newValue)
        );
        
        // Testing tab
        TextField textField = rootTree.Q<TextField>("FileName");
        
        IntegerField intField = rootTree.Q<IntegerField>("LevelCount");

        Button collapseTilesTestingButton = rootTree.Q<Button>("_collapseTilesTest");
        collapseTilesTestingButton.RegisterCallback<ClickEvent>(CollapseTilesTesting);

        Button stopTestingButton = rootTree.Q<Button>("_stopTesting");
        stopTestingButton.RegisterCallback<ClickEvent>(StopTesting);
        
        Button createFileButton = rootTree.Q<Button>("CreateFile");
        createFileButton.RegisterCallback<ClickEvent>((ClickEvent evt) =>
        {
            TestData.CreateFile(textField.text);
        });
        
        intField.RegisterCallback<ChangeEvent<int>>((ChangeEvent<int> evt) => WaveFunctionCollapse.SetLevelCount(evt.newValue));
        
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
        WaveFunctionCollapse.SampleTiles();
    }

    private void GeneratePath(ClickEvent evt)
    {
        WaveFunctionCollapse.StartFindPath(() => {
            SetButtonState("_stopGeneratePath", false);
            SetButtonState("_clearPath", true);
        });

        SetButtonState("_stopGeneratePath", true);
        SetButtonState("_generatePath", false);
    }

    private void StopGeneratePath(ClickEvent evt)
    {
        WaveFunctionCollapse.StopFindPath();
        SetButtonState("_stopGeneratePath", false);
        SetButtonState("_clearPath", true);
    }

    private void ClearPath(ClickEvent evt)
    {
        SetButtonState("_stopGeneratePath", false);
        SetButtonState("_clearPath", false);
        SetButtonState("_generatePath", true);
        WaveFunctionCollapse.ClearPath();
    }

    private void CollapseTiles(ClickEvent evt)
    {
        WaveFunctionCollapse.pauseGeneration = false;
        WaveFunctionCollapse.StartCollapse((int overlaps) => {
            ResetControls();
            PropData.Props.PrintHierarchy();
        });

        SetGenLabels(WaveFunctionCollapse.getTiles, WaveFunctionCollapse.getCollapseTime, WaveFunctionCollapse.collapseWaitTime);
        SetButtonState("_pauseCollapse", true);
        SetButtonState("_stopCollapse", true);
        SetButtonState("_finishCollapse", true);
        SetSliderState("_collapseSpeedSlider", true);
        SetButtonState("_collapseTiles", false);
        SetLabelText("_doneLabel", "");
    }

    private void CollapseTilesTesting(ClickEvent evt)
    {
        WaveFunctionCollapse.pauseGeneration = false;
        WaveFunctionCollapse.StartCollapseTesting(() => {
            SetLabelText("TestLabel", WaveFunctionCollapse.PropText);
            SetButtonState("_collapseTilesTest", true);
            SetButtonState("CreateFile", true);
            SetButtonState("_stopTesting", false);
        }, (round) => UpdateTestingLabel(round));

        SetButtonState("_collapseTilesTest", false);
        SetButtonState("CreateFile", false);
        SetButtonState("_stopTesting", true);
        
        UpdateTestingLabel(-1);
    }

    private void UpdateTestingLabel(int currentRound)
    {
        int totalRounds = rootTree.Q<IntegerField>("LevelCount").value;
        int currRound = currentRound + 1;
        float percentageDone = ((float) currRound) / ((float) totalRounds) * 100f;

        SetLabelText("TestLabel", $"({currRound}/{rootTree.Q<IntegerField>("LevelCount").value}, {percentageDone}%) testing...");
    }

    private void StopTesting(ClickEvent evt)
    {
        WaveFunctionCollapse.pauseGeneration = false;
        WaveFunctionCollapse.StopCollapseTesting();

        SetButtonState("_collapseTilesTest", true);
        SetButtonState("CreateFile", true);
        SetButtonState("_stopTesting", false);

        SetLabelText("TestLabel", "testing stopped manually...");
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
