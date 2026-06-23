using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PropObject : MonoBehaviour
{
    [SerializeField] private Vector3 _center;
    [SerializeField] private Vector3 _rayCenter;
    [SerializeField] private Vector3 _overlapDimensions;
    [SerializeField] private LayerMask _propLayer;
    [SerializeField] private LayerMask _nodeLayer;

    [SerializeField] private float _raycastLength = 1.5f;

    [SerializeField] private bool _lamp = false;
    [SerializeField] private float _radius = 0.1f;

    public Vector3 OverlapDimenstions => _overlapDimensions;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.matrix = transform.localToWorldMatrix;
        
        Gizmos.DrawWireCube(_center, _overlapDimensions);
        Gizmos.DrawWireSphere(_center, _radius);
    }

    public bool CheckOverlapBox(Vector3 pos, Quaternion rot)
    {
        return Physics.OverlapBox(pos + _center, _overlapDimensions / 2, rot, _propLayer).Count() > 0;
    }

    public bool CheckOverlapBox(Vector3 pos, Quaternion rot, Func<List<Collider>, IEnumerable<Collider>> func)
    {
        return func(new List<Collider>(Physics.OverlapBox(pos + _center, _overlapDimensions / 2, rot, _propLayer))).ToList().Count > 0;
    }

    public void UpdateRotation()
    {
        float rotation = this.transform.localEulerAngles.y;

        while (rotation <= 360)
        {
            if (Physics.Raycast(this.transform.position + _rayCenter, this.transform.forward, out RaycastHit hit, _raycastLength, _nodeLayer))
            {
                rotation += UnityEngine.Random.Range(-30.0f, 30.0f);
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
            
            if (Physics.Raycast(this.transform.position + _rayCenter, this.transform.forward, out RaycastHit hit, _raycastLength, _nodeLayer))
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