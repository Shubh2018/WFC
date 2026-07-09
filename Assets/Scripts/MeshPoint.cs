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

        Prop.Props.AddEntry(_hierarchyInfo.parentId, _hierarchyInfo.id, transform.parent.gameObject);

        SpawnProp();
    }

    public void Init()
    {
        SpawnProp();
    }

    public void SpawnProp()
    {
        if (!Utils.IsInsideMaze(transform.position, Vector3.one)) return;
        if (_hierarchyInfo.IsCurrentHierachyLarger()) return;
        if (!_spawnViaSpawner) spawner = new Spawner(Utils.LoadFilteredProps(_spawnTypeTag), 1, 1);

        Prop propObj = ChooseRandomProp();
        
        float rand = UnityEngine.Random.Range(0, 1);

        BoxCollider col = propObj.PropObject.GetComponent<BoxCollider>();
        Quaternion rotation = Quaternion.Euler(new Vector3(0.0f, UnityEngine.Random.Range(0.0f, 360.0f), 0.0f));
        Vector3 pos = transform.position + transform.up * 0.1f + rotation * col.center;

        bool overlap = propObj.PropObject.CheckOverlapBox(pos, rotation, (List<Collider> cols) => cols.Except(new List<Collider>{ GetComponentInParent<BoxCollider>() }));

        if((!_forcedToSpawn && (rand > propObj.SpawnChance)) || overlap) return;

        _spawnedObject = Instantiate(propObj.PropObject, transform, false);

        _spawnedObject.transform.SetParent(transform);
        _spawnedObject.transform.localPosition = Vector3.zero;
        _spawnedObject.transform.localEulerAngles = new Vector3(0, UnityEngine.Random.Range(0, 360), 0);

        Prop.Props.Increase(_hierarchyInfo.id, propObj.PropObject.name);
        _spawnedObject.UpdateChildren(_hierarchyInfo);
    }

    public Prop ChooseRandomProp()
    {
        Spawner propSpawner = _spawnViaSpawner ? _gameObjectsToSpawn : (Spawner) spawner;

        List<Prop> _allProps = new List<Prop>(propSpawner.WallPrefabs);
        _allProps.AddRange(propSpawner.FloorPrefabs);

        return _allProps[UnityEngine.Random.Range(0, _allProps.Count)];
    }
}
