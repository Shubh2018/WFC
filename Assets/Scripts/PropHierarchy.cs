using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using Unity.Entities.UniversalDelegates;

// Keeps track of the amount of spawned props per node, per hierarchy element (surface, point, node), and in total across everything
public class PropHierarchy
{
    public Dictionary<string, int> propCount; // Keeps tracks of prop count for the current instance
    public Dictionary<GameObject, int> propObjs; // List of all gameobjects within this hierachy instance
    public List<PropHierarchy> children; // List of all children aka other surfaces and points spawning their own props
    public PropHierarchy parent; // The parent to this instance, null if root
    public Guid InstanceId; // The id related to this instance taken from the gameobject it relates to, using GetEntityId()
    public bool IsRoot; // True if it is the root, else false
    public bool IsRoom; // True if this entry is part of a node, else false

    // Constructor for the class, sets up an instance for a given parent
    public PropHierarchy(PropHierarchy parentInstance, Guid instanceId, bool isRoom = false)
    {
        Setup(parentInstance, instanceId, isRoom);
    }

    // Necessary to add an ID to the root instance
    public void Setup(PropHierarchy parentInstance, Guid instanceId, bool isRoom = false)
    {
        propCount = new Dictionary<string, int>();
        propObjs = new Dictionary<GameObject, int>();
        children = new List<PropHierarchy>();
        parent = parentInstance;
        InstanceId = instanceId;
        IsRoot = instanceId == Guid.Empty;
        IsRoom = isRoom;
    }

    // Adds a child to the specified parent
    public void AddEntry(Guid parentId, Guid instanceId, GameObject propObj, bool isRoom = false)
    {
        if (parentId == instanceId) return;
        if (parentId == InstanceId)
        {
            children.Add(new PropHierarchy(this, instanceId, isRoom));
            AddGameObject(propObj);
            return;
        }
        PropHierarchy parent = FindEntry(parentId);
        if (parent == null) return;
        parent.AddEntry(parentId, instanceId, propObj, isRoom);
    }

    // Remove a child from whatever parent it is tied to
    public void RemoveEntry(Guid instanceId, GameObject propObj)
    {
        if (instanceId == InstanceId) return;
        PropHierarchy child = FindEntry(instanceId);
        if (child == null || child.parent == null) return;
        child.parent.children.Remove(child);
        child.parent.RemoveGameObject(propObj);
    }

    // Adds a gameobject to a dictionary, if it is already there its value will be increased
    public void AddGameObject(GameObject propObj)
    {
        if (propObj == null) return;
        if (!propObjs.ContainsKey(propObj)) propObjs.Add(propObj, 1);
        else propObjs[propObj]++;
    }

    // Removes a gameobject for the dictionary if it is out of references aka. has a value of zero to its key
    // Since multiple props of a child can reference a gameobject, we need to keep a count before it can be removed completly
    public void RemoveGameObject(GameObject propObj)
    {
        if (propObj == null || !propObjs.ContainsKey(propObj)) return;
        if (propObjs[propObj] == 1) propObjs.Remove(propObj);
        else propObjs[propObj]--;
    }

    // Returns a list of all gameobjects for the current child
    public List<GameObject> GetGameObjects()
    {
        return GetGameObjects(InstanceId);
    }

    // Returns a list of all gameobject of the same child
    // Used for collision detection when deciding when and where to spawn (check out PropObject for details)
    public List<GameObject> GetGameObjects(Guid instanceId)
    {
        PropHierarchy child = FindEntry(instanceId);
        if (child == null) return new List<GameObject>();
        return child.propObjs.Keys.ToList();
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
        if (parent == null) return null;
        else return parent.FindParentRoomEntry();
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
        Debug.Log($"{indentChars}instance: {InstanceId}, isRoot: {IsRoot}, isRoom: {IsRoom}, props: {propCount.Count()}, children: {children.Count()}, gameObject: {propObjs.Count()}");
        if (propObjs.Count() > 0) propObjs.ToList().ForEach(obj => Debug.Log($"{indentChars}> {obj.Key.name}: {obj.Value}"));
        if (propObjs.Count() > 0 && propCount.Count() > 0) Debug.Log($"{indentChars} ------------------------");
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
        return prop.LimitType switch {
            PropLimitTypeEnum.InTotal => prop.LimitCount > SumPropTotalAmount(prop.PropObject.name),
            PropLimitTypeEnum.PerHierarchyElement => prop.LimitCount > SumPropLocalHierachyAmount(instanceId, prop.PropObject.name),
            PropLimitTypeEnum.PerRoom => prop.LimitCount > SumPropRoomAmount(instanceId, prop.PropObject.name),
            _ => false
        };
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