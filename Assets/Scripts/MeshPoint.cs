using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class MeshPoint : MonoBehaviour
{
    [SerializeField] private bool _forcedToSpawn = true;
    [SerializeField] private bool _spawnViaSpawner = false;
    [SerializeField] private PropSpawnTagEnum _spawnTypeTag;
    [SerializeField] private Spawner _gameObjectsToSpawn;
    [SerializeField] public int _spawnHierarchy = 5;
    private PropObject _spawnedObject;
    private PropHierarchy.PropHierachyInfo _hierarchyInfo;
    private static Spawner? spawner = null;
    private WFC wfc;

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.red;

        Gizmos.DrawSphere(Vector3.zero, 0.1f);
        Gizmos.DrawRay(Vector3.zero, transform.TransformDirection(Vector3.up) * .2f);
    }

    public void Init(PropHierarchy.PropHierachyInfo parentHierachyInfo)
    {
        _hierarchyInfo = new PropHierarchy.PropHierachyInfo(parentHierachyInfo, _spawnHierarchy);

        Prop.Props.AddEntry(_hierarchyInfo.parentId, _hierarchyInfo.id, gameObject);

        wfc = FindFirstObjectByType<WFC>();

        SpawnProp();
    }

    public void Init()
    {
        wfc = FindFirstObjectByType<WFC>();

        SpawnProp();
    }

    public void SpawnProp()
    {
        if (!wfc.IsInside(transform.position))
        {
            DestroyImmediate(gameObject);
            return;
        }
        if (_hierarchyInfo.IsCurrentHierachyLarger()) return;

        Prop prop = ChooseRandomProp();
        if(!_forcedToSpawn && (UnityEngine.Random.Range(0, 1) > prop.SpawnChance)) return;

        Sample sample = new Sample() {
            sample = transform.position,
            triangleNormal = Vector3.up,
        };

        PropObject propObj = prop.SpawnFloor(sample, gameObject, _hierarchyInfo, (Vector3 sample, Prop prop) => true);
    }

    public Prop ChooseRandomProp()
    {
        Spawner propSpawner = _spawnViaSpawner ? _gameObjectsToSpawn : new Spawner(AssetManager.LoadFilteredProps(_spawnTypeTag), 1, 1);

        List<Prop> _allProps = new List<Prop>(propSpawner.WallPrefabs);
        _allProps.AddRange(propSpawner.FloorPrefabs);

        return _allProps[UnityEngine.Random.Range(0, _allProps.Count)];
    }
}
