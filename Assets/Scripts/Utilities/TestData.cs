using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

public static class TestData
{
    public static WFC _wfc;
    private static Dictionary<Prop, int> _propCollection = new Dictionary<Prop, int>();
    private static Dictionary<DecorationType, int> _propDecorationCollection = new Dictionary<DecorationType, int>();

    private static int props = 0;
    
    private static List<PropTestData> _propTestDataList = new List<PropTestData>();
    
    private static List<CombinedData> _anndVsEntropy = new List<CombinedData>();
    private static List<CombinedData> _propDensityVsEntropy = new List<CombinedData>();
    private static List<CombinedData> _combinedDataList = new List<CombinedData>();

    public static void SaveData()
    {
        string spatialData = JsonConvert.SerializeObject(_anndVsEntropy, Formatting.Indented);
        string diversityData = JsonConvert.SerializeObject(_propDensityVsEntropy, Formatting.Indented);
        
        string anndDataPath = $"{Application.dataPath}/ANNDvsEntropy.json";
        string propDensityDataPath = $"{Application.dataPath}/PropDensityVsEntropy.json";

        File.WriteAllText(anndDataPath, $"{spatialData}\n\n");
        File.WriteAllText(propDensityDataPath, $"{diversityData}\n\n");

        Debug.Log($"Test Data Saved");
    }

    public static void AddToDict(Prop key, Vector3 pos, Node.NodeType nodeType)
    {
        if (key == null) return;

        _propTestDataList.Add(new PropTestData(key, pos, nodeType));

        if (!_propCollection.TryAdd(key, 1))
        {
            _propCollection[key] += 1;
        }
    }

    public static void AddPropDecorationType(DecorationType decorationType)
    {
        if (!_propDecorationCollection.TryAdd(decorationType, 1))
        {
            _propDecorationCollection[decorationType] += 1;
        }
    }

    public static void CalculateData()
    {
        props = AssetManager.LoadProps(PropPlacementType.Floor).Count;

        int propCount = _propCollection.Sum(prop=>  prop.Key.Placement != PropPlacementType.Wall ? prop.Value : 0);

        float entropy = CalculateEntropy(_propCollection, propCount);

        float averageNND = CalculateAverageNND(_propTestDataList, propCount);
        float propDensity = PropDensity(propCount, _wfc.SpawnedTileCount);
        
        _anndVsEntropy.Add(new CombinedData(averageNND, entropy));
        _propDensityVsEntropy.Add(new CombinedData(propDensity, entropy));

        ClearDict();
    }

    private static float CalculateEntropy(Dictionary<Prop, int> propCollection, int propCount)
    {
        float entropy = 0;

        foreach (var prop in propCollection)
        {
            float proportion = (float)prop.Value / (float)propCount;
            entropy += (-proportion * Mathf.Log(proportion, 2));
        }

        return entropy;
    }

    public static float CalculateRichness(Dictionary<DecorationType, int> propCollections)
    {
        float richness = 0;
        
        foreach (var prop in propCollections)
        {
            if (prop.Value > 0 && prop.Key != DecorationType.None)
            {
                richness += 1;
            }
        }

        return richness;
    }

    private static float PropDensity(int propCount, int floorNodeCount)
    {
        float nodeArea = _wfc.TileSize.x * _wfc.TileSize.z;

        float totalArea = floorNodeCount * nodeArea;

        if (totalArea <= 0)
            return 0.0f;

        return propCount / totalArea;
    }

    private static float CalculateAverageNND(List<PropTestData> props, int propCount)
    {
        float sumDistance = 0;

        foreach (var p in props)
        {
            float minDistance = float.PositiveInfinity;

            foreach (var p1 in props)
            {
                if (p != p1)
                {
                    float dist = Vector3.Distance(p1.pos, p.pos);
                    minDistance = Mathf.Min(minDistance, dist);
                }
            }

            sumDistance += minDistance;
        }

        return sumDistance / propCount;
    }

    public static void ClearDict()
    {
        _propCollection.Clear();
        _propTestDataList.Clear();
    }

    public static void CreateFile(string fileName)
    {
        // string path = $"{Application.dataPath}/{fileName}.txt";
        //
        // if (!File.Exists(path))
        // {
        //     File.Create(path);
        //     _fileName = fileName;
        //     Debug.Log($"Created file {path}");
        // }
        //
        // else
        // {
        //     _fileName = fileName;
        // }
    }
}

public class Data
{
    public float entropy;
    public float size;

    public Data()
    {
        entropy = 0;
        size = 0;
    }
}

public class PropTestData
{
    public Prop prop;
    public Vector3 pos;
    public Node.NodeType nodeType;

    public PropTestData(Prop prop, Vector3 pos, Node.NodeType nodeType)
    {
        this.prop = prop;
        this.pos = pos;
        this.nodeType = nodeType;
    }
}

public class NearestNeighborData
{
    public float entropy;
    public float nearestNeighborDistance;
    public float levelSize;
    public float samplingRadius;

    public NearestNeighborData()
    {
        entropy = 0;
        nearestNeighborDistance = 0;
    }

    public NearestNeighborData(float entropy, float nearestNeighborDistance, float levelSize, float samplingRadius)
    {
        this.entropy = entropy;
        this.nearestNeighborDistance = nearestNeighborDistance;
        this.levelSize = levelSize;
        this.samplingRadius = samplingRadius;
    }
}

public class SpatialData
{
    public float AvgNearestNeighborDistance;
    public float PropDensity;

    public SpatialData(float avgNearestNeighborDistance, float propDensity)
    {
        this.AvgNearestNeighborDistance = avgNearestNeighborDistance;
        this.PropDensity = propDensity;
    }
}

public class DiversityData
{
    public float Richness;
    public float Entropy;

    public DiversityData(float richness, float entropy)
    {
        this.Richness = richness;
        this.Entropy = entropy;
    }
}

public class CombinedData
{
    public float Metric;
    public float Entropy;

    public CombinedData(float metric, float entropy)
    {
        this.Metric = metric;
        this.Entropy = entropy;
    }
}