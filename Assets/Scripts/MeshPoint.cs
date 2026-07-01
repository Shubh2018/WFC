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
    private int _currentHierachyLevel = 0;
    private Guid _id;
    private Guid _parentId;
    private static Spawner? spawner = null;

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.red;

        Gizmos.DrawSphere(Vector3.zero, 0.1f);
        Gizmos.DrawRay(Vector3.zero, transform.TransformDirection(Vector3.up) * .2f);

        /* todo:
            - (X) add prop spawning hierarchy
            - (X) add prop categories for what *type* of object to spawn instead
            - (X) check for size of the object to prevent instantiating too large objects
            - (X) check for obstacles and prevent spawning if blocked
            - Fix bugs:
             > (X) small empty tile node
             > (X) fix points not sorted correctly
             > (X) prop overlapping not fixed
             > (X) max object count per room is not transfered to MeshSurface or MeshPoint
             > check tile node collision to prevent spawning props (right now only props are checked)
             > smapler not spawning enough props as it is done randomly
        */
    }

    public void Init(Guid parentId, int maxHierarchyLevel, int currentLevel)
    {
        _currentHierachyLevel = currentLevel;
        _spawnHierarchy = Mathf.Min(maxHierarchyLevel, _spawnHierarchy);
        _id = Guid.NewGuid();
        _parentId = parentId;

        Prop.Props.AddEntry(_parentId, _id, transform.parent.gameObject);

        SpawnProp();
    }

    public void Init()
    {
        SpawnProp();
    }

    public void SpawnProp()
    {
        if (_currentHierachyLevel > _spawnHierarchy) return;
        if (!_spawnViaSpawner) spawner = new Spawner(Utils.LoadFilteredProps(_spawnTypeTag), 1, 1);

        Prop propObj = ChooseRandomProp();
        float rand = UnityEngine.Random.Range(0, 1);
        bool overlap = propObj.PropObject.CheckOverlapBox(transform.position, transform.rotation, (List<Collider> cols) => cols.Except(new List<Collider>{ GetComponentInParent<BoxCollider>() }));

        Debug.Log($"hierarchy: {_currentHierachyLevel}/{_spawnHierarchy}, prop: {propObj.name}, spawn chance: {propObj.SpawnChance}, random chance: {rand}, forced to spawn: {_forcedToSpawn}, collide: {overlap}");

        if((!_forcedToSpawn && (rand > propObj.SpawnChance)) || overlap) return;

        _spawnedObject = Instantiate(propObj, transform, false).PropObject;

        _spawnedObject.transform.localPosition = Vector3.zero;
        _spawnedObject.transform.localEulerAngles = new Vector3(0, UnityEngine.Random.Range(0, 360), 0);
        _spawnedObject.UpdateChildren(_id, _spawnHierarchy, _currentHierachyLevel+1);
    }

    public Prop ChooseRandomProp()
    {
        Spawner propSpawner = _spawnViaSpawner ? _gameObjectsToSpawn : (Spawner) spawner;

        List<Prop> _allProps = new List<Prop>(propSpawner.WallPrefabs);
        _allProps.AddRange(propSpawner.FloorPrefabs);

        return _allProps[UnityEngine.Random.Range(0, _allProps.Count)];
    }
}
