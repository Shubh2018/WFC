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
    [SerializeField] private bool _spawnViaSpawner = false;
    [SerializeField] private PropSpawnTagEnum _spawnTypeTag;
    [SerializeField] private Spawner _gameObjectsToSpawn;
    [SerializeField] public int _spawnHierarchy = 5;
    [SerializeField] private int _maxFloorCount = 1;
    [SerializeField] private int _maxWallCount = 1;
    private int _currentHierachyLevel = 0;
    private Guid _id;

    // Static variables
    public static List<SampleData> generatedSamples = new List<SampleData>();
    public static MeshSampler meshSampler = null;
    public static int samplingAmount = 5;
    public static Spawner? spawner = null;

    public void Init()
    {
        _id = Guid.NewGuid();
        PropData.Props.AddEntry(Guid.Empty, _id, gameObject, true);
    }

    public static void SampleTiles(MeshSampler sampler, List<NodeData> nodes, Spawner spawner, float radius, int tries)
    {
        meshSampler = sampler;
        meshSampler.SetRadiusAndTries(radius, tries);

        generatedSamples.Clear();

        foreach (NodeData node in nodes)
        {
            if(node.Prefab == null) continue;
        
            SampleData sampleData = new SampleData { nodeData = node };
        
            MeshFilter filter = sampleData.nodeData.Prefab.GetComponent<MeshFilter>();
            sampleData.nodeData.SetRotation(sampleData.nodeData.ClockwiseRotationSteps * 90.0f);
            
            for (int i = 0; i < samplingAmount; i++)
                sampleData.samples.Add(new Samples(meshSampler.GetSamples(filter)));
            
            generatedSamples.Add(sampleData);
        }
    }

    public void Generate(NodeData node, Spawner? objs)
    {
        if (_currentHierachyLevel > _spawnHierarchy) return;

        _nodeData = node;

        meshSampler.SetParent(_id);

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

        meshSampler.AddSamples(selectedSamples);

        if (objs != null) spawner = objs;
        else if (!_spawnViaSpawner) spawner = new Spawner(Utils.LoadFilteredProps(_spawnTypeTag), _maxFloorCount, _maxWallCount);
        else spawner = _gameObjectsToSpawn;

        meshSampler.SetSpawnerData((Spawner) spawner, _spawnHierarchy, _currentHierachyLevel);
        meshSampler.SpawnProps(gameObject, _nodeData.CanHaveObjective);
    }
}
