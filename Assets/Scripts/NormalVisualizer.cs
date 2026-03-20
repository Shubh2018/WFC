using System;
using UnityEngine;

public class NormalVisualizer : MonoBehaviour
{
    private MeshFilter _meshFilter;

    private void Start()
    {
        _meshFilter = GetComponent<MeshFilter>();
    }

    private void OnDrawGizmos()
    {
        Vector3[] vertices = _meshFilter.sharedMesh.vertices;
        Vector3[] normals = _meshFilter.sharedMesh.normals;

        for (int i = 0; i < vertices.Length; i++)
        {
            Gizmos.DrawRay(vertices[i], normals[i]);
        }
    }
}
