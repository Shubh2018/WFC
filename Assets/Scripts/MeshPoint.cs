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
    private PropHierarchy.PropHierachyInfo _hierarchyInfo;

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.red;

        Gizmos.DrawSphere(Vector3.zero, 0.1f);
        Gizmos.DrawRay(Vector3.zero, transform.TransformDirection(Vector3.up) * .2f);
    }

    public void Init(PropHierarchy.PropHierachyInfo parentHierachyInfo)
    {
        _hierarchyInfo = new PropHierarchy.PropHierachyInfo(parentHierachyInfo.id, parentHierachyInfo.maxHierachyLevel, parentHierachyInfo.currentHierachyLevel);

        Prop.Props.AddEntry(_hierarchyInfo.parentId, _hierarchyInfo.id, gameObject);

        SpawnProp();
    }

    public void Init()
    {
        SpawnProp();
    }

    public void SpawnProp()
    {
        if(!ShouldSpawn()) return;

        Spawner propSpawner = _spawnViaSpawner ? _gameObjectsToSpawn : new Spawner(AssetManager.LoadFilteredProps(_spawnTypeTag), 1, 1);

        List<Prop> allProps = new List<Prop>(propSpawner.WallPrefabs);
        allProps.AddRange(propSpawner.FloorPrefabs);

        while (allProps.Count > 0)
        {
            (Prop prop, int count) = Misc.GetRandomPropCDF(allProps, _hierarchyInfo);

            if (!prop) return;

            List<Sample> samples = new() { new() {
                sample = transform.position,
                triangleNormal = Vector3.up,
            }};

            PropObject propObj = prop.SpawnFloor(samples, gameObject, _hierarchyInfo, (Vector3 sample, Prop prop) => true);
            if (propObj) return;
            allProps.Remove(prop);
        }
    }

    private bool ShouldSpawn()
    {
        WFC wfc = FindFirstObjectByType<WFC>();
        if (!wfc.IsInside(transform.position))
        {
            DestroyImmediate(gameObject);
            return false;
        }
        if (_hierarchyInfo.IsCurrentHierachyLarger()) return false;
        return true;
    }
}
