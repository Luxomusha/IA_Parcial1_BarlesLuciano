using System.Collections.Generic;
using UnityEngine;

// Walks a list of waypoints (the result of Pathfinder.FindPath) with clamped
// movement, so the agent never overshoots the point it is heading to.
//
// Typical use from a FSM state:
//
//   private List<Vector3> path;
//   private int pathIndex;
//
//   // on Enter / when the target changed:
//   path = pathfinder.FindPath(transform.position, target.position, excludeSafeZones: true);
//   pathIndex = 0;
//
//   // on Update:
//   bool arrived = PathUtils.Follow(transform, path, ref pathIndex, maxSpeed);
//   if (arrived) { /* change state */ }
public static class PathUtils
{
    // Returns true once the whole path has been walked.
    public static bool Follow(Transform mover, List<Vector3> path, ref int index, float maxSpeed, float arriveTolerance = 0.05f)
    {
        if (path == null || path.Count == 0 || index >= path.Count)
            return true;

        Vector3 toTarget = path[index] - mover.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        Vector3 direction = distance > 0.0001f ? toTarget / distance : Vector3.zero;
        float step = Mathf.Min(maxSpeed * Time.deltaTime, distance);
        mover.position += direction * step;

        if (direction != Vector3.zero)
            mover.forward = direction;

        if (distance - step <= arriveTolerance)
            index++;

        return index >= path.Count;
    }
}
