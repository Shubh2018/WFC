using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapManager : MonoBehaviour
{
    public Tilemap map;
    public TileBase tile;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        map.SetTile(Vector3Int.zero, tile);
    }
}
