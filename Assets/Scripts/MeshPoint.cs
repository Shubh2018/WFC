using UnityEngine;

[RequireComponent(typeof(MeshSampler))]
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
        _hierarchyInfo = new PropHierarchy.PropHierachyInfo(parentHierachyInfo, _spawnHierarchy);

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

        MeshSampler sampler = GetComponent<MeshSampler>();

        sampler.Clear();
        sampler.SetSpawnerData(_hierarchyInfo);
        sampler.SetSamplingGraphProperties(0.25f, 1, 10000, _forcedToSpawn);
        sampler.AddSamples(new() { new() {
            sample = transform.position,
            triangleNormal = Vector3.up,
        }});

        sampler.SpawnProps(gameObject, GetSpawner());
    }

    private Spawner GetSpawner()
    {
        return _spawnViaSpawner ? _gameObjectsToSpawn : new Spawner(AssetManager.LoadFilteredProps(_spawnTypeTag), 1, 1);
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
