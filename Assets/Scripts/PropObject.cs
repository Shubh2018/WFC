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
        if (!_enableScript) return;
        
        float currentAngle = 0;

        while (currentAngle < 360.0f)
        {
            Ray ray = new Ray(transform.position + _center, transform.forward)
            {
                direction = Quaternion.AngleAxis(_step, Vector3.up) * transform.forward
            };
            
            //Debug.Log($"Hit: {Physics.Raycast(ray, out RaycastHit hit, _length, LayerMask.GetMask("Node"))}");
            
            RaycastHit[] hits = Physics.RaycastAll(ray, _length, LayerMask.GetMask("Node"));
            Debug.Log($"HitCount: {hits.Length}");

            if (hits.Length > 0)
            {
                Debug.Log($"Hit: {transform.name} {hits[0].transform.name}");
                Vector3 moveDirection = (ray.origin - hits[0].point).normalized;
                
                transform.position += moveDirection * 1.5f;
                
                Debug.Log($"Move {transform.name} in {moveDirection} direction");
            }
            
            else
                Debug.Log($"No Hits!");
            
            currentAngle += _step;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + _center, transform.forward * _length);
    }
}