using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PropObject : MonoBehaviour
{
    //[SerializeField] private Vector3 _center;
    [SerializeField] private Vector3 _rayCenter;
    //[SerializeField] private Vector3 _overlapDimensions;
    [SerializeField] private float _raycastLength = 1.5f;

    //public Vector3 OverlapDimenstions => _overlapDimensions;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.matrix = transform.localToWorldMatrix;
        
        //Gizmos.DrawWireCube(_center, _overlapDimensions);
        //Gizmos.DrawWireSphere(_center, _raycastLength);
    }

    public bool CheckOverlapBox(Vector3 pos, Quaternion rot)
    {
        BoxCollider col = GetComponent<BoxCollider>();
        Debug.Log($"prop: {gameObject.name}, pos: {pos + col.center}, size: {col.size}, overlap: {Physics.OverlapBox(pos + col.center, col.size / 2, rot, LayerMask.NameToLayer("Prop")).Count() > 0}");
        return Physics.OverlapBox(pos + col.center, col.size / 2, rot, LayerMask.NameToLayer("Prop")).Count() > 0;
    }

    public bool CheckOverlapBox(Vector3 pos, Quaternion rot, Func<List<Collider>, IEnumerable<Collider>> func)
    {
        BoxCollider col = GetComponent<BoxCollider>();
        Debug.Log($"prop: {gameObject.name}, pos: {pos + col.center}, size: {col.size}, overlap: {func(new List<Collider>(Physics.OverlapBox(pos + col.center, col.size / 2, rot, LayerMask.NameToLayer("Prop")))).ToList().Count > 0}");
        return func(new List<Collider>(Physics.OverlapBox(pos + col.center, col.size / 2, rot, LayerMask.NameToLayer("Prop")))).ToList().Count > 0;
    }

    public Vector3 GetSize()
    {
        return GetComponent<BoxCollider>().size;
    }

    public void UpdateRotation()
    {
        float rotation = this.transform.localEulerAngles.y;

        while (rotation <= 360)
        {
            if (Physics.Raycast(this.transform.position + _rayCenter, this.transform.forward, out RaycastHit hit, _raycastLength, LayerMask.NameToLayer("Node")))
            {
                rotation += 90.0f;
                this.transform.localEulerAngles = new Vector3(this.transform.localEulerAngles.x, rotation, this.transform.localEulerAngles.z);
            }

            else break;
        }
    }

    public void UpdateChildren(Guid parentId, int maxHierarchyLevel, int currentLevel)
    {
        foreach (MeshSurface surface in GetComponentsInChildren<MeshSurface>())
            surface.Init(parentId, maxHierarchyLevel, currentLevel);

        foreach (MeshPoint point in GetComponentsInChildren<MeshPoint>())
            point.Init(parentId, maxHierarchyLevel, currentLevel);
    }

    public void IsOverlappingNode()
    {
        float rotation = transform.localEulerAngles.y;
        float step = 20.0f;

        while (rotation <= 360)
        {
            rotation += step;
            
            if (Physics.Raycast(this.transform.position + _rayCenter, this.transform.forward, out RaycastHit hit, _raycastLength, LayerMask.NameToLayer("Node")))
            {
                Vector3 dir = (hit.point - (this.transform.position + _rayCenter)).normalized;
                Vector3 oldPos = this.transform.position;
                this.transform.position -= dir * _raycastLength;
                Debug.Log($"Moved {this.gameObject.name} (before: {oldPos}, after: {this.transform.position})");
                
                this.transform.localEulerAngles = new Vector3(this.transform.localEulerAngles.x, rotation, this.transform.localEulerAngles.z);
            }
        }
    }
}