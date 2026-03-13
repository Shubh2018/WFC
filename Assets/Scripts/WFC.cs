using UnityEngine;
using System;
using System.Linq;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

public class WFC : MonoBehaviour
{
    [SerializeField] private int _width;
    [SerializeField] private int _length;
    [SerializeField] private int _height;
    [SerializeField] private List<NodeData> _nodes = new List<NodeData>();
    [SerializeField] private List<NodeData> _nodesGenerated = new List<NodeData>();
    NodeData[,,] _grid;
    List<Tile> _nodesToCollapse = new List<Tile>();
    public bool pauseGeneration = false;
    double collapseExecutionTime = 0;
    public float collapseWaitTime = 1.0f;
    Vector3Int activeCollapsningTile;
    IEnumerator collapseTilesRoutine;
    bool doneCollapse = false;


    public int getTiles => transform.childCount;
    public double getCollapseTime => collapseExecutionTime;

    // Represents a tile that needs to be collapsed
    private class Tile
    {
        public Vector3Int pos;
        public List<NodeData> potentialNodes;
        public List<Tile> neighbors;
        public bool shouldBeUpdated;

        public Tile(WFC parent, Vector3Int coord, bool update = false)
        {
            pos = coord;
            neighbors = new List<Tile>();
            shouldBeUpdated = update;

            potentialNodes = new List<NodeData>(parent._nodes);
            potentialNodes.AddRange(parent._nodesGenerated);
        }
    }

    private Vector3Int[] offsets = new Vector3Int[]
    {
        Vector3Int.forward,
        Vector3Int.back,
        Vector3Int.right,
        Vector3Int.left,
        Vector3Int.up,
        Vector3Int.down
    };

    public void StartCollapse(Action doneFuncHook) 
    {
        collapseTilesRoutine = CollapseTiles(doneFuncHook);
        StartCoroutine(collapseTilesRoutine);
    }

    public void StopCollapse() 
    {
        doneCollapse = true;
        if (collapseTilesRoutine != null) StopCoroutine(collapseTilesRoutine);
    }

    public void OnDrawGizmos() {
        Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.1f);
        Gizmos.matrix = transform.localToWorldMatrix;

        for (int i = 0; i < _width; i++)
            for (int k = 0; k < _height; k++)
                for (int j = 0; j < _length; j++)
                    Gizmos.DrawWireCube(new Vector3(i - 0.5f, k + 0.5f, j - 0.5f), Vector3.one);
        
        if (!doneCollapse)
        {
            Gizmos.color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
            Gizmos.DrawWireCube(activeCollapsningTile - new Vector3(0.5f, -0.5f, 0.5f), Vector3.one);
        }
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
                    NodeData newNode = ScriptableObject.CreateInstance<NodeData>();

                    newNode.name = currNode.name + "_" + ((j + 1) * 90);
                    newNode.Prefab = currNode.Prefab;
                    newNode.Weight = currNode.Weight;
                    newNode.ClockwiseRotationSteps = j + 1;

                    newNode.Up = currNode.Up;
                    newNode.Down = currNode.Down;
                    
                    switch(j)
                    {
                        case 0: // 90 degrees
                            RotateNodeVerticalFaces(new NodeFaceVertical[]{ newNode.Up, newNode.Down }, 1);
                            newNode.Back = currNode.Right;
                            newNode.Right = currNode.Front;
                            newNode.Front = currNode.Left;
                            newNode.Left = currNode.Back;
                            break;
                        case 1: // 180 degrees
                            RotateNodeVerticalFaces(new NodeFaceVertical[]{ newNode.Up, newNode.Down }, 2);
                            newNode.Back = currNode.Front;
                            newNode.Right = currNode.Left;
                            newNode.Front = currNode.Back;
                            newNode.Left = currNode.Right;
                            break;
                        case 2: // 270 degrees
                            RotateNodeVerticalFaces(new NodeFaceVertical[]{ newNode.Up, newNode.Down }, 3);
                            newNode.Back = currNode.Left;
                            newNode.Right = currNode.Back;
                            newNode.Front = currNode.Right;
                            newNode.Left = currNode.Front;
                            break;            
                    }

                    _nodesGenerated.Add(newNode);
                    //UnityEngine.Debug.Log($"Added node '{newNode.name}'");
                }
            }
        }

        /*UnityEngine.Debug.Log($"Nodes Generated: {(_nodesGenerated.Count)}");

        // Debug Data
        List<NodeData> potentialNodes = new List<NodeData>(_nodes);
        potentialNodes.AddRange(_nodesGenerated);

        string nodeNames = "";

        for(int i = 0; i < potentialNodes.Count; i++)
            nodeNames += $"\n| tile {i + 1}: {potentialNodes[i].name}";
        
        UnityEngine.Debug.Log($"Total nodes used for future generation: {nodeNames}");*/
    }

    private void RotateNodeVerticalFaces(NodeFaceVertical[] faces, int rotationAmount)
    {
        foreach (NodeFaceVertical face in faces)
            if(!face.invariantRotation)
                face.rotationIndex = rotationAmount;
    }

    public void ClearTiles(bool clearAll = false) 
    {
        _nodesToCollapse.Clear();
        if(clearAll) _nodesGenerated.Clear();

        while (transform.childCount > 0) 
            DestroyImmediate(transform.GetChild(0).gameObject);

        UnityEngine.Debug.Log("Cleared Tiles...");
    }

    public IEnumerator CollapseTiles(Action doneFuncHook)
    {
        StartCollapseLabel:
        ClearTiles();

        Stopwatch st = new Stopwatch();
        st.Start();

        UnityEngine.Debug.Log("Collapse Tiles...");

        doneCollapse = false;
        _grid = new NodeData[_width, _height, _length];

        _nodesToCollapse.Clear();
        _nodesToCollapse.Add(new Tile(this, Vector3Int.zero, true));

        while(_nodesToCollapse.Count > 0)
        {
            // Either pause or stop generation based on value
            yield return new WaitUntil(() => !pauseGeneration);

            int tilesCount = _nodesToCollapse.Count;

            for (int i = 0; i < tilesCount; i++)
                CheckNeighbors(_nodesToCollapse[i]);
            
            int tileChosenIndex = CheckEntropy(tilesCount);
            Tile tile = _nodesToCollapse[tileChosenIndex];

            if(tile.potentialNodes.Count < 1)
            {
                _grid[tile.pos.x, tile.pos.y, tile.pos.z] = _nodes[0];
                UnityEngine.Debug.LogWarning($"Can't Collapse on {tile.pos.x}, {tile.pos.y}, {tile.pos.z}");
                goto StartCollapseLabel; // If the tile cannot be collapsed, start over
            }

            else
            {
                // Choose a tile based on weight
                double[] nodeWeights = CalculateNodesWeights(tile.potentialNodes);
                int chosenTileIdx = ChooseWeightedTile(nodeWeights, new System.Random());

                _grid[tile.pos.x, tile.pos.y, tile.pos.z] = tile.potentialNodes[chosenTileIdx];
            }

            UnityEngine.Debug.Log($"chosen tile: ({tile.pos.x}, {tile.pos.y}, {tile.pos.z}), chosen node: {_grid[tile.pos.x, tile.pos.y, tile.pos.z].name}");
            UnityEngine.Debug.Log($"potential nodes: {string.Join(", ", tile.potentialNodes.Select(n => n.name))}");

            activeCollapsningTile = tile.pos;

            yield return new WaitForSeconds(collapseWaitTime);

            CollapseTile(tile);
            _nodesToCollapse.RemoveAt(tileChosenIndex);
        }

        st.Stop();
        collapseExecutionTime = st.ElapsedMilliseconds;
        doneCollapse = true;
        doneFuncHook();
    }

    private double[] CalculateNodesWeights(List<NodeData> nodes) {
        double[] weights = new double[nodes.Count];
        double totalWeight = nodes.Sum(n => n.Weight);

        int i = 0;
        nodes.ForEach(n => weights[i++] = (n.Weight / totalWeight));

        /*foreach (NodeData node in nodes)
            UnityEngine.Debug.Log($"weight: {node.Weight}");
        
        UnityEngine.Debug.Log($"calculated weights: {string.Join(", ", weights)}");*/

        return weights;
    }

    private int ChooseWeightedTile(double[] weight, System.Random rng) {
        double total = 0;
        double amount = rng.NextDouble();

        for(int a = 0; a < weight.Length; a++){
            total += weight[a];

            //UnityEngine.Debug.Log($"choose weight, total: {total}, rng: {amount}");
            
            if(amount <= total){
                //UnityEngine.Debug.Log($"tile chosen: {a}");
                return a;
            }
        }

        return 0;
    }

    private void CheckNeighbors(Tile tile)
    {
        if(!tile.shouldBeUpdated) return; // No neighbor has been collapsed for this tile, so no need to recheck its options

        for(int i = 0; i < offsets.Length - 2; i++)
        {
            Vector3Int neighbor = new Vector3Int(tile.pos.x + offsets[i].x, tile.pos.y + offsets[i].y, tile.pos.z + offsets[i].z);

            //UnityEngine.Debug.Log($"Checking neighbor ({neighbor.x}, {neighbor.y}, {neighbor.z}), valid: {CheckGridValidity(neighbor)}");

            if(CheckGridValidity(neighbor))
            {
                NodeData neighborNode = _grid[neighbor.x, neighbor.y, neighbor.z];

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
                        Tile neighborTile = new Tile(this, neighbor);

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
        NodeData node = _grid[tile.pos.x, tile.pos.y, tile.pos.z];
        int rotationSteps = node.ClockwiseRotationSteps;

        // Make sure that this tile's neighbors get marked to get updated
        foreach (Tile t in tile.neighbors)
            t.shouldBeUpdated = true;
        
        //UnityEngine.Debug.Log($"collapsing tile ({tile.pos.x}, {tile.pos.y}, {tile.pos.z})");

        // Instantiate the tile
        GameObject obj = Instantiate(node.Prefab, GetRotPosVec(tile.pos, rotationSteps), Quaternion.Euler(0, rotationSteps * 90, 0));
        obj.name = node.name; // Rename the node so we know what type has been spawned
        obj.transform.parent = gameObject.transform; // Set this object as parent for editor readability
    }

    private Vector3 GetRotPosVec(Vector3Int pos, int rotationSteps) => pos - rotationSteps switch {
        1 => new Vector3(0, 0, 1),
        2 => new Vector3(1, 0, 1),
        3 => new Vector3(1, 0, 0),
        _ => Vector3Int.zero
    };

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

    private bool CheckGridValidity(Vector3Int pos) =>
           pos.x > -1 && pos.x < _width 
        && pos.y > -1 && pos.y < _height 
        && pos.z > -1 && pos.z < _length;
}