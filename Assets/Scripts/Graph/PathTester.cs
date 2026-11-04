using System.Collections.Generic;
using UnityEngine;

public class PathTester : MonoBehaviour
{
    [SerializeField] private Pathfinder pathfinder;
    [SerializeField] private Transform start;
    [SerializeField] private Transform goal;
    [SerializeField] private bool excludeSafeZones;
    [SerializeField] private Color pathColor = Color.yellow;

    private List<Vector3> path = new();

    private void Update()
    {
        if (pathfinder != null && start != null && goal != null)
            path = pathfinder.FindPath(start.position, goal.position, excludeSafeZones);
    }

    private void OnDrawGizmos()
    {
        if (path == null || path.Count == 0) return;

        Gizmos.color = pathColor;
        for (int i = 0; i < path.Count - 1; i++)
            Gizmos.DrawLine(path[i] + Vector3.up * 0.1f, path[i + 1] + Vector3.up * 0.1f);

        foreach (Vector3 point in path)
            Gizmos.DrawSphere(point + Vector3.up * 0.1f, 0.12f);
    }
}
