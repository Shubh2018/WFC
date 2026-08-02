using UnityEditor;
using UnityEngine.UIElements;
using System;
using UnityEngine;

public class ProgressBox
{
    private float stepRange = -1;
    private int stepCount = -1;
    private VisualElement rootTree;

    public ProgressBox(VisualElement root, WFC wfc)
    {
        rootTree = root;

        // General
        wfc.EditorResetProgress = Reset;
        wfc.EditorCloseProgress = Close;
        wfc.EditorClearBarProgress = ClearBar;
        wfc.EditorClearMessagesProgress = ClearMessages;

        // Progress
        wfc.EditorSetBarProgress = SetBar;
        wfc.EditorSetBarMessageProgress = SetBarMessage;
        wfc.EditorIncreaseBarProgress = IncreaseBar;
        wfc.EditorIncreaseBarMessageProgress = IncreaseBarMessage;

        // Messages
        wfc.EditorMessageProgress = AddMessage;
        wfc.EditorMessageSimpleProgress = AddMessage;
        wfc.EditorAddToMessageProgress = AddToMessage;
        wfc.EditorUpdateDotMessageProgress = UpdateDotMessage;
        wfc.EditorUpdateRecentMessageProgress = UpdateRecentMessage;

        // Step counter
        wfc.EditorBeginStepCounterProgress = BeginStepCounter;
        wfc.EditorTakeStepProgress = TakeStep;
        wfc.EditorStopStepCounterProgress = StopStepCounter;

        // Other
        wfc.EditorGetMessagesCountProgress = GetMessagesCount;
    }

    private void Display(bool state)
    {
        GroupBox progressBox = rootTree.Q<GroupBox>("ProgressBox");
        progressBox.style.display = state ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void Reset()
    {
        Display(true);
        ClearBar();
        ClearMessages();
    }

    public void SetBar(float progress)
    {
        progress = Mathf.Max(Mathf.Min(progress, 100), 0);

        ProgressBar progressBar = rootTree.Q<ProgressBar>("_progressBar");
        progressBar.title = $"{progress}%";
        progressBar.value = progress;
    }

    public void IncreaseBar(float progress)
    {
        ProgressBar progressBar = rootTree.Q<ProgressBar>("_progressBar");
        SetBar(progressBar.value + progress);
    }

    public void AddMessage(string newMessage, Color color)
    {
        Label label = new Label(newMessage);
        label.style.color = new StyleColor(color);

        ScrollView progressMessages = rootTree.Q<ScrollView>("_progressMessages");
        progressMessages.Add(label);
        progressMessages.ScrollTo(label);
    }

    public void AddMessage(string newMessage)
    {
        AddMessage(newMessage, Color.white);
    }

    public void SetBarMessage(float progress, string newMessage)
    {
        SetBar(progress);
        AddMessage(newMessage, Color.white);
    }

    public void IncreaseBarMessage(float progress, string newMessage)
    {
        IncreaseBar(progress);
        AddMessage(newMessage, Color.white);
    }

    public void AddToMessage(int index, string addedText)
    {
        ScrollView progressMessages = rootTree.Q<ScrollView>("_progressMessages");
        if (index < 0 || index >= progressMessages.childCount) return;
        Label recentLabel = (Label) progressMessages.ElementAt(index);
        recentLabel.text += addedText;
        progressMessages.ScrollTo(recentLabel);
    }

    public int GetMessagesCount()
    {
        return rootTree.Q<ScrollView>("_progressMessages").childCount;
    }

    public void UpdateDotMessage(float length, int current, int max)
    {
        int steps = Mathf.CeilToInt(length / max);
        for (int i = (current - 1) * steps; i < current * steps; i++)
            AddToMessage(GetMessagesCount() - 1, ". ");
    }

    public void UpdateRecentMessage(string additionMessage)
    {
        AddToMessage(GetMessagesCount() - 1, additionMessage);
    }

    public void BeginStepCounter(float range, int count)
    {
        stepRange = range;
        stepCount = count;
    }

    public void TakeStep(string message)
    {
        if (stepRange == -1 && stepCount == -1) return;
        IncreaseBarMessage(stepRange / stepCount, message);
    }

    public void StopStepCounter()
    {
        stepRange = stepCount = -1;
    }

    public void ClearBar()
    {
        ProgressBar progressBar = rootTree.Q<ProgressBar>("_progressBar");
        progressBar.title = "0%";
        progressBar.value = 0;
    }

    public void ClearMessages()
    {
        rootTree.Q<ScrollView>("_progressMessages").Clear();
    }

    public void Close()
    {
        Display(false);
    }
}

[CustomEditor(typeof(WFC))]
public class WaveFunctionCollapseEditor : Editor
{
    public VisualTreeAsset editorVisualTree;
    private VisualElement rootTree;
    private WFC WaveFunctionCollapse;
    private ProgressBox progress;
    
    public override VisualElement CreateInspectorGUI()
    {
        WaveFunctionCollapse = (WFC) target;
        
        rootTree = new VisualElement();
        editorVisualTree.CloneTree(rootTree);

        progress = new ProgressBox(rootTree, WaveFunctionCollapse);
        
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
        SetButtonState("_collapseTiles", WaveFunctionCollapse.enabledCollapseButton);

        Button stopCollapse = rootTree.Q<Button>("_stopCollapse");
        stopCollapse.RegisterCallback<ClickEvent>(StopCollapseOfTiles);

        Button pauseCollapse = rootTree.Q<Button>("_pauseCollapse");
        pauseCollapse.RegisterCallback<ClickEvent>(PauseCollapseOfTiles);

        Button clearCollapsedButton = rootTree.Q<Button>("_clearCollapse");
        clearCollapsedButton.RegisterCallback((ClickEvent evt) => {
            StopCollapseOfTiles(evt);
            WaveFunctionCollapse.ClearTiles(false);
            SetButtonState("_clearCollapse", false);
        });
        
        Button clearButton = rootTree.Q<Button>("_clearTiles");
        clearButton.RegisterCallback((ClickEvent evt) =>
        {
            StopCollapseOfTiles(evt);
            WaveFunctionCollapse.ClearTiles(true);
            WaveFunctionCollapse.enabledCollapseButton = false;
            SetButtonState("_collapseTiles", false);
        });

        RegisterIntFieldCallback("NodeSamples", 1, 10);

        AssemblyReloadEvents.afterAssemblyReload += () => {
            WaveFunctionCollapse.enabledCollapseButton = false;
            SetButtonState("_collapseTiles", false);
        };

        Toggle toggleWFCEnvironments = rootTree.Q<Toggle>("_toggleWFCEnvironments");

        toggleWFCEnvironments.RegisterCallback((ChangeEvent<bool> evt) =>
        {
            SaveMeshNodeSettings(evt.newValue);
        });

        SetButtonState("_collapseTiles", WaveFunctionCollapse.doneGeneratingSamples);

        // Debug Settings (A*)
        Toggle togglePathLine = rootTree.Q<Toggle>("_togglePath");
        Toggle togglePathPoints = rootTree.Q<Toggle>("_togglePathPoints");
        Toggle togglePathStairs = rootTree.Q<Toggle>("_togglePathStairs");
        Toggle togglePathField = rootTree.Q<Toggle>("_togglePathField");
        Toggle togglePathFinding = rootTree.Q<Toggle>("_togglePathFinding");
        Toggle togglePathDelay = rootTree.Q<Toggle>("_togglePathDelay");
        
        togglePathLine.RegisterCallback((ChangeEvent<bool> evt) => 
            SavePathSettings(evt.newValue, togglePathPoints.value, togglePathStairs.value, togglePathField.value, togglePathFinding.value, togglePathDelay.value)
        );
        togglePathPoints.RegisterCallback((ChangeEvent<bool> evt) => 
            SavePathSettings(togglePathLine.value, evt.newValue, togglePathStairs.value, togglePathField.value, togglePathFinding.value, togglePathDelay.value)
        );
        togglePathStairs.RegisterCallback((ChangeEvent<bool> evt) => 
            SavePathSettings(togglePathLine.value, togglePathPoints.value, evt.newValue, togglePathField.value, togglePathFinding.value, togglePathDelay.value)
        );
        togglePathField.RegisterCallback((ChangeEvent<bool> evt) => 
            SavePathSettings(togglePathLine.value, togglePathPoints.value, togglePathStairs.value, evt.newValue, togglePathFinding.value, togglePathDelay.value)
        );
        togglePathFinding.RegisterCallback((ChangeEvent<bool> evt) => 
            SavePathSettings(togglePathLine.value, togglePathPoints.value, togglePathStairs.value, togglePathField.value, evt.newValue, togglePathDelay.value)
        );
        togglePathDelay.RegisterCallback((ChangeEvent<bool> evt) => 
            SavePathSettings(togglePathLine.value, togglePathPoints.value, togglePathStairs.value, togglePathField.value, togglePathFinding.value, evt.newValue)
        );

        // Debug Settings (PDS)
        Toggle togglePDSFloorSamples = rootTree.Q<Toggle>("_togglePDSFloorSamples");
        Toggle togglePDSWallSamples = rootTree.Q<Toggle>("_togglePDSWallSamples");
        Toggle togglePDSCornerSamples = rootTree.Q<Toggle>("_togglePDSCornerSamples");
        Toggle togglePDSSamplePoints = rootTree.Q<Toggle>("_togglePDSSamplePoints");
        Toggle toggleSpecialsSpacingDistance = rootTree.Q<Toggle>("_toggleSpecialsSpacingDistance");
        Slider sliderPDSSamplesRenderDistance = rootTree.Q<Slider>("_sliderPDSSamplesRenderDistance");
        
        togglePDSFloorSamples.RegisterCallback((ChangeEvent<bool> evt) => 
            SavePDSSettings(evt.newValue, togglePDSWallSamples.value, togglePDSCornerSamples.value, togglePDSSamplePoints.value, toggleSpecialsSpacingDistance.value, sliderPDSSamplesRenderDistance.value)
        );

        togglePDSWallSamples.RegisterCallback((ChangeEvent<bool> evt) => 
            SavePDSSettings(togglePDSFloorSamples.value, evt.newValue, togglePDSCornerSamples.value, togglePDSSamplePoints.value, toggleSpecialsSpacingDistance.value, sliderPDSSamplesRenderDistance.value)
        );

        togglePDSCornerSamples.RegisterCallback((ChangeEvent<bool> evt) => 
            SavePDSSettings(togglePDSFloorSamples.value, togglePDSWallSamples.value, evt.newValue, togglePDSSamplePoints.value, toggleSpecialsSpacingDistance.value, sliderPDSSamplesRenderDistance.value)
        );

        togglePDSSamplePoints.RegisterCallback((ChangeEvent<bool> evt) => {
            if (evt.newValue) {
                SavePDSSettings(false, false, false, true, toggleSpecialsSpacingDistance.value, sliderPDSSamplesRenderDistance.value);
                togglePDSFloorSamples.value = false;
                togglePDSFloorSamples.SetEnabled(false);
                togglePDSWallSamples.value = false;
                togglePDSWallSamples.SetEnabled(false);
                togglePDSCornerSamples.value = false;
                togglePDSCornerSamples.SetEnabled(false);
            }

            else {
                SavePDSSettings(togglePDSFloorSamples.value, togglePDSWallSamples.value, togglePDSCornerSamples.value, false, toggleSpecialsSpacingDistance.value, sliderPDSSamplesRenderDistance.value);
                togglePDSFloorSamples.SetEnabled(true);
                togglePDSWallSamples.SetEnabled(true);
                togglePDSCornerSamples.SetEnabled(true);
            }
        });

        sliderPDSSamplesRenderDistance.RegisterCallback((ChangeEvent<float> evt) =>
            SavePDSSettings(togglePDSFloorSamples.value, togglePDSWallSamples.value, togglePDSCornerSamples.value, togglePDSSamplePoints.value, toggleSpecialsSpacingDistance.value, evt.newValue)
        );

        toggleSpecialsSpacingDistance.RegisterCallback((ChangeEvent<bool> evt) => 
            SavePDSSettings(togglePDSFloorSamples.value, togglePDSWallSamples.value, togglePDSCornerSamples.value, togglePDSSamplePoints.value, evt.newValue, sliderPDSSamplesRenderDistance.value)
        );
        
        // Testing tab
        TextField textField = rootTree.Q<TextField>("FileName");
        
        IntegerField intField = rootTree.Q<IntegerField>("LevelCount");

        Button collapseTilesTestingButton = rootTree.Q<Button>("_collapseTilesTest");
        collapseTilesTestingButton.RegisterCallback<ClickEvent>(CollapseTilesTesting);

        Button stopTestingButton = rootTree.Q<Button>("_stopTesting");
        stopTestingButton.RegisterCallback<ClickEvent>(StopTesting);
        
        Button createFileButton = rootTree.Q<Button>("CreateFile");
        createFileButton.RegisterCallback((ClickEvent evt) =>
        {
            TestData.CreateFile(textField.text);
        });
        
        return rootTree;
    }

    // Limits an int field automatically to be within a range of a minimum and maximum value
    private void RegisterIntFieldCallback(string fieldName, int min, int max)
    {
        IntegerField field = rootTree.Q<IntegerField>(fieldName);
        if (field == null) return;

        Func<int, int> minMaxFunc = (int data) => Math.Max(min, Math.Min(max, data));

        field.RegisterCallback((InputEvent evt) => {
            if (!string.IsNullOrEmpty(evt.newData))
                field.value = minMaxFunc(Int32.Parse(evt.newData));
        });

        field.RegisterCallback((KeyDownEvent key) => field.value = minMaxFunc(field.value));
    }

    private void GenerateTiles(ClickEvent evt)
    {   
        CoroutineManager.StartCoroutine(WaveFunctionCollapse, "GenerateTiles", WaveFunctionCollapse.GenerateTiles());
        CoroutineManager.StartCoroutine(WaveFunctionCollapse, "SampleTiles", MeshNode.SampleTiles(WaveFunctionCollapse, () =>
        {
            SetButtonState("_collapseTiles", true);
        }));
    }

    private void GeneratePath(ClickEvent evt)
    {
        AStar path = WaveFunctionCollapse.path = WaveFunctionCollapse.GetComponent<AStar>();

        path.GeneratePath(WaveFunctionCollapse.PathPoints);
        CoroutineManager.StartCoroutine(WaveFunctionCollapse, "GeneratePathNodes", WaveFunctionCollapse.GeneratePathNodes(() =>
        {
            SetButtonState("_stopGeneratePath", false);
            SetButtonState("_clearPath", true);
        }));

        SetButtonState("_stopGeneratePath", true);
        SetButtonState("_generatePath", false);
    }

    private void StopGeneratePath(ClickEvent evt)
    {
        WaveFunctionCollapse.path.StopFindingPath();

        SetButtonState("_stopGeneratePath", false);
        SetButtonState("_clearPath", true);
    }

    private void ClearPath(ClickEvent evt)
    {
        AStar path = WaveFunctionCollapse.path;
        
        WaveFunctionCollapse.ClearPathNodes();

        path.StopFindingPath();
        path.ClearPath();

        SetButtonState("_stopGeneratePath", false);
        SetButtonState("_clearPath", false);
        SetButtonState("_generatePath", true);
    }

    private void CollapseTiles(ClickEvent evt)
    {
        if (!WaveFunctionCollapse.doneGeneratingSamples)
        {
            Debug.LogWarning("Samples has not been generated!");
            return;
        }

        WaveFunctionCollapse.pauseGeneration = false;
        CoroutineManager.StartCoroutine(WaveFunctionCollapse, "CollapseTiles", WaveFunctionCollapse.CollapseTiles((() => {}), (int overlaps) => {
            ResetControls();
        }));

        SetButtonState("_pauseCollapse", true);
        SetButtonState("_stopCollapse", true);
        SetButtonState("_collapseTiles", false);
        SetButtonState("_clearCollapse", false);
    }

    private void CollapseTilesTesting(ClickEvent evt)
    {
        int levelCount = WaveFunctionCollapse.LevelCount;
        
        WaveFunctionCollapse.pauseGeneration = false;
        CoroutineManager.StartCoroutine(WaveFunctionCollapse, "CollapseTilesTesting", WaveFunctionCollapse.CollapseTilesTesting(() => {
            SetLabelText("TestLabel", WaveFunctionCollapse.PropText);
            SetButtonState("_collapseTilesTest", true);
            SetButtonState("CreateFile", true);
            SetButtonState("_stopTesting", false);
            TestData.SaveData();
        }, (round) => UpdateTestingLabel(round), levelCount));

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
        CoroutineManager.StopAllCoroutines();

        TestData.SaveData();
        
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
        CoroutineManager.StopAllCoroutines();

        ResetControls();
    }

    private void ResetControls()
    {
        SetButtonText("_pauseCollapse", "Pause");
        SetButtonText("_stopCollapse", "Stop");
        SetButtonState("_collapseTiles", true);
        SetButtonState("_pauseCollapse", false);
        SetButtonState("_stopCollapse", false);
        SetButtonState("_clearCollapse", true);
    }

    // Gizmos debug functions
    // Used with the Unity VisualElement editor to save transitive information to the AStar object
    public void SavePathSettings(bool pathState, bool pathPointsState, bool pathStaircases, bool pathField, bool pathFinding, bool pathDelay)
    {
        AStar path = WaveFunctionCollapse.GetComponent<AStar>();

        // If the path object exist, toggle its settings
        if (path) {
            path.enableGizmosPathPoints = pathPointsState;
            path.enableGizmosPathStaircases = pathStaircases;
            path.enableGizmosPathField = pathField;
            path.enableGizmosPathFinding = pathFinding;
            path.enableGizmosGenerationDelay = pathDelay;
        }

        // Toggle rendering of the LineRenderer used to display the A* path
        LineRenderer lr = WaveFunctionCollapse.GetComponent<LineRenderer>();
        if (lr) lr.enabled = pathState;
    }

    // Used with the Unity VisualElement editor to save transitive information to the Mesh Sampler object
    public void SavePDSSettings(bool pdsFloorSamples, bool pdsWallSamples, bool pdsCornerSamples, bool pdsSamplePoints, bool specialsSpacingRadius, float samplesRenderDistance)
    {
        MeshSampler.enableGizmosFloorSamples = pdsFloorSamples;
        MeshSampler.enableGizmosWallSamples = pdsWallSamples;
        MeshSampler.enableGizmosCornerSamples = pdsCornerSamples;
        MeshSampler.enableGizmosSamplePoints = pdsSamplePoints;
        MeshSampler.enableSpecialsSpacingRadius = specialsSpacingRadius;
        MeshSampler.samplesRenderDistance = samplesRenderDistance;
    }

    public void SaveMeshNodeSettings(bool showEnvs)
    {
        Debug.Log($"WFC mesh node settings: {showEnvs}");
        MeshNode.displayGizmosEnvironments = showEnvs;
    }

    // Misc functions
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
}
