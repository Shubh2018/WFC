using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStar : MonoBehaviour
{
    private class Node : IEquatable<Node>
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

    LineRenderer lineRenderer;
    IEnumerator pathRoutine;
    private List<Vector3> constructedPath = new List<Vector3>();
    private List<Vector3Int> stairsPoints = new List<Vector3Int>();
    private List<Vector3Int> blockedPoints = new List<Vector3Int>();
    private List<Node> openList = new List<Node>();
    private List<Node> closedList = new List<Node>();
    private List<Vector3Int> pathPoints;
    private Vector3Int[] offsets = new Vector3Int[]{
        Vector3Int.forward,
        Vector3Int.back,
        Vector3Int.right,
        Vector3Int.left,
        Vector3Int.up,
        Vector3Int.down,
        Vector3Int.forward + Vector3Int.left,
        Vector3Int.forward + Vector3Int.right,
        Vector3Int.back + Vector3Int.left,
        Vector3Int.back + Vector3Int.right,
        Vector3Int.forward + Vector3Int.left + Vector3Int.up,
        Vector3Int.forward + Vector3Int.right + Vector3Int.up,
        Vector3Int.back + Vector3Int.left + Vector3Int.up,
        Vector3Int.back + Vector3Int.right + Vector3Int.up,
        Vector3Int.forward + Vector3Int.left + Vector3Int.down,
        Vector3Int.forward + Vector3Int.right + Vector3Int.down,
        Vector3Int.back + Vector3Int.left + Vector3Int.down,
        Vector3Int.back + Vector3Int.right + Vector3Int.down
    };
    private Vector3Int[] offsetsStairs = new Vector3Int[]{
        Vector3Int.forward,
        Vector3Int.back,
        Vector3Int.right,
        Vector3Int.left
    };
    private bool doneFindingPath = false;

    public List<Vector3> CollapsedPath => constructedPath;
    public List<Vector3Int> StaircasePoints => blockedPoints;
    public bool IsDoneFindingPath => doneFindingPath;

    // Debugging data for gizmos
    private WFC _parent;
    private List<Node> pathsNodes = new List<Node>();
    private Vector3 baseOffsetPos = new Vector3(-0.5f, 0.0f, -0.5f);
    private bool _settingMoveDiagonal = false;

    public void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        // Draw start and end point
        Gizmos.color = Color.red;
        for (int i = 0; i < pathsNodes.Count; i++)
        {
            Gizmos.DrawSphere(pathsNodes[i].position, 0.1f);
        }

        // Draw the stairs points
        Gizmos.color = Color.blue;
        foreach (Vector3Int point in stairsPoints)
        {
            Gizmos.DrawSphere(point, 0.1f);
        }

        // Draw the blocked areas
        Gizmos.color = Color.red;
        foreach (Vector3Int point in blockedPoints)
        {
            Gizmos.DrawWireCube(point, Vector3.one);
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

    public void UpdateSettings(bool moveDiagonal)
    {
        this._settingMoveDiagonal = moveDiagonal;
    }

    public void GeneratePath(WFC parent, List<Vector3Int> path)
    {
        this._parent = parent;
        this.pathPoints = path;
        this.doneFindingPath = false;
        pathRoutine = FindRoute(path);
        StartCoroutine(pathRoutine);
    }

    public void StopFindingPath() 
    {
        if (pathRoutine != null) StopCoroutine(pathRoutine);
        pathRoutine = null;

        // Reset variables
        constructedPath.Clear();
        openList.Clear();
        closedList.Clear();
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

    private IEnumerator FindRoute(List<Vector3Int> points)
    {
        // Reset lists
        constructedPath.Clear();

        List<Vector3Int> pointsTemp = new List<Vector3Int>(points);
        List<Vector3Int> pointsAutoGen = new List<Vector3Int>();
        List<Vector3Int> pointsBlocked = new List<Vector3Int>();

        // Used to reposition nodes across levels for adding stairs correctly
        for (int j = 0; j < pointsTemp.Count - 1; j++)
        {
            if(pointsTemp[j].y != pointsTemp[j+1].y) 
            {
                Vector3Int newOffsetPos = new Vector3Int(999, 999, 999);

                foreach (Vector3Int offset in offsetsStairs)
                {
                    // Make sure the new point is placed with the correct offset and level according to the next point
                    Vector3Int levelOffset = pointsTemp[j].y < pointsTemp[j+1].y ? Vector3Int.down : Vector3Int.up;
                    Vector3Int newPos = pointsTemp[j+1] + levelOffset + offset * 3;

                    // The new point is only valid if:
                    // - It is within the level
                    // - and does not overlap with another point
                    // - nor overlaps with a previously autogenerated point
                    if (CheckPosValid(newPos) 
                    && !CheckVectorOverlap(pointsTemp, newPos)
                    && !CheckVectorOverlap(pointsAutoGen, newPos + Vector3Int.down)
                    && !CheckVectorOverlap(pointsAutoGen, newPos + Vector3Int.up))
                    {
                        newOffsetPos = newPos;
                        pointsBlocked.Add(newPos - offset);
                        pointsBlocked.Add(newPos - offset * 2);
                        pointsBlocked.Add(newPos - levelOffset - offset);
                        pointsBlocked.Add(newPos - levelOffset - offset * 2);
                    }
                }

                // Insert the point into the points list
                pointsTemp.Insert(j++ + 1, newOffsetPos);
                pointsAutoGen.Add(newOffsetPos);
            }

            stairsPoints = pointsAutoGen;
            blockedPoints = pointsBlocked;
        }

        // Loop through all paths
        for (int j = 0; j < pointsTemp.Count - 1; j++)
        {
            // To be added to the final path once finished
            List<Vector3> tempPath = new List<Vector3>();

            // Setup data
            Node startNode = new Node(null, pointsTemp[j]);
            Node endNode = new Node(null, pointsTemp[j+1]);

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

                for (int k = 0; k < offsets.Length; k++)
                {
                    // If this offset is diagonal and the setting is off, break
                    if (!_settingMoveDiagonal && k > 5)
                        break;

                    // Get node position
                    Vector3Int nodePosition = currentNode.position + offsets[k];

                    // Make sure within range of the level
                    if (!CheckPosValid(nodePosition))
                        continue;

                    // Create new node
                    Node newNode = new Node(currentNode, nodePosition);

                    // Append
                    children.Add(newNode);
                }

                // Loop through children
                foreach (Node childNode in children)
                {
                    // Child is on the closed list
                    foreach (Node closedChild in closedList)
                        if (childNode.Equals(closedChild))
                            continue;

                    // The child is on a blocked path point
                    if (startNode.position.y == endNode.position.y 
                    && CheckVectorOverlap(pointsBlocked, childNode.position, 0.5f))
                        continue;


                    // If we are going downstairs
                    if (endNode.position.y < startNode.position.y)
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
                        if (VecCmp(childNode.position, endNode.position)
                        && childNode.position.y != endNode.position.y)
                            continue;
                    }
                    
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
        if (lineRenderer == null)
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

    private bool VecCmp(Vector3Int a, Vector3Int b, float distance = 1.0f)
    {
        return Vector3Int.Distance(a, b) <= distance;
    }

    private bool CheckPosValid(Vector3Int pos)
    {
        return (pos.x < _parent.getWidth 
             && pos.x >= 0
             && pos.y < _parent.getHeight
             && pos.y >= 0
             && pos.z < _parent.getLength
             && pos.z >= 0);
    }

    public bool CheckVectorOverlap(List<Vector3Int> points, Vector3Int pos, float distance = 1.0f)
    {
        return points.Exists(point => VecCmp(point, pos, distance));
    }
}
