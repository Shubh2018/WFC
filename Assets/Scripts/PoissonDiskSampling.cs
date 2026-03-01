using System;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class PoissonDiskSampling : MonoBehaviour
{
    private List<Vector3> _samples = new List<Vector3>();
    private float _sphereSize = 0.25f;
    
    private List<GameObject> _props = new List<GameObject>();
    
    [SerializeField] private Vector3 _dimensions = new Vector3(10, 10, 10);

    [SerializeField] private float _distance = 1.0f;
    [SerializeField] private int _attempts = 30;

    [SerializeField] private bool _showPoints = false;

    [SerializeField] private int _propCount = 5;
    [SerializeField] private List<GameObject> _propList;

    private void Start()
    {
        Debug.Log(_dimensions);
    }

    public void GenerateSamplingPoints()
    {
        _samples.Clear();
        
        _samples = GeneratePoints(_dimensions, _distance, _attempts);

        if (_props.Count > 0)
        {
            foreach (var prop in _props)
            {
                DestroyImmediate(prop);
            }
            
            _props.Clear();
        }

        for (int i = 0; i < _propCount; i++)
        {
            int randomProp = Random.Range(0, _propList.Count - 1);
            int randomSample = Random.Range(0, _samples.Count - 1);

            GameObject prop = Instantiate(_propList[randomProp], _samples[randomSample], Quaternion.identity);
            prop.transform.SetParent(this.transform);
            _props.Add(prop);
            
            _samples.RemoveAt(randomSample);
        }
    }

    public void ClearSamples()
    {
        _samples.Clear();
        
        foreach(var prop in _props)
            DestroyImmediate(prop);
        
        _props.Clear();
    }
    
    private void OnDrawGizmos()
    {
        if (!_showPoints) return;
        
        Gizmos.color = Color.white;

        foreach (var sample in _samples)
            Gizmos.DrawSphere(sample, _sphereSize);
    }
    
    public List<Vector3> GeneratePoints(Vector3 dimensions, float r, int k)
    {
        float cellSize = r / Mathf.Sqrt(3);

        int gridLength = Mathf.CeilToInt(dimensions.x / cellSize);
        int gridWidth = Mathf.CeilToInt(dimensions.y / cellSize);
        int gridHeight = Mathf.CeilToInt(dimensions.z / cellSize);

        int[,,] grid = new int[gridLength, gridWidth, gridHeight];

        // for (int i = 0; i < gridWidth; i++)
        // {
        //     for(int j = 0; j < gridHeight; j++)
        //         grid[i,j] = -1;
        // }

        List<Vector3> samples = new List<Vector3>();
        List<int> active = new List<int>();
        
        Vector3 point = Vector3.zero;
        
        samples.Add(point);
        active.Add(0);
        
        int gx = Mathf.FloorToInt(point.x / cellSize);
        int gy = Mathf.FloorToInt(point.y / cellSize);
        int gz = Mathf.FloorToInt(point.z / cellSize);
        grid[gx, gy, gz] = 0;

        while (active.Count > 0)
        {
            int randomID = Random.Range(0, active.Count);
            int sampleIndex = active[randomID];
            
            Vector3 center = samples[sampleIndex];

            bool found = false;

            for (int i = 0; i < k; i++)
            {
                Vector3 candidate = GenerateRandomPointsAround(center, r);

                if (candidate.x < 0 || candidate.x >= dimensions.x || candidate.z < 0 || candidate.z >= dimensions.z || candidate.y < 0 || candidate.y >= dimensions.y)
                    continue;

                if (IsValid(candidate, r, cellSize, grid, samples, gridLength, gridWidth, gridHeight))
                {
                    samples.Add(candidate - (Vector3.right * (dimensions.x / 2)));
                    int newIndex = samples.Count - 1;
                    
                    active.Add(newIndex);
                    
                    int candidateGx = Mathf.FloorToInt(candidate.x / cellSize);
                    int candidateGy = Mathf.FloorToInt(candidate.y / cellSize);
                    int candidateGz = Mathf.FloorToInt(candidate.z / cellSize);
                    
                    grid[candidateGx, candidateGy, candidateGz] = newIndex;

                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                (active[randomID], active[^1]) = (active[^1], active[randomID]);

                active.RemoveAt(active.Count - 1);
            }
        }

        return samples;
    }

    private Vector3 GenerateRandomPointsAround(Vector3 center, float r)
    {
        float radius = Random.Range(r, 2 * r);
        float azimuthalAngle = Random.Range(0, 2 * Mathf.PI);
        float polarAngle = Random.Range(0, Mathf.PI);
        
        float x = center.x + radius * Mathf.Sin(polarAngle) * Mathf.Cos(azimuthalAngle);
        float y = center.y + radius * Mathf.Sin(polarAngle) * Mathf.Sin(azimuthalAngle);
        float z = center.z + radius * Mathf.Cos(polarAngle);
        
        return new Vector3(x, y, z);
    }

    private bool IsValid(Vector3 candidate, float r, float cellSize, int[,,] grid, List<Vector3> samples, float gridLength, float gridWidth,
        float gridHeight)
    {
        int gx = Mathf.FloorToInt(candidate.x / cellSize);
        int gy = Mathf.FloorToInt(candidate.y / cellSize);
        int gz = Mathf.FloorToInt(candidate.z / cellSize);

        for (int x = 0; x <= gridLength - 1; x++)
        {
            for (int y = 0; y <= gridHeight - 1; y++)
            {
                for (int z = 0; z <= gridWidth - 1; z++)
                {
                    int sampleIndex = grid[x, y, z];
                    if (sampleIndex != -1)
                    {
                        Vector3 point = samples[sampleIndex];

                        if (Vector3.Distance(candidate, point) <= r)
                            return false;
                    }
                }
            }
        }

        return true;
    }
}
