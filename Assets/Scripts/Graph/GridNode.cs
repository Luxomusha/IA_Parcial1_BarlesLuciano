using UnityEngine;

// Graph node. Not a MonoBehaviour on purpose: there is no need for a real
// GameObject per cell. The whole grid lives as a matrix inside GridGraph.
public class GridNode
{
    public Coordinates Coordinates { get; }
    public Vector3 WorldPosition { get; }

    // true = there is an obstacle here, the node can't be walked.
    public bool Blocked { get; set; }

    // true = the node is inside a Safe zone. Wolves skip it as a neighbor,
    // the Hunter can use it (the exclusion is decided in GetNeighbors).
    public bool IsSafeZone { get; set; }

    public GridNode(Coordinates coordinates, Vector3 worldPosition)
    {
        Coordinates = coordinates;
        WorldPosition = worldPosition;
    }
}
