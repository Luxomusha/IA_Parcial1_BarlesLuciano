using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Agent))]
public class PathMover : MonoBehaviour
{
    private const float PathWaypointTolerance = 0.05f;
    private const float RepathDistance = 0.75f;
    private const float ArriveDistance = 0.3f;
    private const float ReachableCheckRadius = 0.4f;

    [Tooltip("Leave empty to always move in a straight line (no A*).")]
    [SerializeField] private Pathfinder pathfinder;
    [Tooltip("Degrees per second. 0 = face the movement direction instantly.")]
    [SerializeField, Min(0f)] private float turnSpeed = 540f;

    [Header("Collision")]
    [Tooltip("Layers that block movement (level walls).")]
    [SerializeField] private LayerMask obstacleMask;
    [Tooltip("Zones this agent never steps on (Safe zones). Also excluded from its A* paths.")]
    [SerializeField] private LayerMask forbiddenMask;
    [Tooltip("Optional: if set, this capsule is swept against the walls so the agent never goes through them.")]
    [SerializeField] private CapsuleCollider movementCollider;
    [Tooltip("Small world-space clearance kept between the capsule and the walls.")]
    [SerializeField, Min(0f)] private float collisionSkin = 0.01f;
    [Tooltip("Radius used to check if the way is clear when there is no capsule.")]
    [SerializeField, Min(0.05f)] private float bodyRadius = 0.25f;
    [Tooltip("Height of the clear-way check when there is no capsule.")]
    [SerializeField] private float castHeight = 0.4f;

    [Header("Debug")]
    [Tooltip("Logs a warning when A* finds no path to the requested goal.")]
    [SerializeField] private bool warnWhenNoPath;
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color pathColor = Color.cyan;

    private Agent agent;
    private List<Vector3> path = new List<Vector3>();
    private int pathIndex;
    private Vector3 pathGoal;
    private bool hasPathRequest;
    private bool warnedNoPath;

    public bool HasPathfinder => pathfinder != null;
    public bool IsUsingPathfinding => hasPathRequest && path.Count > 0 && pathIndex < path.Count;
    public bool PathFailed => hasPathRequest && path.Count == 0;

    private bool AvoidsForbiddenZones => forbiddenMask.value != 0;
    private Vector3 CastOrigin => transform.position + Vector3.up * castHeight;
    private LayerMask BlockingMask => obstacleMask | forbiddenMask;
    private QueryTriggerInteraction BlockingQuery =>
        AvoidsForbiddenZones ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;

    private void Awake()
    {
        agent = GetComponent<Agent>();
    }

    public float MoveToTarget(Vector3 target, float speed, float stopDistance = 0f)
    {
        if (pathfinder == null || HasClearPath(target))
        {
            ClearPath();
            return MoveDirectly(target, speed, stopDistance);
        }

        FollowPathTo(target, speed);
        return PlanarDistance(target);
    }

    public bool FollowPathTo(Vector3 goal, float speed)
    {
        if (pathfinder == null) return MoveDirectly(goal, speed) <= PathWaypointTolerance;

        if (NeedsNewPath(goal))
            RequestPath(goal);

        if (path.Count == 0 || pathIndex >= path.Count)
        {
            Stop();
            return path.Count > 0;
        }

        if (MoveDirectly(path[pathIndex], speed) <= PathWaypointTolerance)
            pathIndex++;

        return pathIndex >= path.Count;
    }

    public float MoveDirectly(Vector3 target, float speed, float stopDistance = 0f)
    {
        Vector3 toTarget = target - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        if (distance < 0.0001f)
        {
            Stop();
            return 0f;
        }

        Vector3 direction = toTarget / distance;
        float step = Mathf.Min(speed * Time.deltaTime, Mathf.Max(0f, distance - stopDistance));
        if (step > 0f)
            step = LimitStep(direction, step);

        agent.SetVelocity(Time.deltaTime > 0f ? direction * (step / Time.deltaTime) : Vector3.zero);
        transform.position += direction * step;
        Face(direction);

        return distance - step;
    }

    public void ClearPath()
    {
        path.Clear();
        pathIndex = 0;
        hasPathRequest = false;
    }

    public List<Vector3> FindPath(Vector3 goal) =>
        pathfinder != null ? pathfinder.FindPath(transform.position, goal, AvoidsForbiddenZones) : new List<Vector3>();

    public bool HasClearPath(Vector3 point)
    {
        Vector3 toTarget = point - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;
        if (distance < 0.0001f) return true;

        Vector3 direction = toTarget / distance;
        if (movementCollider != null)
        {
            GetCapsule(out Vector3 bottom, out Vector3 top, out float radius);
            return !Physics.CapsuleCast(bottom, top, radius, direction, distance, BlockingMask, BlockingQuery);
        }

        return !Physics.SphereCast(CastOrigin, bodyRadius, direction, out _, distance, BlockingMask, BlockingQuery);
    }

    public bool IsReachable(Vector3 point)
    {
        if (pathfinder == null) return false;
        if (IsInsideObstacle(new Vector3(point.x, CastOrigin.y, point.z), ReachableCheckRadius)) return false;

        return FindPath(point).Count > 0;
    }

    public bool IsInsideObstacle(Vector3 point, float radius) =>
        Physics.CheckSphere(point, radius, obstacleMask, QueryTriggerInteraction.Ignore);

    public void Face(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion look = Quaternion.LookRotation(direction);
        transform.rotation = turnSpeed <= 0f
            ? look
            : Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
    }

    public void Turn(float degrees) => transform.Rotate(0f, degrees, 0f);

    public void Stop() => agent.SetVelocity(Vector3.zero);

    public float PlanarDistance(Vector3 target)
    {
        Vector3 delta = target - transform.position;
        delta.y = 0f;
        return delta.magnitude;
    }

    public bool HasArrived(Vector3 target) => PlanarDistance(target) <= ArriveDistance;

    private bool NeedsNewPath(Vector3 goal)
    {
        return !hasPathRequest || (goal - pathGoal).sqrMagnitude > RepathDistance * RepathDistance;
    }

    private void RequestPath(Vector3 goal)
    {
        path = FindPath(goal);
        pathGoal = goal;
        hasPathRequest = true;
        pathIndex = path.Count > 1 && HasClearPath(path[1]) ? 1 : 0;

        WarnIfNoPath(goal);
    }

    private void WarnIfNoPath(Vector3 goal)
    {
        if (path.Count > 0)
        {
            warnedNoPath = false;
            return;
        }

        if (!warnWhenNoPath || warnedNoPath) return;
        warnedNoPath = true;
        Debug.LogWarning($"{name}: no path from {transform.position} to {goal}.", this);
    }

    private float LimitStep(Vector3 direction, float step)
    {
        if (IsForbidden(transform.position + direction * step)) return 0f;
        if (movementCollider == null) return step;

        GetCapsule(out Vector3 bottom, out Vector3 top, out float radius);
        if (Physics.CheckCapsule(bottom, top, radius, obstacleMask, QueryTriggerInteraction.Ignore)) return 0f;

        if (Physics.CapsuleCast(bottom, top, radius, direction, out RaycastHit hit,
                step + collisionSkin, obstacleMask, QueryTriggerInteraction.Ignore))
            return Mathf.Min(step, Mathf.Max(0f, hit.distance - collisionSkin));

        return step;
    }

    private bool IsForbidden(Vector3 position) =>
        AvoidsForbiddenZones && Physics.CheckSphere(position, bodyRadius, forbiddenMask, QueryTriggerInteraction.Collide);

    private void GetCapsule(out Vector3 bottom, out Vector3 top, out float radius)
    {
        UnityEngine.Bounds bounds = movementCollider.bounds;
        radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
        float halfSegment = Mathf.Max(0f, bounds.extents.y - radius);
        bottom = bounds.center - Vector3.up * halfSegment;
        top = bounds.center + Vector3.up * halfSegment;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos || !Application.isPlaying || !IsUsingPathfinding) return;

        Gizmos.color = pathColor;
        Vector3 lift = Vector3.up * 0.2f;
        for (int i = Mathf.Max(pathIndex, 1); i < path.Count; i++)
            Gizmos.DrawLine(path[i - 1] + lift, path[i] + lift);
        Gizmos.DrawLine(transform.position + lift, path[pathIndex] + lift);
    }
}
