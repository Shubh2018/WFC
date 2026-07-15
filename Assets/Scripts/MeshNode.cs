using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

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
        this.samples = new List<Samples>();
    }
}

public class MeshNode : MonoBehaviour
{
    private Node _nodeData;
    [SerializeField] private PropSpawnTagEnum _spawnTypeTag;
    [SerializeField] private Spawner _gameObjectsToSpawn;
    [SerializeField] public int _spawnHierarchy = 5;
    [SerializeField] private int _maxFloorCount = 1;
    [SerializeField] private int _maxWallCount = 1;
    private PropHierarchy.PropHierachyInfo _hierarchyInfo;

    // Static variables
    public static List<SampleData> generatedSamples = new List<SampleData>();
    public static MeshSampler meshSampler = null;
    public static Spawner? spawner = null;
    public static WFC wfc = null;

    public void Init()
    {
        _hierarchyInfo = new PropHierarchy.PropHierachyInfo(Guid.Empty, _spawnHierarchy, 0);
        Prop.Props.AddEntry(_hierarchyInfo.parentId, _hierarchyInfo.id, gameObject, true);
    }

    public static void SampleTiles(MeshSampler sampler, WFC wave, List<Node> nodes, float radius, int tries, int sampleAmount, int floorGraphLevel, int wallGraphLevel)
    {
        wfc = wave;
        meshSampler = sampler;
        meshSampler.SetSamplingGraphProperties(radius, tries, floorGraphLevel, wallGraphLevel);

        generatedSamples.Clear();

        foreach (Node node in nodes)
        {
            if(node.Prefab == null) continue;
        
            SampleData sampleData = new SampleData { nodeData = node };
        
            MeshFilter filter = sampleData.nodeData.Prefab.GetComponent<MeshFilter>();
            sampleData.nodeData.SetRotation(sampleData.nodeData.ClockwiseRotationSteps * 90.0f);
            
            for (int i = 0; i < sampleAmount; i++)
                sampleData.samples.Add(new Samples(meshSampler.GetSamples(filter)));
            
            generatedSamples.Add(sampleData);
        }
    }

    // Chooses a random set of samples, filteres it and spawns its props
    public void Generate(Node node, Vector3 size)
    {
        if (_hierarchyInfo.IsCurrentHierachyLarger()) return;

        _nodeData = node;

        SampleData sampleData = generatedSamples.Single(s => s.nodeData.name == _nodeData.name);

        if (sampleData == null/* || _nodeData.IsStairPiece*/) return;

        Bounds b = new Bounds(transform.position + new Vector3(0.0f, wfc.TileSize.y / 2, 0.0f), wfc.TileSize);
        int randomSampleSet = UnityEngine.Random.Range(0, sampleData.samples.Count);
        List<Sample> selectedSamples = new(sampleData.samples[randomSampleSet].samples.Select((s) => new Sample()
        {
            sample = gameObject.transform.localPosition + s.sample,
            triangleNormal = s.triangleNormal
        }).Where((s) => meshSampler.IsInsideMesh(s, b)));

        SpawnSeperators();

        meshSampler.SetSpawnerData(_hierarchyInfo);
        meshSampler.AddSamples(selectedSamples);

        Func<(Prop, int)> propFloorSpawnerFunc = () => node.GetRandomPropCDF(PropPlacementType.Floor);
        Func<(Prop, int)> propWallSpawnerFunc = () => node.GetRandomPropCDF(PropPlacementType.Wall);
        Func<Prop, PropNeighborProperty> propNeighborSpawnerFunc = (Prop prop) => prop.GetRandomProp();
        Func<Vector3, Prop, bool> spawnFilterFunc = (Vector3 sample, Prop prop) => IsPropContained(sample, prop.PropObject, size);

        meshSampler.SpawnProps(gameObject, _maxFloorCount, _maxWallCount, propFloorSpawnerFunc, propWallSpawnerFunc, propNeighborSpawnerFunc, spawnFilterFunc);
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
        if (!wfc.IsInside(transform.position + pos + pos)) return; // Doors should only spawn between node's not at the edge of the maze

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
