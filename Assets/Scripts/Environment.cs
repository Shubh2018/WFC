using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public class EnvironmentAttribute : PropertyAttribute { }

[CustomPropertyDrawer(typeof(EnvironmentAttribute))]
public class EnvironmentDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        List<Environment> envs = AssetManager.LoadEnvironments();

        List<string> options = new List<string> { "None" };
        List<string> ids = new List<string> { "" };

        foreach (Environment env in envs)
        {
            options.Add(env.Name);
            ids.Add(env.Name);
        }

        int index = ids.IndexOf(property.stringValue);
        if (index == -1) index = 0;

        index = EditorGUI.Popup(position, label.text, index, options.ToArray());

        property.stringValue = ids[index];
        EditorGUI.EndProperty();
    }
}

[CreateAssetMenu(fileName = "Environment", menuName = "WFC/Environment")]
public class Environment : ScriptableObject
{
    [SerializeField] public string Name;
    [SerializeField] public Color DisplayColor;
    [SerializeField] public List<PropSpawningEntry> SpawningEntries;
    [SerializeField] public Node.NodeType LegalNodesEntries;
    [SerializeField] public bool IgnoreSubElements;
    [SerializeField] public bool CanSpawnSeperators;
    [SerializeField] public int SpawnHierarchy = 5;
    [SerializeField] public int MaxFloorCount = 1;
    [SerializeField] public int MaxWallCount = 1;

    public PropSpawningEntry GetEntry(List<string> keywords, bool subElement = false)
    {
        foreach(PropSpawningEntry entry in SpawningEntries)
        {
            bool inList = keywords.All(k => entry.keywords.Contains(k));
            if (inList && (!subElement && !entry.SubElementsOnly || subElement)) return entry;
        }

        return new PropSpawningEntry{ keywords = new string[]{} };
    }
}

[Serializable]
public class PropSpawningEntry
{
    public string[] keywords;
    public bool SubElementsOnly;
    public bool Required;
}