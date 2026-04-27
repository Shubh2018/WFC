using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PropObject : MonoBehaviour
{
    [SerializeField] private Vector3 _center;
    [SerializeField] private float _length = 1f;

    private float _step = 30.0f;

    void Start()
    {
        CheckOverlaps();    
    }
    
    public void CheckOverlaps()
    {
        float currentAngle = 0;

        while (currentAngle < 360.0f)
        {
            Debug.Log($"Inside CheckOverlap {this.transform.name}");
            
            Ray ray = new Ray(transform.position + _center, transform.forward);
            ray.direction = Quaternion.AngleAxis(_step, Vector3.up) * transform.forward;
            Debug.Log($"Has Hit: {Physics.Raycast(ray, out RaycastHit hit, _length, LayerMask.NameToLayer("Node"))}");
            
            // if ()
            // {
            //     Vector3 moveDirection = (ray.origin - hit.point).normalized;
            //     
            //     transform.position += moveDirection * 2;
            //
            //     Debug.Log($"Move {transform.name} in {moveDirection} direction");
            // }
            
            currentAngle += _step;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + _center, transform.forward * _length);
    }
}