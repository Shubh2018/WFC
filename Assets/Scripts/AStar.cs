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
    private List<Node> openList = new List<Node>();
    private List<Node> closedList = new List<Node>();
    private List<Vector3Int> pathPoints;
    private Vector3Int[] offsets = new Vector3Int[]{
        Vector3Int.forward,
        Vector3Int.back,
        Vector3Int.right,
        Vector3Int.left,
        Vector3Int.forward + Vector3Int.left,
        Vector3Int.forward + Vector3Int.right,
        Vector3Int.back + Vector3Int.left,
        Vector3Int.back + Vector3Int.right
    };

    public List<Vector3> CollapsedPath => constructedPath;

    // Debugging data for gizmos
    private List<Node> pathsNodes = new List<Node>();
    private int width, length;
    private Vector3 baseOffsetPos = new Vector3(-0.5f, 0.0f, -0.5f);

    public void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        // Draw start and end point
        Gizmos.color = Color.red;
        for (int i = 0; i < pathsNodes.Count; i++)
        {
            Gizmos.DrawSphere(pathsNodes[i].position, 0.1f);
        }

        // Draw the field to traverse
        Gizmos.color = Color.blue;
        Gizmos.DrawLineList(new Vector3[8]{

            // Line #1
            new Vector3(-0.5f, 1.0f, -0.5f),
            new Vector3(width - 0.5f, 1.0f, -0.5f),

            // Line #2
            new Vector3(width - 0.5f, 1.0f, -0.5f),
            new Vector3(width - 0.5f, 1.0f, length - 0.5f),

            // Line #3
            new Vector3(width - 0.5f, 1.0f, length - 0.5f),
            new Vector3(-0.5f, 1.0f, length - 0.5f),

            // Line #4
            new Vector3(-0.5f, 1.0f, length - 0.5f),
            new Vector3(-0.5f, 1.0f, -0.5f)
        });

        // Draw the open nodes
        if(openList.Count > 0)
        {
            Gizmos.color = Color.red;

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

    public void GeneratePath(int width, int length, List<Vector3Int> path)
    {
        this.width = width;
        this.length = length;
        this.pathPoints = path;
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
            path.Add(current.position + transform.position);
            current = current.parent;
        }

        path.Reverse();
        return path;
    }

    private IEnumerator FindRoute(List<Vector3Int> paths)
    {
        // Reset lists
        constructedPath.Clear();

        // Loop through all paths
        for (int j = 0; j < paths.Count - 1; j++)
        {
            // To be added to the final path once finished
            List<Vector3> tempPath = new List<Vector3>();

            // Setup data
            Node startNode = new Node(null, paths[j]);
            Node endNode = new Node(null, paths[j+1]);

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

                foreach (Vector3Int offset in offsets)
                {
                    // Get node position
                    Vector3Int nodePosition = currentNode.position + offset;

                    // Make sure within range
                    if (nodePosition[0] > width 
                    || nodePosition[0] < 0
                    || nodePosition[2] > length
                    || nodePosition[2] < 0)
                        continue;

                    // Make sure walkable terrain
                    // TODO

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
                    
                    // Create the f, g, and h values
                    childNode.g = currentNode.g + 1;
                    childNode.h = Math.Pow(childNode.position[0] - endNode.position[0], 2) + Math.Pow(childNode.position[2] - endNode.position[2], 2);
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
        Debug.Log($"lineRenderer path length: {constructedPath.Count}");
        lineRenderer.SetPositions(constructedPath.Concat(tempPath).ToArray());
    }
}
