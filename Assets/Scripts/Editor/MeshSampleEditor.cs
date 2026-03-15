using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(MeshSampler))]
public class MeshSampleEditor : Editor
{
    public VisualTreeAsset _treeAsset;
    private MeshSampler _meshSampler;

    Mesh mesh;

    private HashSet<int> triangleIndex = new HashSet<int>();

    private void OnEnable()
    {
        _meshSampler = (MeshSampler)target;
        // mesh = _meshSampler.Mesh;
        
        // SceneView.duringSceneGui += OnSceneGUI_Custom;
    }

    private void OnDisable()
    {
        // SceneView.duringSceneGui -= OnSceneGUI_Custom;
    }

    public override VisualElement CreateInspectorGUI()
    {
        VisualElement root = new VisualElement();

        _treeAsset.CloneTree(root);

        Button generateSampleButton = root.Q<Button>("Generate");
        generateSampleButton.RegisterCallback<ClickEvent>(GenerateSamples);

        Button clearSampleButton = root.Q<Button>("Clear");
        clearSampleButton.RegisterCallback<ClickEvent>(ClearSamples);

        return root;
    }

    void OnSceneGUI_Custom(SceneView sceneView)
    {
        //TODO: On selecting the triangle, calculate normal and go through all other vertices and calculate the normal of the triangles they make.
        //TODO: Add it to the list and paint the selection for each of the triangle.
        
        if (mesh == null) return;
        
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        
        Event e = Event.current;
        
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.TryGetComponent<MeshFilter>(out var filter))
                {
                    int triIndex = hit.triangleIndex;
                    
                    if (!e.control && !e.shift)
                        triangleIndex.Clear();
                    
                    if (triangleIndex.Contains(triIndex))
                        triangleIndex.Remove(triIndex);
                    else
                        triangleIndex.Add(triIndex);
                    
                    e.Use();
                    SceneView.RepaintAll();
                }
            }
        }
        
        DrawSelectedFaces();
    }

    private void DrawSelectedFaces()
    {
        if (triangleIndex.Count <= 0)
            return;

        var vertices = mesh.vertices;
        var triangles = mesh.triangles;
        
        var t = _meshSampler.transform;
        
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
        Handles.color = new Color(1, 0.6f, 0, 0.4f);

        foreach (var index in triangleIndex)
        {
            int i0 = triangles[index * 3 + 0];
            int i1 = triangles[index * 3 + 1];
            int i2 = triangles[index * 3 + 2];
            
            Vector3 v0 = t.TransformPoint(vertices[i0]);
            Vector3 v1 = t.TransformPoint(vertices[i1]);
            Vector3 v2 = t.TransformPoint(vertices[i2]);

            //normal = Vector3.Cross((v1 - v0), (v2 - v0)).normalized;   

            //Handles.DrawAAConvexPolygon(new Vector3[] {v0, v1, v2});         
        }
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
