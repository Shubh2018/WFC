using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

public enum SurfaceType
{
    Floor,
    Wall,
    Ceiling
};

public class MeshSampler : MonoBehaviour
{
    private List<MeshFilter> _meshFilter = new List<MeshFilter>();

    [SerializeField] private float _radius;
    [SerializeField] private int _tries = 30;

    [SerializeField] private Spawner _gameObjectsToSpawn;

    private Dictionary<Mesh, int[]> _triangles;
    private Dictionary<Mesh, Vector3[]> _vertices;

    private List<Sample> _floorSamples = new List<Sample>();
    private List<Sample> _wallSamples = new List<Sample>();

    private int safety = 10000;
    private Material[] _meshMaterials;

    private MeshCollider _collider;
    private Mesh _combinedMesh;

    private List<Sample> _samplePoints = new List<Sample>();
    private List<Sample> _pointsInside = new List<Sample>();
    
    private List<GameObject> _spawnedObjects = new List<GameObject>();

    public void Generate()
    {
        if (_samplePoints.Count > 0 && _meshFilter.Count > 0)
            Clear();

        _meshFilter = GetComponentsInChildren<MeshFilter>().ToList();

        _samplePoints.Clear();

        foreach (MeshFilter meshFilter in _meshFilter)
        {
            Debug.Log($"{transform.TransformPoint(meshFilter.transform.position)}: {transform.TransformVector(meshFilter.transform.localScale)}");
            _samplePoints.AddRange(SampleMesh(meshFilter, _radius, _tries));
        }
        

        SpawnProps();
    }

    public void Clear()
    {
        _floorSamples.Clear();  
        _wallSamples.Clear();
        _samplePoints.Clear();
        _pointsInside.Clear();

        _meshFilter.Clear();
        
        foreach(var spawnedObject in _spawnedObjects)
            DestroyImmediate(spawnedObject);
        
        _spawnedObjects.Clear();
    }

    private void OnDrawGizmos()
    {
        gameObject.TryGetComponent<MeshCollider>(out var meshCollider);

        if (meshCollider)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(meshCollider.bounds.center, meshCollider.bounds.size);
        }

        Gizmos.color = Color.white;

        foreach (var floorPoint in _samplePoints)
        {
            Gizmos.DrawSphere(floorPoint.sample, 0.05f);
            Gizmos.DrawRay(floorPoint.sample, floorPoint.triangleNormal * 1.0f);
        }
    }

    private List<Sample> SampleMesh(MeshFilter mesh, float radius, int tries)
    {
        List<Sample> samples = new List<Sample>();
        List<int> active = new List<int>();

        (float[] area, float[] cdf, float totalArea) =
            BuildTriangleAreaCDF(mesh.sharedMesh.vertices, mesh.sharedMesh.triangles);
        (Vector3 min, Vector3 max) = BuildBoundingBox(mesh.sharedMesh.vertices);

        (Vector3[,,] grid, float cellSize, int gx, int gy, int gz) = InitializeGrid(min, max, radius);

        int triangleIndex = SampleTriangleIndexFromCDF(cdf);

        int[] triangles = mesh.sharedMesh.triangles;
        Vector3[] vertices = mesh.sharedMesh.vertices;
        Vector3[] normals = mesh.sharedMesh.normals;

        int i0 = triangles[triangleIndex * 3 + 0];
        int i1 = triangles[triangleIndex * 3 + 1];
        int i2 = triangles[triangleIndex * 3 + 2];

        Vector3 p = SamplePointInTriangle(vertices[i0], vertices[i1], vertices[i2]);
        InsertSampleToGrid(p, grid, min, cellSize);

        Sample sample = new Sample()
        {
            sample = p,
            triangleNormal = Vector3.Cross((vertices[i1] - vertices[i0]), (vertices[i2] - vertices[i0])).normalized,
        };

        samples.Add(sample);
        active.Add(0);

        int tryCount = 0;

        while (active.Count > 0 && tryCount < safety)
        {
            tryCount += 1;

            int activeIndex = Random.Range(0, active.Count);
            int index = active[activeIndex];

            Sample s = samples[index];

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

                    sample = new Sample()
                    {
                        sample = candidate,
                        triangleNormal = Vector3.Cross((vertices[i1] - vertices[i0]), (vertices[i2] - vertices[i0]))
                            .normalized,
                    };

                    samples.Add(sample);
                    int newIndex = samples.Count - 1;

                    active.Add(newIndex);
                    accepted = true;
                }
            }

            if (!accepted)
            {
                (active[activeIndex], active[^1]) = (active[^1], active[activeIndex]);
                active.Remove(active.Count - 1);
            }
        }

        for (int i = 0; i < samples.Count; i++)
        {
            Sample s = samples[i];
            s.sample = mesh.transform.TransformPoint(s.sample);
            s.triangleNormal = mesh.transform.TransformDirection(s.triangleNormal);
            samples[i] = s;
        }
        
        for (int i = samples.Count - 1; i >= 0; i--)
        {
            if (!IsInside(samples[i], mesh.transform.position))
                samples.RemoveAt(i);
        }
        
        samples = samples.OrderBy(s => s.sample.y).ToList();
        float halfHeightY = mesh.transform.localScale.y / 2;
        
        SortSamples(samples);
        
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
            cdf[i] /= totalArea;

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

    private bool IsInside(Sample pInterior, Vector3 meshPos)
    {
        Vector3 dir = (meshPos - pInterior.sample);

        float d = Vector3.Dot(dir.normalized, pInterior.triangleNormal);
        float floor = Vector3.Dot(dir.normalized, Vector3.up);
        
        return d >= 0 || floor >= 1;
    }

    private void SortSamples(List<Sample> samples)
    {
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        foreach (var s in samples)
        {
            if(Mathf.Ceil(s.sample.y) < minY)
                minY = Mathf.Ceil(s.sample.y);
            
            if(Mathf.Floor(s.sample.y) > maxY)
                maxY = Mathf.Floor(s.sample.y);
        }

        float thresholdMin = Mathf.Abs((minY + maxY) / 2) * 1f;
        float thresholdMax = Mathf.Abs((minY + maxY) / 2) * 1.75f;
        
        _floorSamples.AddRange(samples.FindAll(s => (s.sample.y < thresholdMin) && (Vector3.Dot(s.triangleNormal, Vector3.up) > 0)));
        _wallSamples.AddRange(samples.FindAll(s => (s.sample.y > thresholdMin && s.sample.y <= thresholdMax)));
    }

    private void SpawnProps()
    {
        int wallCount = _gameObjectsToSpawn.WallPropCount;
        int floorCount = _gameObjectsToSpawn.FloorPropCount;
        
        int sampleIndex = 0;
        int random = 0;
        
        while (floorCount > 0)
        {
            random = Random.Range(0, _gameObjectsToSpawn.FloorPrefabs.Count);
            sampleIndex = Random.Range(0, _floorSamples.Count);
            
            _spawnedObjects.Add(Instantiate(_gameObjectsToSpawn.FloorPrefabs[random].Prop, _floorSamples[sampleIndex].sample, Quaternion.identity));
            _floorSamples.RemoveAt(sampleIndex);
            
            floorCount -= 1;
        }

        while (wallCount > 0)
        {
            sampleIndex = Random.Range(0, _wallSamples.Count);
            random = Random.Range(0, _gameObjectsToSpawn.WallPrefabs.Count);
            
            GameObject obj = Instantiate(_gameObjectsToSpawn.WallPrefabs[random].Prop,
                _wallSamples[sampleIndex].sample, Quaternion.identity);
            
            obj.transform.forward = _wallSamples[sampleIndex].triangleNormal;
            
            _spawnedObjects.Add(obj);
            _wallSamples.RemoveAt(sampleIndex);

            wallCount -= 1;
        }
    }
}

[System.Serializable]
public struct Sample
{
    public Vector3 sample;
    public Vector3 triangleNormal;
}

[System.Serializable]
public struct Spawner
{
    [SerializeField] private List<PropData> _wallPrefabs;
    [SerializeField] private List<PropData> _floorPrefabs;
    
    [SerializeField] private int _wallPropCount;
    [SerializeField] private int _floorPropCount;

    public List<PropData> WallPrefabs => _wallPrefabs;
    public List<PropData> FloorPrefabs => _floorPrefabs;
    public int WallPropCount => _wallPropCount;
    public int FloorPropCount => _floorPropCount;
}