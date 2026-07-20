using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public static class TestData
{
    private static Dictionary<string, int> propCollection = new Dictionary<string, int>();
    private static string _fileName = string.Empty;
    
    public static void SaveData()
    {
        string data = JsonConvert.SerializeObject(propCollection, Formatting.Indented);
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