using System.Collections.Generic;
using UnityEngine;

public static class PathUtils
{
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
