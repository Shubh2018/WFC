using UnityEngine;
using System;

public class MeshSurface : MonoBehaviour
{
    private MeshSampler _meshSampler;
    private BoxCollider _boxCollider;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    [SerializeField] private bool _spawnViaSpawner = false;
    [SerializeField] private PropSpawnTagEnum _spawnTypeTag;
    [SerializeField] private Spawner _gameObjectsToSpawn;
    [SerializeField] private Vector3 _surfaceSize = Vector3.one;
    [SerializeField] private int _spawnHierarchy = 5;
    [SerializeField] private int _maxPropCount = 1;
    private WFC wfc;
    private PropHierarchy.PropHierachyInfo _hierarchyInfo;

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        Gizmos.DrawCube(new Vector3(0.0f, _surfaceSize.y / 2, 0.0f), _surfaceSize);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(new Vector3(0.0f, _surfaceSize.y / 2, 0.0f), _surfaceSize);
    }

    private void OnValidate()
    {
        if (gameObject.GetComponent<MeshSampler>() == null) 
        {
            _meshSampler = gameObject.AddComponent<MeshSampler>();
            _meshSampler.enableGizmosFloorSamples = true;
            _meshSampler.enableGizmosWallSamples = true;
            _meshSampler.enableGizmosSamplePoints = true;
            _meshSampler.samplesRenderDistance = 1000;
        }

        if (gameObject.GetComponent<BoxCollider>() == null) 
        {
            _boxCollider = gameObject.AddComponent<BoxCollider>();
        }

        if (gameObject.GetComponent<MeshFilter>() == null) 
        {
            _meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        if (gameObject.GetComponent<MeshRenderer>() == null)
        {
            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            _meshRenderer.material.color = Color.grey;
            _meshRenderer.enabled = false;
        }
    }

    public void Init(PropHierarchy.PropHierachyInfo parentHierachyInfo)
    {
        OnValidate();

        _meshSampler = gameObject.GetComponent<MeshSampler>();
        _boxCollider = gameObject.GetComponent<BoxCollider>();
        _meshFilter = gameObject.GetComponent<MeshFilter>();
        _meshRenderer = gameObject.GetComponent<MeshRenderer>();

        _meshFilter.sharedMesh = Misc.CreatePlaneMesh(_surfaceSize / 2);
        _meshRenderer.material.color = Color.grey;

        wfc = FindFirstObjectByType<WFC>();

        _hierarchyInfo = new PropHierarchy.PropHierachyInfo(parentHierachyInfo, _spawnHierarchy);

        Prop.Props.AddEntry(_hierarchyInfo.parentId, _hierarchyInfo.id, gameObject);

        Generate();
    }

    private void Generate()
    {
        if (!wfc.IsInside(GetComponent<BoxCollider>().bounds))
        {
            DestroyImmediate(gameObject);
            return;
        }
        if (_hierarchyInfo.IsCurrentHierachyLarger()) return;
        
        _meshSampler.Clear();
        _meshSampler.SetSpawnerData(_hierarchyInfo);
        _meshSampler.SetSamplingGraphProperties(0.25f, 1, 1, 1);
        _meshSampler.AddSamples(_meshSampler.GetSamples(_meshFilter));

        Spawner spawner = _spawnViaSpawner ? _gameObjectsToSpawn : new Spawner(AssetManager.LoadFilteredProps(_spawnTypeTag), _maxPropCount, _maxPropCount);

        Func<(Prop, int)> propFloorSpawnerFunc = () => (spawner.FloorPrefabs[UnityEngine.Random.Range(0, spawner.FloorPrefabs.Count)], spawner.FloorPrefabs.Count);
        Func<(Prop, int)> propWallSpawnerFunc = () => (spawner.WallPrefabs[UnityEngine.Random.Range(0, spawner.WallPrefabs.Count)], spawner.WallPrefabs.Count);
        Func<Prop, PropNeighborProperty> propNeighborSpawnerFunc = (Prop prop) => prop.GetRandomProp();
        Func<Vector3, Prop, bool> spawnFilterFunc = (Vector3 sample, Prop prop) => IsPropContained(sample, prop.PropObject);

        _meshSampler.SpawnProps(gameObject, spawner.maxFloorPropCount, spawner.maxWallPropCount, propFloorSpawnerFunc, propWallSpawnerFunc, propNeighborSpawnerFunc, spawnFilterFunc);
    }

    private bool IsPropContained(Vector3 sample, PropObject obj)
    {
        Vector3 angles = transform.parent ? transform.parent.eulerAngles : Vector3.zero;

        Bounds myBounds = new Bounds(transform.position, _surfaceSize);
        Bounds otherBounds = new Bounds(Misc.RotatePointAroundPivot(sample, transform.position, angles * -1), obj.GetSize);

        return myBounds.Contains(otherBounds.min) && myBounds.Contains(otherBounds.max);
    }
}
