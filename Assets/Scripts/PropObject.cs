using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PropObject : MonoBehaviour
{
    /*
    fix bugs:
    - [X] floor samples on walls (filtereing problem)
    - [X] floor props spawning inside wall (check for collision)
    - [X] prop points spawning outside of a node (check to prevent this)
    - sample generator sometimes spawn next to no samples
    - [X] sometimes no props spawn at all
    - [X] sometimes static position means wall props spawn inside the floor
    - [X] look vector is zero bug
    - [X] weird sampling bug
    - [X] wall props sometimes not spawning when walls face a specific way (properly related to rem)
    - [X] coroutines not working together with the interface buttons

    features:
    - add prop size spawning check
    - [X] add warning when samples are not generated for nodes
    */

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.matrix = transform.localToWorldMatrix;
    }

    public bool CheckOverlapBox(Vector3 pos, Quaternion rot)
    {
        BoxCollider col = GetComponent<BoxCollider>();
        return Physics.OverlapBox(pos, col.size / 2, rot, LayerMask.GetMask("Prop", "Node")).Count() > 0;
    }

    public bool CheckOverlapBox(Vector3 pos, Quaternion rot, Func<List<Collider>, IEnumerable<Collider>> func)
    {
        BoxCollider col = GetComponent<BoxCollider>();
        return func(new List<Collider>(Physics.OverlapBox(pos, col.size / 2, rot, LayerMask.GetMask("Prop", "Node")))).ToList().Count() > 0;
    }

    public Vector3 GetSize()
    {
        return GetComponent<BoxCollider>().size;
    }

    public void UpdateChildren(PropHierarchy.PropHierachyInfo parentHierarchy)
    {
        foreach (MeshSurface surface in GetComponentsInChildren<MeshSurface>())
            surface.Init(parentHierarchy);

        foreach (MeshPoint point in GetComponentsInChildren<MeshPoint>())
            point.Init(parentHierarchy);
    }
}