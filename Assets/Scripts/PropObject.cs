using System;
using UnityEngine;

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
    
    public int IsOverlappingProp()
    {
        int overlapCount = 0;
        
        if (!_lamp)
        {
           Collider[] colliders = Physics.OverlapBox(this.transform.position + _center, _overlapDimensions / 2, this.transform.rotation, _propLayer);
   
           if (colliders.Length <= 0) return 0;

           overlapCount += 2;
           
           Debug.Log($"Destroyed {this.gameObject.name}");
           DestroyImmediate(this.gameObject); 
        }

        else
        {
            Collider[] colliders = Physics.OverlapSphere(this.transform.position + _center, _radius, _propLayer);
            
            if(colliders.Length <= 0) return 0;

            overlapCount += 1;
            
            Debug.Log($"Destroyed {this.gameObject.name}");
            DestroyImmediate(this.gameObject); 
        }
        
        return overlapCount;
    }

    public void UpdateRotation()
    {
        float rotation = this.transform.localEulerAngles.y;

        while (rotation <= 360)
        {
            if (Physics.Raycast(this.transform.position + _rayCenter, this.transform.forward, out RaycastHit hit,
                    _raycastLength, _nodeLayer))
            {
                rotation += UnityEngine.Random.Range(-30.0f, 30.0f);
                this.transform.localEulerAngles = new Vector3(this.transform.localEulerAngles.x, rotation, this.transform.localEulerAngles.z);
            }

            else
                break;
        }
    }

    public void IsOverlappingNode()
    {
        float rotation = this.transform.localEulerAngles.y;
        float step = 20.0f;

        while (rotation <= 360)
        {
            rotation += step;
            
            if (Physics.Raycast(this.transform.position + _rayCenter, this.transform.forward, out RaycastHit hit,
                    _raycastLength, _nodeLayer))
            {
                Vector3 dir = (hit.point - (this.transform.position + _rayCenter)).normalized;
                this.transform.position -= dir * _raycastLength;
                Debug.Log($"Moved {this.gameObject.name}");
                
                this.transform.localEulerAngles = new Vector3(this.transform.localEulerAngles.x, rotation, this.transform.localEulerAngles.z);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        
        if(_lamp)
            Gizmos.DrawWireSphere(this.transform.position + _center, _radius);
        
        Gizmos.DrawWireCube(this.transform.position + _center, _overlapDimensions);
        Gizmos.DrawRay(this.transform.position + _rayCenter, this.transform.forward * _raycastLength);
    }
}