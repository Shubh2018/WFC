using System.IO;
using UnityEngine;

public class TestData
{
    private static string _fileName = string.Empty;
    
    public static void SaveData(string data)
    {
        string path = $"{Application.dataPath}/{_fileName}.txt";
        
        // File.Open(path, FileMode.Append, FileAccess.Write);
        
        File.AppendAllText(path, $"{data}\n\n");
        
        Debug.Log($"Test Data Saved to {path}");
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
