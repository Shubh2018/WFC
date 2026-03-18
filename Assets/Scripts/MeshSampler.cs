using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public enum SurfaceType
{
    Floor, Wall, Ceiling
};

public class MeshSampler : MonoBehaviour
{
    private MeshFilter[] _meshFilter;

    [SerializeField] private float _radius;
    [SerializeField] private int _tries = 30;

    private Dictionary<Mesh, int[]> _triangles;
    private Dictionary<Mesh, Vector3[]> _vertices;

    private List<Vector3> _samples = new List<Vector3>();

    private int safety = 10000;
    private Material[] _meshMaterials;

    private MeshCollider _collider;
    
    private List<Sample> _samplePoints = new List<Sample>();
    private List<Sample> _pointsInside = new List<Sample>();

    public void Generate()
    {
        if(_samplePoints.Count > 0)
            Clear();
            
        _meshFilter = GetComponentsInChildren<MeshFilter>();
        _meshMaterials = new Material[_meshFilter.Length];
        
        CombineInstance[] instances = new CombineInstance[_meshFilter.Length];

        for (int i = 0; i < _meshFilter.Length; i++)
        {
            MeshRenderer renderer = _meshFilter[i].GetComponent<MeshRenderer>();
            
            instances[i] = new CombineInstance()
            {
                mesh = _meshFilter[i].sharedMesh,
                transform = _meshFilter[i].transform.localToWorldMatrix,
            };
            
            _meshFilter[i].gameObject.SetActive(false);
            _meshMaterials[i] = renderer.sharedMaterial;
        }
        
        Mesh combinedMesh = new Mesh
        {
            name = this.gameObject.name
        };
        combinedMesh.CombineMeshes(instances);

        this.gameObject.AddComponent<MeshFilter>().sharedMesh = combinedMesh;
        this.gameObject.AddComponent<MeshRenderer>().sharedMaterial = _meshMaterials[0];
        _collider = this.gameObject.AddComponent<MeshCollider>();
        
        this.gameObject.layer = LayerMask.NameToLayer("Level");
        
        _samples.Clear();
        _samples = SampleMesh(combinedMesh.vertices, combinedMesh.triangles, _radius, _tries);

        foreach(var s in _samplePoints)
        {
            Vector3 p = s.sample;
            Vector3 n = s.triangleNormal;

            float epsilon = 1f;
            Vector3 pInterior = p - n * epsilon;

            if (IsInside(pInterior, _collider, 10000))
            {
                _pointsInside.Add(s);
            }
        }
    }

    public void Clear()
    {
        this.gameObject.layer = LayerMask.NameToLayer("Default");
        
        if (gameObject.TryGetComponent<MeshFilter>(out var meshFilter))
            DestroyImmediate(meshFilter);
        if(_collider)
            DestroyImmediate(_collider);
        if(gameObject.TryGetComponent<MeshRenderer>(out var renderer))
            DestroyImmediate(renderer);
        
        foreach(var filter in _meshFilter)
            filter.gameObject.SetActive(true);
        
        _samplePoints.Clear();
        _pointsInside.Clear();
    }

    private void OnDrawGizmos()
    {
        this.gameObject.TryGetComponent<MeshCollider>(out var meshCollider);

        if (meshCollider)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(meshCollider.bounds.center, meshCollider.bounds.size); 
        }
        
        Gizmos.color = Color.white;
        
        foreach (var samplePoint in _pointsInside)
            Gizmos.DrawSphere(samplePoint.sample, 0.1f);
    }

    private List<Vector3> SampleMesh(Vector3[] vertices, int[] triangles, float radius, int tries)
    {
        List<Vector3> samples = new List<Vector3>();
        List<int> active = new List<int>();

        (float[] area, float[] cdf, float totalArea) = BuildTriangleAreaCDF(vertices, triangles);
        (Vector3 min, Vector3 max) = BuildBoundingBox(vertices);

        (Vector3[,,] grid, float cellSize, int gx, int gy, int gz) = InitializeGrid(min, max, radius);

        int triangleIndex = SampleTriangleIndexFromCDF(cdf);

        int i0 = triangles[triangleIndex * 3 + 0];
        int i1 = triangles[triangleIndex * 3 + 1];
        int i2 = triangles[triangleIndex * 3 + 2];

        Vector3 p = SamplePointInTriangle(vertices[i0], vertices[i1], vertices[i2]);
        InsertSampleToGrid(p, grid, min, cellSize);

        samples.Add(p);
        active.Add(0);

        Sample sample = new Sample()
        {
            sample = p,
            triangleNormal = Vector3.Cross(vertices[i2] - vertices[i0], vertices[i2] - vertices[i0]).normalized,
        };
        
        _samplePoints.Add(sample);

        int tryCount = 0;

        while (active.Count > 0 && tryCount < safety)
        {
            tryCount += 1;

            int activeIndex = Random.Range(0, active.Count);
            int index = active[activeIndex];

            Vector3 s = samples[index];

            bool accepted = false;

            for (int i = 0; i < _tries; i++)
            {
                int triIndex = SampleTriangleIndexFromCDF(cdf);

                i0 = triangles[triIndex * 3 + 0];
                i1 = triangles[triIndex * 3 + 1];
                i2 = triangles[triIndex * 3 + 2];

                Vector3 candidate = SamplePointInTriangle(vertices[i0], vertices[i1], vertices[i2]);

                if (IsValid(candidate, radius, grid, min, cellSize, gx, gy, gz))
                {
                    InsertSampleToGrid(candidate, grid, min, cellSize);

                    samples.Add(candidate);
                    int newIndex = samples.Count - 1;

                    active.Add(newIndex);
                    accepted = true;
                    
                    sample = new Sample()
                    {
                        sample = candidate,
                        triangleNormal = Vector3.Cross(vertices[i2] - vertices[i0], vertices[i2] - vertices[i0]).normalized,
                    };
                    
                    _samplePoints.Add(sample);
                }
            }

            if (!accepted)
            {
                (active[activeIndex], active[^1]) = (active[^1], active[activeIndex]);
                active.Remove(active.Count - 1);
            }
        }

        return samples;
    }

    private (float[], float[], float) BuildTriangleAreaCDF(Vector3[] vertices, int[] triangles)
    {
        int count = triangles.Length / 3;

        float[] area = new float[count];
        float[] cdf = new float[count];

        float totalArea = 0;

        for (int i = 0; i < count; i++)
        {
            int i0 = triangles[i * 3 + 0];
            int i1 = triangles[i * 3 + 1];
            int i2 = triangles[i * 3 + 2];

            Vector3 v0 = vertices[i0];
            Vector3 v1 = vertices[i1];
            Vector3 v2 = vertices[i2];

            area[i] = Vector3.Cross((v1 - v0), (v2 - v0)).magnitude * 0.5f;

            totalArea += area[i];
            cdf[i] = totalArea;
        }

        for (int i = 0; i < count; i++)
            cdf[i] = cdf[i] / totalArea;

        return (area, cdf, totalArea);
    }

    private int SampleTriangleIndexFromCDF(float[] cdf)
    {
        float rand = Random.value;

        int low = 0;
        int high = cdf.Length - 1;

        while (low < high)
        {
            int mid = (low + high) / 2;

            if (cdf[mid] >= rand)
                high = mid;
            else
                low = mid + 1;
        }

        return low;
    }

    private Vector3 SamplePointInTriangle(Vector3 v0, Vector3 v1, Vector3 v2)
    {
        float u = Random.value;
        float v = Random.value;

        if (u + v > 1)
        {
            u = 1 - u;
            v = 1 - v;
        }

        Vector3 p = v0 + u * (v1 - v0) + v * (v2 - v0);
        return p;
    }

    private (Vector3, Vector3) BuildBoundingBox(Vector3[] vertices)
    {
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;

        foreach (var v in vertices)
        {
            min.x = Mathf.Min(min.x, v.x);
            min.y = Mathf.Min(min.y, v.y);
            min.z = Mathf.Min(min.z, v.z);

            max.x = Mathf.Max(max.x, v.x);
            max.y = Mathf.Max(max.y, v.y);
            max.z = Mathf.Max(max.z, v.z);
        }

        return (min, max);
    }

    private (Vector3[,,], float, int, int, int) InitializeGrid(Vector3 min, Vector3 max, float radius)
    {
        float cellSize = radius / Mathf.Sqrt(3);

        int gridX = Mathf.FloorToInt((max.x - min.x) / cellSize) + 1;
        int gridY = Mathf.FloorToInt((max.y - min.y) / cellSize) + 1;
        int gridZ = Mathf.FloorToInt((max.z - min.z) / cellSize) + 1;

        Vector3[,,] grid = new Vector3[gridX, gridY, gridZ];
        return (grid, cellSize, gridX, gridY, gridZ);
    }

    private Vector3Int PointToGrid(Vector3 p, Vector3 min, float cellSize)
    {
        int gx = Mathf.FloorToInt((p.x - min.x) / cellSize);
        int gy = Mathf.FloorToInt((p.y - min.y) / cellSize);
        int gz = Mathf.FloorToInt((p.z - min.z) / cellSize);

        return new Vector3Int(gx, gy, gz);
    }

    private bool IsValid(Vector3 point, float radius, Vector3[,,] grid, Vector3 min, float cellSize, int gridX,
        int gridY, int gridZ)
    {
        Vector3Int g = PointToGrid(point, min, cellSize);

        for (int x = g.x - 2; x <= g.x + 2; x++)
        {
            if (x < 0 || x >= gridX) continue;

            for (int y = g.y - 2; y <= g.y + 2; y++)
            {
                if (y < 0 || y >= gridY) continue;

                for (int z = g.z - 2; z <= g.z + 2; z++)
                {
                    if (z < 0 || z >= gridZ) continue;

                    Vector3 q = grid[x, y, z];

                    if (Vector3.Distance(q, point) < radius)
                        return false;
                }
            }
        }

        return true;
    }

    private void InsertSampleToGrid(Vector3 point, Vector3[,,] grid, Vector3 min, float cellSize)
    {
        Vector3Int g = PointToGrid(point, min, cellSize);
        grid[g.x, g.y, g.z] = point;
    }

    private bool IsInside(Vector3 pInterior, MeshCollider collider, int maxSteps = 64)
    {
        Vector3 dir = Vector3.right;
        int hits = 0;
        float remaining = 10000f;
        float epsilon = 0.01f;
        Vector3 origin = pInterior;
        
        for(int i = 0; i < maxSteps && remaining > 0.0f; i++)
        {
            bool hit = Physics.Raycast(origin, dir, out RaycastHit col, remaining, 1 << collider.gameObject.layer);

            if (hit)
            {
                hits++;

                float advance = col.distance + epsilon;
                remaining -= advance;
                origin = origin + dir * advance;
            }

            else
                break;
        }
        
        return (hits % 2) == 1;
    }
}

public struct Sample
{
    public Vector3 sample;
    public Vector3 triangleNormal;
}