using UnityEngine;
using System.Collections.Generic;

public class MeshSampler : MonoBehaviour
{
    private Mesh _mesh;

    private List<Triangle> _triangles = new List<Triangle>();
    private List<Vector3> _samples = new List<Vector3>();

    private int _tries = 30;


    void Awake()
    {
        _mesh = GetComponent<MeshFilter>().sharedMesh;
    }

    void Start()
    {
        ComputeMeshData();
        _samples = SamplePoints();
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
            };

            _triangles.Add(triangle);
        }
    }

    private List<Vector3> SamplePoints()
    {
        List<Vector3> samples = new List<Vector3>();

        foreach(var triangle in _triangles)
        {
            for(int i = 0; i < _tries; i++)
            {
                Vector3 barycentricCoordinate = transform.TransformPoint(GetBarycentricCoordinate(triangle));

                if(!samples.Contains(barycentricCoordinate))
                    samples.Add(barycentricCoordinate);   
            }
        }

        return samples;
    }

    private Vector3 GetBarycentricCoordinate(Triangle triangle)
    {
        float u = Random.value;
        float v = Random.value;

        if (u + v > 1)
        {
            u = 1 - u;
            v = 1 - v;
        }

        Vector3 barycentricCoordinate = triangle.v0 + u * (triangle.v1 - triangle.v0) + v * (triangle.v2 - triangle.v0);

        return barycentricCoordinate;
    }
}

struct Triangle
{
    public Vector3 v0, v1, v2;
    public int i0, i1, i2;
}
