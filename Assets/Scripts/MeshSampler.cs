using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System;

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

    [SerializeField] public int maxWallPropCount;
    [SerializeField] public int maxFloorPropCount;

    public List<PropData> WallPrefabs => _wallPrefabs;
    public List<PropData> FloorPrefabs => _floorPrefabs;

    public Spawner(Spawner spawner, int? maxFloorProps = null, int? maxWallProps = null)
    {
        _wallPrefabs = new List<PropData>(spawner.WallPrefabs);
        _floorPrefabs = new List<PropData>(spawner.FloorPrefabs);

        maxFloorPropCount = maxFloorProps ?? spawner.maxFloorPropCount;
        maxWallPropCount = maxWallProps ?? spawner.maxWallPropCount;
    }

    public Spawner(int maxFloorProps = 5, int maxWallProps = 5)
    {
        _wallPrefabs = new List<PropData>();
        _floorPrefabs = new List<PropData>();

        maxFloorPropCount = maxFloorProps;
        maxWallPropCount = maxWallProps;
    }

    public void AddProp(PropData prop) 
    {
        switch(prop.Placement)
        {
            case PropPlacementType.Floor:
                _floorPrefabs.Add(prop);
                break;
            case PropPlacementType.Wall:
                _wallPrefabs.Add(prop);
                break;
            default:
                break;
        }
    }
}

public enum SurfaceType
{
    Floor,
    Wall,
    Ceiling
};

public class MeshSampler : MonoBehaviour
{
    private List<MeshFilter> _meshFilter = new List<MeshFilter>();

    private float _radius;
    private int _tries = 30;

    private Spawner _gameObjectsToSpawn;

    private Dictionary<Mesh, int[]> _triangles;
    private Dictionary<Mesh, Vector3[]> _vertices; 

    private readonly List<Sample> _floorSamples = new List<Sample>();
    private readonly List<Sample> _wallSamples = new List<Sample>();

    private int safety = 10000;
    private int _objectivesSpawned = 0;
    private Material[] _meshMaterials;

    private MeshCollider _collider;
    private Mesh _combinedMesh;

    private List<Sample> _samplePoints = new List<Sample>();
    private List<Sample> _pointsInside = new List<Sample>();
    private List<Sample> _samplesNearWalls = new List<Sample>();
    private List<Sample> _samplesInMid = new List<Sample>();

    private List<Sample> _floorSamplesAll = new List<Sample>();
    private List<Sample> _wallSamplesAll = new List<Sample>();
    private List<Sample> _samplePointsAll = new List<Sample>();

    private List<GameObject> _spawnedObjects = new List<GameObject>();
    private float _spawnChance = 0.3f;
    private int _spawnHierarchy = 5;
    private int _currentHierachyLevel = 0;
    public Guid _parentId;
    private Dictionary<PropData, int> _props = new Dictionary<PropData, int>();
    private Dictionary<string, int> _propCount = new Dictionary<string, int>();

    public Dictionary<string, int> PropCount => _propCount;

    private int _floorPropGraphLevel;
    private int _wallPropGraphLevel;

    public string PropText { get; private set; }

    // Debug Settings
    public bool enableGizmosFloorSamples = false;
    public bool enableGizmosWallSamples = false;
    public bool enableGizmosSamplePoints = false;
    public float samplesRenderDistance = 20;

    public void Generate(MeshFilter meshFilter)
    {
        if (_samplePoints.Count > 0 && _meshFilter.Count > 0)
            Clear();

        _samplePoints.AddRange(SampleMesh(meshFilter, _radius, _tries));
    }

    public void SetSamplingGraphProperties(float radius, int tries, int floorPropGraphLevel, int wallPropGraphLevel)
    {
        _radius = radius;
        _tries = tries;
        _floorPropGraphLevel = floorPropGraphLevel;
        _wallPropGraphLevel = wallPropGraphLevel;
    }

    public void SetParent(Guid parentId)
    {
        _parentId = parentId;
    }

    public void SetSpawnerData(Spawner spawner, int spawnHierarchy = 5, int currentLevel = 0)
    {
        _gameObjectsToSpawn = new Spawner(spawner);
        _spawnHierarchy = spawnHierarchy;
        _currentHierachyLevel = currentLevel;
    }

    public void AddSamples(List<Sample> samplePoints)
    {
        _samplePoints.Clear();
        _samplePoints.AddRange(samplePoints);
    }

    public void Clear()
    {
        _objectivesSpawned = 0;

        _floorSamples.Clear();
        _wallSamples.Clear();
        _samplePoints.Clear();
        _pointsInside.Clear();

        _floorSamplesAll.Clear();
        _wallSamplesAll.Clear();
        _samplePointsAll.Clear();

        _samplesNearWalls.Clear();
        _samplesInMid.Clear();

        _meshFilter.Clear();
        _props.Clear();

        foreach(var spawnedObject in _spawnedObjects)
            DestroyImmediate(spawnedObject);

        _spawnedObjects.Clear();
    }

    private bool WithinDisOfCam(Vector3 pos, float maxDistance)
    {
        Camera cam = Camera.current;
        float d = Vector3.Distance(pos, cam.transform.position);
        return d <= maxDistance;
    }

    private void OnDrawGizmos()
    {
        if (enableGizmosFloorSamples) {
            Gizmos.color = Color.white;

            foreach (var floorPoint in _floorSamplesAll)
            {
                if (!WithinDisOfCam(floorPoint.sample, samplesRenderDistance)) continue;
                Gizmos.DrawSphere(floorPoint.sample, 0.1f);
                Gizmos.DrawRay(floorPoint.sample, floorPoint.triangleNormal * .2f);
            }
        }

        if (enableGizmosWallSamples) {
            Gizmos.color = Color.blue;

            foreach (var wallPoint in _wallSamplesAll)
            {
                if (!WithinDisOfCam(wallPoint.sample, samplesRenderDistance)) continue;
                Gizmos.DrawSphere(wallPoint.sample, 0.1f);
                Gizmos.DrawRay(wallPoint.sample, wallPoint.triangleNormal * .2f);
            }
        }

        if (enableGizmosSamplePoints) {
            Gizmos.color = Color.red;

            foreach (var samplePoint in _samplePointsAll)
            {
                if (!WithinDisOfCam(samplePoint.sample, samplesRenderDistance)) continue;
                Gizmos.DrawSphere(samplePoint.sample, 0.1f);
                Gizmos.DrawRay(samplePoint.sample, samplePoint.triangleNormal * .2f);
            }
        }
    }

    public List<Sample> GetSamples(MeshFilter mesh)
    {
        return SampleMesh(mesh, _radius, _tries);
    }

    // Method to sample the meshes. Return a list of Sample
    private List<Sample> SampleMesh(MeshFilter mesh, float radius, int tries)
    {
        Debug.Log($"Sampled Mesh: {mesh.transform.name}");

        List<Sample> samples = new List<Sample>();
        List<int> active = new List<int>();

        int[] triangles = mesh.sharedMesh.triangles;
        Vector3[] vertices = mesh.sharedMesh.vertices;

        float[] cdf = BuildTriangleAreaCDF(vertices, triangles);
        (Vector3 min, Vector3 max) = BuildBoundingBox(vertices);
        (Vector3[,,] grid, float cellSize, Vector3Int gridSize) = InitializeGrid(min, max, radius);

        int tryCount = 0;

        do
        {
            bool accepted = false;

            for (int i = 0; i < _tries; i++)
            {
                int triIndex = SampleTriangleIndexFromCDF(cdf);

                int i0 = triangles[triIndex * 3 + 0];
                int i1 = triangles[triIndex * 3 + 1];
                int i2 = triangles[triIndex * 3 + 2];

                Vector3 candidate = SamplePointInTriangle(vertices[i0], vertices[i1], vertices[i2]);
                Vector3 normal = Vector3.Cross((vertices[i1] - vertices[i0]), (vertices[i2] - vertices[i0])).normalized;

                if ((active.Count == 0 || IsValid(candidate, radius, grid, min, cellSize, gridSize)) && IsInside(candidate, normal, mesh.transform.position))
                {
                    InsertSampleToGrid(candidate, grid, min, cellSize);

                    samples.Add(new Sample() {
                        sample = mesh.transform.TransformPoint(candidate),
                        triangleNormal = mesh.transform.TransformDirection(normal),
                    });

                    active.Add(samples.Count - 1);
                    accepted = true;
                }
            }

            if (!accepted)
            {
                int activeIndex = UnityEngine.Random.Range(0, active.Count);
                (active[activeIndex], active[^1]) = (active[^1], active[activeIndex]);
                active.Remove(active.Count - 1);
            }

        } while(tryCount++ < safety && active.Count > 0);

        return samples.OrderBy(s => s.sample.y).ToList();
    }

    public int SpawnProps(GameObject obj, bool canHaveObjectes, Func<Vector3, PropData, bool, bool> spawnFilterFunc = null)
    {
        (Vector3 minMesh, Vector3 maxMesh) = SortSamplesInMesh(_samplePoints);

        int overlapCount = 0;
        Vector3 midPoint = (minMesh + maxMesh) / 2;

        spawnFilterFunc = spawnFilterFunc ?? ((Vector3 sample, PropData prop, bool propType) => true);

        StartCoroutine(SpawnFloorProps(obj, midPoint, canHaveObjectes, spawnFilterFunc));
        StartCoroutine(SpawnWallProps(obj, midPoint, spawnFilterFunc));

        _samplePoints.Clear();

        PropText += $"Prop List: \n";
        
        return overlapCount;
    }

    // Builds mesh triangles' CDF.
    private float[] BuildTriangleAreaCDF(Vector3[] vertices, int[] triangles)
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

        return cdf;
    }

    // returns a random triangle, based on its area
    private int SampleTriangleIndexFromCDF(float[] cdf)
    {
        float rand = UnityEngine.Random.value;

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

    // Samples point ina triangle based on its barycentric coordinates
    private Vector3 SamplePointInTriangle(Vector3 v0, Vector3 v1, Vector3 v2)
    {
        float u = UnityEngine.Random.value;
        float v = UnityEngine.Random.value;

        if (u + v > 1)
        {
            u = 1 - u;
            v = 1 - v;
        }

        Vector3 p = v0 + u * (v1 - v0) + v * (v2 - v0);
        return p;
    }

    // Builds the bounding box of the mesh
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

    // Initializes the grid to store the references to samples
    private (Vector3[,,], float, Vector3Int) InitializeGrid(Vector3 min, Vector3 max, float radius)
    {
        float cellSize = radius / Mathf.Sqrt(3);

        Vector3Int g = PointToGrid(max, min, cellSize) + Vector3Int.one;
        Vector3[,,] grid = new Vector3[g.x, g.y, g.z];

        return (grid, cellSize, g);
    }

    private Vector3Int PointToGrid(Vector3 p, Vector3 min, float cellSize)
    {
        int gx = Mathf.FloorToInt((p.x - min.x) / cellSize);
        int gy = Mathf.FloorToInt((p.y - min.y) / cellSize);
        int gz = Mathf.FloorToInt((p.z - min.z) / cellSize);

        return new Vector3Int(gx, gy, gz);
    }

    // Validates a point if it is more than a specific radius away from every specific point already on the mesh
    private bool IsValid(Vector3 point, float radius, Vector3[,,] grid, Vector3 min, float cellSize, Vector3Int gridSize)
    {
        Vector3Int g = PointToGrid(point, min, cellSize);

        for (int x = Mathf.Max(g.x - 2, 0); x <= Mathf.Min(g.x + 2, gridSize.x - 1); x++)
            for (int y = Mathf.Max(g.y - 2, 0); y <= Mathf.Min(g.y + 2, gridSize.y - 1); y++)
                for (int z = Mathf.Max(g.z - 2, 0); z <= Mathf.Min(g.z + 2, gridSize.z - 1); z++)
                    if (Vector3.Distance(grid[x, y, z], point) < radius) 
                        return false;
                        
        return true;
    }

    // Inserts the sample in the grids.
    private void InsertSampleToGrid(Vector3 point, Vector3[,,] grid, Vector3 min, float cellSize)
    {
        Vector3Int g = PointToGrid(point, min, cellSize);
        grid[g.x, g.y, g.z] = point;
    }

    private bool IsInside(Vector3 sample, Vector3 normal, Vector3 meshPos)
    {
        Vector3 dir = (meshPos - sample);

        float d = Vector3.Dot(dir.normalized, normal);
        float floor = Vector3.Dot(dir.normalized, Vector3.up);

        return (d >= 0 || floor >= 1);
    }

    public (Vector3, Vector3) SortSamplesInMesh(List<Sample> samples)
    {
        PropText = "";

        _props.Clear();
        _wallSamples.Clear();
        _floorSamples.Clear();
        _samplesNearWalls.Clear();

        (Vector3 min, Vector3 max) = BuildBoundingBox(samples.Select(v => v.sample).ToArray());

        float thresholdMin = Mathf.Abs((min.y + max.y) / 2) * 0.25f;
        float thresholdMax = Mathf.Abs((min.y + max.y) / 2) * 1.25f;

        _floorSamples.AddRange(samples.FindAll(s => (s.sample.y < thresholdMin) &&
                                                    Vector3.Dot(s.triangleNormal, Vector3.up) > 0 &&
                                                     s.sample.x > min.x && s.sample.x < max.x && s.sample.z > min.z && s.sample.z < max.z));
        
        _samplePointsAll.AddRange(samples);
        _floorSamples.AddRange(samples.FindAll(s => Vector3.Dot(s.triangleNormal, Vector3.up) > 0));
        samples.RemoveAll(s => Vector3.Dot(s.triangleNormal, Vector3.up) > 0);
        _floorSamplesAll.AddRange(_floorSamples);
    
        _wallSamples.AddRange(samples.FindAll(s => s.sample.y > thresholdMin));
        _wallSamples.RemoveAll((s) => Vector3.Dot(s.sample, Vector3.up) == 1 || Vector3.Dot(s.sample, Vector3.up) == -1);
        _wallSamplesAll.AddRange(_wallSamples);

        return (min, max);
    }

    private void FilterWallSamples(SpawnPosition spawnPos, int rem, Vector3 min, Vector3 max, Vector3 mid, Vector3 halfMin, Vector3 halfMax, out List<Sample> samples)
    {
        samples = new List<Sample>();

        if(rem == 0)
        {
            samples.AddRange(_wallSamples.FindAll((s) => s.sample.z > mid.z && s.sample.z < mid.z));

            switch(spawnPos)
            {
                case SpawnPosition.Center: 
                    samples.AddRange(_wallSamples.FindAll((s) => (s.sample.z > halfMin.z && s.sample.z < halfMax.z) && (s.sample.y > halfMin.y && s.sample.y < halfMax.y)));
                    break;

                case SpawnPosition.North: 
                    samples.AddRange(_wallSamples.FindAll((s) => (s.sample.z > halfMin.z && s.sample.z < halfMax.z) && (s.sample.y > halfMax.y && s.sample.y < max.y)));
                    break;

                case SpawnPosition.South: 
                    samples.AddRange(_wallSamples.FindAll((s) => (s.sample.z > halfMin.z && s.sample.z < halfMax.z) && (s.sample.y < halfMin.y && s.sample.y > min.y)));
                    break;

                case SpawnPosition.East: 
                    samples.AddRange(_wallSamples.FindAll((s) => (s.sample.z > halfMax.z && s.sample.z < max.z) && (s.sample.y > halfMin.y && s.sample.y < halfMax.y)));
                    break;

                case SpawnPosition.West: 
                    samples.AddRange(_wallSamples.FindAll((s) => (s.sample.z > min.z && s.sample.z < halfMin.z) && (s.sample.y > halfMin.y && s.sample.y < halfMax.y)));
                    break;

                case SpawnPosition.NorthEast: 
                    samples.AddRange(_wallSamples.FindAll((s) => (s.sample.z > halfMax.z && s.sample.z < max.z) && (s.sample.y > halfMax.y && s.sample.y < max.y)));
                    break;

                case SpawnPosition.NorthWest: 
                    samples.AddRange(_wallSamples.FindAll((s) => (s.sample.z > min.z && s.sample.z < halfMin.z) && (s.sample.y > halfMax.y && s.sample.y < max.y)));
                    break;

                case SpawnPosition.SouthEast: 
                    samples.AddRange(_wallSamples.FindAll((s) => (s.sample.z > halfMax.z && s.sample.z < max.z) && (s.sample.y > min.y && s.sample.y < halfMin.y)));
                    break;

                case SpawnPosition.SouthWest: 
                    samples.AddRange(_wallSamples.FindAll((s) => (s.sample.z > min.z && s.sample.z < halfMin.z) && (s.sample.y > min.y && s.sample.y < halfMin.y)));
                    break;

                default: 
                    samples.AddRange(_wallSamples);
                    break;
            }
        }

        else
        {
            samples.AddRange(_wallSamples.FindAll((s) => s.sample.x > mid.x && s.sample.x < mid.x));

            switch(spawnPos)
            {
                case SpawnPosition.Center: 
                    samples.AddRange(_wallSamples.FindAll((s) => (s.sample.x > halfMin.x && s.sample.x < halfMax.x) && (s.sample.y > halfMin.y && s.sample.y < halfMax.y)));
                    break;

                case SpawnPosition.North: 
                    samples.AddRange(_wallSamples.FindAll((s) => (s.sample.x > halfMin.x && s.sample.x < halfMax.x) && (s.sample.y > halfMax.y && s.sample.y < max.y)));
                    break;

                case SpawnPosition.South: 
                    samples.AddRange(_wallSamples.FindAll((s) => (s.sample.x > halfMin.x && s.sample.x < halfMax.x) && (s.sample.y < halfMin.y && s.sample.y > min.y)));
                    break;

                case SpawnPosition.East: 
                    samples.AddRange(_wallSamples.FindAll((s) => (s.sample.x > halfMax.x && s.sample.x < max.x) && (s.sample.y > halfMin.y && s.sample.y < halfMax.y)));
                    break;

                case SpawnPosition.West: 
                    samples.AddRange(_wallSamples.FindAll((s) => (s.sample.x > min.x && s.sample.x < halfMin.x) && (s.sample.y > halfMin.y && s.sample.y < halfMax.y)));
                    break;

                case SpawnPosition.NorthEast: 
                    samples.AddRange(_wallSamples.FindAll((s) => (s.sample.x > halfMax.x && s.sample.x < max.x) && (s.sample.y > halfMax.y && s.sample.y < max.y)));
                    break;

                case SpawnPosition.NorthWest: 
                    samples.AddRange(_wallSamples.FindAll((s) => (s.sample.x > min.x && s.sample.x < halfMin.x) && (s.sample.y > halfMax.y && s.sample.y < max.y)));
                    break;

                case SpawnPosition.SouthEast: 
                    samples.AddRange(_wallSamples.FindAll((s) => (s.sample.x > halfMax.x && s.sample.x < max.x) && (s.sample.y > min.y && s.sample.y < halfMin.y)));
                    break;

                case SpawnPosition.SouthWest: 
                    samples.AddRange(_wallSamples.FindAll((s) => (s.sample.x > min.x && s.sample.x < halfMin.x) && (s.sample.y > min.y && s.sample.y < halfMin.y)));
                    break;

                default: 
                    samples.AddRange(_wallSamples);
                    break;
            }
        }
    }

    private List<Sample> GetWallSamplesBySpawnPosition(int rem, SpawnPosition spawnPos, Vector3 min, Vector3 max, bool useStaticPosition)
    {
        // min = transform.InverseTransformPoint(min);
        // max = transform.InverseTransformPoint(max);

        Vector3 mid = (min + max) / 2;
        Vector3 halfMin = (min + mid) / 2;
        Vector3 halfMax = (mid + max) / 2;

        // List<Sample> samples = new List<Sample>();

        FilterWallSamples(spawnPos, rem, min, max, mid, halfMin, halfMax, out List<Sample> samples);

        if(useStaticPosition)
        {
            float d = float.PositiveInfinity;

            min = Vector3.positiveInfinity;
            max = Vector3.negativeInfinity;

            Sample closestSample = new Sample
            {
                sample = Vector3.zero,
                triangleNormal = Vector3.zero
            };

            foreach(var s in samples)
            {
                min.x = Mathf.Min(s.sample.x, min.x);
                min.y = Mathf.Min(s.sample.y, min.y);
                min.z = Mathf.Min(s.sample.z, min.z);

                max.x = Mathf.Max(s.sample.x, max.x);
                max.y = Mathf.Max(s.sample.y, max.y);
                max.z = Mathf.Max(s.sample.z, max.z);
            }

            min = transform.InverseTransformPoint(min);
            max = transform.InverseTransformPoint(max);

            mid = (min + max) / 2;

            foreach(var s in samples)
            {
                float distance = Vector3.Distance(s.sample, mid);

                if(distance < d)
                {
                    d = distance;
                    closestSample = s;
                }    
            }

            samples.Clear();
            samples.Add(closestSample);
        }

        return samples;
    }

    private List<Sample> GetFloorSamplesBySpawnPosition(SpawnPosition spawnPos, Vector3 min, Vector3 max, bool useStaticPosition)
    {
        min = transform.InverseTransformPoint(min);
        max = transform.InverseTransformPoint(max);

        Vector3 mid = (min + max) / 2;

        Vector3 halfMin = (min + mid) / 2;
        Vector3 halfMax = (mid + max) / 2;

        List<Sample> samples = new List<Sample>();

        if(!useStaticPosition)
        {
            switch(spawnPos)
            {
                case SpawnPosition.North: 
                    samples.AddRange(_floorSamples.FindAll((s) => s.sample.z > halfMax.z && s.sample.x < halfMax.x && s.sample.x > halfMin.x));
                    break;
                case SpawnPosition.South: 
                    samples.AddRange(_floorSamples.FindAll((s) => s.sample.z < halfMin.z && s.sample.x < halfMax.x && s.sample.x > halfMin.x));
                    break;
                case SpawnPosition.East:  
                    samples.AddRange(_floorSamples.FindAll((s) => s.sample.x > halfMax.x && s.sample.z > halfMin.z && s.sample.z < halfMax.z));
                    break;
                case SpawnPosition.West:  
                    samples.AddRange(_floorSamples.FindAll((s) => s.sample.x < halfMin.x && s.sample.z > halfMin.z && s.sample.z < halfMax.z));
                    break;
                case SpawnPosition.NorthEast: 
                    samples.AddRange(_floorSamples.FindAll((s) => s.sample.x > halfMax.x && s.sample.z > halfMax.z));
                    break;
                case SpawnPosition.NorthWest: 
                    samples.AddRange(_floorSamples.FindAll((s) => s.sample.x < halfMin.x && s.sample.z > halfMax.z));
                    break;
                case SpawnPosition.SouthEast: 
                    samples.AddRange(_floorSamples.FindAll((s) => s.sample.x > halfMax.x && s.sample.z < halfMin.z));
                    break;
                case SpawnPosition.SouthWest: 
                    samples.AddRange(_floorSamples.FindAll((s) => s.sample.x < halfMin.x && s.sample.z < halfMin.z));
                    break;
                case SpawnPosition.Center: 
                    samples.AddRange(_floorSamples.FindAll((s) => s.sample.x > halfMin.x && s.sample.x < halfMax.x && s.sample.z > halfMin.z && s.sample.z < halfMax.z));
                    break;
                default: 
                    samples.AddRange(_floorSamples);
                    break;
            }
        }

        else
        {
            switch(spawnPos)
            {
                case SpawnPosition.North: 
                    samples.Add(new Sample{sample = new Vector3(mid.x, min.y, max.z), triangleNormal = Vector3.up});
                    break;
                case SpawnPosition.South: 
                    samples.Add(new Sample{sample = new Vector3(mid.x, min.y, min.z), triangleNormal = Vector3.up});
                    break;
                case SpawnPosition.East:  
                    samples.Add(new Sample{sample = new Vector3(max.x, min.y, mid.z), triangleNormal = Vector3.up});
                    break;
                case SpawnPosition.West:  
                    samples.Add(new Sample{sample = new Vector3(min.x, min.y, mid.z), triangleNormal = Vector3.up});
                    break;
                case SpawnPosition.NorthEast: 
                    samples.Add(new Sample{sample = new Vector3(max.x, min.y, max.z), triangleNormal = Vector3.up});
                    break;
                case SpawnPosition.NorthWest: 
                    samples.Add(new Sample{sample = new Vector3(min.x, min.y, max.z), triangleNormal = Vector3.up});
                    break;
                case SpawnPosition.SouthEast: 
                    samples.Add(new Sample{sample = new Vector3(max.x, min.y, min.z), triangleNormal = Vector3.up});
                    break;
                case SpawnPosition.SouthWest: 
                    samples.Add(new Sample{sample = min, triangleNormal = Vector3.up});
                    break;
                case SpawnPosition.Center: 
                    samples.Add(new Sample{sample = new Vector3(mid.x, min.y, mid.z), triangleNormal = Vector3.up});
                    break;
                default: 
                    samples.Add(new Sample{sample = Vector3.zero, triangleNormal = Vector3.up});
                    break;
            }
        }

        return samples;
    }

    private IEnumerator SpawnFloorProps(GameObject nodeObj, Vector3 midPoint, bool canHaveObjectes, Func<Vector3, PropData, bool, bool> spawnFilterFunc)
    {
        if (_gameObjectsToSpawn.FloorPrefabs.Count != 0)
        {
            Spawner toSpawn = new Spawner(_gameObjectsToSpawn);

            int floorCount = toSpawn.maxFloorPropCount;

            if (canHaveObjectes && _objectivesSpawned == 0)
            {
                List<PropData> props = toSpawn.FloorPrefabs.FindAll((prop) => prop.PropType == Prop.Objective);

                if (props.Count > 0) {

                    int random = UnityEngine.Random.Range(0, props.Count);
                    PropData prop = props[random];

                    PropObject propObj = Instantiate(prop.Prop, nodeObj.transform, false).GetComponent<PropObject>();

                    propObj.transform.localPosition = Vector3.zero;
                    propObj.UpdateRotation();

                    _objectivesSpawned++;

                    PropData.Props.Increase(_parentId, prop.Prop.name);
                    propObj.UpdateChildren(_parentId, _spawnHierarchy, _currentHierachyLevel+1);
                }
            }

            else
            {
                toSpawn.FloorPrefabs.RemoveAll((prop) => prop.PropType == Prop.Objective);

                List<Sample> filteredSamples = new List<Sample>(_floorSamples);
                
                while (floorCount > 0 && filteredSamples.Count > 0)
                {
                    int random = UnityEngine.Random.Range(0, toSpawn.FloorPrefabs.Count);
                    int sampleIndex = UnityEngine.Random.Range(0, filteredSamples.Count);

                    PropData prop = toSpawn.FloorPrefabs[random];
                    Sample s = filteredSamples[sampleIndex];

                    if (PropData.Props.CanSpawnProp(_parentId, prop))
                    {
                        Vector4 rot = new Vector3(0, UnityEngine.Random.Range(0, 360), 0);
                        List<Collider> cols = PropData.Props.GetGameObjects(_parentId).Select(obj => obj.GetComponent<Collider>()).ToList();

                        if (UnityEngine.Random.Range(0, 1) > prop.SpawnChance) continue;
                        if (!spawnFilterFunc(s.sample, prop, false)) continue;
                        if (prop.Prop.GetComponent<PropObject>().CheckOverlapBox(s.sample, Quaternion.Euler(rot), (List<Collider> cols2) => cols2.Intersect(cols))) continue;

                        PropObject propObj = Instantiate(prop.Prop, nodeObj.transform, false).GetComponent<PropObject>();

                        propObj.transform.localPosition = nodeObj.transform.InverseTransformPoint(s.sample);
                        propObj.transform.localEulerAngles = rot;

                        PropData.Props.Increase(_parentId, prop.Prop.name);
                        propObj.UpdateChildren(_parentId, _spawnHierarchy, _currentHierachyLevel+1);

                        if(prop.CheckOrientation)
                        {
                            Vector3 dir = midPoint - s.sample;
                            dir.y = 0;
                            propObj.transform.forward = dir;
                        }

                        _spawnedObjects.Add(propObj.gameObject);

                        filteredSamples.RemoveAt(sampleIndex);
                        filteredSamples.RemoveAll((sample) => Vector3.Distance(sample.sample, s.sample) < .75f);
                        
                        floorCount--;

                        yield return null;
                        continue;
                    }

                    filteredSamples.RemoveAt(sampleIndex);

                    yield return null;
                }

                Debug.Log("Done spawning floor props");
            }
        }
    }

    private IEnumerator SpawnWallProps(GameObject go, Vector3 midPoint, Func<Vector3, PropData, bool, bool> spawnFilterFunc)
    {
        if (_gameObjectsToSpawn.WallPrefabs.Count != 0)
        {
            Spawner toSpawn = new Spawner(_gameObjectsToSpawn);

            int wallCount = toSpawn.maxWallPropCount;

            List<Sample> filteredSamples = new List<Sample>(_wallSamples);

            while (wallCount > 0 && filteredSamples.Count > 0)
            {
                int random = UnityEngine.Random.Range(0, toSpawn.WallPrefabs.Count);
                int sampleIndex = UnityEngine.Random.Range(0, filteredSamples.Count);

                PropData prop = toSpawn.WallPrefabs[random];
                Sample s = filteredSamples[sampleIndex];
                
                if (PropData.Props.CanSpawnProp(_parentId, prop))
                {
                    List<Collider> cols = PropData.Props.GetGameObjects(_parentId).Select(obj => obj.GetComponent<Collider>()).ToList();

                    if (UnityEngine.Random.Range(0, 1) > prop.SpawnChance) continue;
                    if (!spawnFilterFunc(s.sample, prop, true)) continue;
                    if (prop.Prop.GetComponent<PropObject>().CheckOverlapBox(s.sample, Quaternion.LookRotation(s.triangleNormal), (List<Collider> cols2) => cols2.Intersect(cols))) continue;

                    PropObject propObj = Instantiate(prop.Prop, go.transform).GetComponent<PropObject>();

                    propObj.transform.position = s.sample;
                    propObj.transform.forward = s.triangleNormal;

                    PropData.Props.Increase(_parentId, prop.Prop.name);
                    propObj.UpdateChildren(_parentId, _spawnHierarchy, _currentHierachyLevel+1);
                    
                    _spawnedObjects.Add(propObj.gameObject);
                    filteredSamples.RemoveAt(sampleIndex);

                    wallCount--;

                    yield return null;
                    continue;
                }

                filteredSamples.RemoveAt(sampleIndex);

                yield return null;
            }

            Debug.Log("Done spawning wall props");
        }
    }

    /*
        private int SpawnFloorProps(NodeData node, GameObject nodeObj, Vector3 min, Vector3 max)
    {
        int overlapCount = 0;
        Prop prop = node.GetRandomPropCDF(PropPlacementType.Floor);

        if(!prop) return 0;
        
        List<Sample> samplesInRange = new List<Sample>();
        samplesInRange.AddRange(GetFloorSamplesBySpawnPosition(prop.SpawnPosition, min, max, prop.UseStaticPositions));

        Sample spawnSample = samplesInRange[Random.Range(0, samplesInRange.Count)];
        samplesInRange.Remove(spawnSample);
        _floorSamples.Remove(spawnSample);

        PropObject propObj = Instantiate(prop.PropObject, spawnSample.sample, Quaternion.Euler(new Vector3(0.0f, Random.Range(0.0f, 360.0f), 0.0f)));
        propObj.transform.SetParent(nodeObj.transform);
    
        int i = 0;

        while(i < _floorPropGraphLevel - 1)
        {
            i += 1;

            PropNeighborProperty randomPropNeighbor = prop.GetRandomProp(node);

            if(randomPropNeighbor == null) continue;

            Prop propNeighbor = randomPropNeighbor.prop;

            float propMaxDistance = randomPropNeighbor.maxDistance;

            samplesInRange.Clear();

            samplesInRange.AddRange(_floorSamples.FindAll((s) => Vector3.Distance(s.sample, spawnSample.sample) >= propMaxDistance 
                                        && Vector3.Distance(s.sample, spawnSample.sample) < propMaxDistance * 2));
            
            if(samplesInRange.Count == 0) continue;
            spawnSample = samplesInRange[Random.Range(0, samplesInRange.Count)];

            samplesInRange.Remove(spawnSample);
            _floorSamples.Remove(spawnSample);

            propObj = Instantiate(propNeighbor.PropObject, spawnSample.sample, Quaternion.Euler(0, Random.Range(0f, 360f), 0f));
            propObj.transform.SetParent(nodeObj.transform);

            propObj.UpdateRotation();

            prop = propNeighbor;
        }

        return overlapCount;
    }

    private int SpawnWallProps(NodeData node, GameObject go, Vector3 min, Vector3 max)
    {
        int overlapCount = 0;

        int i = 0;

        Vector3 div = go.transform.eulerAngles / 90.0f;
        int rem = (int)(div.y % 2);

        Prop prop = node.GetRandomPropCDF(PropPlacementType.Wall);

        if(prop == null) return 0;
        
        List<Sample> samplesList = GetWallSamplesBySpawnPosition(rem, prop.SpawnPosition, min, max, prop.UseStaticPositions);

        if(samplesList.Count <= 0) return 0;

        Sample randomSample = samplesList[Random.Range(0, samplesList.Count)];
        
        PropObject propObj = Instantiate(prop.PropObject, go.transform);
        propObj.transform.position = randomSample.sample;
        propObj.transform.forward = randomSample.triangleNormal;

        samplesList.Clear();

        while (i < _wallPropGraphLevel- 1)
        {
            i += 1;

            PropNeighborProperty neighbor = prop.GetRandomProp(node);
            Prop neighborProp = neighbor.prop;
            
            float spawnChance = Random.Range(0.0f, 1.0f);

            if(spawnChance > neighbor.spawnChance) break;

            rem = 1- rem;

            samplesList.AddRange(GetWallSamplesBySpawnPosition(rem, neighborProp.SpawnPosition, min, max, neighborProp.UseStaticPositions));

            randomSample = samplesList[Random.Range(0, samplesList.Count)];

            propObj = Instantiate(neighborProp.PropObject, go.transform);
            propObj.transform.position = randomSample.sample;
            propObj.transform.forward = randomSample.triangleNormal;

            samplesList.Clear();
            // samplesList.RemoveAll((s) => s.sample.x == randomSample.sample.x || s.sample.z == randomSample.sample.z);
        }

        return overlapCount;
    }
}
    */
}