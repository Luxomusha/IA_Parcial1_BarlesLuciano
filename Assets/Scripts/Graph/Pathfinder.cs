using System.Collections.Generic;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    [SerializeField] private GridGraph graph;

    public List<Vector3> FindPath(
        Vector3 startPosition,
        Vector3 goalPosition,
        bool excludeSafeZones = false)
    {
        List<Vector3> waypoints = new();

        if (graph == null)
        {
            Debug.LogWarning("Pathfinder needs a GridGraph reference.", this);
            return waypoints;
        }

        GridNode start = graph.GetNodeFromWorldPosition(startPosition);
        GridNode goal = graph.GetNodeFromWorldPosition(goalPosition);

        if (start == null || goal == null)
            return waypoints;

        if (start.Blocked || goal.Blocked)
            return waypoints;

        if (excludeSafeZones && (start.IsSafeZone || goal.IsSafeZone))
            return waypoints;

        List<GridNode> nodePath = AStar(start, goal, excludeSafeZones);

        foreach (GridNode node in nodePath)
            waypoints.Add(node.WorldPosition);

        return waypoints;
    }

    private List<GridNode> AStar(
        GridNode start,
        GridNode goal,
        bool excludeSafeZones)
    {
        List<GridNode> openSet = new() { start };
        HashSet<GridNode> closedSet = new();

        Dictionary<GridNode, float> gScore = new()
        {
            [start] = 0f
        };

        Dictionary<GridNode, float> fScore = new()
        {
            [start] = Heuristic(start, goal)
        };

        Dictionary<GridNode, GridNode> cameFrom = new();

        while (openSet.Count > 0)
        {
            GridNode current = openSet[0];

            for (int i = 1; i < openSet.Count; i++)
            {
                GridNode candidate = openSet[i];

                if (fScore[candidate] < fScore[current])
                    current = candidate;
            }

            if (current == goal)
                return ReconstructPath(cameFrom, current);

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (GridNode neighbor in
                     graph.GetNeighbors(current, excludeSafeZones))
            {
                if (closedSet.Contains(neighbor))
                    continue;

                float tentativeG = gScore[current] + 1f;

                if (gScore.TryGetValue(neighbor, out float knownG)
                    && tentativeG >= knownG)
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeG;
                fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);

                if (!openSet.Contains(neighbor))
                    openSet.Add(neighbor);
            }
        }

        return new List<GridNode>();
    }

    private float Heuristic(GridNode a, GridNode b)
    {
        return Mathf.Abs(a.Coordinates.Row - b.Coordinates.Row)
             + Mathf.Abs(a.Coordinates.Column - b.Coordinates.Column);
    }

    private List<GridNode> ReconstructPath(
        Dictionary<GridNode, GridNode> cameFrom,
        GridNode current)
    {
        List<GridNode> path = new() { current };

        while (cameFrom.TryGetValue(current, out GridNode previous))
        {
            current = previous;
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}