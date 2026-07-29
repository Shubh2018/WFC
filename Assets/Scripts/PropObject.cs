using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PropObject : MonoBehaviour
{
    private Prop _prop;
    public Prop Prop { get { return _prop; } set { _prop = value; } }
    public Vector3 GetSize => GetComponent<BoxCollider>().size;
    public Vector3 GetColCenterAbs => transform.position + GetComponent<BoxCollider>().center;

    private Vector3 GetPointBetween(PropObject prop, Vector3 pos)
    {
        BoxCollider col = prop.GetComponent<BoxCollider>();
        float colMaxSize = Mathf.Max(col.size.x, col.size.y, col.size.z) / 2.0f;
        float distance = Vector3.Distance(transform.position + col.center, pos);
        return Vector3.Lerp(transform.position + col.center, pos, colMaxSize / distance);
    }

    public bool OutsideSpacing(Prop prop, Vector3 pos)
    {
        BoxCollider col = GetComponent<BoxCollider>();
        return _prop.SpacingEnabled && Vector3.Distance(GetPointBetween(prop.PropObject, pos), col.center + pos) > _prop.SpacingAmount;
    }

    public bool CheckOverlapBox(Vector3 pos, Quaternion rot, Func<List<Collider>, IEnumerable<Collider>> func)
    {
        BoxCollider col = GetComponent<BoxCollider>();
        return func(new List<Collider>(Physics.OverlapBox(pos, col.size / 2, rot, LayerMask.GetMask("Prop", "Node")))).ToList().Count() > 0;
    }

    // Checks for overlaps in a props 360 degree circumference, in 15 degree intervals
    // Chooses and returns a random valid rotation afterwards
    public Quaternion CheckOverlapBoxCircumference(Vector3 pos, Func<List<Collider>, IEnumerable<Collider>> func)
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

        return validRotation.Count > 0 ? Quaternion.Euler(validRotation[UnityEngine.Random.Range(0, validRotation.Count)]) : Misc.quatEmpty;
    }

    public void UpdateChildren(PropHierarchy.PropHierachyInfo parentHierarchy)
    {
        foreach (MeshSurface surface in GetComponentsInChildren<MeshSurface>())
            surface.Init(parentHierarchy);

        foreach (MeshPoint point in GetComponentsInChildren<MeshPoint>())
            point.Init(parentHierarchy);
    }

    public Quaternion GetRotation(GameObject parent, PropPlacementType propType, PropRotationTypeEnum rotType, float amount)
    {
        if (rotType == PropRotationTypeEnum.Default) return Quaternion.identity;

        bool isTypeWall = propType == PropPlacementType.Wall;
        bool isRotWorld = rotType == PropRotationTypeEnum.World;

        Vector3 angles = isRotWorld ? transform.eulerAngles : transform.localEulerAngles;
        Quaternion rot = Quaternion.Euler(isTypeWall ? new(angles.x, angles.y, amount) : new(angles.x, amount, angles.z));

        return isRotWorld ? rot : parent.transform.rotation * rot;
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