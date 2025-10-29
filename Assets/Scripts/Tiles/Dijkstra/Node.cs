using UnityEngine;
using System.Collections.Generic;

public class Node
{
    public Vector3 worldPosition;
    public Vector2Int gridPosition;
    public int tileIndex;
    public List<Edge> edges;
    public float gCost; // Costo desde el inicio
    public float hCost; // Heurística hasta el final
    public Node parent;
    public bool isWalkable;

    public float FCost => gCost + hCost;

    public Node(Vector3 worldPos, Vector2Int gridPos, int tileIdx, bool walkable = true)
    {
        worldPosition = worldPos;
        gridPosition = gridPos;
        tileIndex = tileIdx;
        edges = new List<Edge>();
        isWalkable = walkable;
        gCost = float.MaxValue;
        hCost = 0;
        parent = null;
    }
}

public class Edge
{
    public Node targetNode;
    public float cost;

    public Edge(Node target, float edgeCost = 1f)
    {
        targetNode = target;
        cost = edgeCost;
    }
}