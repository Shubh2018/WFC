using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;

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

    private WFC _wfc;

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

        _wfc = GetComponent<WFC>();
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
        Debug.Log($"Sampled Mesh: {mesh.transform.name}");

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
        overlapCount += SpawnPropsOnWall(node, obj, minMesh, maxMesh);

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

        foreach (var v in samples)
        {
            min.x = Mathf.Min(min.x, v.sample.x);
            min.y = Mathf.Min(min.y, v.sample.y);
            min.z = Mathf.Min(min.z, v.sample.z);

            max.x = Mathf.Max(max.x, v.sample.x);
            max.y = Mathf.Max(max.y, v.sample.y);
            max.z = Mathf.Max(max.z, v.sample.z);
        }

        float thresholdMin = Mathf.Abs((min.y + max.y) / 2) * 0.25f;
        float thresholdMax = Mathf.Abs((min.y + max.y) / 2) * 1.25f;

        _floorSamples.AddRange(samples.FindAll(s => (s.sample.y < thresholdMin) &&
                                                    (Vector3.Dot(s.triangleNormal, Vector3.up) > 0 &&
                                                     (s.sample.x > min.x && s.sample.x < max.x) && (s.sample.z > min.z && s.sample.z < max.z))));
        
        _samplePointsAll.AddRange(samples);
        _floorSamples.AddRange(samples.FindAll(s => (Vector3.Dot(s.triangleNormal, Vector3.up) > 0)));
        samples.RemoveAll(s => (Vector3.Dot(s.triangleNormal, Vector3.up) > 0));
        _floorSamplesAll.AddRange(_floorSamples);
    
        _wallSamples.AddRange(samples.FindAll(s => s.sample.y > thresholdMin));
        _wallSamples.RemoveAll((s) => Vector3.Dot(s.sample, Vector3.up) == 1 || Vector3.Dot(s.sample, Vector3.up) == -1);
        _wallSamplesAll.AddRange(_wallSamples);

        return (min, max);
    }

    private Sample GetStaticSamples(SpawnPosition spawnPos, Vector3 min, Vector3 max)
    {
        min = transform.InverseTransformPoint(min);
        max = transform.InverseTransformPoint(max);

        Vector3 mid = (min + max) / 2;

        switch(spawnPos)
        {
            case SpawnPosition.North: return new Sample{sample = new Vector3(mid.x, min.y, max.z), triangleNormal = Vector3.up};
            case SpawnPosition.South: return new Sample{sample = new Vector3(mid.x, min.y, min.z), triangleNormal = Vector3.up};
            case SpawnPosition.East:  return new Sample{sample = new Vector3(max.x, min.y, mid.z), triangleNormal = Vector3.up};
            case SpawnPosition.West:  return new Sample{sample = new Vector3(min.x, min.y, mid.z), triangleNormal = Vector3.up};

            case SpawnPosition.NorthEast: return new Sample{sample = new Vector3(max.x, min.y, max.z), triangleNormal = Vector3.up};
            case SpawnPosition.NorthWest: return new Sample{sample = new Vector3(min.x, min.y, max.z), triangleNormal = Vector3.up};
            case SpawnPosition.SouthEast: return new Sample{sample = new Vector3(max.x, min.y, min.z), triangleNormal = Vector3.up};
            case SpawnPosition.SouthWest: return new Sample{sample = min, triangleNormal = Vector3.up};

            case SpawnPosition.Center: return new Sample{sample = new Vector3(mid.x, min.y, mid.z), triangleNormal = Vector3.up};

            default: return new Sample{sample = Vector3.zero, triangleNormal = Vector3.up};
        }
    }

    private void FilterWallSamples(SpawnPosition spawnPos, int rem, Vector3 min, Vector3 max, Vector3 mid, Vector3 halfMin, Vector3 halfMax, out List<Sample> samples)
    {
        samples = new List<Sample>();

        if(rem == 0)
        {
            samples.AddRange(_wallSamples.FindAll((s) => s.sample.z > mid.z && s.sample.z < mid.z));

            switch(spawnPos)
            {
                case SpawnPosition.Center: samples.AddRange(_wallSamples.FindAll((s) => (s.sample.z > halfMin.z && s.sample.z < halfMax.z) && (s.sample.y > halfMin.y && s.sample.y < halfMax.y)));
                    break;

                case SpawnPosition.North: samples.AddRange(_wallSamples.FindAll((s) => (s.sample.z > halfMin.z && s.sample.z < halfMax.z) && (s.sample.y > halfMax.y && s.sample.y < max.y)));
                    break;

                case SpawnPosition.South: samples.AddRange(_wallSamples.FindAll((s) => (s.sample.z > halfMin.z && s.sample.z < halfMax.z) && (s.sample.y < halfMin.y && s.sample.y > min.y)));
                    break;

                case SpawnPosition.East: samples.AddRange(_wallSamples.FindAll((s) => (s.sample.z > halfMax.z && s.sample.z < max.z) && (s.sample.y > halfMin.y && s.sample.y < halfMax.y)));
                    break;

                case SpawnPosition.West: samples.AddRange(_wallSamples.FindAll((s) => (s.sample.z > min.z && s.sample.z < halfMin.z) && (s.sample.y > halfMin.y && s.sample.y < halfMax.y)));
                    break;

                case SpawnPosition.NorthEast: samples.AddRange(_wallSamples.FindAll((s) => (s.sample.z > halfMax.z && s.sample.z < max.z) && (s.sample.y > halfMax.y && s.sample.y < max.y)));
                    break;

                case SpawnPosition.NorthWest: samples.AddRange(_wallSamples.FindAll((s) => (s.sample.z > min.z && s.sample.z < halfMin.z) && (s.sample.y > halfMax.y && s.sample.y < max.y)));
                    break;

                case SpawnPosition.SouthEast: samples.AddRange(_wallSamples.FindAll((s) => (s.sample.z > halfMax.z && s.sample.z < max.z) && (s.sample.y > min.y && s.sample.y < halfMin.y)));
                    break;

                case SpawnPosition.SouthWest: samples.AddRange(_wallSamples.FindAll((s) => (s.sample.z > min.z && s.sample.z < halfMin.z) && (s.sample.y > min.y && s.sample.y < halfMin.y)));
                    break;

                default: samples.AddRange(_wallSamples);
                    break;
            }
        }

        else
        {
            samples.AddRange(_wallSamples.FindAll((s) => s.sample.x > mid.x && s.sample.x < mid.x));

            switch(spawnPos)
            {
                case SpawnPosition.Center: samples.AddRange(_wallSamples.FindAll((s) => (s.sample.x > halfMin.x && s.sample.x < halfMax.x) && (s.sample.y > halfMin.y && s.sample.y < halfMax.y)));
                    break;

                case SpawnPosition.North: samples.AddRange(_wallSamples.FindAll((s) => (s.sample.x > halfMin.x && s.sample.x < halfMax.x) && (s.sample.y > halfMax.y && s.sample.y < max.y)));
                    break;

                case SpawnPosition.South: samples.AddRange(_wallSamples.FindAll((s) => (s.sample.x > halfMin.x && s.sample.x < halfMax.x) && (s.sample.y < halfMin.y && s.sample.y > min.y)));
                    break;

                case SpawnPosition.East: samples.AddRange(_wallSamples.FindAll((s) => (s.sample.x > halfMax.x && s.sample.x < max.x) && (s.sample.y > halfMin.y && s.sample.y < halfMax.y)));
                    break;

                case SpawnPosition.West: samples.AddRange(_wallSamples.FindAll((s) => (s.sample.x > min.x && s.sample.x < halfMin.x) && (s.sample.y > halfMin.y && s.sample.y < halfMax.y)));
                    break;

                case SpawnPosition.NorthEast: samples.AddRange(_wallSamples.FindAll((s) => (s.sample.x > halfMax.x && s.sample.x < max.x) && (s.sample.y > halfMax.y && s.sample.y < max.y)));
                    break;

                case SpawnPosition.NorthWest: samples.AddRange(_wallSamples.FindAll((s) => (s.sample.x > min.x && s.sample.x < halfMin.x) && (s.sample.y > halfMax.y && s.sample.y < max.y)));
                    break;

                case SpawnPosition.SouthEast: samples.AddRange(_wallSamples.FindAll((s) => (s.sample.x > halfMax.x && s.sample.x < max.x) && (s.sample.y > min.y && s.sample.y < halfMin.y)));
                    break;

                case SpawnPosition.SouthWest: samples.AddRange(_wallSamples.FindAll((s) => (s.sample.x > min.x && s.sample.x < halfMin.x) && (s.sample.y > min.y && s.sample.y < halfMin.y)));
                    break;

                default: samples.AddRange(_wallSamples);
                    break;
            }
        }
    }

    private List<Sample> GetWallSamplesBySpawnPosition(int rem, SpawnPosition spawnPos, Vector3 min, Vector3 max, bool useStaticPosition)
    {
        min = transform.InverseTransformPoint(min);
        max = transform.InverseTransformPoint(max);

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

    private List<Sample> GetSamplesBySpawnPosition(SpawnPosition spawnPos, Vector3 min, Vector3 max)
    {
        min = transform.InverseTransformPoint(min);
        max = transform.InverseTransformPoint(max);

        Vector3 mid = (min + max) / 2;

        Vector3 halfMin = (min + mid) / 2;
        Vector3 halfMax = (mid + max) / 2;

        switch(spawnPos)
        {
            case SpawnPosition.North: return _floorSamples.FindAll((s) => s.sample.z > halfMax.z && s.sample.x < halfMax.x && s.sample.x > halfMin.x);
            case SpawnPosition.South: return _floorSamples.FindAll((s) => s.sample.z < halfMin.z && s.sample.x < halfMax.x && s.sample.x > halfMin.x);
            case SpawnPosition.East:  return _floorSamples.FindAll((s) => s.sample.x > halfMax.x && s.sample.z > halfMin.z && s.sample.z < halfMax.z);
            case SpawnPosition.West:  return _floorSamples.FindAll((s) => s.sample.x < halfMin.x && s.sample.z > halfMin.z && s.sample.z < halfMax.z);

            case SpawnPosition.NorthEast: return _floorSamples.FindAll((s) => s.sample.x > halfMax.x && s.sample.z > halfMax.z);
            case SpawnPosition.NorthWest: return _floorSamples.FindAll((s) => s.sample.x < halfMin.x && s.sample.z > halfMax.z);
            case SpawnPosition.SouthEast: return _floorSamples.FindAll((s) => s.sample.x > halfMax.x && s.sample.z < halfMin.z);
            case SpawnPosition.SouthWest: return _floorSamples.FindAll((s) => s.sample.x < halfMin.x && s.sample.z < halfMin.z);

            case SpawnPosition.Center: return _floorSamples.FindAll((s) => s.sample.x > halfMin.x && s.sample.x < halfMax.x && s.sample.z > halfMin.z && s.sample.z < halfMax.z); 

            default: return _floorSamples;
        }
    }

    private int SpawnFloorProps(NodeData node, GameObject nodeObj, Vector3 min, Vector3 max)
    {
        int overlapCount = 0;

        // validProps.RemoveAll((p) => p.CompareNodeType(node.nodeType) == 0);

        Prop prop = node.GetRandomPropCDF(PropPlacementType.Floor);

        if(!prop) return 0;
        
        List<Sample> samplesInRange = new List<Sample>();

        if(prop.UseStaticPositions)
            samplesInRange.Add(GetStaticSamples(prop.SpawnPosition, min, max));

        else
            samplesInRange.AddRange(GetSamplesBySpawnPosition(prop.SpawnPosition, min, max));

        Sample spawnSample = samplesInRange[Random.Range(0, samplesInRange.Count)];
        samplesInRange.Remove(spawnSample);
        _floorSamples.Remove(spawnSample);

        // Sample spawnSample = prop.SpawnPosition == SpawnPosition.North ? sampleNorth[Random.Range(0, sampleNorth.Count)] : prop.SpawnPosition == SpawnPosition.South ? sampleSouth[Random.Range(0, sampleSouth.Count)] : samplesInRange[Random.Range(0, samplesInRange.Count)];
        PropObject propObj = Instantiate(prop.PropObject, spawnSample.sample, Quaternion.identity);
        propObj.transform.SetParent(nodeObj.transform);

        propObj.transform.localEulerAngles = new Vector3(0.0f, Random.Range(0f, 360f), 0.0f);
    
        int i = 0;

        while(i < 2)
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

    private int SpawnPropsOnWall(NodeData node, GameObject go, Vector3 min, Vector3 max)
    {
        int overlapCount = 0;

        int i = 0;

        Vector3 div = go.transform.eulerAngles / 90.0f;
        int rem = (int)(div.y % 2);

        Prop prop = node.GetRandomPropCDF(PropPlacementType.Wall);

        if(prop == null) return 0;
        
        List<Sample> samplesList = GetWallSamplesBySpawnPosition(rem, prop.SpawnPosition, min, max, prop.UseStaticPositions);

        int randomWall = Random.Range(0, 2);

        if(samplesList.Count <= 0) return 0;

        Sample randomSample = samplesList[Random.Range(0, samplesList.Count)];
        
        PropObject propObj = Instantiate(prop.PropObject, go.transform);
        propObj.transform.position = randomSample.sample;
        propObj.transform.forward = randomSample.triangleNormal;

        samplesList.Clear();

        while (i < 1)
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

    private int SpawnWallProps(NodeData node, GameObject go, Vector3 min, Vector3 max)
    {
        int overlapCount = 0;
        
        if (node.IsStairPiece) return 0;

        Spawner toSpawn = new Spawner(_gameObjectsToSpawn);

        int wallCount = toSpawn.MaxWallPropCountPerRoom;

        List<Sample> filteredSamples = new List<Sample>();
        // filteredSamples.AddRange(WallMid(min, max));

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