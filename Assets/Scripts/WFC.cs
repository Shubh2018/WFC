using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

// Represents a tile that needs to be collapsed
public class Tile
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

        potentialNodes = new List<NodeData>(parent.getNodes);
        potentialNodes.AddRange(parent.getNodesGen);
        potentialNodes = potentialNodes.Where(node => !node.name.Contains("Staircase")).ToList();
    }
}

public class PathNode
{
    public PathNodeData data = new PathNodeData();
    public List<int> pathIndicies = new List<int>();
    public WFC parent;

    public PathNode(WFC parent)
    {
        this.parent = parent; // Used to reference the AStar path list
    }

    public bool CheckContainsPos(int index)
    {
        // If this object contains no indicies yet
        if (pathIndicies.Count == 0) return false;

        Vector3Int a = Vector3Int.FloorToInt(parent.path.CollapsedPath[index]);
        Vector3Int b = Vector3Int.FloorToInt(parent.path.CollapsedPath[pathIndicies[0]]);

        // If the first and provides index are at the same coordinate
        if (a.x == b.x && a.y == b.y && a.z == b.z) return true;

        // If their coordinates are different
        return false;
    }

    public void AddPath(int index)
    {
        List<Vector3> path = parent.path.CollapsedPath; // The list of path points

        if (path.Count == 0 || index < 0 || index >= path.Count) return; // check if this index is invalid

        Vector3 temp = new Vector3(-999, -999, -999); // temp vector to check for invalid ngihbours
        Vector3 currPath = path[index]; // get the current path vector
        Vector3[] relationship = new Vector3[2] { temp, temp }; // setup relationship between the previous and next path positions

        // Make an array of path coordinates
        if (index > 0) relationship[0] = path[index - 1];
        if (index < (path.Count - 1)) relationship[1] = path[index + 1];

        // Go through the neighboors and compare their coordinates with the one in the middle
        foreach (Vector3 neighbor in new[]{ relationship[0], relationship[1] })
        {
            if (neighbor.x <= -999) continue; // No neighbour in this direction, ignore
            if (neighbor.y != currPath.y) continue; // The neighbor is on another level, ignore

            Vector3 delta = (currPath - neighbor).normalized; // Find the delta value
            Vector3 cross = Vector3.Cross(Vector3.up, delta); // Calculate the cross product

            // Edge case regarding the height of the node
            //if (currPath.y < neighbor.y) cross += Vector3.up;
            //else if (currPath.y > neighbor.y) cross += Vector3.down;

            // Update the face type depending on how the vectors are facing eachother
            if (cross.x == -1) data.Front = NodeFace.Name.Path;
            if (cross.x == 1) data.Back = NodeFace.Name.Path;
            //if (cross.y == 1) data.Up = NodeFace.Name.Path;
            //if (cross.y == -1) data.Down = NodeFace.Name.Path;
            if (cross.z == 1) data.Right = NodeFace.Name.Path;
            if (cross.z == -1) data.Left = NodeFace.Name.Path;
        }

        // Only add the index if it matches the vector and it is not already there
        // Used in case the same point exists multiple times but with different neighbours
        if (!pathIndicies.Contains(index)) pathIndicies.Add(index);
    }

    private NodeData FindStairCaseNode(List<NodeData> nodes)
    {
        StairCase stairs = parent.path.GetStaircase(pathIndicies[0]);
        Vector3Int pos = Vector3Int.FloorToInt(parent.path.CollapsedPath[pathIndicies[0]]);
        string name = "";

        if (Utils.VecCmp(stairs.bottomEntrance, pos, 0.5f)) name = "StaircaseEnd";
        else if (Utils.VecCmp(stairs.bottomStairs, pos, 0.5f)) name = "StaircaseFront";
        else if (Utils.VecCmp(stairs.topExit, pos, 0.5f)) name = "StaircaseTopFront";
        else if (Utils.VecCmp(stairs.topCorner, pos, 0.5f)) name = "StaircaseTopEnd";

        name = stairs.rotation > 0 ? $"{name}_{stairs.rotation * 90}" : name;

        UnityEngine.Debug.Log($"staircase: {pos}, name: {name}");

        return nodes.Find((NodeData node) => node.name == name);
    }

    public List<NodeData> GetPotentialNodes()
    {
        List<NodeData> potentialNodes = new List<NodeData>(parent.getNodes);
        potentialNodes.AddRange(parent.getNodesGen);

        // Check if this node is part of a staircase
        if (parent.path.CheckStaircaseOverlap(Vector3Int.FloorToInt(parent.path.CollapsedPath[pathIndicies[0]]))) 
            return new List<NodeData> { FindStairCaseNode(potentialNodes) };
        
        // Since this node is not a staircase, filter out any staircase nodes
        potentialNodes = potentialNodes.Where(node => !node.name.Contains("Staircase")).ToList();

        UnityEngine.Debug.Log($"coords: {String.Join(", ", pathIndicies)}");
        UnityEngine.Debug.Log($"potential nodes before: {potentialNodes.Count()}");

        for (int i = potentialNodes.Count - 1; i >= 0; i--)
        {
            NodeData node = potentialNodes[i];

            if (data.Left != NodeFace.Name.None && node.Left.name != data.Left
            || data.Right != NodeFace.Name.None && node.Right.name != data.Right
            || data.Front != NodeFace.Name.None && node.Front.name != data.Front
            || data.Back != NodeFace.Name.None && node.Back.name != data.Back
            || data.Up != NodeFace.Name.None && node.Up.name != data.Up
            || data.Down != NodeFace.Name.None && node.Down.name != data.Down)
                potentialNodes.RemoveAt(i);
        }

        UnityEngine.Debug.Log($"potential nodes after: {potentialNodes.Count()}");

        return potentialNodes;
    }
}

public class WFC : MonoBehaviour
{
    // Serilised Fields
    [SerializeField] private int _width;
    [SerializeField] private int _length;
    [SerializeField] private int _height;
    [SerializeField] private List<NodeData> _nodes = new List<NodeData>();
    [SerializeField] private List<NodeData> _nodesGenerated = new List<NodeData>();
    [SerializeField] private List<Vector3Int> _pathPoints = new List<Vector3Int>();

    // Private Variables
    NodeData[,,] _grid;
    List<Tile> _nodesToCollapse = new List<Tile>();
    List<PathNode> pathNodes = new List<PathNode>();
    double collapseExecutionTime = 0;
    public float collapseWaitTime = 1.0f;
    Vector3Int activeCollapsningTile;
    IEnumerator collapseTilesRoutine;
    bool doneCollapse = false;
    bool doneGeneratingPath = false;

    // Public Variables
    public AStar path;
    public bool pauseGeneration = false;

    // Getters
    public int getTiles => transform.childCount;
    public double getCollapseTime => collapseExecutionTime;
    public int getWidth => _width;
    public int getHeight => _height;
    public int getLength => _length;
    public List<NodeData> getNodes => _nodes;
    public List<NodeData> getNodesGen => _nodesGenerated;

    // Gizmos Debug Settings
    public bool enableGizmosFacesText = false;
    public bool enableGizmosGrid = false;
    public bool enableGizmosCoords = false;
    public bool enableGizmosPathRouting = false;
    public bool enableGizmosPathPoints = false;
    public bool enableGizmosNodeName = false;

    public void StartFindPath()
    {
        if ((path = gameObject.GetComponent<AStar>()) && path == null) 
            path = gameObject.AddComponent<AStar>();

        doneGeneratingPath = false;
        path.GeneratePath(this, _pathPoints);

        StartCoroutine(GeneratePathNodes());
    }

    public void StopFindPath() => path.StopFindingPath();
    public void ClearPath() {
        path.StopFindingPath();
        path.ClearPath();
    }

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

    public void SavePathSettings(bool pathState, bool pathPointsState, bool pathStaircases, bool pathField, bool pathFinding, bool pathDelay)
    {
        if (path) {
            path.enableGizmosPathPoints = pathPointsState;
            path.enableGizmosPathStaircases = pathStaircases;
            path.enableGizmosPathField = pathField;
            path.enableGizmosPathFinding = pathFinding;
            path.enableGizmosGenerationDelay = pathDelay;
        }

        LineRenderer lr = gameObject.GetComponent<LineRenderer>();
        if (lr) lr.enabled = pathState;
    }

    public void OnDrawGizmos() {
        Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.1f);
        Gizmos.matrix = transform.localToWorldMatrix;

        if (enableGizmosGrid || enableGizmosCoords)
        {
            for (int i = 0; i < _width; i++)
            {
                for (int k = 0; k < _height; k++)
                {
                    for (int j = 0; j < _length; j++)
                    {
                        if (enableGizmosGrid) Gizmos.DrawWireCube(new Vector3(i, k + 0.5f, j), Vector3.one);
                        if (enableGizmosCoords) Handles.Label(new Vector3(i - 0.5f, k ,j - 0.5f) + transform.position, $"({i}, {j}, {k})");
                    }
                }
            }
            
            if (!doneCollapse)
            {
                Gizmos.color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                Gizmos.DrawWireCube(activeCollapsningTile - new Vector3(0.0f, -0.5f, 0.0f), Vector3.one);
            }
        }

        if (enableGizmosPathPoints)
        {
            foreach (Vector3Int point in _pathPoints)
            {
                Gizmos.color = Color.orange;
                Gizmos.DrawSphere(point, 0.1f);
            }
        }

        if (enableGizmosPathRouting)
        {
            foreach (PathNode point in pathNodes)
            {
                foreach (int index in point.pathIndicies)
                {
                    if (index >= path.CollapsedPath.Count) continue;

                    Gizmos.color = point.data.Up != NodeFace.Name.None ? Color.green : Color.red;
                    Gizmos.DrawLine(path.CollapsedPath[index] + new Vector3(0.49f, 0.0f, 0.49f), path.CollapsedPath[index] + new Vector3(-0.49f, 0.0f, -0.49f)); // Up

                    Gizmos.color = point.data.Down != NodeFace.Name.None ? Color.green : Color.red;
                    Gizmos.DrawLine(path.CollapsedPath[index] + new Vector3(-0.49f, 0.0f, 0.49f), path.CollapsedPath[index] + new Vector3(0.49f, 0.0f, -0.49f)); // Down

                    Gizmos.color = point.data.Front != NodeFace.Name.None ? Color.green : Color.red;
                    Gizmos.DrawLine(path.CollapsedPath[index] + new Vector3(0.49f, 0.0f, 0.49f), path.CollapsedPath[index] + new Vector3(-0.49f, 0.0f, 0.49f)); // Left

                    Gizmos.color = point.data.Back != NodeFace.Name.None ? Color.green : Color.red;
                    Gizmos.DrawLine(path.CollapsedPath[index] + new Vector3(0.49f, 0.0f, -0.49f), path.CollapsedPath[index] + new Vector3(-0.49f, 0.0f, -0.49f)); // Right

                    Gizmos.color = point.data.Left != NodeFace.Name.None ? Color.green : Color.red;
                    Gizmos.DrawLine(path.CollapsedPath[index] + new Vector3(-0.49f, 0.0f, 0.49f), path.CollapsedPath[index] + new Vector3(-0.49f, 0.0f, -0.49f)); // Forward

                    Gizmos.color = point.data.Right != NodeFace.Name.None ? Color.green : Color.red;
                    Gizmos.DrawLine(path.CollapsedPath[index] + new Vector3(0.49f, 0.0f, 0.49f), path.CollapsedPath[index] + new Vector3(0.49f, 0.0f, -0.49f)); // Back
                }
            }
        }

        if (_grid != null && (enableGizmosFacesText || enableGizmosNodeName))
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    for (int z = 0; z < _length; z++)
                    {
                        if (!_grid[x, y, z]) continue;
                        NodeData node = _grid[x, y, z];

                        if (enableGizmosFacesText)
                        {
                            Handles.Label(new Vector3(x - 0.4f, y + 0.5f, z) + transform.position, $"{node.Left.name}");
                            Handles.Label(new Vector3(x + 0.4f, y + 0.5f, z) + transform.position, $"{node.Right.name}");
                            Handles.Label(new Vector3(x, y + 0.5f, z + 0.4f) + transform.position, $"{node.Front.name}");
                            Handles.Label(new Vector3(x, y + 0.5f, z - 0.4f) + transform.position, $"{node.Back.name}");
                            Handles.Label(new Vector3(x, y + 0.5f, z - 0.4f) + transform.position, $"{node.Back.name}");
                            Handles.Label(new Vector3(x, y + 0.1f, z) + transform.position, $"{node.Down.name}");
                            Handles.Label(new Vector3(x, y + 0.9f, z) + transform.position, $"{node.Up.name}");
                        }

                        if (enableGizmosNodeName)
                            Handles.Label(new Vector3(x, y + 0.5f, z) + transform.position, $"{node.name}");
                    }
                }
            }
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

            // Only rotate nodes with a positive weight that are not symmetrical all the way around (like the crossroad)
            if((currNode.Left.type != NodeFaceHorizontal.Type.None 
            || currNode.Right.type != NodeFaceHorizontal.Type.None
            || currNode.Front.type != NodeFaceHorizontal.Type.None
            || currNode.Back.type != NodeFaceHorizontal.Type.None
            || currFaceNames.Distinct().Skip(1).Any())
            && currNode.Weight > 0) 
            {
                // Rotate object clockwise a maximum of three times
                for(int j = 0; j < 3; j++)
                {
                    // If the tile is symmetrical on two sides, ignore rotation and continue
                    if(j == 1 && currNode.Right.name == currNode.Left.name && currNode.Back.name == currNode.Front.name
                    || j == 2 && currNode.Back.name == currNode.Front.name && currNode.Right.name == currNode.Left.name)
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
                }
            }
        }
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
        _grid = null;

        while (transform.childCount > 0) 
            DestroyImmediate(transform.GetChild(0).gameObject);

        UnityEngine.Debug.Log("Cleared Tiles...");
    }

    private IEnumerator GeneratePathNodes()
    {
        yield return new WaitUntil(() => path.IsDoneFindingPath);

        pathNodes.Clear();

        for (int i = 0; i < path.CollapsedPath.Count; i++)
        {
            foreach (PathNode pathNode in pathNodes)
            {
                if(pathNode.CheckContainsPos(i))
                {
                    pathNode.AddPath(i);
                    goto nextLabel;
                }
            }

            PathNode newPathNode = new PathNode(this);
            newPathNode.AddPath(i);
            pathNodes.Add(newPathNode);

            nextLabel:;
        }

        UnityEngine.Debug.Log($"Total paths: {path.CollapsedPath.Count}");

        doneGeneratingPath = true;
        UnityEngine.Debug.Log("done generating path nodes...");
    }

    public IEnumerator CollapseTiles(Action doneFuncHook)
    {
        //StartCollapseLabel:
        ClearTiles();

        Stopwatch st = new Stopwatch();
        st.Start();

        UnityEngine.Debug.Log("Collapse Tiles...");

        doneCollapse = false;
        _grid = new NodeData[_width, _height, _length];

        _nodesToCollapse.Clear();

        // Start generating tiles with their potential nodes for the path points
        if (pathNodes.Count > 0)
        {
            List<Vector3Int> points = new List<Vector3Int>(); // Used to check for dublicates

            for (int i = 0; i < path.CollapsedPath.Count; i++)
            {
                // Current point
                Vector3Int point = Vector3Int.FloorToInt(path.CollapsedPath[i]);

                // If this point has already been generated, continue
                if (points.FindIndex(p => p.x == point.x && p.y == point.y && p.z == point.z) >= 0) continue;

                PathNode currNode = null;

                foreach(PathNode node in pathNodes)
                {
                    if (node.CheckContainsPos(i))
                    {
                        currNode = node;
                        break;
                    }
                }

                // If a point is not connected to a node it is an bug
                if (currNode == null)
                {
                    UnityEngine.Debug.LogWarning($"path point {i} does not have a related path node!");
                    goto doneCollapseLabel;
                }

                // Create a tile for the given point and filter its potential nodes
                Tile tile = new Tile(this, point, true);
                tile.potentialNodes = currNode.GetPotentialNodes();

                // Add the tile as one to collapse and the point as already done
                _nodesToCollapse.Add(tile);
                points.Add(point);
            }
        } else _nodesToCollapse.Add(new Tile(this, Vector3Int.zero, true));

        // The dungeon might have multiple floors
        for (int story = 0; story < _height; story++)
        {
            // Continue to collapse tiles on the current floor
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
                    UnityEngine.Debug.LogWarning($"Cannot Collapse on {tile.pos.x}, {tile.pos.y}, {tile.pos.z}");

                    activeCollapsningTile = tile.pos;
                    goto doneCollapseLabel;
                }

                else
                {
                    // Choose a node based on weight
                    double[] nodeWeights = CalculateNodesWeights(tile.potentialNodes);
                    int chosenTileIdx = ChooseWeightedTile(nodeWeights, new System.Random());

                    _grid[tile.pos.x, tile.pos.y, tile.pos.z] = tile.potentialNodes[chosenTileIdx];
                }

                activeCollapsningTile = tile.pos;

                yield return new WaitForSeconds(collapseWaitTime);

                CollapseTile(tile);
                _nodesToCollapse.RemoveAt(tileChosenIndex);
            }
        }

        doneCollapse = true;
        doneCollapseLabel:;

        st.Stop();
        collapseExecutionTime = st.ElapsedMilliseconds;
        doneFuncHook();
    }

    private double[] CalculateNodesWeights(List<NodeData> nodes) {
        double[] weights = new double[nodes.Count];
        double totalWeight = nodes.Sum(n => n.Weight);

        int i = 0;
        nodes.ForEach(n => weights[i++] = (n.Weight / totalWeight));

        return weights;
    }

    private int ChooseWeightedTile(double[] weight, System.Random rng) {
        double total = 0;
        double amount = rng.NextDouble();

        for(int a = 0; a < weight.Length; a++){
            total += weight[a];
            
            if(amount <= total) return a;
        }

        return 0;
    }

    private void CheckNeighbors(Tile tile)
    {
        if(!tile.shouldBeUpdated) return; // No neighbor has been collapsed for this tile, so no need to recheck its options

        for(int i = 0; i < Utils.offsets.Length; i++)
        {
            Vector3Int neighbor = tile.pos + Utils.offsets[i];

            if(Utils.CheckPosValid(neighbor, _width, _height, _length))
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
                        case 4: WhittleNodes(tile.potentialNodes, neighborNode.Down, "up");
                            break;
                        case 5: WhittleNodes(tile.potentialNodes, neighborNode.Up, "down");
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

    private bool CheckTileOnPath(Tile tile)
    {
        foreach(Vector3 pos in path.CollapsedPath)
            if (Vector3Int.FloorToInt(pos).Equals(tile.pos))
                return true;
        return false;
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

        // If this is a helper tile, it cannot be instantiated so return instead
        if (node.Prefab == null) return;

        // Instantiate the tile
        GameObject obj = Instantiate(node.Prefab, tile.pos + transform.position, Quaternion.Euler(0, rotationSteps * 90, 0));
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
            // - Two neighbouring tiles faces match and:
            // > they are both symmetrical
            // > or one face is original and the other is flipped
            if (nodeType.name == validType.name
            && (nodeType.symmetry && validType.symmetry 
            || (nodeType.type == NodeFaceHorizontal.Type.Flipped && validType.type == NodeFaceHorizontal.Type.Original 
            || nodeType.type == NodeFaceHorizontal.Type.Original && validType.type == NodeFaceHorizontal.Type.Flipped)))
                continue;

            potentialNodes.RemoveAt(i);
        }
    }

    private void WhittleNodes(List<NodeData> potentialNodes, NodeFaceVertical validType, string direction)
    {
        for(int i = potentialNodes.Count - 1; i > -1; i--)
        {
            NodeFaceVertical nodeType;

            switch(direction) {
                case "up":
                    nodeType = potentialNodes[i].Up;
                    break;
                default:
                    nodeType = potentialNodes[i].Down;
                    break;
            }

            // Vertical tile faces only fit together if:
            // - Two neighbouring tiles faces match and: 
            // > they both have invariant rotation
            // > or have the same rotation index
            if (nodeType.name == validType.name
            && (nodeType.invariantRotation && validType.invariantRotation
            || nodeType.rotationIndex == validType.rotationIndex))
                continue;

            potentialNodes.RemoveAt(i);
        }
    }
}