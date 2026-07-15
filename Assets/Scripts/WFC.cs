using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

// Represents a tile that needs to be collapsed
public class Tile
{
    public Vector3Int pos;
    public List<Node> potentialNodes;
    public List<Tile> neighbors;
    public bool shouldBeUpdated;

    public Tile(WFC parent, Vector3Int coord, bool update = false)
    {
        pos = coord;
        neighbors = new List<Tile>();
        shouldBeUpdated = update;

        potentialNodes = new List<Node>(parent.getNodes);
        potentialNodes.AddRange(parent.getNodesGen);
        potentialNodes = potentialNodes.Where(node => !node.IsStairPiece).ToList();
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

            // Update the face type depending on how the vectors are facing eachother
            if (cross.x == -1) data.Front = NodeFace.Name.Path;
            if (cross.x == 1) data.Back = NodeFace.Name.Path;
            if (cross.z == 1) data.Right = NodeFace.Name.Path;
            if (cross.z == -1) data.Left = NodeFace.Name.Path;
        }

        // Only add the index if it matches the vector and it is not already there
        // Used in case the same point exists multiple times but with different neighbours
        if (!pathIndicies.Contains(index)) pathIndicies.Add(index);
    }

    private Node FindStairCaseNode(List<Node> nodes)
    {
        StairCase stairs = parent.path.GetStaircase(pathIndicies[0]);
        Vector3Int pos = Vector3Int.FloorToInt(parent.path.CollapsedPath[pathIndicies[0]]);
        string name = "";

        if (Misc.VecCmp(stairs.bottomEntrance, pos, 0.5f)) name = "StaircaseEnd";
        else if (Misc.VecCmp(stairs.bottomStairs, pos, 0.5f)) name = "StaircaseFront";
        else if (Misc.VecCmp(stairs.topExit, pos, 0.5f)) name = "StaircaseTopFront";
        else if (Misc.VecCmp(stairs.topCorner, pos, 0.5f)) name = "StaircaseTopEnd";

        name = stairs.rotation > 0 ? $"{name}_{stairs.rotation * 90}" : name;

        return nodes.Find((Node node) => node.name == name);
    }

    public List<Node> GetPotentialNodes()
    {
        List<Node> potentialNodes = new List<Node>(parent.getNodes);
        potentialNodes.AddRange(parent.getNodesGen);
        
        // Check if this node is part of a staircase
        if (parent.path.CheckStaircaseOverlap(Vector3Int.FloorToInt(parent.path.CollapsedPath[pathIndicies[0]]))) 
            return new List<Node> { FindStairCaseNode(potentialNodes) };
        
        // Since this node is not a staircase, filter out any staircase nodes
        potentialNodes = potentialNodes.Where(node => !node.IsStairPiece).ToList();

        for (int i = potentialNodes.Count - 1; i >= 0; i--)
        {
            Node node = potentialNodes[i];

            if (data.Left != NodeFace.Name.None && node.Left.name != data.Left
            || data.Right != NodeFace.Name.None && node.Right.name != data.Right
            || data.Front != NodeFace.Name.None && node.Front.name != data.Front
            || data.Back != NodeFace.Name.None && node.Back.name != data.Back
            || data.Up != NodeFace.Name.None && node.Up.name != data.Up
            || data.Down != NodeFace.Name.None && node.Down.name != data.Down)
                potentialNodes.RemoveAt(i);
        }

        return potentialNodes;
    }
}

[RequireComponent(typeof(AStar))]
[RequireComponent(typeof(MeshSampler))]
public class WFC : MonoBehaviour
{
    // Serilised Fields
    [SerializeField] private int _width;
    [SerializeField] private int _length;
    [SerializeField] private int _height;
    [SerializeField] public int _samplesPerNode;
    
    [SerializeField] private Vector3Int _tileSize = Vector3Int.one;
    
    [SerializeField] private List<Node> _nodes = new List<Node>();
    [SerializeField] private List<Node> _nodesGenerated = new List<Node>();
    [SerializeField] private List<Vector3Int> _pathPoints = new List<Vector3Int>();

    [SerializeField] private float _samplingRadius = 0.5f;
    [SerializeField] private int _samplingTries = 30;
    [SerializeField] private bool _overrideObjList = false;
    [SerializeField] private int _floorPropGraphLevel = 2;
    [SerializeField] private int _wallPropGraphLevel = 2;

    [SerializeField] private int _levelCount = 0;

    public string PropText { get; private set; } = "";

    // Private Variables
    Node[,,] _grid;
    List<Tile> _nodesToCollapse = new List<Tile>();
    List<PathNode> pathNodes = new List<PathNode>();
    double collapseExecutionTime = 0;
    public float collapseWaitTime = 1.0f;
    Vector3Int activeCollapsningTile;
    enum Direction // DO NOT CHANCE THE ORDER OF ELEMENTS IN THIS ENUM!
    {
        Front,
        Back,
        Right,
        Left,
        Up,
        Down
    }

    // Public Variables
    public AStar path;
    public bool pauseGeneration = false;
    public bool doneGeneratingSamples = false;
    public bool enabledCollapseButton = false;
    public static WFC wfc = null;
    
    // Getters
    public int getTiles => transform.childCount;
    public double getCollapseTime => collapseExecutionTime;
    public int getWidth => _width;
    public int getHeight => _height;
    public int getLength => _length;
    public List<Node> getNodes => _nodes;
    public List<Node> getNodesGen => _nodesGenerated;
    public Vector3Int TileSize => _tileSize;
    
    // Gizmos Debug Settings
    // -- WFC
    public bool enableGizmosGrid = false;
    public bool enableGizmosCoords = false;
    public bool enableGizmosFacesText = false;
    public bool enableGizmosNodeName = false;
    // -- A*
    public bool enableGizmosPath = false;
    public bool enableGizmosPathPoints = false;
    public bool enableGizmosPathRouting = false;
    public bool enableGizmosPathStaircases = false;
    public bool enableGizmosPathField = false;
    public bool enableGizmosPathFinding = false;
    public bool enableGizmosDelay = false;
    // -- PDS
    public bool enableGizmosFloorSamples = false;
    public bool enableGizmosWallSamples = false;
    public bool enableGizmosCornerSamples = false;
    public bool enableGizmosSamplePoints = false;

    public void StartFindPath(Action doneFuncHook)
    {
        if ((path = gameObject.GetComponent<AStar>()) && path == null) 
            path = gameObject.AddComponent<AStar>();

        path.GeneratePath(this, _pathPoints);

        CoroutineManager.StartCoroutine(this, "GeneratePathNodes", GeneratePathNodes(doneFuncHook));
    }

    public void StopFindPath() => path.StopFindingPath();
    public void ClearPath() {
        path.StopFindingPath();
        path.ClearPath();
        pathNodes.Clear();
    }

    public void StartCollapse(Action<int> doneFuncHook) 
    {
        CoroutineManager.StartCoroutine(this, "CollapseTiles", CollapseTiles(doneFuncHook));
    }

    public void StopCollapse() 
    {
        CoroutineManager.StopCoroutine(this, "CollapseTiles");
        CoroutineManager.StopAllCoroutines();
    }

    public void StartCollapseTesting(Action doneFuncHook, Action<int> updateFuncHook)
    {
        CoroutineManager.StartCoroutine(this, "CollapseTilesTesting", CollapseTilesTesting(doneFuncHook, updateFuncHook, _levelCount));
    }

    public void StopCollapseTesting()
    {
        CoroutineManager.StopCoroutine(this, "CollapseTilesTesting");
        CoroutineManager.StopAllCoroutines();
    }

    // Used with the Unity VisualElement editor to save transitive information to the AStar object
    public void SavePathSettings(bool pathState, bool pathPointsState, bool pathStaircases, bool pathField, bool pathFinding, bool pathDelay)
    {
        // If the path object exist, toggle its settings
        if (path) {
            path.enableGizmosPathPoints = pathPointsState;
            path.enableGizmosPathStaircases = pathStaircases;
            path.enableGizmosPathField = pathField;
            path.enableGizmosPathFinding = pathFinding;
            path.enableGizmosGenerationDelay = pathDelay;
        }

        // Toggle rendering of the LineRenderer used to display the A* path
        LineRenderer lr = gameObject.GetComponent<LineRenderer>();
        if (lr) lr.enabled = pathState;
    }

    // Used with the Unity VisualElement editor to save transitive information to the Mesh Sampler object
    public void SavePDSSettings(bool pdsFloorSamples, bool pdsWallSamples, bool pdsCornerSamples, bool pdsSamplePoints, float samplesRenderDistance)
    {
        MeshSampler sampler = gameObject.GetComponent<MeshSampler>();

        // If the mesh sampler object exist, toggle its settings
        if (sampler) {
            sampler.enableGizmosFloorSamples = pdsFloorSamples;
            sampler.enableGizmosWallSamples = pdsWallSamples;
            sampler.enableGizmosCornerSamples = pdsCornerSamples;
            sampler.enableGizmosSamplePoints = pdsSamplePoints;
            sampler.samplesRenderDistance = samplesRenderDistance;
        }
    }

    public void OnDrawGizmos()
    {
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
                        Vector3Int tilePos = _tileSize * new Vector3Int(i, k, j);
                        if (enableGizmosGrid) Gizmos.DrawWireCube(tilePos + new Vector3(0.0f, _tileSize.y * 0.5f, 0.0f), _tileSize);
                        if (enableGizmosCoords) Handles.Label(tilePos -  new Vector3(_tileSize.x * 0.5f, 0 ,_tileSize.z * 0.5f) + transform.position, $"({i}, {j}, {k})");
                    }
                }
            }
            
            if (CoroutineManager.IsAlive(this, new[]{ "CollapseTiles", "CollapseTilesTesting" }))
            {
                Gizmos.color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                Gizmos.DrawWireCube(activeCollapsningTile * _tileSize - new Vector3(0.0f, _tileSize.y * -0.5f, 0.0f), _tileSize);
            }
        }

        if (enableGizmosPathPoints)
        {
            float sphereSize = (TileSize.x + TileSize.z) / 2 * 0.05f;

            foreach (Vector3Int point in _pathPoints)
            {
                Gizmos.color = Color.orange;
                Gizmos.DrawSphere(point * _tileSize, sphereSize);
            }
        }

        if (enableGizmosPathRouting)
        {
            foreach (PathNode point in pathNodes)
            {
                foreach (int index in point.pathIndicies)
                {
                    if (index >= path.CollapsedPath.Count) continue;

                    Vector3 pos = Vector3.Scale(path.CollapsedPath[index], TileSize);
                    Vector3 offset = Vector3.Scale(new Vector3(0.49f, 0.0f, 0.49f), TileSize);

                    Gizmos.color = point.data.Up != NodeFace.Name.None ? Color.green : Color.red;
                    Gizmos.DrawLine(pos + offset, pos + Vector3.Scale(offset, new Vector3Int(-1, 1, -1))); // Up

                    Gizmos.color = point.data.Down != NodeFace.Name.None ? Color.green : Color.red;
                    Gizmos.DrawLine(pos + Vector3.Scale(offset, new Vector3Int(-1, 1, 1)), pos + Vector3.Scale(offset, new Vector3Int(1, 1, -1))); // Down

                    Gizmos.color = point.data.Front != NodeFace.Name.None ? Color.green : Color.red;
                    Gizmos.DrawLine(pos + offset, pos + Vector3.Scale(offset, new Vector3Int(-1, 1, 1))); // Left

                    Gizmos.color = point.data.Back != NodeFace.Name.None ? Color.green : Color.red;
                    Gizmos.DrawLine(pos + Vector3.Scale(offset, new Vector3Int(1, 1, -1)), pos + Vector3.Scale(offset, new Vector3Int(-1, 1, -1))); // Right

                    Gizmos.color = point.data.Left != NodeFace.Name.None ? Color.green : Color.red;
                    Gizmos.DrawLine(pos + Vector3.Scale(offset, new Vector3Int(-1, 1, 1)), pos + Vector3.Scale(offset, new Vector3Int(-1, 1, -1))); // Forward

                    Gizmos.color = point.data.Right != NodeFace.Name.None ? Color.green : Color.red;
                    Gizmos.DrawLine(pos + offset, pos + Vector3.Scale(offset, new Vector3Int(1, 1, -1))); // Back
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
                        Node node = _grid[x, y, z];
                        Vector3Int tilePos = _tileSize * new Vector3Int(x, y, z);

                        if (enableGizmosFacesText)
                        {
                            Handles.Label(tilePos + new Vector3(_tileSize.x * -0.4f, _tileSize.y * 0.5f, 0.0f) + transform.position, $"{node.Left.name}");
                            Handles.Label(tilePos + new Vector3(_tileSize.x * 0.4f, _tileSize.y * 0.5f, 0.0f) + transform.position, $"{node.Right.name}");
                            Handles.Label(tilePos + new Vector3(0.0f, _tileSize.y * 0.5f, _tileSize.z * 0.4f) + transform.position, $"{node.Front.name}");
                            Handles.Label(tilePos + new Vector3(0.0f, _tileSize.y * 0.5f, _tileSize.z * -0.4f) + transform.position, $"{node.Back.name}");
                            Handles.Label(tilePos + new Vector3(0.0f, _tileSize.y * 0.5f, _tileSize.z * -0.4f) + transform.position, $"{node.Back.name}");
                            Handles.Label(tilePos + new Vector3(0.0f, _tileSize.y * 0.1f, 0.0f) + transform.position, $"{node.Down.name}");
                            Handles.Label(tilePos + new Vector3(0.0f, _tileSize.y * 0.9f, 0.0f) + transform.position, $"{node.Up.name}");
                        }

                        if (enableGizmosNodeName)
                            Handles.Label(tilePos + new Vector3(0.0f, _tileSize.y * 0.5f, 0.0f) + transform.position, $"{node.name}");
                    }
                }
            }
        }
    }
    
    public void SetLevelCount(int count) => _levelCount = count;
    public (Vector3, Vector3) GetBoundary()
    {
        Vector3 min = new Vector3(_tileSize.x / -2, 0.0f, _tileSize.z / -2);
        Vector3 max = Vector3.Scale(_tileSize, new(_width, _height, _length)) + min;

        return (min, max);
    }

    // Check point is inside WFC bounderies
    public bool IsInside(Vector3 point)
    {
        (Vector3 min, Vector3 max) = GetBoundary();

        return point.x >= min.x
            && point.x <= max.x
            && point.y >= min.y
            && point.y <= max.y
            && point.z >= min.z
            && point.z <= max.z;
    }

    // Check if a boundary (like from a collider) is inside the WFC bounderies
    public bool IsInside(Bounds bounds)
    {
        return IsInside(bounds.min) && IsInside(bounds.max);
    }

    // Generate new tiles by creating new ones by rotating the current ones
    public void GenerateTiles()
    {
        // Clear previously generated nodes
        _nodesGenerated.Clear();

        // Go through all created nodes and rotate those that need it
        foreach(Node currNode in _nodes) 
        {
            // Only rotate nodes with a positive weight that are not symmetrical all the way around
            if (!currNode.ShouldRotate()) continue;

            // Rotate object clockwise a maximum of three times
            for(int j = 0; j < 3; j++)
            {
                // Break if the tile is symmetrical along two sides
                if(j > 0 && currNode.IsBilateralSymmetric()) break;

                // Setup new tile data
                Node newNode = Instantiate(currNode);

                newNode.name = currNode.name + "_" + ((j + 1) * 90);
                newNode.ClockwiseRotationSteps = j + 1;
                newNode.nodeType = (Node.NodeType) Random.Range(0, 3);

                // Rotate the node
                newNode.Rotate(j + 1);

                // Add the node to the list of autogenerated objects
                _nodesGenerated.Add(newNode);
            }
        }
    }

    public void SampleTiles(Action doneFuncHook)
    {
        doneGeneratingSamples = false;
        MeshSampler sampler = gameObject.GetComponent<MeshSampler>();
        List<Node> nodes = new List<Node>(_nodes);
        nodes.AddRange(_nodesGenerated);

        MeshNode.SampleTiles(sampler, this, nodes, _samplingRadius, _samplingTries, _samplesPerNode, _floorPropGraphLevel, _wallPropGraphLevel);
        doneFuncHook();
    }

    public void ClearTiles(bool clearAll = false) 
    {
        PropText = "";
        _nodesToCollapse.Clear();
        Prop.Props.Clear();

        gameObject.GetComponent<MeshSampler>().Clear();
        
        if(clearAll) _nodesGenerated.Clear();
        _grid = null;

        while (transform.childCount > 0) 
            DestroyImmediate(transform.GetChild(0).gameObject);

        UnityEngine.Debug.Log("Cleared Tiles...");
    }

    private IEnumerator GeneratePathNodes(Action doneFuncHook)
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

        doneFuncHook();

        UnityEngine.Debug.Log("done generating path nodes...");
    }

    public IEnumerator CollapseTiles(Action<int> doneFuncHook)
    {
        wfc = this;
        int overlaps = 0;
        ClearTiles();
        
        Stopwatch st = new Stopwatch();
        st.Start();

        UnityEngine.Debug.Log("Collapse Tiles...");

        _grid = new Node[_width, _height, _length];

        _nodesToCollapse.Clear();

        // Start generating tiles with their potential nodes for the path points
        if (pathNodes.Count > 0)
        {
            List<Vector3Int> points = new List<Vector3Int>(); // Used to check for duplicates

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

                // If a point is not connected to a node it is a bug
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

                overlaps += CollapseTile(tile);

                yield return new WaitUntil(() => !CoroutineManager.HasAliveRoutinesExcept(this));

                _nodesToCollapse.RemoveAt(tileChosenIndex);
            }
        }
        
        doneCollapseLabel:;

        st.Stop();
        collapseExecutionTime = st.ElapsedMilliseconds;
        doneFuncHook(overlaps);
    }

    public IEnumerator CollapseTilesTesting(Action doneFuncHook, Action<int> updateFuncHook, int levelCount)
    {
        float qualityScore = 0;
        
        for (int k = 0; k < levelCount; k++)
        {
            int overlaps = 0;
            int totalProps = 0;
            bool roundDone = false;

            CoroutineManager.StartCoroutine(this, "CollapseTiles", CollapseTiles((o) => {
                overlaps = o;
                roundDone = true;
            }));

            yield return new WaitUntil(() => roundDone);
            
            /*foreach (var prop in _meshSampler._props)
            {
                PropText += $"{prop.Key}: {prop.Value} \n";
                totalProps += prop.Value;
            }*/

            PropText += $"Overlaps: {overlaps}\n";
            PropText += $"Totalprops: {totalProps}\n\n";
            
            if (totalProps != 0)
            {
                float score = 1 - ((float)(overlaps) / (float)(totalProps));

                PropText += $"OverlapPercentage: {((float)(overlaps) / (float)(totalProps)) * 100f}%\n";
                PropText += $"Quality Score: {score}\n";

                qualityScore += score;
            }

            updateFuncHook(k);

            yield return null;
        }

        PropText += $"\nAverage Qaulity Score: {qualityScore / _levelCount}";
        
        TestData.SaveData(PropText);
        doneFuncHook();
    }

    private double[] CalculateNodesWeights(List<Node> nodes) {
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

        for(int i = 0; i < Misc.offsets.Length; i++)
        {
            Vector3Int neighbor = tile.pos + Misc.offsets[i];

            if(Misc.CheckPosValid(neighbor, _width, _height, _length))
            {
                Node neighborNode = _grid[neighbor.x, neighbor.y, neighbor.z];

                if(neighborNode)
                {
                    switch ((Direction) i)
                    {
                        case Direction.Front: 
                            WhittleNodes(tile.potentialNodes, neighborNode.Back, Direction.Front);
                            break;
                        case Direction.Back: 
                            WhittleNodes(tile.potentialNodes, neighborNode.Front, Direction.Back);
                            break;
                        case Direction.Right: 
                            WhittleNodes(tile.potentialNodes, neighborNode.Left, Direction.Right);
                            break;
                        case Direction.Left: 
                            WhittleNodes(tile.potentialNodes, neighborNode.Right, Direction.Left);
                            break;
                        case Direction.Up: 
                            WhittleNodes(tile.potentialNodes, neighborNode.Down, Direction.Up);
                            break;
                        case Direction.Down: 
                            WhittleNodes(tile.potentialNodes, neighborNode.Up, Direction.Down);
                            break;
                    }
                }

                else
                {
                    if(!_nodesToCollapse.Any(n => n.pos == neighbor)) 
                    {
                        UnityEngine.Debug.Log($"creating neighbor tile...");
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

    private int CollapseTile(Tile tile)
    {
        // Get node object
        Node node = _grid[tile.pos.x, tile.pos.y, tile.pos.z];

        // Make sure that this tile's neighbors get marked to get updated
        foreach (Tile t in tile.neighbors)
            t.shouldBeUpdated = true;

        // If this is a helper tile, it cannot be instantiated so return instead
        if (node.Prefab == null) return 0;

        // Set object information
        Vector3 pos = (tile.pos * _tileSize) + transform.position;
        Quaternion rot = Quaternion.Euler(0, node.ClockwiseRotationSteps * 90, 0);
        
        // Instantiate the tile
        GameObject obj = Instantiate(node.Prefab, pos, rot);
        obj.name = node.name; // Rename the node so we know what type has been spawned
        obj.transform.parent = gameObject.transform; // Set this object as parent for editor readability

        // Spawn props on the node
        MeshNode mesh = obj.GetComponent<MeshNode>();
        mesh?.Init();
        mesh?.Generate(node, TileSize);

        // Print a warning if this tile does not have a mesh for some reason
        if (mesh == null) UnityEngine.Debug.LogWarning($"Node Prefab '{node.Prefab.name}' does not have a MeshNode!");

        return 0;
    }

    private void WhittleNodes(List<Node> potentialNodes, NodeFaceHorizontal validType, Direction direction)
    {
        for(int i = potentialNodes.Count - 1; i >= 0; i--)
        {
            NodeFaceHorizontal nodeType = direction switch {
                Direction.Left => potentialNodes[i].Left,
                Direction.Right => potentialNodes[i].Right,
                Direction.Front => potentialNodes[i].Front,
                _ => potentialNodes[i].Back
            };

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

    private void WhittleNodes(List<Node> potentialNodes, NodeFaceVertical validType, Direction direction)
    {
        for(int i = potentialNodes.Count - 1; i >= 0; i--)
        {
            NodeFaceVertical nodeType = direction switch {
                Direction.Up => potentialNodes[i].Up,
                _ => potentialNodes[i].Down
            };

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