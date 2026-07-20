using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public static class TestData
{
    private static Dictionary<string, int> propCollection = new Dictionary<string, int>();
    private static string _fileName = string.Empty;
    
    public static void SaveData(Data d)
    {
        string data = JsonConvert.SerializeObject(d, Formatting.Indented);
        string path = $"{Application.dataPath}/Test1.csv";
        
        // File.Open(path, FileMode.Append, FileAccess.Write);
        
        File.AppendAllText(path, $"{data}\n\n");
        
        Debug.Log($"Test Data Saved to {path}");
    }

    public static void AddToDict(string key)
    {
        if (String.IsNullOrEmpty(key)) return;
        
        if (!propCollection.TryAdd(key, 1))
        {
            propCollection[key] += 1;
        }
    }

    public static void CalculateData()
    {
        int props = AssetManager.LoadProps(PropPlacementType.Floor).Count + AssetManager.LoadProps(PropPlacementType.Wall).Count;

        int propCount = 0;
        float entropy = 0;

        foreach (var prop in propCollection)
        {
            propCount += prop.Value;
        }

        foreach (var prop in propCollection)
        {
            float proportion = (float)prop.Value / (float)propCount;
            entropy += (-proportion * Mathf.Log(proportion, 2)) / (Mathf.Log(props, 2));
        }
        
        Data data = new Data();
        data.entropy = entropy;
        
        SaveData(data);
    }

    public static void ClearDict()
    {
        propCollection.Clear();
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

    public Data()
    {
        entropy = 0;
    }
}