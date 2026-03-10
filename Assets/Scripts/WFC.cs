using UnityEngine;
using System;
using System.Linq;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

public class WFC : MonoBehaviour
{
    [SerializeField] private int _width;
    [SerializeField] private int _height;
    [SerializeField] private List<NodeData> _nodes = new List<NodeData>();
    [SerializeField] private List<NodeData> _nodesGenerated = new List<NodeData>();
    NodeData[,] _grid;
    List<Tile> _nodesToCollapse = new List<Tile>();
    double collapseExecutionTime = 0;

    public int getTiles => transform.childCount;
    public double getCollapseTime => collapseExecutionTime;

    // Represents a tile that needs to be collapsed
    private class Tile
    {
        public Vector2Int pos;
        public List<NodeData> potentialNodes;
        public List<Tile> neighbors;
        public bool shouldBeUpdated;

        public Tile(WFC parent, int x, int y, bool update = false)
        {
            pos = new Vector2Int(x, y);
            neighbors = new List<Tile>();
            shouldBeUpdated = update;

            potentialNodes = new List<NodeData>(parent._nodes);
            potentialNodes.AddRange(parent._nodesGenerated);
        }
    }

    private Vector2Int[] offsets = new Vector2Int[]
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.right,
        Vector2Int.left
    };

    public void OnDrawGizmos() {
        Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.1f);
        Gizmos.matrix = transform.localToWorldMatrix;

        for (int i = 0; i < _width; i++)
            for (int j = 0; j < _height; j++)
                Gizmos.DrawWireCube(new Vector3(i - 0.5f, 0, j - 0.5f), Vector3.one);
    }

    // Generate new tiles by creating new ones by rotating the current ones
    public void GenerateTiles()
    {
        _nodesGenerated.Clear();

        int nodes = _nodes.Count;

        // Go through all created nodes and rotate those that need it
        for(int i = 0; i < nodes; i++) 
        {
            NodeData currNode = _nodes[i];
            List<NodeFaceHorizontal.Name> currFaceNames = new List<NodeFaceHorizontal.Name>{ currNode.Back.name, currNode.Right.name, currNode.Front.name, currNode.Left.name };

            // Only rotate nodes that are not symmetrical all the way around (like the crossroad)
            if(currNode.Left.type != NodeFaceHorizontal.Type.None 
            || currNode.Right.type != NodeFaceHorizontal.Type.None
            || currNode.Front.type != NodeFaceHorizontal.Type.None
            || currNode.Back.type != NodeFaceHorizontal.Type.None
            || currFaceNames.Distinct().Skip(1).Any()) 
            {
                // Rotate object clockwise a maximum of three times
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
                    newNode.Weight = currNode.Weight;
                    newNode.ClockwiseRotationSteps = j + 1;
                    
                    switch(j)
                    {
                        case 0: // 90 degrees
                            newNode.Back = currNode.Right;
                            newNode.Right = currNode.Front;
                            newNode.Front = currNode.Left;
                            newNode.Left = currNode.Back;
                            break;
                        case 1: // 180 degrees
                            newNode.Back = currNode.Front;
                            newNode.Right = currNode.Left;
                            newNode.Front = currNode.Back;
                            newNode.Left = currNode.Right;
                            break;
                        case 2: // 270 degrees
                            newNode.Back = currNode.Left;
                            newNode.Right = currNode.Back;
                            newNode.Front = currNode.Right;
                            newNode.Left = currNode.Front;
                            break;            
                    }

                    _nodesGenerated.Add(newNode);
                    UnityEngine.Debug.Log($"Added node '{newNode.name}'");
                }
            }
        }

        UnityEngine.Debug.Log($"Nodes Generated: {(_nodesGenerated.Count)}");

        // Debug Data
        List<NodeData> potentialNodes = new List<NodeData>(_nodes);
        potentialNodes.AddRange(_nodesGenerated);

        string nodeNames = "";

        for(int i = 0; i < potentialNodes.Count; i++)
            nodeNames += $"\n| tile {i + 1}: {potentialNodes[i].name}";
        
        UnityEngine.Debug.Log($"Total nodes used for future generation: {nodeNames}");
    }

    public void ClearTiles(bool clearAll = false) 
    {
        _nodesToCollapse.Clear();
        if(clearAll) _nodesGenerated.Clear();

        while (transform.childCount > 0) 
            DestroyImmediate(transform.GetChild(0).gameObject);

        UnityEngine.Debug.Log("Cleared Tiles...");
    }

    public void CollapseTiles()
    {
        StartCollapseLabel:
        ClearTiles();

        Stopwatch st = new Stopwatch();
        st.Start();

        UnityEngine.Debug.Log("Collapse Tiles...");

        _grid = new NodeData[_width, _height];

        _nodesToCollapse.Clear();
        _nodesToCollapse.Add(new Tile(this, 0, 0, true));

        while(_nodesToCollapse.Count > 0)
        {
            int tilesCount = _nodesToCollapse.Count;

            for (int i = 0; i < tilesCount; i++)
                CheckNeighbors(_nodesToCollapse[i]);
            
            int tileChosenIndex = CheckEntropy(tilesCount);
            Tile tile = _nodesToCollapse[tileChosenIndex];

            if(tile.potentialNodes.Count < 1)
            {
                _grid[tile.pos.x, tile.pos.y] = _nodes[0];
                UnityEngine.Debug.LogWarning($"Can't Collapse on {tile.pos.x}, {tile.pos.y}");
                goto StartCollapseLabel; // If the tile cannot be collapsed, start over
            }

            else
            {
                // Choose a tile based on weight
                double[] nodeWeights = CalculateNodesWeights(tile.potentialNodes);
                int chosenTileIdx = ChooseWeightedTile(nodeWeights, new System.Random());

                _grid[tile.pos.x, tile.pos.y] = tile.potentialNodes[chosenTileIdx];
            }

            CollapseTile(tile);
            _nodesToCollapse.RemoveAt(tileChosenIndex);
        }

        st.Stop();
        collapseExecutionTime = st.ElapsedMilliseconds;
    }

    private double[] CalculateNodesWeights(List<NodeData> nodes) {
        double[] weights = new double[nodes.Count];
        double totalWeight = nodes.Sum(n => n.Weight);

        int i = 0;
        nodes.ForEach(n => weights[i++] = (n.Weight / totalWeight));

        foreach (NodeData node in nodes)
            UnityEngine.Debug.Log($"weight: {node.Weight}");
        
        UnityEngine.Debug.Log($"calculated weights: {string.Join(", ", weights)}");

        return weights;
    }

    private int ChooseWeightedTile(double[] weight, System.Random rng) {
        double total = 0;
        double amount = rng.NextDouble();

        for(int a = 0; a < weight.Length; a++){
            total += weight[a];

            UnityEngine.Debug.Log($"choose weight, total: {total}, rng: {amount}");
            
            if(amount <= total){
                UnityEngine.Debug.Log($"tile chosen: {a}");
                return a;
            }
        }

        return 0;
    }

    private void CheckNeighbors(Tile tile)
    {
        if(!tile.shouldBeUpdated) return; // No neighbor has been collapsed for this tile, to no need to recheck its options

        for(int i = 0; i < offsets.Length; i++)
        {
            Vector2Int neighbor = new Vector2Int(tile.pos.x + offsets[i].x, tile.pos.y + offsets[i].y);

            if(CheckGridValidity(neighbor))
            {
                NodeData neighborNode = _grid[neighbor.x, neighbor.y];

                if(neighborNode)
                {
                    switch (i)
                    {
                        case 0: WhittleNodes(tile.potentialNodes, neighborNode.Back, "front");
                            break;
                        case 1: WhittleNodes(tile.potentialNodes, neighborNode.Front, "back");
                            break;
                        case 2: WhittleNodes(tile.potentialNodes, neighborNode.Left, "right");
                            break;
                        case 3: WhittleNodes(tile.potentialNodes, neighborNode.Right, "left");
                            break;
                    }
                }

                else
                {
                    if(!_nodesToCollapse.Any(n => n.pos == neighbor)) {
                        Tile neighborTile = new Tile(this, neighbor.x, neighbor.y);

                        _nodesToCollapse.Add(neighborTile);
                        tile.neighbors.Add(neighborTile);
                    }
                }
            }
        }
    }

    private int CheckEntropy(int tilesCount)
    {
        int idx = 0;

        for (int i = 0; i < tilesCount; i++)
            if (_nodesToCollapse[i].potentialNodes.Count < _nodesToCollapse[idx].potentialNodes.Count)
                idx = i; // Choose the tile with the least amount of options
        
        return idx;
    }

    private void CollapseTile(Tile tile)
    {
        int x = tile.pos.x;
        int y = tile.pos.y;

        NodeData node = _grid[x, y];
        int rotationSteps = node.ClockwiseRotationSteps;

        // Make sure that this tile's neighbors get marked to get updated
        foreach (Tile t in tile.neighbors)
            t.shouldBeUpdated = true;

        // Instantiate the tile
        GameObject obj = Instantiate(node.Prefab, GetRotPosVec(x, y, rotationSteps), Quaternion.Euler(0, rotationSteps * 90, 0));
        obj.name = node.name; // Rename the node so we know what type has been spawned
        obj.transform.parent = gameObject.transform; // Set this object as parent for editor readability
    }

    private Vector3 GetRotPosVec(int posX, int posY, int rotationSteps) 
    {
        switch(rotationSteps)
        {
            case 1:
                return new Vector3(posX, 0, posY - 1);
            case 2:
                return new Vector3(posX - 1, 0, posY - 1);
            case 3:
                return new Vector3(posX - 1, 0, posY);
            default:
                return new Vector3(posX, 0, posY);
        }
    }

    private void WhittleNodes(List<NodeData> potentialNodes, NodeFaceHorizontal validType, string direction)
    {
        for(int i = potentialNodes.Count - 1; i > -1; i--)
        {
            NodeFaceHorizontal nodeType;

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

            // Horizontal tile faces only fit together if:
            // - The face names match and either:
            // > they are both symmetrical
            // > or one face is original and the other is flipped
            if (nodeType.name == validType.name
            && (nodeType.symmetry && validType.symmetry 
            || (nodeType.type == NodeFaceHorizontal.Type.Flipped && validType.type == NodeFaceHorizontal.Type.Original 
            || nodeType.type == NodeFaceHorizontal.Type.Original && validType.type == NodeFaceHorizontal.Type.Flipped)
            ))
                continue;

            potentialNodes.RemoveAt(i);
        }
    }

    private bool CheckGridValidity(Vector2Int pos)
    {
        return (pos.x > -1 && pos.x < _width && pos.y > -1 && pos.y < _height);
    }
}