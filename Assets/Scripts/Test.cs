using System;
using UnityEngine;
using System.Collections.Generic;

public class Test : MonoBehaviour
{
    private List<Vector2> _samples = new List<Vector2>();
    
    [SerializeField] private Vector2 _dimensions = new Vector2(10, 10);

    [SerializeField] private float _distance = 1.0f;
    [SerializeField] private int _attempts = 30;

    [SerializeField] private float _sphereSize = 1.0f;
    
    private void OnValidate()
    {
        _samples = PoissonDiskSampling.GeneratePoints(_dimensions.x, _dimensions.y, _distance, _attempts);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        
        Gizmos.DrawWireCube(transform.position, _dimensions);


        foreach (var point in _samples)
        {
            Gizmos.DrawSphere(point, _sphereSize);
        }
    }
}