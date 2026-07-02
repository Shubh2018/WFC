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
    public NodeData nodeData;
    public List<Samples> samples;

    public SampleData()
    {
        this.samples = new List<Samples>();
    }
}

public class MeshNode : MonoBehaviour
{
    private NodeData _nodeData;
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

    public void Init()
    {
        _hierarchyInfo = new PropHierarchy.PropHierachyInfo(Guid.Empty, _spawnHierarchy, 0);
        Prop.Props.AddEntry(_hierarchyInfo.parentId, _hierarchyInfo.id, gameObject, true);
    }

    public static void SampleTiles(MeshSampler sampler, List<NodeData> nodes, float radius, int tries, int sampleAmount, int floorGraphLevel, int wallGraphLevel)
    {
        meshSampler = sampler;
        meshSampler.SetSamplingGraphProperties(radius, tries, floorGraphLevel, wallGraphLevel);

        generatedSamples.Clear();

        foreach (NodeData node in nodes)
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

    public void Generate(NodeData node)
    {
        if (_hierarchyInfo.IsCurrentHierachyLarger()) return;

        _nodeData = node;

        SampleData sampleData = generatedSamples.Single(s => s.nodeData == _nodeData);

        if (sampleData == null || _nodeData.IsStairPiece) return;

        List<Sample> selectedSamples = new List<Sample>();
        int randomSampleSet = UnityEngine.Random.Range(0, sampleData.samples.Count);

        foreach (var sample in sampleData.samples[randomSampleSet].samples)
        {                    
            selectedSamples.Add(new Sample() {
                sample = gameObject.transform.localPosition + sample.sample,
                triangleNormal = sample.triangleNormal
            });
        }

        meshSampler.SetSpawnerData(_hierarchyInfo);
        meshSampler.AddSamples(selectedSamples);

        Func<Prop> propFloorSpawnerFunc = () => node.GetRandomPropCDF(PropPlacementType.Floor);
        Func<Prop> propWallSpawnerFunc = () => node.GetRandomPropCDF(PropPlacementType.Wall);
        Func<Prop, PropNeighborProperty> propNeighborSpawnerFunc = (Prop prop) => prop.GetRandomProp();

        meshSampler.SpawnProps(gameObject, _maxFloorCount, _maxWallCount, propFloorSpawnerFunc, propWallSpawnerFunc, propNeighborSpawnerFunc);
    }

    private bool IsPropContained(Vector3 sample, PropObject obj)
    {
        Bounds myBounds = new Bounds(transform.position, GetComponent<Collider>().bounds.size);
        Bounds otherBounds = new Bounds(sample, obj.GetSize());

        return myBounds.Contains(otherBounds.min) && myBounds.Contains(otherBounds.max);
    }
}
