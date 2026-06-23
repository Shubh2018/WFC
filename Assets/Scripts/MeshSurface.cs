using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System;

public class MeshSurface : MonoBehaviour
{
    private MeshSampler _meshSampler;
    private BoxCollider _boxCollider;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private List<Sample> _samples;
    [SerializeField] private bool _spawnViaSpawner = false;
    [SerializeField] private PropSpawnTagEnum _spawnTypeTag;
    [SerializeField] private Spawner _gameObjectsToSpawn;
    [SerializeField] private Vector3 _surfaceSize = Vector3.one;
    [SerializeField] public int _spawnHierarchy = 5;
    private int _currentHierachyLevel = 0;
    private static Spawner? spawner = null;
    private Guid _id;
    private Guid _parentId;

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

    public void Init(Guid parentId, int maxHierarchyLevel, int currentLevel)
    {
        OnValidate();

        _meshSampler = gameObject.GetComponent<MeshSampler>();
        _boxCollider = gameObject.GetComponent<BoxCollider>();
        _meshFilter = gameObject.GetComponent<MeshFilter>();
        _meshRenderer = gameObject.GetComponent<MeshRenderer>();

        _meshFilter.sharedMesh = Utils.CreatePlaneMesh(_surfaceSize / 2);
        _meshRenderer.material.color = Color.grey;

        _currentHierachyLevel = currentLevel;
        _spawnHierarchy = Mathf.Min(maxHierarchyLevel, _spawnHierarchy);
        _id = Guid.NewGuid();
        _parentId = parentId;

        Debug.Log($"id: {_id}, parent: {_parentId}");

        PropData.Props.AddEntry(_parentId, _id, transform.parent?.gameObject);

        Generate();
    }

    private void Generate()
    {
        if (_currentHierachyLevel > _spawnHierarchy) return;
        
        _meshSampler.SetRadiusAndTries(0.25f, 1);
        _meshSampler.SetParent(_id);

        _samples = _meshSampler.GetSamples(_meshFilter);

        _meshSampler.Clear();
        _meshSampler.AddSamples(_samples);

        if (!_spawnViaSpawner) spawner = Utils.LoadProps("Assets/Scripts/Props/", (PropData prop) => prop.SpawnTag == _spawnTypeTag);

        _meshSampler.SetSpawnerData(_spawnViaSpawner ? _gameObjectsToSpawn : (Spawner) spawner, _spawnHierarchy, _currentHierachyLevel);
        _meshSampler.SpawnProps(gameObject, false, (Vector3 sample, PropData prop, bool propType) => IsPropContained(sample, prop.Prop.GetComponent<PropObject>()));
    }

    private bool IsPropContained(Vector3 sample, PropObject obj)
    {
        Vector3 angles = transform.parent ? transform.parent.eulerAngles : Vector3.zero;

        Bounds myBounds = new Bounds(transform.position, _surfaceSize);
        Bounds otherBounds = new Bounds(Utils.RotatePointAroundPivot(sample, transform.position, angles * -1), obj.OverlapDimenstions);

        return myBounds.Contains(otherBounds.min) && myBounds.Contains(otherBounds.max);
    }
}
