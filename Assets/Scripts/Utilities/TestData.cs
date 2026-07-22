using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

public static class TestData
{
    private static Dictionary<string, int> propCollection = new Dictionary<string, int>();
    private static string _fileName = string.Empty;

    private static float _sizeNormalized;
    private static List<Data> dataList = new List<Data>();
    
    public static void SaveData()
    {
        string data = JsonConvert.SerializeObject(dataList, Formatting.Indented);
        string path = $"{Application.dataPath}/Test1.json";
        
        // File.Open(path, FileMode.Append, FileAccess.Write);
        
        File.AppendAllText(path, $"{data}\n\n");
        
        Debug.Log($"Test Data Saved to {path}");
    }

    public static void AddToDict(string key, float size)
    {
        if (String.IsNullOrEmpty(key)) return;
        
        if (!propCollection.TryAdd(key, 1))
        {
            propCollection[key] += 1;
        }

        _sizeNormalized = (WFC.wfc.CurrentSize - WFC.wfc.MinSize) / (WFC.wfc.MaxSize - WFC.wfc.MinSize);
    }

    public static void CalculateData()
    {
        int props = AssetManager.LoadProps(PropPlacementType.Floor).Count + AssetManager.LoadProps(PropPlacementType.Wall).Count;

        float entropy = 0;

        int propCount = propCollection.Sum(prop => prop.Value);

        foreach (var prop in propCollection)
        {
            float proportion = (float)prop.Value / (float)propCount;
            entropy += (-proportion * Mathf.Log(proportion, 2)); /// (Mathf.Log(props, 2));
        }
        
        Data d = new Data();
        d.entropy = entropy;
        d.size = _sizeNormalized;
        
        dataList.Add(d);
    }

    public static void ClearDict()
    {
        propCollection.Clear();
        dataList.Clear();
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