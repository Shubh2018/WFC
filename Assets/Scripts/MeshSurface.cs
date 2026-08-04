using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(MeshSampler))]
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

        _hierarchyInfo = new PropHierarchy.PropHierachyInfo(parentHierachyInfo, _spawnHierarchy);

        if (_hierarchyInfo.IsCurrentHierachyLarger()) return;

        Prop.Props.AddEntry(_hierarchyInfo.parentId, _hierarchyInfo.id, gameObject);

        Generate();
    }

    private void Generate()
    {
        _meshSampler.Clear();
        _meshSampler.SetSpawnerData(_hierarchyInfo);
        _meshSampler.SetSamplingGraphProperties(0.25f, 1, 10000);
        _meshSampler.AddSamples(new(_meshSampler.GetSamples(_meshFilter)));

        _meshSampler.SpawnProps(gameObject, GetSpawner(), (sample, prop) => IsPropContained(sample, prop.PropObject));
    }

    // Used to filter out props that spawn on this surface based on environmental settings
    private Spawner GetSpawner()
    {
        int maxFloorProps = Mathf.CeilToInt(_maxPropCount / 2.0f);
        int maxWallProps = Mathf.FloorToInt(_maxPropCount / 2.0f);

        MeshNode mesh = Prop.Props?.GetParentRoomNode(_hierarchyInfo.id);
        Environment env = mesh?.GetEnvironment;
        Spawner spawner = _spawnViaSpawner ? _gameObjectsToSpawn : new Spawner(AssetManager.LoadFilteredProps(_spawnTypeTag), maxFloorProps, maxWallProps);

        if (env == null) return spawner;

        List<Prop> floorProps = spawner.FloorPrefabs.FindAll(p => env.IgnoreSubElements || env.GetEntry(p.KeyWords, true).keywords.Length > 0);
        List<Prop> wallProps = spawner.WallPrefabs.FindAll(p => env.IgnoreSubElements || env.GetEntry(p.KeyWords, true).keywords.Length > 0);

        spawner = new Spawner(floorProps, wallProps);
        spawner.maxFloorPropCount = maxFloorProps;
        spawner.maxWallPropCount = maxWallProps;

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
