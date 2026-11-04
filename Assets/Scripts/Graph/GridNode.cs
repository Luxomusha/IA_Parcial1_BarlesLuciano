using UnityEngine;

public class GridNode
{
    public Coordinates Coordinates { get; }
    public Vector3 WorldPosition { get; }

    public bool Blocked { get; set; }

    public bool IsSafeZone { get; set; }

    public GridNode(Coordinates coordinates, Vector3 worldPosition)
    {
        Coordinates = coordinates;
        WorldPosition = worldPosition;
    }
}
