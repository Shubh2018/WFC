using UnityEngine;
using System.Collections.Generic;

public class Test : MonoBehaviour
{
    private List<Vector3> _samples = new List<Vector3>();
    
    [SerializeField] private Vector2 _dimensions = new Vector2(10, 10);

    [SerializeField] private float _distance = 1.0f;
    [SerializeField] private int _attempts = 30;

    [SerializeField] private float _sphereSize = 1.0f;

    [SerializeField] private int _propCount = 5;
    [SerializeField] private List<GameObject> _propList;
    
    private void OnValidate()
    {
        if(_distance < 0)
            _distance = 0.1f;

        if(_attempts < 0)
            _attempts = 1;

        if(_sphereSize < 0)
            _sphereSize = 0.1f;

        if(_dimensions.x < 0 || _dimensions.y < 0)
        {
            _dimensions.x = 0.1f;
            _dimensions.y = 0.1f;
        }

        _samples = PoissonDiskSampling.GeneratePoints(_dimensions.x, _dimensions.y, _distance, _attempts);

        for(int i = 0; i < _propCount; i++)
        {
            Vector3 randomPos = _samples[Random.Range(0, _samples.Count - 1)];
            Instantiate(_propList[Random.Range(0, _propList.Count - 1)], randomPos, Quaternion.identity);

            _samples.Remove(randomPos);
            
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        
        Gizmos.DrawWireCube(new Vector3(_dimensions.x / 2, 0.0f, _dimensions.y / 2), new Vector3(_dimensions.x, 0.0f, _dimensions.y));

        foreach (var point in _samples)
        {
            Gizmos.DrawSphere(point, _sphereSize);
        }
    }
}