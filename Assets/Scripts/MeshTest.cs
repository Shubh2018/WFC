using UnityEngine;
using System.Collections.Generic;

public class MeshTest : MonoBehaviour
{
    [SerializeField] private GameObject obj;
    private Mesh _mesh;

    private int[] _triangles;
    private Vector3[] _vertices;
    
    List<Vector3> _barycentricCoordinates = new List<Vector3>();

    private void Awake()
    {
        _mesh = obj.GetComponentInChildren<MeshFilter>().sharedMesh;
        
        _triangles = _mesh.triangles;
        _vertices = _mesh.vertices;
    }

    private void Start()
    {
        int triIndex = Random.Range(0, _triangles.Length / 3);

        for (int i = 0; i < _triangles.Length / 3; i++)
        {
            int i0 = _triangles[i * 3 + 0];
            int i1 = _triangles[i * 3 + 1];
            int i2 = _triangles[i * 3 + 2];
        
            Vector3 v0 = _mesh.vertices[i0];
            Vector3 v1 = _mesh.vertices[i1];
            Vector3 v2 = _mesh.vertices[i2];

            float u, v;

            u = Random.value;
            v = Random.value;

            if (u + v > 1)
            {
                u = 1f - u;
                v = 1f - v;
            }
            
            Vector3 p = v0 + u * (v1 - v0) + v * (v2 - v0);

            _barycentricCoordinates.Add(transform.TransformPoint(p));
        }

        /*int i0 = _triangles[triIndex * 3 + 0];
        int i1 = _triangles[triIndex * 3 + 1];
        int i2 = _triangles[triIndex * 3 + 2];
        
        Vector3 v0 = _mesh.vertices[i0];
        Vector3 v1 = _mesh.vertices[i1];
        Vector3 v2 = _mesh.vertices[i2];

        float u, v;

        u = Random.value;
        v = Random.value;

        if (u + v > 1)
        {
            u = 1f - u;
            v = 1f - v;
        }*/
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        
        foreach(var coordinate in _barycentricCoordinates)
            Gizmos.DrawSphere(coordinate, 0.01f);
    }
}
