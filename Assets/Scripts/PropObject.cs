using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PropObject : MonoBehaviour
{
    /*
    todo:
    - [X] Add doors between room / structure types
    - [X] Fix junctions not spawning samples
    - Add a room / structure layout planner (also make environments and prop type work again)
    - Add a player path where props cannot spawn
    - Add prop relations with rotation
    - Do cleanup to make sure every feature works
    */

    public Vector3 GetSize => GetComponent<BoxCollider>().size;

    public bool CheckOverlapBox(Vector3 pos, Quaternion rot, Func<List<Collider>, IEnumerable<Collider>> func)
    {
        BoxCollider col = GetComponent<BoxCollider>();
        return func(new List<Collider>(Physics.OverlapBox(pos, col.size / 2, rot, LayerMask.GetMask("Prop", "Node")))).ToList().Count() > 0;
    }

    // Checks for overlaps in a props 360 degree circumference, in 15 degree intervals
    // Chooses and returns a random valid rotation afterwards
    public Vector3 CheckOverlapBoxCircumference(Vector3 pos, Func<List<Collider>, IEnumerable<Collider>> func)
    {
        BoxCollider col = GetComponent<BoxCollider>();
        List<Vector3> validRotation = new();
        Vector3 rotation = Vector3.zero;

        for (int i = 0; i <= 24; i++)
        {
            Vector3 pos2 = pos + Quaternion.Euler(rotation) * col.center;
            if (!CheckOverlapBox(pos2, Quaternion.Euler(rotation), func))
                validRotation.Add(rotation);
            rotation += new Vector3(0.0f, 15.0f, 0.0f);
        }

        return validRotation.Count > 0 ? validRotation[UnityEngine.Random.Range(0, validRotation.Count)] : Vector3.one;
    }

    public void UpdateChildren(PropHierarchy.PropHierachyInfo parentHierarchy)
    {
        foreach (MeshSurface surface in GetComponentsInChildren<MeshSurface>())
            surface.Init(parentHierarchy);

        foreach (MeshPoint point in GetComponentsInChildren<MeshPoint>())
            point.Init(parentHierarchy);
    }

    public void RotateTo(PropPlacementType propType, PropRotationTypeEnum rotType, float amount)
    {
        if (rotType == PropRotationTypeEnum.Default) return;

        bool isTypeWall = propType == PropPlacementType.Wall;
        bool isRotWorld = rotType == PropRotationTypeEnum.World;

        Vector3 angles = isRotWorld ? transform.eulerAngles : transform.localEulerAngles;
        Quaternion rot = Quaternion.Euler(isTypeWall ? new(angles.x, angles.y, amount) : new(angles.x, amount, angles.z));

        if (isRotWorld)
            transform.rotation = rot;
        else
            transform.localRotation = rot;
    }
}