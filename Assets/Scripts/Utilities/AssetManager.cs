using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

public static class AssetManager
{
    private static string propsPath = "Assets/Scripts/Props/";

    // Loads a single prop asset inside of a folder
    public static Prop LoadProp(string path, Func<Prop, bool> filterFunc)
    {
        string[] assets = Directory.GetFiles(path, "*.asset");

        foreach(string prop in assets)
        {
            Prop propData = (Prop) AssetDatabase.LoadAssetAtPath(prop, typeof(Prop));
            if (filterFunc(propData)) return propData;
        }

        return null;
    }

    // Loads and returns a single prop based on their placement type and name
    public static Prop LoadProp(string name, PropPlacementType placementType)
    {
        return LoadProp(propsPath, (Prop prop) => prop.name == name && prop.Placement == placementType);
    }

    // Loads all prop assets inside of a folder
    public static Spawner LoadProps(string path, Func<Prop, bool> filterFunc)
    {
        string[] assets = Directory.GetFiles(path, "*.asset");
        Spawner spawner = new Spawner(5, 5);

        foreach(string prop in assets)
        {
            Prop propData = (Prop) AssetDatabase.LoadAssetAtPath(prop, typeof(Prop));
            if (filterFunc(propData)) spawner.AddProp(propData);
        }

        return spawner;
    }

    // Loads all props within a specific size range
    public static Spawner LoadFilteredProps(PropSpawnTagEnum tag)
    {
        return LoadProps(propsPath, (Prop prop) => tag switch {
            PropSpawnTagEnum.Small or PropSpawnTagEnum.SmallToMedium => prop.SpawnTag == PropSpawnTagEnum.Small,
            PropSpawnTagEnum.Medium or PropSpawnTagEnum.SmallToMedium => prop.SpawnTag == PropSpawnTagEnum.Medium,
            PropSpawnTagEnum.Medium or PropSpawnTagEnum.MediumToLarge => prop.SpawnTag == PropSpawnTagEnum.Medium,
            PropSpawnTagEnum.Large or PropSpawnTagEnum.MediumToLarge => prop.SpawnTag == PropSpawnTagEnum.Large,
            PropSpawnTagEnum.Any => true,
            _ => false
        });
    }

    // Loads and returns a list of all props based on their placement type
    public static List<Prop> LoadProps(PropPlacementType placementType)
    {
        Spawner spawner = LoadProps(propsPath, (Prop prop) => prop.Placement == placementType);
        
        return placementType switch {
            PropPlacementType.Wall => spawner.WallPrefabs,
            PropPlacementType.Floor => spawner.FloorPrefabs,
            _ => null
        };
    }
}