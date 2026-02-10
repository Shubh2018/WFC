using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WFC : MonoBehaviour
{
    [SerializeField] private int _width;
    [SerializeField] private int _height;
    [SerializeField] private List<NodeData> _nodes = new List<NodeData>();

    private NodeData[,] _grid;

    private List<Vector2Int> _nodesToCollapse = new List<Vector2Int>(); 

    private Vector2Int[] offsets = new Vector2Int[]
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.right,
        Vector2Int.left
    };

    private void Start()
    {
        _grid = new NodeData[_width, _height];

        StartCoroutine(CollapseWorld());
    }

    private IEnumerator CollapseWorld()
    {
        _nodesToCollapse.Clear();

        _nodesToCollapse.Add(new Vector2Int(0, 0));

        while(_nodesToCollapse.Count > 0)
        {
            int x = _nodesToCollapse[0].x;
            int y = _nodesToCollapse[0].y;

            List<NodeData> potentialNodes = new List<NodeData>(_nodes);

            for(int i = 0; i < offsets.Length; i++)
            {
                Vector2Int neighbor = new Vector2Int(x + offsets[i].x, y + offsets[i].y);

                if(CheckGridValidity(neighbor))
                {
                    NodeData neighborNode = _grid[neighbor.x, neighbor.y];

                    if(neighborNode != null)
                    {
                        switch (i)
                        {
                            case 0: WhittleNodes(potentialNodes, neighborNode.Down.CompatibleNeighbors);
                                break;
                            case 1: WhittleNodes(potentialNodes, neighborNode.Up.CompatibleNeighbors);
                                break;
                            case 2: WhittleNodes(potentialNodes, neighborNode.Left.CompatibleNeighbors);
                                break;
                            case 3: WhittleNodes(potentialNodes, neighborNode.Right.CompatibleNeighbors);
                                break;
                        }
                    }

                    else
                    {
                        if(!_nodesToCollapse.Contains(neighbor)) _nodesToCollapse.Add(neighbor);
                    }
                }
            }

            if(potentialNodes.Count < 1)
            {
                _grid[x, y] = _nodes[0];
                Debug.LogWarning($"Can't Collapse on {x}, {y}");
            }

            else
            {
                _grid[x, y] = potentialNodes[Random.Range(0, potentialNodes.Count)];
            }

            yield return new WaitForSeconds(0.1f);

            GameObject node = Instantiate(_grid[x, y].Prefab, new Vector3Int(x, 0, y), Quaternion.identity);
            _nodesToCollapse.RemoveAt(0);
        }
    }

    private void WhittleNodes(List<NodeData> potentialNodes, List<NodeData> validNodes)
    {
        for(int i = potentialNodes.Count - 1; i > -1; i--)
        {
            if(!validNodes.Contains(potentialNodes[i]))
                potentialNodes.RemoveAt(i);
        }
    }

    private bool CheckGridValidity(Vector2Int pos)
    {
        if(pos.x > -1 && pos.x < _width && pos.y > -1 && pos.y < _height) return true;

        return false;
    }
}