using UnityEngine;
using System;
using System.Collections.Generic;

public class MeshSurface : MonoBehaviour
{
    private MeshSampler _meshSampler;
    private MeshFilter _meshFilter;
    private BoxCollider _boxCollider;
    private MeshRenderer _meshRenderer;
    [SerializeField] private bool _spawnViaSpawner = false;
    [SerializeField] private PropSpawnTagEnum _spawnTypeTag;
    [SerializeField] private Spawner _gameObjectsToSpawn;
    [SerializeField] private Vector3 _surfaceSize = Vector3.one;
    [SerializeField] private int _spawnHierarchy = 5;
    [SerializeField] private int _maxPropCount = 1;
    private PropHierarchy.PropHierachyInfo _hierarchyInfo;

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        Gizmos.DrawCube(new Vector3(0.0f, _surfaceSize.y / 2, 0.0f), _surfaceSize);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(new Vector3(0.0f, _surfaceSize.y / 2, 0.0f), _surfaceSize);
    }

    public void Init()
    {
        _meshSampler = GetComponent<MeshSampler>();
        _boxCollider = GetComponent<BoxCollider>();
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();

        _meshFilter.sharedMesh = Misc.CreatePlaneMesh(_surfaceSize / 2);
        _meshRenderer.material.color = Color.grey;

        if (_hierarchyInfo.IsCurrentHierachyLarger()) return;

        Prop.Props.AddEntry(Guid.Empty, Guid.NewGuid(), gameObject);

        Generate();
    }

    public void Init(PropHierarchy.PropHierachyInfo parentHierachyInfo)
    {
        _meshSampler = GetComponent<MeshSampler>();
        _boxCollider = GetComponent<BoxCollider>();
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();

        _meshFilter.sharedMesh = Misc.CreatePlaneMesh(_surfaceSize / 2);
        _meshRenderer.material.color = Color.grey;

        _hierarchyInfo = new PropHierarchy.PropHierachyInfo(parentHierachyInfo.id, parentHierachyInfo.maxHierachyLevel, parentHierachyInfo.currentHierachyLevel);

        if (_hierarchyInfo.IsCurrentHierachyLarger()) return;

        Prop.Props.AddEntry(_hierarchyInfo.parentId, _hierarchyInfo.id, gameObject);

        Generate();
    }

    private void Generate()
    {
        _meshSampler.Clear();
        _meshSampler.SetSpawnerData(_hierarchyInfo);
        _meshSampler.SetSamplingGraphProperties(0.25f, 1, 1, 1);
        _meshSampler.AddSamples(_meshSampler.GetSamples(_meshFilter));

        Spawner spawner = GetSpawner();

        Func<(Prop, int)> propFloorSpawnerFunc = () => Misc.GetRandomPropCDF(spawner.FloorPrefabs, _hierarchyInfo);
        Func<(Prop, int)> propWallSpawnerFunc = () => Misc.GetRandomPropCDF(spawner.WallPrefabs, _hierarchyInfo);
        Func<Prop, PropNeighborProperty> propNeighborSpawnerFunc = (Prop prop) => prop.GetRandomProp();
        Func<Vector3, Prop, bool> spawnFilterFunc = (Vector3 sample, Prop prop) => IsPropContained(sample, prop.PropObject);

        _meshSampler.SpawnProps(gameObject, spawner.maxFloorPropCount, spawner.maxWallPropCount, propFloorSpawnerFunc, propWallSpawnerFunc, propNeighborSpawnerFunc, spawnFilterFunc);
    }

    // Used to filter out props that spawn on this surface based on environmental settings
    private Spawner GetSpawner()
    {
        MeshNode mesh = Prop.Props?.GetParentRoomNode(_hierarchyInfo.id);
        Environment env = mesh?.GetEnvironment;
        Spawner spawner = _spawnViaSpawner ? _gameObjectsToSpawn : new Spawner(AssetManager.LoadFilteredProps(_spawnTypeTag), Mathf.CeilToInt(_maxPropCount / 2.0f), Mathf.FloorToInt(_maxPropCount / 2.0f));

        if (env == null) return spawner;

        List<Prop> floorProps = spawner.FloorPrefabs.FindAll(p => env.IgnoreSubElements || env.GetEntry(p.KeyWords, true) != null);
        List<Prop> wallProps = spawner.WallPrefabs.FindAll(p => env.IgnoreSubElements || env.GetEntry(p.KeyWords, true) != null);

        spawner = new Spawner(floorProps, wallProps);
        spawner.maxFloorPropCount = Mathf.CeilToInt(_maxPropCount / 2.0f);
        spawner.maxWallPropCount = Mathf.FloorToInt(_maxPropCount / 2.0f);

        return spawner;
    }

    private bool IsPropContained(Vector3 sample, PropObject obj)
    {
        Vector3 angles = transform.parent ? transform.parent.eulerAngles : Vector3.zero;

        Bounds myBounds = new Bounds(transform.position, _surfaceSize);
        Bounds otherBounds = new Bounds(Misc.RotatePointAroundPivot(sample, transform.position, angles * -1), obj.GetSize);

        return myBounds.Contains(otherBounds.min) && myBounds.Contains(otherBounds.max);
    }
}
