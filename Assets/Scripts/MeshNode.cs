using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using System.Collections;
using System.Diagnostics;

[System.Serializable]
public class Samples
{
    public List<Sample> samples;

    public Samples(List<Sample> samples)
    {
        this.samples = samples;
    }
}

[System.Serializable]
public class SampleData
{
    public Node nodeData;
    public List<Samples> samples;

    public SampleData()
    {
        samples = new List<Samples>();
    }
}

public class MeshNode : MonoBehaviour
{
    private Node _nodeData;
    private Environment _envObj = null;
    private PropHierarchy.PropHierachyInfo _hierarchyInfo;

    // Static variables
    public static List<SampleData> generatedSamples = new List<SampleData>();
    public static MeshSampler meshSampler = null;
    public static Spawner? spawner = null;
    public static WFC wfc = null;
    public static bool displayGizmosEnvironments = false;

    // Getters / Setters
    public Environment GetEnvironment => _envObj;
    public Node NodeData => _nodeData;

    public void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        if (displayGizmosEnvironments && wfc != null)
        {
            Color color = Color.gray;
            string label = "No Environment";

            if (_envObj != null)
            {
                color = _envObj.DisplayColor;
                label = _envObj.Name;
            }

            Gizmos.color = color;
            Gizmos.DrawWireCube(new(0, 0.5f * wfc.TileSize.y, 0), (Vector3) wfc.TileSize * 0.95f);
            Handles.Label(transform.position + new Vector3(0.0f, wfc.TileSize.y * 0.95f, 0.0f), label);
        }
    }

    public static IEnumerator SampleTiles(WFC wave, Action funcDoneHook)
    {
        yield return new WaitUntil(() => wave.doneGeneratingTiles);

        Stopwatch st = new Stopwatch();
        st.Start();

        wave.doneGeneratingSamples = false;
        wave.EditorMessageSimpleProgress("> (2/2) Sampling tiles ");
        wave.EditorMessageProgress($"Radius: {wave.SamplingRadius}, tries: {wave.SamplingTries}, samples per node: {wave.SamplesPerNode}", Color.gray);

        yield return null;

        wfc = wave;
        meshSampler = wave.GetComponent<MeshSampler>();
        meshSampler.SetSamplingGraphProperties(wave.SamplingRadius, wave.SamplingTries, wave.SamplingSafety);

        generatedSamples.Clear();

        List<Node> nodes = new(wave.getNodes);
        nodes.AddRange(wave.getNodesGen);
        nodes = nodes.Where(n => n.Prefab != null).ToList();

        wave.EditorMessageProgress($"Nodes to sample after filtering: {nodes.Count}", Color.gray);
        wave.EditorBeginStepCounterProgress(90.0f, nodes.Count);

        yield return null;

        for (int j = 0; j < nodes.Count; j++)
        {
            Node node = nodes[j];
            SampleData sampleData = new SampleData { nodeData = node };
        
            MeshFilter filter = sampleData.nodeData.Prefab.GetComponent<MeshFilter>();
            sampleData.nodeData.SetRotation(sampleData.nodeData.ClockwiseRotationSteps * 90.0f);
            
            for (int i = 0; i < wave.SamplesPerNode; i++)
                sampleData.samples.Add(new( meshSampler.GetSamples(filter)));
            
            generatedSamples.Add(sampleData);

            wave.EditorTakeStepProgress($"[{j + 1}/{nodes.Count}] {node.name} ({sampleData.samples.Count} sets, {sampleData.samples.Sum(s => s.samples.Count)} total samples)");

            yield return null;
        }

        st.Stop();

        wave.doneGeneratingSamples = true;
        wave.EditorMessageProgress($"Sampling tiles in {st.ElapsedMilliseconds} ms ({st.ElapsedMilliseconds / 1000f} s)", Color.gray);
        wave.EditorMessageProgress($"Total samples: {generatedSamples.Count}", Color.gray);
        wave.EditorStopStepCounterProgress();
        wave.EditorSetBarProgress(100);
        wave.EditorMessageProgress("Done generating nodes", Color.green);

        funcDoneHook();
    }

    // Chooses a random set of samples, filteres it and spawns its props
    public void Generate(Node node, Vector3 size)
    {
        _envObj = AssetManager.LoadRandomEnvironment(node.nodeType);
        _hierarchyInfo = new PropHierarchy.PropHierachyInfo(Guid.Empty, _envObj.SpawnHierarchy, 0);
        Prop.Props.AddEntry(_hierarchyInfo.parentId, _hierarchyInfo.id, gameObject, true);

        if (_hierarchyInfo.IsCurrentHierachyLarger()) return;

        _nodeData = node;

        SampleData sampleData = generatedSamples.Single(s => s.nodeData.name == _nodeData.name);

        int randomSampleSet = UnityEngine.Random.Range(0, sampleData.samples.Count);
        List<Sample> selectedSamples = new(sampleData.samples[randomSampleSet].samples.Select((s) => new Sample()
        {
            sample = gameObject.transform.localPosition + s.sample,
            triangleNormal = s.triangleNormal
        }).Where(IsSampleNotInsideMesh));

        meshSampler.SetSpawnerData(_hierarchyInfo);
        meshSampler.AddSamples(selectedSamples);

        if (sampleData == null || _envObj == null) return; // this node has either no samples or no environment, so nothing can spawn here
        if (_envObj.CanSpawnSeperators) SpawnSeperators(); // Only spawn walls and beams if the environment allows it

        meshSampler.SpawnProps(gameObject, GetSpawner(), (sample, prop) => IsPropContained(sample, prop.PropObject, size) || _nodeData.IsStairPiece);
    }

    private Spawner GetSpawner()
    {
        List<Prop> floorProps = new(AssetManager.LoadProps(PropPlacementType.Floor).Where(p => !_nodeData.exceptionsProps.Contains(p)));
        List<Prop> wallProps = new(AssetManager.LoadProps(PropPlacementType.Wall).Where(p => !_nodeData.exceptionsProps.Contains(p)));

        Spawner spawner = new(floorProps, wallProps)
        {
            maxFloorPropCount = _envObj.MaxFloorCount,
            maxWallPropCount = _envObj.MaxWallCount
        };

        return spawner;
    }

    // Checks according to the type of node what samples are not inside of the mesh aka. not inside a wall
    private bool IsSampleNotInsideMesh(Sample s)
    {
        Bounds b = GetComponent<MeshCollider>().bounds;
        List<Vector3> samplePoints = new() { b.center };

        if (_nodeData.Left.name != NodeFace.Name.Wall) samplePoints.Add(b.center + new Vector3(wfc.TileSize.x / -2.5f, 0, 0));
        if (_nodeData.Right.name != NodeFace.Name.Wall) samplePoints.Add(b.center + new Vector3(wfc.TileSize.x / 2.5f, 0, 0));
        if (_nodeData.Front.name != NodeFace.Name.Wall) samplePoints.Add(b.center + new Vector3(0, 0, wfc.TileSize.z / 2.5f));
        if (_nodeData.Back.name != NodeFace.Name.Wall) samplePoints.Add(b.center + new Vector3(0, 0, wfc.TileSize.z / -2.5f));

        samplePoints = samplePoints.Select(p => Misc.RotatePointAroundPivot(p, b.center, -transform.eulerAngles)).ToList();

        return !meshSampler.IsInsideMesh(s, samplePoints.ToArray());
    }

    // Spawns door deviders to seperate this node from other nodes
    private void SpawnSeperators()
    {
        if (_nodeData.IsStairPiece) return;

        if (_nodeData.Left.name != NodeFace.Name.Wall) SpawnDoor(new(wfc.TileSize.x / -2, 0, 0));
        if (_nodeData.Right.name != NodeFace.Name.Wall) SpawnDoor(new(wfc.TileSize.x / 2, 0, 0));
        if (_nodeData.Front.name != NodeFace.Name.Wall) SpawnDoor(new(0, 0, wfc.TileSize.z / 2));
        if (_nodeData.Back.name != NodeFace.Name.Wall) SpawnDoor(new(0, 0, wfc.TileSize.z / -2));

        SpawnBeam();
    }

    private void SpawnDoor(Vector3 pos)
    {
        if (UnityEngine.Random.Range(0.0f, 1.0f) > 0.5f) return;
        if (!wfc.IsInside(transform.position + pos + pos)) return; // Doors should only spawn between node's, not at the edge of the maze

        // Spawn wall
        Prop propWall = AssetManager.LoadProp("Wall_Door", PropPlacementType.Floor);

        bool reflect = pos.x != 0.0f;
        int neg = (pos.x < 0 || pos.z < 0) ? -1 : 1;

        Vector3 absPos = transform.position + pos;
        Quaternion rot = reflect ? Quaternion.Euler(new(0, 90, 0)) : Quaternion.identity;
        Vector3 scale = new(0.85f, 0.85f, 0.85f);

        if (propWall.PropObject.CheckOverlapBox(absPos, rot, (List<Collider> cols) => cols.Where(c => c.transform.name.Contains("Wall_DoorStand")))) return;

        SpawnProp(propWall, absPos, rot, scale);

        // Spawn door
        Prop propDoor = AssetManager.LoadProp("Door_Middle", PropPlacementType.Floor);

        Vector3 doorAbs = absPos + (reflect ? new(0, 0, 0.585f) : new(0.585f, 0, 0));
        Quaternion doorRot = reflect ? Quaternion.Euler(new(0, 270 + UnityEngine.Random.Range(-90, 90), 0)) : Quaternion.identity;

        SpawnProp(propDoor, doorAbs, doorRot, scale);
    }

    public void SpawnBeam()
    {
        if (!_nodeData.AllowBeamSpawn || UnityEngine.Random.Range(0.0f, 1.0f) > 0.5f) return;

        Prop prop = AssetManager.LoadProp("Wall_Beam", PropPlacementType.Floor);

        SpawnProp(prop, transform.position, Quaternion.identity, new(0.35f, 0.85f, 0.35f));
    }

    private void SpawnProp(Prop prop, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        PropObject propObj = Instantiate(prop.PropObject, pos, rot);
        propObj.transform.SetParent(transform);
        propObj.transform.localScale = scale;

        Prop.Props.Increase(_hierarchyInfo.id, prop.name);
        propObj.UpdateChildren(_hierarchyInfo);
    }

    private bool IsPropContained(Vector3 sample, PropObject obj, Vector3 size)
    {
        Bounds myBounds = new Bounds(transform.position, size);
        Bounds otherBounds = new Bounds(sample, obj.GetSize);

        return myBounds.Contains(otherBounds.min) && myBounds.Contains(otherBounds.max);
    }
}
