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
    
    public int IsOverlappingProp()
    {
        int overlapCount = 0;
        
        if (!_lamp)
        {
           Collider[] colliders = Physics.OverlapBox(this.transform.position + _center, _overlapDimensions, this.transform.rotation, _propLayer);
   
           if (colliders.Length <= 0) return 0;

           overlapCount += 2;
           
           Debug.Log($"Destroyed {this.gameObject.name} ({this.transform.position})");
           DestroyImmediate(this.gameObject); 
        }

        else
        {
            Collider[] colliders = Physics.OverlapSphere(this.transform.position + _center, _radius, _propLayer);
            
            if(colliders.Length <= 0) return 0;

            overlapCount += 1;
            
            Debug.Log($"Destroyed {this.gameObject.name} ({this.transform.position})");
            DestroyImmediate(this.gameObject); 
        }
        
        return overlapCount;
    }

    public bool IsOverlappingPropSphere(Vector3 pos, List<Collider> exceptions = null)
    {
        List<Collider> hitColliders = new List<Collider>(Physics.OverlapBox(pos + _center, _overlapDimensions, this.transform.rotation, _propLayer & _nodeLayer)).Except(exceptions).ToList();

        return hitColliders.Count > 0;
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
        float rotation = this.transform.localEulerAngles.y;
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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.matrix = transform.localToWorldMatrix;
        
        if(_lamp)
            Gizmos.DrawWireSphere(_center, _radius);
        
        Gizmos.DrawWireCube(_center, _overlapDimensions);
        Gizmos.DrawRay(_rayCenter, this.transform.forward * _raycastLength);
    }
}