using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

public class MeshSampler : MonoBehaviour
{
    [SerializeField] private MeshFilter _mesh;

    [SerializeField] private int _poolSize;
    [SerializeField] private float _radius;

    private List<Vector3> _samplePool = new List<Vector3>();
    private List<Vector3> _samples = new List<Vector3>();

    List<float> _faceAreas = new List<float>();
    List<float> _faceAreaCumulativeSum = new List<float>();

    private void Start()
    {
        PrecomputeMeshData(_mesh.sharedMesh);
        _samplePool = GenerateInitialSamplePool(_mesh.sharedMesh, _poolSize);
        _samples = SampleMesh(_mesh.sharedMesh, _samplePool);
    }

    private List<Vector3> SampleMesh(Mesh m, List<Vector3> samples)
    {
        List<Vector3> accepted = new List<Vector3>();

        List<Vector3> spatialIndex = accepted;

        foreach (var candidate in samples)
        {
            if(IsValid(candidate, _radius, spatialIndex))
                accepted.Add(transform.TransformPoint(candidate));
        }

        return accepted;
    }

    private bool IsValid(Vector3 candidate, float radius, List<Vector3> spatialIndex)
    {
        float radiusSq = radius * radius;

        for(int i = 0; i < spatialIndex.Count; i++)
        {
            Vector3 s = spatialIndex[i];
            float distSq = (s - candidate).sqrMagnitude;

            if (distSq < radiusSq)
            {
                return false;
            }
        }

        return true;
    }

    private void PrecomputeMeshData(Mesh m)
    {
        int[] triangles = m.triangles;
        Vector3[] vertices = m.vertices;

        int faceCount = triangles.Length / 3;

        for (int i = 0; i < faceCount; i++)
        {
            int i0 = triangles[i * 3 + 0];
            int i1 = triangles[i * 3 + 1]; 
            int i2 = triangles[i * 3 + 2];

            Vector3 p0 = vertices[i0];
            Vector3 p1 = vertices[i1];
            Vector3 p2 = vertices[i2];

            _faceAreas.Add(CalculateArea(p0, p1, p2));
        }

        _faceAreaCumulativeSum = CumulativeAreaSum(_faceAreas);
    }

    private float CalculateArea(Vector3 v1, Vector3 v2, Vector3 v3)
    {   
        float edge1 = Vector3.Distance(v1, v2);
        float edge2 = Vector3.Distance(v2, v3);
        float edge3 = Vector3.Distance(v3, v1);

        float semiPerimeter = (edge1 +  edge2 + edge3) / 2;

        return Mathf.Sqrt(semiPerimeter * (semiPerimeter - edge1) * (semiPerimeter - edge2) + (semiPerimeter - edge3));
    }

    private List<float> CumulativeAreaSum(List<float> faceArea)
    {
        List<float> result = new List<float>();

        result.Add(faceArea[0]);

        for (int i = 1; i < faceArea.Count; i++)
        {
            result.Add(result[i - 1] + faceArea[i]);
        }

        return result;
    }

    private int SampleFaceIndexByArea()
    {
        float totalArea = _faceAreaCumulativeSum[^1];

        float r = Random.Range(0, totalArea);

        for(int i = 0; i < _faceAreaCumulativeSum.Count - 1; i++)
        {
            if (r <= _faceAreaCumulativeSum[i])
                return i;
        }

        return _faceAreaCumulativeSum.Count - 1;
    }

    private List<Vector3> GenerateInitialSamplePool(Mesh m, int n)
    {
        List<Vector3> samplePool = new List<Vector3>();

        int[] triangles = m.triangles;
        Vector3[] vertices = m.vertices;

        for(int i = 0; i < n; i++)
        {
            int faceIndex = SampleFaceIndexByArea();

            int i0 = triangles[faceIndex];
            int i1 = triangles[faceIndex + 1];
            int i2 = triangles[faceIndex + 2];

            Vector3 p0 = vertices[i0];
            Vector3 p1 = vertices[i1];
            Vector3 p2 = vertices[i2];

            Vector3 candidate = SamplePointInTriangle(p0, p1, p2);

            samplePool.Add(candidate);
        }

        return samplePool;
    }

    private Vector3 SamplePointInTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        float u = Random.Range(0, 1);
        float v = Random.Range(0, 1);

        if (u + v > 1)
        {
            u = 1 - u;
            v = 1 - v;
        }

        return v1 + u * (v2 - v1) + v * (v3 - v1);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;

        foreach (var sample in _samples)
            Gizmos.DrawSphere(sample, 0.005f);
    }

    //private bool IsValid(Vector3 p)
    //{
    //    return false;
    //}
}
