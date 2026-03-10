using UnityEngine;
using System.Collections.Generic;

public class MeshSampler : MonoBehaviour
{
    private Mesh _mesh;

    private List<Triangle> _triangles = new List<Triangle>();
    private List<Vector3> _pool = new List<Vector3>();
    
    private List<Vector3> _samples = new List<Vector3>();

    private int _pointsCount = 30;
    [SerializeField] private float _radius = 0.5f;
    [SerializeField] private int _tries = 20;
    
    private int maxLoops = 1000;

    void Awake()
    {
        _mesh = GetComponent<MeshFilter>().sharedMesh;
    }

    void Start()
    {
        ComputeMeshData();
        // SamplePoints();
        
        _samples = Sample(_radius);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;

        foreach(var sample in _samples)
        {
            Gizmos.DrawSphere(sample, 0.005f);
        }
    }

    private void ComputeMeshData()
    {
        int[] triangles = _mesh.triangles;
        Vector3[] vertices = _mesh.vertices;

        int triCount = triangles.Length / 3;

        for(int i = 0; i < triCount; i++)
        {
            int i0 = triangles[i * 3 + 0];
            int i1 = triangles[i * 3 + 1];
            int i2 = triangles[i * 3 + 2];

            Triangle triangle = new Triangle
            {
                i0 = i0,
                i1 = i1,
                i2 = i2,

                v0 = vertices[i0],
                v1 = vertices[i1],
                v2 = vertices[i2],
                
                samples = new List<Vector3>(),
            };

            _triangles.Add(triangle);
        }
    }

    private void SamplePoints()
    {
        foreach(var triangle in _triangles)
        {
            for(int i = 0; i < _pointsCount; i++)
            {
                Vector3 barycentricCoordinate = transform.TransformPoint(GenerateBarycentricCoordinate(triangle));
                
                if(!triangle.samples.Contains(barycentricCoordinate))
                    triangle.samples.Add(barycentricCoordinate);   
            }
        }
    }

    private List<Vector3> Sample(float radius)
    {
        List<Vector3> samples = new List<Vector3>();
        
        foreach (var triangle in _triangles)
        {
            // Triangle triangle = _triangles[Random.Range(0, _triangles.Count)];
        
            // List<Vector3> pool = triangle.samples;
            List<int> active = new List<int>();
             
            Vector3 point = GenerateBarycentricCoordinate(triangle);
            
            samples.Add(point);
            active.Add(0);

            int currentLoop = 0;
            
            while (active.Count > 0 && currentLoop < maxLoops)
            {
                currentLoop++;
                
                int randomIndex = Random.Range(0, active.Count);
                int index = active[randomIndex];
                
                Vector3 center = samples[index];
                
                bool found = false;

                for (int i = 0; i < _tries; i++)
                {
                    Vector3 candidate = GenerateBarycentricCoordinate(triangle);

                    //if (candidate == Vector3.zero) continue;

                    if (IsValid(candidate, center, radius))
                    {
                        samples.Add(candidate);
                        
                        int newIndex = samples.Count - 1;
                        active.Add(newIndex);

                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    (active[randomIndex], active[^1]) = (active[^1], active[randomIndex]);
                
                    active.RemoveAt(active.Count - 1);
                }
            }
        }
        
        return samples;
    }
    
    private bool IsValid(Vector3 candidate, Vector3 centre, float r)
    {
        if (Vector3.Distance(candidate, centre) <= r)
            return false;

        return true;
    }
    
    private Vector3 FindRandomPointsAround(Triangle triangle, Vector3 center, float r)
    {
        float radius = Random.Range(r, 2 * r);
        float azimuthalAngle = Random.Range(0, 2 * Mathf.PI);
        float polarAngle = Random.Range(0, Mathf.PI);

        foreach (var sample in triangle.samples)
        {
            if (Vector3.Distance(center, sample) <= radius)
                return sample;
        }
        
        return Vector3.zero;
    }

    private Vector3 GenerateBarycentricCoordinate(Triangle triangle)
    {
        float u = Random.value;
        float v = Random.value;

        if (u + v > 1)
        {
            u = 1 - u;
            v = 1 - v;
        }

        Vector3 barycentricCoordinate = triangle.v0 + u * (triangle.v1 - triangle.v0) + v * (triangle.v2 - triangle.v0);

        return transform.TransformPoint(barycentricCoordinate);
    }
}

[System.Serializable]
struct Triangle
{
    public Vector3 v0, v1, v2;
    public int i0, i1, i2;

    public List<Vector3> samples;
}
