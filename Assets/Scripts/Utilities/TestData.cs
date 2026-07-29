using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

public static class TestData
{
    public static WFC _wfc;
    private static Dictionary<string, int> _propCollection = new Dictionary<string, int>();
    private static string _fileName = string.Empty;

    private static float _sizeNormalized;
    private static List<Data> _dataList = new List<Data>();
    private static List<Data> _dataListNotNormalized = new List<Data>();

    private static List<PropTestData> _propTestDataList = new List<PropTestData>();
    private static List<NearestNeighborData> _nearestNeighborDataList = new List<NearestNeighborData>();
    
    private static List<NearestNeighborData> _nearestNeighborDataListNotNormalized = new List<NearestNeighborData>();

    public static void SaveData()
    {
        string data = JsonConvert.SerializeObject(_nearestNeighborDataList, Formatting.Indented);
        string path = $"{Application.dataPath}/Test_Entropy+NND.json";
        // File.Open(path, FileMode.Append, FileAccess.Write);

        File.WriteAllText(path, $"{data}\n\n");

        data = JsonConvert.SerializeObject(_dataList, Formatting.Indented);
        path = $"{Application.dataPath}/Test_Entropy+Size.json";
        
        File.WriteAllText(path, $"{data}\n\n");
        
        data = JsonConvert.SerializeObject(_nearestNeighborDataListNotNormalized, Formatting.Indented);
        path = $"{Application.dataPath}/Test_Entropy+NND+NotNormalized.json";
        // File.Open(path, FileMode.Append, FileAccess.Write);

        File.WriteAllText(path, $"{data}\n\n");
        
        data = JsonConvert.SerializeObject(_dataListNotNormalized, Formatting.Indented);
        path = $"{Application.dataPath}/Test_Entropy+Size+NotNormalized.json";
        
        File.WriteAllText(path, $"{data}\n\n");
        
        Debug.Log($"Test Data Saved");
        
        ClearDict();
    }

    public static void AddToDict(string key, Vector3 pos)
    {
        if (String.IsNullOrEmpty(key)) return;
        
        _propTestDataList.Add(new PropTestData(key, pos));

        if (!_propCollection.TryAdd(key, 1))
        {
            _propCollection[key] += 1;
        }

        _sizeNormalized = (_wfc.CurrentSize - _wfc.MinSize) / (_wfc.MaxSize - _wfc.MinSize);
    }

    public static void CalculateData()
    {
        int props = AssetManager.LoadProps(PropPlacementType.Floor).Count +
                    AssetManager.LoadProps(PropPlacementType.Wall).Count;

        float entropy = 0;

        int propCount = _propCollection.Sum(prop => prop.Value);

        foreach (var prop in _propCollection)
        {
            float proportion = (float)prop.Value / (float)propCount;
            entropy += (-proportion * Mathf.Log(proportion, 2));
        }

        float entropyNormalized = entropy / Mathf.Log(props, 2);
        float sumDistance = 0;
        float nearestNeighborDistance = 0;

        foreach (var p in _propTestDataList)
        {
            float minDistance = float.PositiveInfinity;

            foreach (var p1 in _propTestDataList)
            {
                if (p != p1)
                {
                    float dist = Vector3.Distance(p1.pos, p.pos);
                    minDistance = Mathf.Min(minDistance, dist);
                }
            }
            
            sumDistance += minDistance;
        }
        
        float avgNND = sumDistance / _propTestDataList.Count;
        
        _nearestNeighborDataList.Add(new NearestNeighborData(entropyNormalized, avgNND));
        _nearestNeighborDataListNotNormalized.Add(new NearestNeighborData(entropy, sumDistance));

        Data d = new Data();
        d.entropy = entropyNormalized;
        d.size = _sizeNormalized;
        
        _dataList.Add(d);
        
        Data d2 = new Data();
        d2.entropy = entropy;
        d2.size = _wfc.CurrentSize;
        
        _dataListNotNormalized.Add(d2);
    }

    public static void ClearDict()
    {
        _propCollection.Clear();
        _propTestDataList.Clear();
    }

    public static void CreateFile(string fileName)
    {
        string path = $"{Application.dataPath}/{fileName}.txt";

        if (!File.Exists(path))
        {
            File.Create(path);
            _fileName = fileName;
            Debug.Log($"Created file {path}");
        }

        else
        {
            _fileName = fileName;
        }
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
    public string name;
    public Vector3 pos;

    public PropTestData(string name, Vector3 pos)
    {
        this.name = name;
        this.pos = pos;
    }
}

public class NearestNeighborData
{
    public float entropy;
    public float nearestNeighborDistance;

    public NearestNeighborData()
    {
        entropy = 0;
        nearestNeighborDistance = 0;
    }

    public NearestNeighborData(float entropy, float nearestNeighborDistance)
    {
        this.entropy = entropy;
        this.nearestNeighborDistance = nearestNeighborDistance;
    }
}