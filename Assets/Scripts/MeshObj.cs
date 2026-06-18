using UnityEngine;
using System;
using System.Collections.Generic;

public class MeshObj : MonoBehaviour
{
    [SerializeField] protected bool spawnViaSpawner = false;
    [SerializeField] protected PropSpawnTagEnum _spawnTypeTag;
    [SerializeField] protected Spawner _gameObjectsToSpawn;
    [SerializeField] protected int spawnHierarchy = 5;
    protected int currentHierachyLevel = 0;
    protected Spawner? spawner = null;
    protected Guid id;
    protected Guid parentId;
}