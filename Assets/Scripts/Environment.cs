using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Environment", menuName = "WFC/Environment")]
public class Environment : ScriptableObject
{
    [SerializeField] public string Name;
    [SerializeField] public Color DisplayColor;
    [SerializeField] public List<PropSpawningEntry> SpawningEntries;
    [SerializeField] public Node.NodeType LegalNodesEntries;
    [SerializeField] public bool IgnoreSubElements;
    [SerializeField] public bool CanSpawnSeperators;
    [SerializeField] public int MaxFloorCount = 1;
    [SerializeField] public int MaxWallCount = 1;

    public PropSpawningEntry GetEntry(List<string> keywords, bool subElement = false)
    {
        foreach(PropSpawningEntry entry in SpawningEntries)
        {
            bool inList = keywords.All(k => entry.keywords.Contains(k));
            if (inList && (!subElement && !entry.SubElementsOnly || subElement)) return entry;
        }

        return null;
    }
}

[Serializable]
public class PropSpawningEntry
{
    public string[] keywords;
    public bool SubElementsOnly;
    public bool Required;
}