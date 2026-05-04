using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PropObject : MonoBehaviour
{
    [SerializeField] private Vector3 _center;
    [SerializeField] private float _length = 1f;
    [SerializeField] private LayerMask _layerToCheck;

    [SerializeField] private bool _enableScript = false;
    
    private float _step = 10.0f;
    
    public void CheckOverlaps()
    {
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + _center, transform.forward * _length);
    }
}