using System;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
    [SerializeField, Min(0f)] private float viewRadius = 7f;
    [SerializeField, Range(1f, 360f)] private float viewAngle = 110f;
    [SerializeField] private float eyeHeight = 0.4f;
    [Tooltip("Layers that block the line of sight (level walls).")]
    [SerializeField] private LayerMask occlusionMask;
    [Tooltip("Maximum colliders checked per query. Raise it when many targets can be in range (boids).")]
    [SerializeField, Min(1)] private int maxColliders = 16;
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(1f, 0.6f, 0f);

    private const int GizmoSegments = 20;

    private Collider[] buffer;

    public float ViewRadius => viewRadius;
    public Vector3 EyePosition => transform.position + Vector3.up * eyeHeight;
    public Color GizmoColor { get => gizmoColor; set => gizmoColor = value; }

    private void Awake()
    {
        buffer = new Collider[maxColliders];
    }

    public bool CanSee(Vector3 point) => IsInViewCone(point) && HasLineOfSight(point);

    public T FindClosestVisible<T>(LayerMask mask, Predicate<T> isValid = null) where T : Component
    {
        return FindClosest(viewRadius, mask, isValid, true, true);
    }

    public T FindClosestInRadius<T>(float radius, LayerMask mask, Predicate<T> isValid = null) where T : Component
    {
        return FindClosest(radius, mask, isValid, false, false);
    }

    private T FindClosest<T>(float radius, LayerMask mask, Predicate<T> isValid, bool requireCone, bool requireSight) where T : Component
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, buffer, mask, QueryTriggerInteraction.Collide);
        T closest = null;
        float bestSqrDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            T candidate = buffer[i].GetComponentInParent<T>();
            if (!IsAcceptable(candidate, isValid, requireCone, requireSight)) continue;

            float sqrDistance = (candidate.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance >= bestSqrDistance) continue;

            bestSqrDistance = sqrDistance;
            closest = candidate;
        }

        return closest;
    }

    public T FindInRadius<T>(float radius, LayerMask mask, Predicate<T> isValid = null) where T : Component
    {
        return Find(radius, mask, isValid, false, false);
    }

    public T FindInSight<T>(LayerMask mask, Predicate<T> isValid = null) where T : Component
    {
        return Find(viewRadius, mask, isValid, false, true);
    }

    public T FindVisible<T>(LayerMask mask, Predicate<T> isValid = null) where T : Component
    {
        return Find(viewRadius, mask, isValid, true, true);
    }

    public bool IsInViewCone(Vector3 point)
    {
        Vector3 toPoint = point - transform.position;
        toPoint.y = 0f;

        if (toPoint.sqrMagnitude > viewRadius * viewRadius) return false;
        if (toPoint.sqrMagnitude < 0.0001f) return true;

        return Vector3.Angle(transform.forward, toPoint) <= viewAngle * 0.5f;
    }

    public bool HasLineOfSight(Vector3 point)
    {
        Vector3 from = EyePosition;
        Vector3 to = new Vector3(point.x, from.y, point.z);
        return !Physics.Linecast(from, to, occlusionMask, QueryTriggerInteraction.Ignore);
    }

    private T Find<T>(float radius, LayerMask mask, Predicate<T> isValid, bool requireCone, bool requireSight) where T : Component
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, buffer, mask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            T candidate = buffer[i].GetComponentInParent<T>();
            if (IsAcceptable(candidate, isValid, requireCone, requireSight))
                return candidate;
        }

        return null;
    }

    private bool IsAcceptable<T>(T candidate, Predicate<T> isValid, bool requireCone, bool requireSight) where T : Component
    {
        if (candidate == null) return false;
        if (isValid != null && !isValid(candidate)) return false;

        Vector3 position = candidate.transform.position;
        if (requireCone && !IsInViewCone(position)) return false;
        if (requireSight && !HasLineOfSight(position)) return false;

        return true;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = GizmoColor;
        Vector3 eye = EyePosition;
        Vector3 left = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.forward;

        Gizmos.DrawLine(eye, eye + left * viewRadius);
        Gizmos.DrawLine(eye, eye + right * viewRadius);
        DrawArc(eye);
    }

    private void DrawArc(Vector3 eye)
    {
        Vector3 previous = eye + Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward * viewRadius;

        for (int i = 1; i <= GizmoSegments; i++)
        {
            float angle = -viewAngle * 0.5f + viewAngle * i / GizmoSegments;
            Vector3 point = eye + Quaternion.Euler(0f, angle, 0f) * transform.forward * viewRadius;
            Gizmos.DrawLine(previous, point);
            previous = point;
        }
    }
}
