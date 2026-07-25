using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class NodeData : IEquatable<NodeData>
{
    public NodeData parent;
    public Vector3Int position;
    public double g, h, f; 

    public NodeData(NodeData parent, Vector3Int position)
    {
        this.parent = parent;
        this.position = position;
        this.g = this.h = this.f = 0.0f;
    }

    public bool Equals(NodeData other)
    {
        if (this.position == null) return false;
        return this.position.Equals(other.position);
    }

    public bool IsDirTo(NodeData other)
    {
        if (other == null) return false;
        for (int i = 6; i < Misc.offsets3.Length; i++)
            if (position == (other.position + Misc.offsets3[i]))
                return true;
        return false;
    }
}

public class StairCase
{
    public Vector3Int bottomEntrance;
    public Vector3Int bottomStairs;
    public Vector3Int topCorner;
    public Vector3Int topExit;
    public int rotation;

    public StairCase(Vector3Int p1, Vector3Int p2, Vector3Int p3, Vector3Int p4)
    {
        this.bottomEntrance = p1;
        this.bottomStairs = p2;
        this.topCorner = p3;
        this.topExit = p4;
        this.rotation = GetRotation(p1, p2);
    }

    // Sets the rotation of the staircase based on the coordinates
    private int GetRotation(Vector3Int p1, Vector3Int p2)
    {
        if (p1.z < p2.z) return 0;
        else if (p1.x < p2.x) return 1;
        else if (p1.z > p2.z) return 2;
        else return 3;
    }

    // Used to check if this staircase piece contains a specific vector coordinate
    public bool CheckContainsPos(Vector3Int pos)
    {
        return (Misc.VecCmp(pos, bottomEntrance, 0.5f)
             || Misc.VecCmp(pos, bottomStairs, 0.5f)
             || Misc.VecCmp(pos, topCorner, 0.5f)
             || Misc.VecCmp(pos, topExit, 0.5f));
    }

    // Draws the gizmo box for the staircase
    public void DrawGizmoBox(WFC parent)
    {
        Gizmos.color = Color.orange;

        Vector3 size = parent.TileSize * (topCorner.x != topExit.x ? new Vector3Int(2, 2, 1) : new Vector3Int(1, 2, 2));
        Vector3 center = ((Vector3) (parent.TileSize * (bottomEntrance + bottomStairs + topCorner + topExit))) / 4.0f + new Vector3(0, parent.TileSize.y * 0.5f, 0);

        Gizmos.DrawWireCube(center, size);
    }

    // Draws the gizmo points for the staircase pieces
    public void DrawGizmoPoints(WFC parent)
    {
        // Misc
        Vector3 offset = new Vector3(0.0f, parent.TileSize.y * 0.5f, 0.0f);
        float sphereSize = (parent.TileSize.x + parent.TileSize.z) / 2 * 0.05f;

        // Lowest entrance point
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(parent.TileSize * bottomEntrance + offset, sphereSize);

        // Lowest stairs point
        Gizmos.color = Color.purple;
        Gizmos.DrawSphere(parent.TileSize * bottomStairs + offset, sphereSize);

        // Top exit point
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(parent.TileSize * topExit + offset, sphereSize);

        // Top corner point
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(parent.TileSize * topCorner + offset, sphereSize);
    }
}

public class AStar : MonoBehaviour
{
    // Private Variables
    LineRenderer lineRenderer;
    IEnumerator pathRoutine;
    private List<Vector3> constructedPath = new List<Vector3>();
    private List<StairCase> staircases = new List<StairCase>();
    private List<NodeData> openList = new List<NodeData>();
    private List<NodeData> closedList = new List<NodeData>();
    private bool doneFindingPath = false;

    // Getters
    public List<Vector3> CollapsedPath => constructedPath;
    public List<StairCase> GetStaircases => staircases;
    public bool IsDoneFindingPath => doneFindingPath;

    // Debugging data for gizmos
    private WFC _parent;
    public bool enableGizmosPathPoints = false;
    public bool enableGizmosPathStaircases = false;
    public bool enableGizmosPathField = false;
    public bool enableGizmosPathFinding = false;
    public bool enableGizmosGenerationDelay = false;

    public void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        if (enableGizmosPathStaircases)
        {
            // Draw the staircases
            foreach (StairCase staircase in staircases)
            {
                staircase.DrawGizmoBox(_parent);
                staircase.DrawGizmoPoints(_parent);
            }
        }

        // Draw the field to traverse
        if (_parent != null && enableGizmosPathField)
        {
            Gizmos.color = Color.blue;
            for (int height = 0; height < _parent.getHeight; height++)
            {
                Gizmos.DrawLineList(new Vector3[8]{
                    // Line #1
                    Vector3.Scale(new Vector3(-0.5f, height, -0.5f), _parent.TileSize),
                    Vector3.Scale(new Vector3(_parent.getWidth - 0.5f, height, -0.5f), _parent.TileSize),

                    // Line #2
                    Vector3.Scale(new Vector3(_parent.getWidth - 0.5f, height, -0.5f), _parent.TileSize),
                    Vector3.Scale(new Vector3(_parent.getWidth - 0.5f, height, _parent.getLength - 0.5f), _parent.TileSize),

                    // Line #3
                    Vector3.Scale(new Vector3(_parent.getWidth - 0.5f, height, _parent.getLength - 0.5f), _parent.TileSize),
                    Vector3.Scale(new Vector3(-0.5f, height, _parent.getLength - 0.5f), _parent.TileSize),

                    // Line #4
                    Vector3.Scale(new Vector3(-0.5f, height, _parent.getLength - 0.5f), _parent.TileSize),
                    Vector3.Scale(new Vector3(-0.5f, height, -0.5f), _parent.TileSize)
                });
            }
        }

        // Draw the open nodes
        if(enableGizmosPathFinding && openList.Count > 0)
        {
            Gizmos.color = Color.orange;

            foreach(NodeData node in openList)
            {
                Gizmos.DrawWireCube(_parent.TileSize * node.position, Vector3.Scale(Vector3.one, _parent.TileSize));
            }
        }

        // Draw the closed nodes
        if(enableGizmosPathFinding && closedList.Count > 0)
        {
            Gizmos.color = Color.green;

            foreach(NodeData node in closedList)
            {
                Gizmos.DrawWireCube(_parent.TileSize * node.position, Vector3.Scale(Vector3.one, _parent.TileSize));
            }
        }
    }

    public void GeneratePath(WFC parent, List<Vector3Int> path)
    {
        _parent = parent;
        doneFindingPath = false;

        parent.EditorResetProgress();
        parent.EditorMessageSimpleProgress("Generating A* Path...");
        parent.EditorMessageSimpleProgress($"> (1/2) Finding routes between {path.Count} points ");

        if (!CheckPathValidity(path))
        {
            parent.EditorMessageProgress("# Cannot find route, some points are invalid...", Color.yellow);
            parent.EditorMessageProgress("Done finding routes", Color.green);
            return;
        }

        CoroutineManager.StartCoroutine(this, "FindRoute", FindRoute(new List<Vector3Int>(path)));
    }

    public void StopFindingPath() 
    {
        // Editor message
        if (!doneFindingPath)
        {
            _parent.EditorMessageProgress($"@ Path finding forcefully stopped by user...", Color.red);
            _parent.EditorSetBarProgress(100);
            _parent.EditorMessageProgress("Done finding routes", Color.green);
        }

        // Stop the coroutine
        CoroutineManager.StopAllCoroutines();
        pathRoutine = null;
        doneFindingPath = true;

        // Reset variables
        openList.Clear();
        closedList.Clear();
    }

    public void ClearPath()
    {
        // Editor message
        if (!doneFindingPath) _parent?.EditorMessageProgress("Clearing previous path", Color.gray);

        // Reset variables
        constructedPath.Clear();
        staircases.Clear();
        openList.Clear();
        closedList.Clear();

        // Reset the linerenderer
        if (lineRenderer) lineRenderer.positionCount = 0;
    }

    private List<Vector3> CollapsePath(NodeData endNode)
    {
        List<Vector3> path = new List<Vector3>();
        NodeData current = endNode;

        while (current != null)
        {
            path.Add(current.position);
            current = current.parent;
        }

        path.Reverse();
        return path;
    }

    // Path points are only valid if:
    // - They are within the level
    // - They are not occupying the same space
    // - They are vertically no more than 1 vertically space away from eachother
    private bool CheckPathValidity(List<Vector3Int> path)
    {
        for (int i = 0; i < path.Count; i++)
        {
            Vector3Int p1 = path[i];

            // If the point is outside the level
            if (!Misc.CheckPosValid(p1, _parent.getWidth, _parent.getHeight, _parent.getLength)) return false;

            // If the two points are vertically more than 1 grid tile away from eachother
            if (i < (path.Count - 1) && Math.Abs(p1.y - path[i+1].y) > 1) return false;

            // Compare the current point with all the others
            for (int j = 0; j < path.Count; j++)
            {
                Vector3Int p3 = path[j];

                if (i == j) continue; // This point is the same in both instances, continue
                if (Misc.VecCmp(p1, p3, 0.0f)) return false; // If the point is at the same location as another point
            }
        }

        // No errors found, all points are valid
        return true;
    }

    private IEnumerator FindRoute(List<Vector3Int> points)
    {
        ClearPath(); // Clear previously generated data

        _parent.EditorMessageSimpleProgress("Creating staircase sub-points ");

        yield return null;

        // Used to reposition nodes across levels for adding stairs correctly
        for (int j = 0; j < points.Count - 1; j++)
        {
            Vector3Int currPoint = points[j];
            Vector3Int nextPoint = points[j+1];

            // If the two points are at a different level, place a staircase here
            if(currPoint.y != nextPoint.y) 
            {
                // Find a direction in which there are space for the staircase
                // The order is randomised to make it more interesting
                foreach (Vector3Int offset in Misc.offsets2.OrderBy(i => Guid.NewGuid()).ToList())
                {
                    // Make sure the new point is placed with the correct offset and level according to the next point
                    Vector3Int levelOffset = currPoint.y < nextPoint.y ? Vector3Int.down : Vector3Int.up;
                    Vector3Int newPos = nextPoint + levelOffset + offset * 3;

                    bool val = Misc.CheckPosValid(newPos, _parent.getWidth, _parent.getHeight, _parent.getLength) 
                    && !Misc.CheckVectorOverlap(points, newPos, 0.1f)
                    && !Misc.CheckVectorOverlap(points, newPos + Vector3Int.down, 0.1f)
                    && !Misc.CheckVectorOverlap(points, newPos + Vector3Int.up, 0.1f);

                    // The new point is only valid if:
                    // - It is within the level
                    // - and does not overlap with another point
                    // - nor overlaps with a previously autogenerated point either above or below
                    if (Misc.CheckPosValid(newPos, _parent.getWidth, _parent.getHeight, _parent.getLength) 
                    && !Misc.CheckVectorOverlap(points, newPos, 0.1f)
                    && !Misc.CheckVectorOverlap(points, newPos + Vector3Int.down, 0.1f)
                    && !Misc.CheckVectorOverlap(points, newPos + Vector3Int.up, 0.1f))
                    {
                        // List of all the points making up a staircase
                        Vector3Int p1 = newPos - offset;
                        Vector3Int p2 = newPos - offset * 2;
                        Vector3Int p3 = newPos - levelOffset - offset;
                        Vector3Int p4 = newPos - levelOffset - offset * 2;

                        // Insert the location for the staircase into the points list
                        points.Insert(j + 1, newPos);

                        // Adding the points depends on if we go up or down a staircase
                        if (currPoint.y < nextPoint.y) 
                        {
                            staircases.Add(new StairCase(p1, p2, p3, p4));
                            points.Insert(j + 2, p1);
                            points.Insert(j + 3, p2);
                            points.Insert(j + 4, p3);
                            points.Insert(j + 5, p4);
                        }

                        else 
                        {
                            staircases.Add(new StairCase(p4, p3, p2, p1));
                            points.Insert(j + 2, p4);
                            points.Insert(j + 3, p3);
                            points.Insert(j + 4, p2);
                            points.Insert(j + 5, p1);
                        }

                        j += 5;

                        // A staircase position has been found, continue
                        goto genStairsLoop;
                    }
                }

                // If there cannot be generated a starcase here we have an issue
                _parent.EditorMessageProgress($"@ A staircase cannot be generated at ({nextPoint.x}, {nextPoint.y}, {nextPoint.z})", Color.red);
                goto doneLabel;
            }

            genStairsLoop:;
            _parent.EditorUpdateDotMessageProgress(150.0f, j + 1, points.Count);
            yield return null;
        }

        _parent.EditorUpdateRecentMessageProgress("Done");
        _parent.EditorIncreaseBarProgress(15);

        _parent.EditorMessageProgress("Started finding routes", Color.gray);
        _parent.EditorBeginStepCounterProgress(80.0f, points.Count);

        yield return null;

        // Loop through all paths
        for (int j = 0; j < points.Count - 1; j++)
        {
            // To be added to the final path once finished
            List<Vector3> tempPath = new List<Vector3>();

            // Setup data
            NodeData startNode = new NodeData(null, points[j]);
            NodeData endNode = new NodeData(null, points[j+1]);

            openList.Add(startNode);

            // Loop until the end is found
            while (openList.Count > 0)
            {
                // Get the current node
                NodeData currentNode = openList[0];
                int currentIndex = 0;

                for (int i = 0; i < openList.Count; i++)
                {
                    NodeData item = openList[i];

                    // The node is only acceptable if it is closer and only moves diagonal inside a staircase
                    if (item.f < currentNode.f
                    && (item.parent == null 
                    || CheckStaircaseOverlap(item.parent.position) 
                    || !CheckStaircaseOverlap(item.parent.position) && !item.IsDirTo(item.parent)))
                    {
                        currentNode = item;
                        currentIndex = i;
                    }
                }

                // Pop current off open list, add to closed list
                openList.RemoveAt(currentIndex);
                closedList.Add(currentNode);

                // Collapse the path constantly for debugging
                tempPath = CollapsePath(currentNode);

                // Found the goal
                if (currentNode.Equals(endNode))
                {
                    VisualisePath(tempPath);
                    break;
                }

                // Generate children
                List<NodeData> children = new List<NodeData>();

                foreach (Vector3Int offset in Misc.offsets3)
                {
                    // Get node position
                    Vector3Int nodePosition = currentNode.position + offset;

                    // Make sure within range of the level
                    if (!Misc.CheckPosValid(nodePosition, _parent.getWidth, _parent.getHeight, _parent.getLength))
                        continue;

                    // Create new node
                    NodeData newNode = new NodeData(currentNode, nodePosition);

                    // Append
                    children.Add(newNode);
                }

                // Loop through children
                for (int k = 0; k < children.Count; k++)
                {
                    NodeData childNode = children[k];

                    // Child is on the closed list
                    foreach (NodeData closedChild in closedList)
                        if (childNode.Equals(closedChild))
                            continue;

                    // The child is on a staircase when it should not be
                    if ((!CheckStaircaseOverlap(startNode.position)
                    && !CheckStaircaseOverlap(endNode.position))
                    && (CheckStaircaseOverlap(childNode.position)
                    || k > 5))
                        continue;
                    
                    // Create the f, g, and h values
                    childNode.g = currentNode.g + 1;
                    childNode.h = Math.Pow(childNode.position[0] - endNode.position[0], 2) + Math.Pow(childNode.position[1] - endNode.position[1], 2) + Math.Pow(childNode.position[2] - endNode.position[2], 2);
                    childNode.f = childNode.g + childNode.h;

                    // Child is already in the open list
                    foreach (NodeData openNode in openList)
                        if (childNode.Equals(openNode) && childNode.g > openNode.g)
                            continue;
                    
                    // Add the child to the open list
                    openList.Add(childNode);
                }

                VisualisePath(tempPath);
                yield return enableGizmosGenerationDelay ? new WaitForSeconds(0.5f) : null;
            }

            // Editor progress message
            _parent.EditorTakeStepProgress($"Route between {startNode.position} - {endNode.position} found");

            // Add the temporary generated path to the permanent one
            constructedPath.AddRange(tempPath);

            // Reset lists
            openList.Clear();
            closedList.Clear();

            yield return null;
        }

        doneLabel:;
        doneFindingPath = true;

        // Editor progress message
        _parent.EditorStopStepCounterProgress();
        _parent.EditorMessageProgress($"Total routes: {points.Count - 1}", Color.gray);
        _parent.EditorMessageProgress("Done finding routes...", Color.gray);

        yield return null;
    }

    private void VisualisePath(List<Vector3> tempPath)
    {
        if ((lineRenderer = gameObject.GetComponent<LineRenderer>()) && lineRenderer == null)
        {
            // Create the line renderer object
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

            // Set the color
            lineRenderer.startColor = Color.red;
            lineRenderer.endColor = Color.red;

            // Set the width
            lineRenderer.startWidth = 0.05f;
            lineRenderer.endWidth = 0.05f;
        }

        lineRenderer.positionCount = constructedPath.Count + tempPath.Count;
        lineRenderer.SetPositions(constructedPath.Concat(tempPath).Select(p => Vector3.Scale(p + transform.position, _parent.TileSize)).ToArray());
    }

    public bool CheckStaircaseOverlap(Vector3Int pos)
    {
        return staircases.Exists((StairCase stairs) => stairs.CheckContainsPos(pos));
    }

    public StairCase GetStaircase(int index)
    {
        return staircases.Find((StairCase stair) => stair.CheckContainsPos(Vector3Int.FloorToInt(constructedPath[index])));
    }
}
