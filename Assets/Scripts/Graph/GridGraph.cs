using System.Collections.Generic;
using UnityEngine;

public class GridGraph : MonoBehaviour
{
    private static readonly (int dRow, int dColumn)[] Directions =
    {
        (1, 0), (-1, 0), (0, 1), (0, -1)
    };

    [Header("Grid")]
    [Tooltip("World position of the grid corner (row 0, column 0).")]
    [SerializeField] private Vector3 origin = Vector3.zero;
    [SerializeField] private int rows = 20;
    [SerializeField] private int columns = 20;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private float nodeHeight = 0f;

    [Header("Obstacle detection")]
    [Tooltip("Layer used by the level walls and obstacles.")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float obstacleCheckRadius = 0.4f;

    [Header("Safe zones")]
    [Tooltip("Layer used by the colliders that mark the Safe zone floors.")]
    [SerializeField] private LayerMask safeZoneMask;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [Tooltip("Regenerates the grid while editing so the gizmos follow the level.")]
    [SerializeField] private bool previewInEditor = true;

    private GridNode[,] grid;

    private void Awake() => GenerateGrid();

    public void GenerateGrid()
    {
        grid = new GridNode[rows, columns];

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                Vector3 worldPosition = origin + new Vector3(column * cellSize, nodeHeight, row * cellSize);
                GridNode node = new GridNode(new Coordinates(row, column), worldPosition);

                node.Blocked = Physics.CheckSphere(worldPosition, obstacleCheckRadius, obstacleMask, QueryTriggerInteraction.Collide);
                node.IsSafeZone = Physics.CheckSphere(worldPosition, obstacleCheckRadius, safeZoneMask, QueryTriggerInteraction.Collide);

                grid[row, column] = node;
            }
        }
    }

    public GridNode GetNodeFromWorldPosition(Vector3 worldPosition)
    {
        int column = Mathf.RoundToInt((worldPosition.x - origin.x) / cellSize);
        int row = Mathf.RoundToInt((worldPosition.z - origin.z) / cellSize);

        if (!IsInsideGrid(row, column)) return null;
        return grid[row, column];
    }

    public List<GridNode> GetNeighbors(GridNode current, bool excludeSafeZones = false)
    {
        List<GridNode> neighbors = new();

        foreach (var (dRow, dColumn) in Directions)
        {
            int row = current.Coordinates.Row + dRow;
            int column = current.Coordinates.Column + dColumn;

            if (!IsInsideGrid(row, column)) continue;

            GridNode neighbor = grid[row, column];
            if (neighbor.Blocked) continue;
            if (excludeSafeZones && neighbor.IsSafeZone) continue;

            neighbors.Add(neighbor);
        }
        return neighbors;
    }

    private bool IsInsideGrid(int row, int column) =>
        row >= 0 && row < rows && column >= 0 && column < columns;

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        if (!Application.isPlaying && previewInEditor) GenerateGrid();
        if (grid == null) return;

        foreach (GridNode node in grid)
        {
            Gizmos.color = node.Blocked ? Color.red : (node.IsSafeZone ? Color.cyan : Color.green);
            Gizmos.DrawWireCube(node.WorldPosition, Vector3.one * (cellSize * 0.2f));
        }
    }
}
