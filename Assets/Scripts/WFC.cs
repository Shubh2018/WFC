using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Level 
{
    public NodeData[,] _grid;
    public List<Vector2Int> _nodesToCollapse = new List<Vector2Int>(); 
}

public class WFC : MonoBehaviour
{
    [SerializeField] private int _width;
    [SerializeField] private int _height;
    [SerializeField] private int _levels;
    [SerializeField] private List<NodeData> _nodes = new List<NodeData>();
    private Level[] _gridLevels;

    private Vector2Int[] offsets = new Vector2Int[]
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.right,
        Vector2Int.left
    };

    private void Start()
    {
        _gridLevels = new Level[_levels];

        for (int l = 0; l < _levels; l++) {
            _gridLevels[l] = new Level();
            _gridLevels[l]._grid = new NodeData[_width, _height];
        }

        StartCoroutine(CollapseWorld());
    }

    private IEnumerator CollapseWorld()
    {
        for (int l = 0; l < _levels; l++) 
        {
            _gridLevels[l]._nodesToCollapse.Clear();
            _gridLevels[l]._nodesToCollapse.Add(new Vector2Int(0, 0));

            while(_gridLevels[l]._nodesToCollapse.Count > 0)
            {
                int x = _gridLevels[l]._nodesToCollapse[0].x;
                int y = _gridLevels[l]._nodesToCollapse[0].y;

                List<NodeData> potentialNodes = new List<NodeData>(_nodes);

                for(int i = 0; i < offsets.Length; i++)
                {
                    Vector2Int neighbor = new Vector2Int(x + offsets[i].x, y + offsets[i].y);

                    if(CheckGridValidity(neighbor))
                    {
                        NodeData neighborNode = _gridLevels[l]._grid[neighbor.x, neighbor.y];

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
                            if(!_gridLevels[l]._nodesToCollapse.Contains(neighbor)) _gridLevels[l]._nodesToCollapse.Add(neighbor);
                        }
                    }
                }

                if(potentialNodes.Count < 1)
                {
                    _gridLevels[l]._grid[x, y] = _nodes[0];
                    Debug.LogWarning($"Can't Collapse on {x}, {y}");
                }

                else
                {
                    _gridLevels[l]._grid[x, y] = potentialNodes[Random.Range(0, potentialNodes.Count)];
                }

                if(_gridLevels[l]._grid[x, y].TopsidePrefab && (l+1) < _levels) 
                {
                    Debug.Log(_gridLevels[l]._grid[x, y].TopsidePrefab);
                    _gridLevels[l+1]._grid[x, y] = _gridLevels[l]._grid[x, y].TopsidePrefab;
                }

                yield return new WaitForSeconds(0.1f);

                if (_gridLevels[l]._grid[x, y].Prefab) {
                    GameObject node = Instantiate(_gridLevels[l]._grid[x, y].Prefab, new Vector3(x, l * 0.37f, y), Quaternion.identity);
                }

                _gridLevels[l]._nodesToCollapse.RemoveAt(0);
            }
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
        return (pos.x > -1 && pos.x < _width && pos.y > -1 && pos.y < _height);
    }
}