using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using System.Threading.Tasks;
using UnityEditor.Build.Pipeline;
using UnityEngine.UI;

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

    private Dictionary<PropData, int> _props = new Dictionary<PropData, int>();
    private Dictionary<string, int> _propCount = new Dictionary<string, int>();
    public Dictionary<string, int> PropCount => _propCount;

    private float _spawnChance = 0.3f;

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

        // _meshFilter = GetComponentsInChildren<MeshFilter>().ToList();
        //
        // _samplePoints.Clear();
        //
        // foreach (MeshFilter meshFilter in _meshFilter)
        //     _samplePoints.AddRange(SampleMesh(meshFilter, _radius, _tries));

        // SpawnProps();
    }

    public void SetRadiusAndTries(float radius, int tries)
    {
        _radius = radius;
        _tries = tries;
    }

    public void SetSpawnerData(Spawner spawner)
    {
        _gameObjectsToSpawn = new Spawner(spawner);
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

        Debug.Log($"FloorSamples: {_floorSamplesAll.Count}");
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
                Gizmos.DrawRay(floorPoint.sample, floorPoint.triangleNormal * .1f);
            }
        }

        if (enableGizmosWallSamples) {
            Gizmos.color = Color.blue;

            foreach (var wallPoint in _wallSamplesAll)
            {
                if (!WithinDisOfCam(wallPoint.sample, samplesRenderDistance)) continue;
                Gizmos.DrawSphere(wallPoint.sample, 0.1f);
                Gizmos.DrawRay(wallPoint.sample, wallPoint.triangleNormal * .1f);
            }
        }

        if (enableGizmosSamplePoints) {
            Gizmos.color = Color.red;

            foreach (var samplePoint in _samplePointsAll)
            {
                if (!WithinDisOfCam(samplePoint.sample, samplesRenderDistance)) continue;
                Gizmos.DrawSphere(samplePoint.sample, 0.1f);
                Gizmos.DrawRay(samplePoint.sample, samplePoint.triangleNormal * .1f);
            }
        }
    }

    public List<Sample> GetSamples(MeshFilter mesh)
    {
        return SampleMesh(mesh, _radius, _tries);
    }

    private List<Sample> SampleMesh(MeshFilter mesh, float radius, int tries) // Method to sample the meshes. Return a list of Sample
    {
        List<Sample> samples = new List<Sample>();
        List<int> active = new List<int>();

        float[] cdf = BuildTriangleAreaCDF(mesh.sharedMesh.vertices, mesh.sharedMesh.triangles);  // Builds mesh triangles' CDF.
        (Vector3 min, Vector3 max) = BuildBoundingBox(mesh.sharedMesh.vertices); // Builds the bound box of the mesh

        (Vector3[,,] grid, float cellSize, int gx, int gy, int gz) = InitializeGrid(min, max, radius); // Initializes the grid to store the references to samples

        int triangleIndex = SampleTriangleIndexFromCDF(cdf);    // returns a random triangle, based on its area

        int[] triangles = mesh.sharedMesh.triangles;
        Vector3[] vertices = mesh.sharedMesh.vertices;

        int i0 = triangles[triangleIndex * 3 + 0];
        int i1 = triangles[triangleIndex * 3 + 1];
        int i2 = triangles[triangleIndex * 3 + 2];

        Vector3 p = SamplePointInTriangle(vertices[i0], vertices[i1], vertices[i2]); // Samples point ina triangle based on its barycentric coordinates
        InsertSampleToGrid(p, grid, min, cellSize); // Inserts the sample in the grids.

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

        for (int i = samples.Count - 1; i >= 0; i--)
        {
            if (!IsInside(samples[i], mesh.transform.position))
                samples.RemoveAt(i);
        }

        for (int i = 0; i < samples.Count; i++)
        {
            Sample s = samples[i];
            s.sample = mesh.transform.TransformPoint(s.sample);
            s.triangleNormal = mesh.transform.TransformDirection(s.triangleNormal);
            samples[i] = s;
        }

        samples = samples.OrderBy(s => s.sample.y).ToList();

        return samples;
    }

    public int SpawnProps(NodeData node, GameObject obj)
    {
        (Vector3 minMesh, Vector3 maxMesh) = SortSamplesInMesh(_samplePoints);

        int overlapCount = 0;

        overlapCount += SpawnFloorProps(node, obj, minMesh, maxMesh);
        overlapCount += SpawnWallProps(node, obj, minMesh, maxMesh);

        _samplePoints.Clear();

        AddPropToList();
        
        return overlapCount;
    }

    private void AddPropToList()
    {
        PropText += $"Prop List: \n";

        foreach(var prop in _props)
        {
            if(_propCount.ContainsKey(prop.Key.Prop.name))
                _propCount[prop.Key.Prop.name] += prop.Value;
            else
                _propCount.Add(prop.Key.Prop.name, prop.Value);
        }
    }

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

    private (Vector3, Vector3) SortSamplesInMesh(List<Sample> samples)
    {
        PropText = "";

        _props.Clear();

        _wallSamples.Clear();
        _floorSamples.Clear();
        _samplesNearWalls.Clear();

        // _floorSamplesAll.Clear();
        // _wallSamplesAll.Clear();

        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;

        float minDist = 4f;
        float maxDist = 4.5f;

        foreach (var v in samples)
        {
            min.x = Mathf.Min(min.x, v.sample.x);
            min.y = Mathf.Min(min.y, v.sample.y);
            min.z = Mathf.Min(min.z, v.sample.z);

            max.x = Mathf.Max(max.x, v.sample.x);
            max.y = Mathf.Max(max.y, v.sample.y);
            max.z = Mathf.Max(max.z, v.sample.z);
        }

        float thresholdMin = Mathf.Abs((min.y + max.y) / 2) * 1.1f;
        float thresholdMax = Mathf.Abs((min.y + max.y) / 2) * 1.25f;

        float mid = (Mathf.Abs((min.y + max.y) / 2) + max.y) / 2;

        _floorSamples.AddRange(samples.FindAll(s => (s.sample.y < thresholdMin) &&
                                                    (Vector3.Dot(s.triangleNormal, Vector3.up) > 0 &&
                                                     (s.sample.x > min.x && s.sample.x < max.x) && (s.sample.z > min.z && s.sample.z < max.z))));
        
        _samplePointsAll.AddRange(samples);
        _floorSamples.AddRange(samples.FindAll(s => (Vector3.Dot(s.triangleNormal, Vector3.up) > 0)));
        samples.RemoveAll(s => (Vector3.Dot(s.triangleNormal, Vector3.up) > 0));
        _floorSamplesAll.AddRange(_floorSamples);
    
        _wallSamples.AddRange(samples.FindAll(s => Mathf.Abs(mid - s.sample.y) < 0.1f));
        _wallSamplesAll.AddRange(_wallSamples);

        // for (int i = 0; i < _wallSamples.Count; i++)
        // {
        //     for (int j = 0; j < _floorSamples.Count; j++)
        //     {
        //         Vector3 floorSample = _floorSamples[j].sample;
        //         floorSample.y = _wallSamples[i].sample.y;
        //                 if (Vector3.Distance(_wallSamples[i].sample, floorSample) > minDist
        //             && Vector3.Distance(_wallSamples[i].sample, floorSample) <= maxDist)
        //         {
        //             _samplesNearWalls.Add(_floorSamples[j]);
        //             _floorSamples.RemoveAt(j);
        //         }
        //                 else
        //         {
        //             _samplesInMid.Add(_floorSamples[j]);
        //         }
        //     }
        // }

        return (min, max);
    }

    private int SpawnFloorProps(NodeData node, GameObject nodeObj, Vector3 min, Vector3 max)
    {
        int overlapCount = 0;
    
        int randomPropIndex = Random.Range(0, node.validProps.Count);

        Prop prop = node.validProps[randomPropIndex].GetRandomProp();

        Debug.Log($"Selected Prop: {prop.name}");

        return overlapCount;
    }

    private int SpawnWallProps(NodeData node, GameObject go, Vector3 min, Vector3 max)
    {
        int overlapCount = 0;
        
        if (node.IsStairPiece) return 0;
        
        Vector3 midPoint = (min + max) / 2;

        Spawner toSpawn = new Spawner(_gameObjectsToSpawn);

        int wallCount = toSpawn.MaxWallPropCountPerRoom;

        List<Sample> filteredSamples = new List<Sample>();
        filteredSamples.AddRange(_wallSamples);

        while (wallCount > 0 && toSpawn.WallPrefabs.Count > 0 && filteredSamples.Count > 0)
        {
            int propCount = 0;

            int random = Random.Range(0, toSpawn.WallPrefabs.Count);

            PropData prop = toSpawn.WallPrefabs[random];

            int sampleIndex = Random.Range(0, filteredSamples.Count);
            
            Sample s = filteredSamples[sampleIndex];
            _wallSamples.Remove(s);
            
            if (Random.Range(0, 1) > prop.SpawnChance)
                continue;
            
            if (propCount < prop.MaxCount)
            {
                PropObject obj = Instantiate(prop.Prop, go.transform).GetComponent<PropObject>();

                obj.transform.position = s.sample;
                obj.transform.forward = s.triangleNormal;

                overlapCount += obj.IsOverlappingProp();

                if (!obj) continue;

                propCount += 1;
                
                _spawnedObjects.Add(obj.gameObject);
                filteredSamples.RemoveAt(sampleIndex);

                wallCount -= 1;
            }

            else
            {
                toSpawn.WallPrefabs.RemoveAt(random);
            }
            
            if (_props.TryGetValue(prop, out var value))
                propCount = value;
            else
                _props.Add(prop, propCount);
            
            _props[prop] = propCount;
        }

        filteredSamples.Clear();

        return overlapCount;
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

    [SerializeField] private int maxWallPropCountPerRoom;
    [SerializeField] private int maxFloorPropCountPerRoom;

    public List<PropData> WallPrefabs => _wallPrefabs;
    public List<PropData> FloorPrefabs => _floorPrefabs;
    public int MaxWallPropCountPerRoom => maxWallPropCountPerRoom;
    public int MaxFloorPropCountPerRoom => maxFloorPropCountPerRoom;

    public Spawner(Spawner spawner)
    {
        _wallPrefabs = new List<PropData>(spawner.WallPrefabs);
        _floorPrefabs = new List<PropData>(spawner.FloorPrefabs);

        maxFloorPropCountPerRoom = spawner.MaxFloorPropCountPerRoom;
        maxWallPropCountPerRoom = spawner.MaxWallPropCountPerRoom;
    }
}