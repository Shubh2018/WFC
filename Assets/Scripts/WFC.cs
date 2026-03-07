using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class WFC : MonoBehaviour
{
    [SerializeField] private int _width;
    [SerializeField] private int _height;
    [SerializeField] private List<NodeData> _nodes = new List<NodeData>();
    NodeData[,] _grid;
    List<Vector2Int> _nodesToCollapse = new List<Vector2Int>(); 
    List<GameObject> _tiles = new List<GameObject>();

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

        GenerateTiles();

        StartCoroutine(CollapseWorld());
    }

    // Generate new tiles by creating new ones by rotating the current ones
    public void GenerateTiles()
    {
        int nodes = _nodes.Count;

        // Go through all created nodes and rotate those that need it
        for(int i = 0; i < nodes; i++) 
        {
            NodeData currNode = _nodes[i];
            List<NodeFace.Name> currFaceNames = new List<NodeFace.Name>{ currNode.Back.name, currNode.Right.name, currNode.Front.name, currNode.Left.name };

            // Only rotate nodes that are not symmetrical all the way around (like the crossroad)
            if(currNode.Left.type != NodeFace.Type.None 
            || currNode.Right.type != NodeFace.Type.None
            || currNode.Front.type != NodeFace.Type.None
            || currNode.Back.type != NodeFace.Type.None
            || currFaceNames.Distinct().Skip(1).Any()) 
            {
                // Rotate object clockwise three times
                for(int j = 0; j < 3; j++)
                {
                    // If the tile is symmetrical on two sides, ignore rotation and continue
                    if(j == 1 && currNode.Right.name == currNode.Left.name
                    || j == 2 && currNode.Back.name == currNode.Front.name)
                        continue;

                    // Setup new tile data
                    NodeData newNode = new NodeData();

                    newNode.name = currNode.name + "_" + ((j + 1) * 90);
                    newNode.Prefab = currNode.Prefab;
                    newNode.ClockwiseRotationSteps = j + 1;
                    
                    switch(j)
                    {
                        case 0: // 90 degrees
                            newNode.Back = currNode.Left;
                            newNode.Right = currNode.Back;
                            newNode.Front = currNode.Right;
                            newNode.Left = currNode.Front;
                            break;
                        case 1: // 180 degrees
                            newNode.Back = currNode.Front;
                            newNode.Right = currNode.Left;
                            newNode.Front = currNode.Back;
                            newNode.Left = currNode.Right;
                            break;
                        case 2: // 270 degrees
                            newNode.Back = currNode.Right;
                            newNode.Right = currNode.Front;
                            newNode.Front = currNode.Left;
                            newNode.Left = currNode.Front;
                            break;            
                    }

                    _nodes.Add(newNode);
                    Debug.Log("Added node '" + newNode.name + "'");
                }
            }
        }

        Debug.Log("Nodes Generated: " + (_nodes.Count - nodes));
    }

    public void ClearTiles() 
    {
        _nodesToCollapse.Clear();
        
        foreach(var tile in _tiles)
            DestroyImmediate(tile);
        
        _tiles.Clear();

        Debug.Log("Cleared Tiles...");
    }

    public IEnumerator CollapseWorld()
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

                    if(neighborNode)
                    {
                        switch (i)
                        {
                            case 0: WhittleNodes(potentialNodes, neighborNode.Back, "front");
                                break;
                            case 1: WhittleNodes(potentialNodes, neighborNode.Front, "back");
                                break;
                            case 2: WhittleNodes(potentialNodes, neighborNode.Left, "right");
                                break;
                            case 3: WhittleNodes(potentialNodes, neighborNode.Right, "left");
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
                _grid[x, y] = potentialNodes[UnityEngine.Random.Range(0, potentialNodes.Count)];
            }

            yield return new WaitForSeconds(0.1f);

            int rotationSteps = _grid[x, y].ClockwiseRotationSteps;
            Quaternion rotation = Quaternion.Euler(0, rotationSteps * 90, 0);

            GameObject node = Instantiate(_grid[x, y].Prefab, new Vector3(x, 0, y), rotation);
            _tiles.Add(node);
            _nodesToCollapse.RemoveAt(0);
        }
    }

    /*private Vector3 GetRotPosVec(int posX, int posY, int rotationSteps) {
        int x = Convert.ToInt32(rotationSteps == 3);
        int y = Convert.ToInt32(rotationSteps == 1 || rotationSteps == 2);
        return new Vector3(posX + x, 0, posY - y);
    }*/

    private void WhittleNodes(List<NodeData> potentialNodes, NodeFace validType, string direction)
    {
        for(int i = potentialNodes.Count - 1; i > -1; i--)
        {
            NodeFace nodeType;

            switch(direction) {
                case "left":
                    nodeType = potentialNodes[i].Left;
                    break;
                case "right":
                    nodeType = potentialNodes[i].Right;
                    break;
                case "front":
                    nodeType = potentialNodes[i].Front;
                    break;
                default:
                    nodeType = potentialNodes[i].Back;
                    break;
            }

            if(nodeType.name != validType.name)
                potentialNodes.RemoveAt(i);
        }
    }

    private bool CheckGridValidity(Vector2Int pos)
    {
        return (pos.x > -1 && pos.x < _width && pos.y > -1 && pos.y < _height);
    }
}