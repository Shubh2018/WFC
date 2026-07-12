using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

// Keeps track of the amount of spawned props per node, per hierarchy element (surface, point, node), and in total across everything
public class PropHierarchy
{
    public Dictionary<string, int> propCount; // Keeps tracks of prop count for the current instance
    public GameObject childObj; // A gameobject refering to either a MeshNode, MeshSurface or MeshPoint
    public List<PropHierarchy> children; // List of all children aka other surfaces and points spawning their own props
    public PropHierarchy parent; // The parent to this instance, null if root
    public Guid InstanceId; // The id related to this instance taken from the gameobject it relates to, using GetEntityId()
    public bool IsRoot; // True if it is the root, else false
    public bool IsRoom; // True if this entry is part of a node, else false

    // Struct used to pass data around
    public struct PropHierachyInfo
    {
        public Guid id;
        public Guid parentId;
        public int maxHierachyLevel;
        public int currentHierachyLevel;

        public PropHierachyInfo(Guid parentId, int maxHierachyLevel, int currentHierachyLevel)
        {
            id = Guid.NewGuid();

            this.parentId = parentId;
            this.maxHierachyLevel = maxHierachyLevel;
            this.currentHierachyLevel = currentHierachyLevel;
        }

        public PropHierachyInfo(PropHierachyInfo parentHierachy, int localMaxHierarchyLevel)
        {
            id = parentHierachy.id;
            parentId = parentHierachy.parentId;
            maxHierachyLevel = Mathf.Min(parentHierachy.maxHierachyLevel, localMaxHierarchyLevel);
            currentHierachyLevel = parentHierachy.currentHierachyLevel + 1;
        }

        public bool IsCurrentHierachyLarger()
        {
            return currentHierachyLevel > maxHierachyLevel;
        }
    }

    // Constructor for the class, sets up an instance for a given parent
    public PropHierarchy(PropHierarchy parentInstance, Guid instanceId, GameObject gameObj = null, bool isRoom = false)
    {
        Setup(parentInstance, instanceId, gameObj, isRoom);
    }

    // Necessary to add an ID to the root instance
    public void Setup(PropHierarchy parentInstance, Guid instanceId, GameObject gameObj, bool isRoom = false)
    {
        propCount = new Dictionary<string, int>();
        childObj = gameObj;
        children = new List<PropHierarchy>();
        parent = parentInstance;
        InstanceId = instanceId;
        IsRoot = instanceId == Guid.Empty;
        IsRoom = isRoom;
    }

    // Adds a child to the specified parent
    public void AddEntry(Guid parentId, Guid instanceId, GameObject gameObj, bool isRoom = false)
    {
        if (parentId == instanceId) return;
        if (parentId == InstanceId)
        {
            children.Add(new PropHierarchy(this, instanceId, gameObj, isRoom));
            return;
        }
        PropHierarchy parent = FindEntry(parentId);
        if (parent == null) return;
        parent.AddEntry(parentId, instanceId, gameObj, isRoom);
    }

    // Remove a child from whatever parent it is tied to
    public void RemoveEntry(Guid instanceId)
    {
        if (instanceId == InstanceId) return;
        PropHierarchy child = FindEntry(instanceId);
        if (child == null || child.parent == null) return;
        child.parent.children.Remove(child);
    }

    // Returns true if this instance contains a child with the specified id, else false
    public bool DoesContainChild(Guid instanceId)
    {
        return children.Any(child => child.InstanceId == instanceId);
    }

    // Finds and returns the child in the hierarchy with the specified id, else null
    public PropHierarchy FindEntry(Guid instanceId)
    {
        if (InstanceId == instanceId) return this;
        foreach (PropHierarchy child in children)
        {
            PropHierarchy currChild = child.FindEntry(instanceId);
            if (currChild != null) return currChild;
        }
        return null;
    }

    // Tries to find a room hierachy element if there is one, else null
    public PropHierarchy FindParentRoomEntry()
    {
        if (IsRoom) return this;
        return parent?.FindParentRoomEntry() ?? null;
    }

    // Returns the value for a prop, else zero if its entry does not exist
    public int GetPropValue(string propName)
    {
        if (propCount.ContainsKey(propName))
            return propCount[propName];
        else
            return 0;
    }

    // Sets the value for a prop counter
    public void SetPropValue(string propName, int value)
    {
        if (propCount.ContainsKey(propName))
            propCount[propName] = value;
        else
            propCount.Add(propName, value);
    }

    // Prints out all values for props for this child
    public void PrintPropValues(int indent = 0)
    {
        string indentChars = new string('\t', indent);
        Debug.Log($"{indentChars}instance: {InstanceId}, isRoot: {IsRoot}, isRoom: {IsRoom}, props: {propCount.Count()}, children: {children.Count()}, childObj: {childObj != null}");
        if (propCount.Count() > 0) propCount.ToList().ForEach(prop => Debug.Log($"{indentChars}> {prop.Key}: {prop.Value}"));
    }

    // Print out all values for this child and its children
    public void PrintChildren(int indent = 0)
    {
        PrintPropValues(indent);
        children.ForEach(children => children.PrintChildren(indent+1));
    }

    // Prints out the entire hierachy from the top
    public void PrintHierarchy()
    {
        Debug.Log("Full prop hierarchy:");
        GetRoot().PrintChildren();
    }

    // Increases the value for a prop with a specified name within a specific child
    // Returns if a child with the specified id does not exist
    public void Increase(Guid instanceId, string propName)
    {
        if (InstanceId == instanceId) 
        {
            SetPropValue(propName, GetPropValue(propName) + 1);
            return;
        }
        PropHierarchy child = FindEntry(instanceId);
        if (child == null) return;
        child.Increase(instanceId, propName);
    }

    // Decreases the value for a prop with a specified name within a specific child
    // Returns if a child with the specified id does not exist
    public void Decrease(Guid instanceId, string propName)
    {
        if (InstanceId == instanceId) 
            SetPropValue(propName, Mathf.Max(GetPropValue(propName) - 1, 0));
        PropHierarchy child = FindEntry(instanceId);
        if (child == null) return;
        child.Increase(instanceId, propName);
    }

    // Returns the root object, the parent without a parent of its own
    public PropHierarchy GetRoot()
    {
        if (parent == null) return this;
        return parent.GetRoot();
    }

    // Returns true if a specific prop can be spawned based on its conditions, else false
    public bool CanSpawnProp(Guid instanceId, Prop prop)
    {
        bool l = prop.LimitType switch {
            PropLimitTypeEnum.InTotal => prop.LimitCount == SumPropTotalAmount(prop.PropObject.name),
            PropLimitTypeEnum.PerHierarchyElement => prop.LimitCount == SumPropLocalHierachyAmount(instanceId, prop.PropObject.name),
            PropLimitTypeEnum.PerRoom => prop.LimitCount == SumPropRoomAmount(instanceId, prop.PropObject.name),
            _ => false
        };
        return !l;
    }

    // Returns the total sum scattered around for a prop with a given name from the root and down
    public int SumPropTotalAmount(string propName)
    {
        return GetRoot().SumPropHierachyAmount(propName);
    }

    // Returns the sum for a prop with a given name for the nearest room
    public int SumPropRoomAmount(Guid instanceId, string propName)
    {
        PropHierarchy child = FindEntry(instanceId)?.FindParentRoomEntry();
        if (child == null) return SumPropTotalAmount(propName); // If no room exists, do total instead as a backup
        return child.SumPropLocalAmount(propName);
    }

    // Returns the sum for a prop with a given name for the current hieachy element
    public int SumPropLocalHierachyAmount(Guid instanceId, string propName)
    {
        PropHierarchy child = GetRoot().FindEntry(instanceId);
        if (child == null) return 0;
        return child.SumPropLocalAmount(propName);
    }

    // Returns the sum for a prop with a given name from the current child and down
    public int SumPropHierachyAmount(string propName)
    {
        int sum = SumPropLocalAmount(propName);
        sum += children.Sum(child => child.SumPropHierachyAmount(propName));
        return sum;
    }

    // Returns the sum for a prop with a given name for the current child only
    public int SumPropLocalAmount(string propName)
    {
        return propCount.Sum(prop => prop.Key == propName ? prop.Value : 0);
    }

    // Removes a child from the current list of children
    public void RemoveChild(Guid instanceId)
    {
        children.Remove(children.Find(child => child.InstanceId == instanceId));
    }

    // Clears the list of all children
    public void ClearChildren()
    {
        children.Clear();
        childObj = null;
    }

    // Clears the list of all prop countings
    public void ClearProps()
    {
        propCount.Clear();
    }

    // Clears all children and props countings
    public void Clear()
    {
        if (IsRoot) InstanceId = Guid.Empty;
        ClearChildren();
        ClearProps();
    }
}