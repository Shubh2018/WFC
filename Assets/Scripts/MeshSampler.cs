using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System;

[Serializable]
public struct Sample
{
    public Vector3 sample;
    public Vector3 triangleNormal;
}

[Serializable]
public struct Spawner
{
    [SerializeField] private List<Prop> _wallPrefabs;
    [SerializeField] private List<Prop> _floorPrefabs;

    [SerializeField] public int maxWallPropCount;
    [SerializeField] public int maxFloorPropCount;

    public List<Prop> WallPrefabs => _wallPrefabs;
    public List<Prop> FloorPrefabs => _floorPrefabs;

    public Spawner(Spawner spawner, int? maxFloorProps = null, int? maxWallProps = null)
    {
        _wallPrefabs = new List<Prop>(spawner.WallPrefabs);
        _floorPrefabs = new List<Prop>(spawner.FloorPrefabs);

        maxFloorPropCount = maxFloorProps ?? spawner.maxFloorPropCount;
        maxWallPropCount = maxWallProps ?? spawner.maxWallPropCount;
    }

    public Spawner(List<Prop> floorProps, List<Prop> wallProps)
    {
        _wallPrefabs = wallProps;
        _floorPrefabs = floorProps;

        maxFloorPropCount = floorProps.Count;
        maxWallPropCount = wallProps.Count;
    }

    public Spawner(int maxFloorProps = 5, int maxWallProps = 5)
    {
        _wallPrefabs = new List<Prop>();
        _floorPrefabs = new List<Prop>();

        maxFloorPropCount = maxFloorProps;
        maxWallPropCount = maxWallProps;
    }

    public void AddProp(Prop prop) 
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

    private List<Sample> _samplePoints = new List<Sample>();
    private readonly List<Sample> _floorSamples = new List<Sample>();
    private readonly List<Sample> _wallSamples = new List<Sample>();
    private readonly List<Sample> _cornerSamples = new List<Sample>();

    private int safety = 10000;

    private List<List<Sample>> _floorSamplesAll = new List<List<Sample>>();
    private List<List<Sample>> _wallSamplesAll = new List<List<Sample>>();
    private List<List<Sample>> _cornerSamplesAll = new List<List<Sample>>();
    private List<List<Sample>> _samplePointsAll = new List<List<Sample>>();

    private PropHierarchy.PropHierachyInfo _hierarchyInfo;
    private Dictionary<Prop, int> _props = new Dictionary<Prop, int>();
    private Dictionary<string, int> _propCount = new Dictionary<string, int>();

    public Dictionary<string, int> PropCount => _propCount;

    private int _floorPropGraphLevel;
    private int _wallPropGraphLevel;

    public string PropText { get; private set; }

    // Debug Settings
    public bool enableGizmosFloorSamples = false;
    public bool enableGizmosWallSamples = false;
    public bool enableGizmosCornerSamples = false;
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

    public void SetSpawnerData(PropHierarchy.PropHierachyInfo parentHierarchy)
    {
        _hierarchyInfo = new PropHierarchy.PropHierachyInfo(parentHierarchy, 5);
    }

    public void AddSamples(List<Sample> samplePoints)
    {
        _samplePoints.Clear();
        _samplePoints.AddRange(samplePoints);
    }

    public void Clear()
    {
        _floorSamples.Clear();
        _wallSamples.Clear();
        _cornerSamples.Clear();
        _samplePoints.Clear();

        _floorSamplesAll.Clear();
        _wallSamplesAll.Clear();
        _cornerSamplesAll.Clear();
        _samplePointsAll.Clear();

        _meshFilter.Clear();
        _props.Clear();

        var tempList = transform.Cast<Transform>().ToList();
        foreach(var child in tempList)
        {
            DestroyImmediate(child.gameObject);
        }
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
            foreach(var floorList in _floorSamplesAll)
            {
                Gizmos.color = Color.white;
                foreach (var floorPoint in floorList)
                {
                    if (!WithinDisOfCam(floorPoint.sample, samplesRenderDistance)) continue;
                    Gizmos.DrawSphere(floorPoint.sample, 0.1f);
                    Gizmos.DrawRay(floorPoint.sample, floorPoint.triangleNormal * .2f);
                }
            }
        }

        if (enableGizmosWallSamples) {
            foreach(var wallList in _wallSamplesAll)
            {
                Gizmos.color = Color.orange;
                foreach (var wallPoint in wallList)
                {
                    if (!WithinDisOfCam(wallPoint.sample, samplesRenderDistance)) continue;
                    Gizmos.DrawSphere(wallPoint.sample, 0.1f);
                    Gizmos.DrawRay(wallPoint.sample, wallPoint.triangleNormal * .2f);
                }
            }
        }

        if (enableGizmosCornerSamples) {
            Gizmos.color = Color.gold;

            foreach(var cornerList in _cornerSamplesAll)
            {
                foreach (var cornerPoint in cornerList)
                {
                    if (!WithinDisOfCam(cornerPoint.sample, samplesRenderDistance)) continue;
                    Gizmos.DrawSphere(cornerPoint.sample, 0.1f);
                    Gizmos.DrawRay(cornerPoint.sample, cornerPoint.triangleNormal * .2f);
                }
            }
        }

        if (enableGizmosSamplePoints) {
            Gizmos.color = Color.red;

            foreach (var sampleList in _samplePointsAll)
            {
                foreach (var samplePoint in sampleList)
                {
                    if (!WithinDisOfCam(samplePoint.sample, samplesRenderDistance)) continue;
                    Gizmos.DrawSphere(samplePoint.sample, 0.1f);
                    Gizmos.DrawRay(samplePoint.sample, samplePoint.triangleNormal * .2f);
                }   
            }
        }
    }

    public List<Sample> GetSamples(MeshFilter mesh)
    {
        List<Sample> samples;
        while ((samples = SampleMesh(mesh, _radius, _tries)).Count() < 5); // Easy failsafe as sometimes only a handful of points are spawned for some reason
        return samples;
    }

    // Method to sample the meshes. Return a list of Sample
    private List<Sample> SampleMesh(MeshFilter mesh, float radius, int tries)
    {
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
                Vector3 normal = Vector3.Cross(vertices[i1] - vertices[i0], vertices[i2] - vertices[i0]).normalized;

                if ((active.Count == 0 || IsValid(candidate, radius, grid, min, cellSize, gridSize)) && IsInside(candidate, normal, mesh.sharedMesh.bounds))
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

    public void SpawnProps(GameObject obj, int maxFloorObjs, int maxWallObjs, Func<(Prop, int)> propSpawnerFloorFunc, Func<(Prop, int)> propSpawnerWallFunc, Func<Prop, PropNeighborProperty> propNeighborSpawnerFunc, Func<Vector3, Prop, bool> spawnFilterFunc = null)
    {
        (Vector3 min, Vector3 max) = BuildBoundingBox(_samplePoints.Select(v => v.sample).ToArray());
        
        SortSamplesInMesh(_samplePoints);

        spawnFilterFunc = spawnFilterFunc ?? ((Vector3 sample, Prop prop) => true);

        IEnumerator spawnFloorPropsRoutine = SpawnFloorProps(obj, maxFloorObjs, min, max, propSpawnerFloorFunc, propNeighborSpawnerFunc, spawnFilterFunc);
        IEnumerator spawnWallPropsRoutine = SpawnWallProps(obj, maxWallObjs, min, max, propSpawnerWallFunc, propNeighborSpawnerFunc, (Vector3 v, Prop p) => true);

        CoroutineManager.StartCoroutine(this, "SpawnFloorProps", spawnFloorPropsRoutine);
        CoroutineManager.StartCoroutine(this, "SpawnWallProps", spawnWallPropsRoutine);

        _samplePoints.Clear();
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

            area[i] = Vector3.Cross(v1 - v0, v2 - v0).magnitude * 0.5f;

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

    // To check against samples pointing away from the mash
    // In other words returns true if a point is on the inside of a node mesh, false if otherwise
    private bool IsInside(Vector3 sample, Vector3 normal, Bounds bounds)
    {
        Vector3 dir = sample - bounds.center;

        float d = Vector3.Dot(dir.normalized, normal);
        float floor = Vector3.Dot(dir.normalized, Vector3.up);

        return (d <= 0 || floor > 1) && bounds.Contains(sample);
    }

    // Used to check after a node mesh has been spawned that a sample is not inside of the mesh collision
    public bool IsInsideMesh(Sample sample, Vector3[] samplePoints)
    {
        return !samplePoints.Any(pos =>
        {
            Vector3 dir = (sample.sample - pos).normalized;
            float dis = Vector3.Distance(pos, sample.sample) - 0.01f;
            return !Physics.Raycast(pos, dir, dis);
        });
    }

    public void SortSamplesInMesh(List<Sample> samples)
    {
        // Clear previous added samples
        _wallSamples.Clear();
        _floorSamples.Clear();
        _cornerSamples.Clear();

        // list of all samples generated in this batch
        _samplePointsAll.Add(new List<Sample>(samples));

        // filter wall samples
        _wallSamples.AddRange(samples.FindAll(s => Mathf.Abs(Vector3.Dot(s.triangleNormal, Vector3.up)) <= 0.2f));
        samples = samples.Except(_wallSamples).ToList();
        _wallSamplesAll.Add(new List<Sample>(_wallSamples));

        // filter floor samples
        _floorSamples.AddRange(samples.FindAll(s => Vector3.Dot(s.sample, Vector3.up) != 1 && Vector3.Dot(s.sample, Vector3.up) != -1));
        samples = samples.Except(_floorSamples).ToList();
        _floorSamplesAll.Add(new List<Sample>(_floorSamples));

        // Filter floor beside wall samples
        _cornerSamples.AddRange(_floorSamples.FindAll(fs => _wallSamples.Any(ws => Vector3.Distance(fs.sample, ws.sample) <= 0.6f)));
        _cornerSamplesAll.Add(new List<Sample>(_cornerSamples));
    }

    private List<Sample> GetFloorSamplesBySpawnPosition(Prop prop, Vector3 min, Vector3 max)
    {
        min = transform.InverseTransformPoint(min);
        max = transform.InverseTransformPoint(max);

        Vector3 mid = (min + max) / 2;
        Vector3 halfMin = (min + mid) / 2;
        Vector3 halfMax = (mid + max) / 2;

        if (prop.UseStaticPositions && (!prop.SpawnInCorners && _cornerSamples.Count() > 0 || _cornerSamples.Count() == 0)) return new List<Sample> 
        {
            new() {
                sample = prop.SpawnPosition switch
                {
                    SpawnPosition.North => new Vector3(mid.x, min.y, max.z),
                    SpawnPosition.South => new Vector3(mid.x, min.y, min.z),
                    SpawnPosition.East => new Vector3(max.x, min.y, mid.z),
                    SpawnPosition.West => new Vector3(min.x, min.y, mid.z),
                    SpawnPosition.NorthEast => new Vector3(max.x, min.y, max.z),
                    SpawnPosition.NorthWest => new Vector3(min.x, min.y, max.z),
                    SpawnPosition.SouthEast => new Vector3(max.x, min.y, min.z),
                    SpawnPosition.SouthWest => min,
                    _ => Vector3.zero
                },
                triangleNormal = Vector3.up
            }
        };

        return new((prop.SpawnInCorners && _cornerSamples.Count() > 0 ? _cornerSamples : _floorSamples).FindAll((s) => prop.SpawnPosition switch
        {
            SpawnPosition.North => s.sample.z > halfMax.z && s.sample.x < halfMax.x && s.sample.x > halfMin.x,
            SpawnPosition.South => s.sample.z < halfMin.z && s.sample.x < halfMax.x && s.sample.x > halfMin.x,
            SpawnPosition.East => s.sample.x > halfMax.x && s.sample.z > halfMin.z && s.sample.z < halfMax.z,
            SpawnPosition.West => s.sample.x < halfMin.x && s.sample.z > halfMin.z && s.sample.z < halfMax.z,
            SpawnPosition.NorthEast => s.sample.x > halfMax.x && s.sample.z > halfMax.z,
            SpawnPosition.NorthWest => s.sample.x < halfMin.x && s.sample.z > halfMax.z,
            SpawnPosition.SouthEast => s.sample.x > halfMax.x && s.sample.z < halfMin.z,
            SpawnPosition.SouthWest => s.sample.x < halfMin.x && s.sample.z < halfMin.z,
            SpawnPosition.Center => s.sample.x > halfMin.x && s.sample.x < halfMax.x && s.sample.z > halfMin.z && s.sample.z < halfMax.z,
            _ => true
        }));
    }

    private List<Sample> FilterWallSamples(SpawnPosition spawnPos, Vector3 min, Vector3 max, Vector3 mid, Vector3 halfMin, Vector3 halfMax)
    {
        Func<int, Func<Vector3, float>> remFunc = (int rem) => (Vector3 vec) => rem == 0 ? vec.z : vec.x;
        Func<Func<Vector3, float>, List<Sample>> samplesFunc = (remFunc) => new List<Sample>(_wallSamples.FindAll((s) => remFunc(s.sample) > remFunc(mid) && remFunc(s.sample) < remFunc(mid)))
        .Concat(_wallSamples.FindAll((s) => spawnPos switch
        {
            SpawnPosition.North => remFunc(s.sample) > remFunc(halfMin) && remFunc(s.sample) < remFunc(halfMax) && s.sample.y > halfMax.y && s.sample.y < max.y,
            SpawnPosition.South => remFunc(s.sample) > remFunc(halfMin) && remFunc(s.sample) < remFunc(halfMax) && s.sample.y < halfMin.y && s.sample.y > min.y,
            SpawnPosition.East => remFunc(s.sample) > remFunc(halfMax) && remFunc(s.sample) < remFunc(max) && s.sample.y > halfMin.y && s.sample.y < halfMax.y,
            SpawnPosition.West => remFunc(s.sample) > remFunc(min) && remFunc(s.sample) < remFunc(halfMin) && s.sample.y > halfMin.y && s.sample.y < halfMax.y,
            SpawnPosition.NorthEast => remFunc(s.sample) > remFunc(halfMax) && remFunc(s.sample) < remFunc(max) && s.sample.y > halfMax.y && s.sample.y < max.y,
            SpawnPosition.NorthWest => remFunc(s.sample) > remFunc(min) && remFunc(s.sample) < remFunc(halfMin) && s.sample.y > halfMax.y && s.sample.y < max.y,
            SpawnPosition.SouthEast => remFunc(s.sample) > remFunc(halfMax) && remFunc(s.sample) < remFunc(max) && s.sample.y > min.y && s.sample.y < halfMin.y,
            SpawnPosition.SouthWest => remFunc(s.sample) > remFunc(min) && remFunc(s.sample) < remFunc(halfMin) && s.sample.y > min.y && s.sample.y < halfMin.y,
            SpawnPosition.Center => remFunc(s.sample) > remFunc(halfMin) && remFunc(s.sample) < remFunc(halfMax) && s.sample.y > halfMin.y && s.sample.y < halfMax.y,
            _ => true
        })).ToList();

        List<Sample> samples = samplesFunc(remFunc(0));
        samples.AddRange(samplesFunc(remFunc(1)));

        return samples;

    }

    private List<Sample> GetWallSamplesBySpawnPosition(Prop prop, Vector3 min, Vector3 max)
    {
        Vector3 mid = (min + max) / 2;
        Vector3 halfMin = (min + mid) / 2;
        Vector3 halfMax = (mid + max) / 2;

        List<Sample> samples = FilterWallSamples(prop.SpawnPosition, min, max, mid, halfMin, halfMax);

        if (samples.Count() == 0) return new List<Sample>();

        if(prop.UseStaticPositions)
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

    private IEnumerator SpawnFloorProps(GameObject nodeObj, int objCount, Vector3 min, Vector3 max, Func<(Prop, int)> propSpawner, Func<Prop, PropNeighborProperty> propNeighborSpawner, Func<Vector3, Prop, bool> spawnFilterFunc)
    {
        int floorCount = objCount;
        int tries = 25;

        while (floorCount > 0 && tries--> 0)
        {
            (Prop prop, int count) = propSpawner();
            if(!prop) yield break;
        
            List<Sample> samplesInRange = Misc.ShuffleList(new List<Sample>(GetFloorSamplesBySpawnPosition(prop, min, max)));
            if (samplesInRange.Count == 0) yield break;

            PropObject propObj = prop.SpawnFloor(samplesInRange, nodeObj, _hierarchyInfo, spawnFilterFunc);

            yield return null;

            if (!propObj) continue;

            for (int i = 0; i < _floorPropGraphLevel; i++)
            {
                PropNeighborProperty randomPropNeighbor = propNeighborSpawner(prop);
                if(randomPropNeighbor == null) break;

                float propMaxDistance = randomPropNeighbor.maxDistance;
                Vector3 parentPos = propObj.transform.position;
                samplesInRange = new List<Sample>((prop.SpawnInCorners && _cornerSamples.Count() > 0 ? _cornerSamples : _floorSamples).FindAll((s) => Vector3.Distance(s.sample, parentPos) >= propMaxDistance && Vector3.Distance(s.sample, parentPos) < propMaxDistance * 2));
                if(samplesInRange.Count == 0) break;

                propObj = randomPropNeighbor.prop.SpawnFloor(samplesInRange, nodeObj, _hierarchyInfo, spawnFilterFunc);

                yield return null;

                if (!propObj) continue;
                floorCount--;
            }

            floorCount--;
        }
    }

    private IEnumerator SpawnWallProps(GameObject nodeObj, int objCount, Vector3 min, Vector3 max, Func<(Prop, int)> propSpawner, Func<Prop, PropNeighborProperty> propNeighborSpawner, Func<Vector3, Prop, bool> spawnFilterFunc)
    {
        int wallCount = objCount;
        int tries = 25;

        while (wallCount > 0 && tries--> 0)
        {
            (Prop prop, int count) = propSpawner();
            if (!prop) yield break;

            List<Sample> samplesInRange = Misc.ShuffleList(new List<Sample>(GetWallSamplesBySpawnPosition(prop, min, max)));
            if (samplesInRange.Count == 0) yield break;

            PropObject propObj = prop.SpawnWall(samplesInRange, nodeObj, _hierarchyInfo, spawnFilterFunc);

            yield return null;

            if (!propObj) continue;

            for (int i = 0; i < _wallPropGraphLevel; i++)
            {
                PropNeighborProperty randomPropNeighbor = propNeighborSpawner(prop);
                if(randomPropNeighbor == null) break;

                samplesInRange = new List<Sample>(GetWallSamplesBySpawnPosition(randomPropNeighbor.prop, min, max));
                if (samplesInRange.Count == 0) break;

                propObj = randomPropNeighbor.prop.SpawnWall(samplesInRange, nodeObj, _hierarchyInfo, spawnFilterFunc);

                yield return null;

                if (!propObj) continue;
                wallCount--;
            }

            wallCount--;
        }
    }
}