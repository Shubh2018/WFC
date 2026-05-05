using System;
using System.Runtime.CompilerServices;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PropObject : MonoBehaviour
{
    [SerializeField] private Vector3 _center;
    [SerializeField] private Vector3 _overlapDimensions;
    [SerializeField] private LayerMask _propLayer;
    [SerializeField] private LayerMask _nodeLayer;
    
    public void IsOverlappingProp()
    {
        Collider[] colliders = Physics.OverlapBox(this.transform.position + _center, _overlapDimensions / 2, Quaternion.identity, _propLayer);

        if (colliders.Length <= 0) return;

        Debug.Log($"Destroyed {this.gameObject.name}");
        DestroyImmediate(this.gameObject);
    }

    public bool IsOverlappingNode()
    {
        Collider[] colliders = Physics.OverlapBox(this.transform.position + _center, _overlapDimensions / 2, Quaternion.identity, _nodeLayer);

        return colliders.Length > 0;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireCube(this.transform.position + _center, _overlapDimensions);
    }
}