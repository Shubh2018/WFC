using UnityEngine;
using System.Linq;

public class ColDetectorTest : MonoBehaviour
{
    Vector3 _pos, _size;
    
    void OnDrawGizmos()
    {
        bool overlap = CheckOverlapBox(transform.position, transform.rotation);
        if (overlap) Debug.Log("COLLISION!");

        BoxCollider col = GetComponent<BoxCollider>();

        Gizmos.color = Color.red;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawSphere(Vector3.zero + col.center, 0.1f);
        Gizmos.DrawWireCube(Vector3.zero + col.center, col.size);
    }

    public bool CheckOverlapBox(Vector3 pos, Quaternion rot)
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null || !col.enabled) return false;
        Debug.Log($"pos: {transform.TransformPoint(col.center)}, {pos + col.center}, rot: {transform.eulerAngles}, size: {col.size}");

        _pos = col.center;
        _size = col.size;

        return Physics.OverlapBox(pos + col.center, col.size / 2, rot, LayerMask.GetMask("Prop", "Node")).Count() > 0;
    }
}
