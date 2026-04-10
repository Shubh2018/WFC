using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : IEquatable<Node>
{
    public Node parent;
    public Vector3Int position;
    public double g, h, f; 

    public Node(Node parent, Vector3Int position)
    {
        this.parent = parent;
        this.position = position;
        this.g = this.h = this.f = 0.0f;
    }

    public bool Equals(Node other)
    {
        if (this.position == null) return false;
        return this.position.Equals(other.position);
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

    private int GetRotation(Vector3Int p1, Vector3Int p2)
    {
        if (p1.z < p2.z) return 0;
        else if (p1.x < p2.x) return 1;
        else if (p1.z > p2.z) return 2;
        else return 3;
    }

    public bool CheckContainsPos(Vector3Int pos)
    {
        return (Utils.VecCmp(pos, bottomEntrance, 0.5f)
             || Utils.VecCmp(pos, bottomStairs, 0.5f)
             || Utils.VecCmp(pos, topCorner, 0.5f)
             || Utils.VecCmp(pos, topExit, 0.5f));
    }

    public void DrawGizmoBox()
    {
        Gizmos.color = Color.orange;

        Vector3 size = topCorner.x != topExit.x ? new Vector3Int(2, 2, 1) : new Vector3Int(1, 2, 2);
        Vector3 center = ((Vector3) (bottomEntrance + bottomStairs + topCorner + topExit)) / 4.0f + new Vector3(0, 0.5f, 0);

        Gizmos.DrawWireCube(center, size);
    }

    public void DrawGizmoPoints()
    {
        // Lowest entrance point
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(bottomEntrance + new Vector3(0.0f, 0.5f, 0.0f), 0.05f);

        // Lowest stairs point
        Gizmos.color = Color.purple;
        Gizmos.DrawSphere(bottomStairs + new Vector3(0.0f, 0.5f, 0.0f), 0.05f);

        // Top exit point
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(topExit + new Vector3(0.0f, 0.5f, 0.0f), 0.05f);

        // Top corner point
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(topCorner + new Vector3(0.0f, 0.5f, 0.0f), 0.05f);
    }
}

public class AStar : MonoBehaviour
{
    // Private Variables
    LineRenderer lineRenderer;
    IEnumerator pathRoutine;
    private List<Vector3> constructedPath = new List<Vector3>();
    private List<StairCase> staircases = new List<StairCase>();
    private List<Node> openList = new List<Node>();
    private List<Node> closedList = new List<Node>();
    private bool doneFindingPath = false;

    // Getters
    public List<Vector3> CollapsedPath => constructedPath;
    public List<StairCase> GetStaircases => staircases;
    public bool IsDoneFindingPath => doneFindingPath;

    // Debugging data for gizmos
    private WFC _parent;
    private List<Node> pathsNodes = new List<Node>();

    public void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        // Draw start and end point
        Gizmos.color = Color.red;
        for (int i = 0; i < pathsNodes.Count; i++)
        {
            Gizmos.DrawSphere(pathsNodes[i].position, 0.1f);
        }

        // Draw the staircases
        foreach (StairCase staircase in staircases)
        {
            staircase.DrawGizmoBox();
            staircase.DrawGizmoPoints();
        }

        // Draw the field to traverse
        Gizmos.color = Color.blue;
        if (_parent != null)
        {
            for (int height = 0; height < _parent.getHeight; height++)
            {
                Gizmos.DrawLineList(new Vector3[8]{
                    // Line #1
                    new Vector3(-0.5f, height, -0.5f),
                    new Vector3(_parent.getWidth - 0.5f, height, -0.5f),

                    // Line #2
                    new Vector3(_parent.getWidth - 0.5f, height, -0.5f),
                    new Vector3(_parent.getWidth - 0.5f, height, _parent.getLength - 0.5f),

                    // Line #3
                    new Vector3(_parent.getWidth - 0.5f, height, _parent.getLength - 0.5f),
                    new Vector3(-0.5f, height, _parent.getLength - 0.5f),

                    // Line #4
                    new Vector3(-0.5f, height, _parent.getLength - 0.5f),
                    new Vector3(-0.5f, height, -0.5f)
                });
            }
        }

        // Draw the open nodes
        if(openList.Count > 0)
        {
            Gizmos.color = Color.orange;

            foreach(Node node in openList)
            {
                Gizmos.DrawWireCube(node.position, Vector3.one);
            }
        }

        // Draw the closed nodes
        if(closedList.Count > 0)
        {
            Gizmos.color = Color.green;

            foreach(Node node in closedList)
            {
                Gizmos.DrawWireCube(node.position, Vector3.one);
            }
        }
    }

    public void GeneratePath(WFC parent, List<Vector3Int> path)
    {
        this._parent = parent;
        this.doneFindingPath = false;

        if (!CheckPathValidity(path))
        {
            UnityEngine.Debug.LogWarning("Some path points are invalid...");
            return;
        }

        pathRoutine = FindRoute(new List<Vector3Int>(path));
        StartCoroutine(pathRoutine);
    }

    public void StopFindingPath() 
    {
        // Stop the coroutine
        if (pathRoutine != null) StopCoroutine(pathRoutine);
        pathRoutine = null;
        doneFindingPath = true;

        // Reset variables
        openList.Clear();
        closedList.Clear();
    }

    public void ClearPath()
    {
        // Reset variables
        constructedPath.Clear();
        staircases.Clear();
        openList.Clear();
        closedList.Clear();

        // Reset the linerenderer
        if (lineRenderer) lineRenderer.positionCount = 0;
    }

    private List<Vector3> CollapsePath(Node endNode)
    {
        List<Vector3> path = new List<Vector3>();
        Node current = endNode;

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
            if (!Utils.CheckPosValid(p1, _parent.getWidth, _parent.getHeight, _parent.getLength)) return false;

            // If the two points are vertically more than 1 grid tile away from eachother
            if (i < (path.Count - 1) && Math.Abs(p1.y - path[i+1].y) > 1) return false;

            // Compare the current point with all the others
            for (int j = 0; j < path.Count; j++)
            {
                Vector3Int p3 = path[j];

                if (i == j) continue; // This point is the same in both instances, continue
                if (Utils.VecCmp(p1, p3, 0.0f)) return false; // If the point is at the same location as another point
            }
        }

        // No errors found, all points are valid
        return true;
    }

    private IEnumerator FindRoute(List<Vector3Int> points)
    {
        ClearPath(); // Clear previously generated data

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
                foreach (Vector3Int offset in Utils.offsets2.OrderBy(i => Guid.NewGuid()).ToList())
                {
                    // Make sure the new point is placed with the correct offset and level according to the next point
                    Vector3Int levelOffset = currPoint.y < nextPoint.y ? Vector3Int.down : Vector3Int.up;
                    Vector3Int newPos = nextPoint + levelOffset + offset * 3;

                    // The new point is only valid if:
                    // - It is within the level
                    // - and does not overlap with another point
                    // - nor overlaps with a previously autogenerated point either above or below
                    if (Utils.CheckPosValid(newPos, _parent.getWidth, _parent.getHeight, _parent.getLength) 
                    && !Utils.CheckVectorOverlap(points, newPos)
                    && !Utils.CheckVectorOverlap(points, newPos + Vector3Int.down)
                    && !Utils.CheckVectorOverlap(points, newPos + Vector3Int.up))
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
                UnityEngine.Debug.LogWarning($"A staircase cannot be generated in this location: ({currPoint.x}, {currPoint.y}, {currPoint.z})");
            }

            genStairsLoop:;
        }

        // Loop through all paths
        for (int j = 0; j < points.Count - 1; j++)
        {
            // To be added to the final path once finished
            List<Vector3> tempPath = new List<Vector3>();

            // Setup data
            Node startNode = new Node(null, points[j]);
            Node endNode = new Node(null, points[j+1]);

            openList.Add(startNode);

            Debug.Log("Started finding route...");

            // Loop until the end is found
            while (openList.Count > 0)
            {
                // Get the current node
                Node currentNode = openList[0];
                int currentIndex = 0;

                for (int i = 0; i < openList.Count; i++)
                {
                    Node item = openList[i];

                    if (item.f < currentNode.f)
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
                    Debug.Log("Done finding route...");
                    break;
                }

                // Generate children
                List<Node> children = new List<Node>();

                foreach (Vector3Int offset in Utils.offsets3)
                {
                    // Get node position
                    Vector3Int nodePosition = currentNode.position + offset;

                    // Make sure within range of the level
                    if (!Utils.CheckPosValid(nodePosition, _parent.getWidth, _parent.getHeight, _parent.getLength))
                        continue;

                    // Create new node
                    Node newNode = new Node(currentNode, nodePosition);

                    // Append
                    children.Add(newNode);
                }

                // Loop through children
                for (int k = 0; k < children.Count; k++)
                {
                    Node childNode = children[k];

                    // Child is on the closed list
                    foreach (Node closedChild in closedList)
                        if (childNode.Equals(closedChild))
                            continue;

                    // The child is on a staircase when it should not be
                    if ((!CheckStaircaseOverlap(startNode.position)
                    && !CheckStaircaseOverlap(endNode.position))
                    && (CheckStaircaseOverlap(childNode.position)
                    || k > 5))
                        continue;

                    // If we are going downstairs
                    /*if (endNode.position.y < startNode.position.y)
                    {
                        // Make sure the first step always moves forward
                        if (currentNode == startNode 
                        && childNode.position.y < currentNode.position.y)
                            continue;
                        
                        // Make sure the second to first step always moves down
                        if (closedList.Last() != startNode 
                        && closedList.Last().position.y == startNode.position.y
                        && childNode.position.y >= startNode.position.y)
                            continue;
                    }

                    // If we are going upstairs
                    if (endNode.position.y > startNode.position.y)
                    {
                        // Make sure the second to last child always moves up
                        if (Utils.VecCmp(childNode.position, endNode.position)
                        && childNode.position.y != endNode.position.y)
                            continue;
                    }*/
                    
                    // Create the f, g, and h values
                    childNode.g = currentNode.g + 1;
                    childNode.h = Math.Pow(childNode.position[0] - endNode.position[0], 2) + Math.Pow(childNode.position[1] - endNode.position[1], 2) + Math.Pow(childNode.position[2] - endNode.position[2], 2);
                    childNode.f = childNode.g + childNode.h;

                    // Child is already in the open list
                    foreach (Node openNode in openList)
                        if (childNode.Equals(openNode) && childNode.g > openNode.g)
                            continue;
                    
                    // Add the child to the open list
                    openList.Add(childNode);
                }

                Debug.Log($"current status; open nodes: {openList.Count}, closed nodes: {closedList.Count}");

                VisualisePath(tempPath);
                yield return new WaitForSeconds(0.5f);
            }

            // Add the temporary generated path to the permanent one
            constructedPath.AddRange(tempPath);

            // Reset lists
            openList.Clear();
            closedList.Clear();
        }

        doneFindingPath = true;
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
        lineRenderer.SetPositions(constructedPath.Concat(tempPath).Select(p => p + transform.position).ToArray());
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
