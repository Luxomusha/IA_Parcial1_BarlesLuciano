using System;
using UnityEngine;

[RequireComponent(typeof(FieldOfView), typeof(BoidFlocking), typeof(BoidGatherable))]
[RequireComponent(typeof(AgentAudio))]
public class Boid : Agent
{
    private const float FacingThreshold = 0.01f;

    [Header("Movement")]
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float maxSteering = 10f;
    [Tooltip("Degrees per second the boid turns towards its velocity. 0 = instant.")]
    [SerializeField, Min(0f)] private float turnSpeed = 360f;

    [Header("Evade")]
    [SerializeField] private float evadeRadius = 6f;
    [SerializeField] private LayerMask hunterMask;

    [Header("Bait")]
    [SerializeField] private float baitDetectionRadius = 10f;
    [SerializeField] private float arriveSlowRadius = 2f;
    [SerializeField] private float interactRadius = 0.5f;
    [SerializeField] private LayerMask baitMask;

    [Header("Area")]
    [Tooltip("Area the boid wraps around and respawns in.")]
    [SerializeField] private FlockArea flockArea;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;

    private static readonly Predicate<Bait> IsAvailableBait = bait => bait.IsAvailable;

    private FieldOfView perception;
    private BoidFlocking flocking;
    private float spawnHeight;

    public BoidGatherable Gatherable { get; private set; }
    public bool IsHooked => Gatherable != null && Gatherable.IsHooked;
    public bool IsDown => Gatherable != null && Gatherable.IsDown;
    public bool IsFlocking => !IsHooked && !IsDown;

    private void Awake()
    {
        perception = GetComponent<FieldOfView>();
        flocking = GetComponent<BoidFlocking>();
        Gatherable = GetComponent<BoidGatherable>();
        spawnHeight = transform.position.y;
    }

    private void Start()
    {
        _velocity = flocking.WanderDirection * maxSpeed * 0.5f;
    }

    private void Update()
    {
        if (!IsFlocking) return;

        flocking.DetectNeighbors();
        Vector3 steering = ChooseSteering(out Bait reachedBait);

        if (reachedBait != null)
        {
            reachedBait.Consume(this);
            return;
        }

        Move(steering);
    }

    public void Stop() => _velocity = Vector3.zero;

    public void Relaunch()
    {
        _velocity = Steering.RandomPlanarDirection() * maxSpeed * 0.5f;
        if (flockArea == null) return;

        Vector3 point = flockArea.RandomPoint();
        point.y = spawnHeight;
        transform.position = point;
    }

    private Vector3 ChooseSteering(out Bait reachedBait)
    {
        reachedBait = null;

        Hunter threat = perception.FindInRadius<Hunter>(evadeRadius, hunterMask);
        if (threat != null)
            return Steering.Evade(transform.position, _velocity, maxSpeed, threat.transform.position, threat.Velocity);

        Bait bait = perception.FindClosestInRadius(baitDetectionRadius, baitMask, IsAvailableBait);
        if (bait != null)
            return SteerToBait(bait, out reachedBait);

        return flocking.FlockSteering(_velocity, maxSpeed);
    }

    private Vector3 SteerToBait(Bait bait, out Bait reachedBait)
    {
        Vector3 baitPosition = bait.transform.position;
        reachedBait = Vector3.Distance(transform.position, baitPosition) <= interactRadius ? bait : null;
        if (reachedBait != null) return Vector3.zero;

        return Steering.Arrive(transform.position, _velocity, maxSpeed, baitPosition, arriveSlowRadius)
             + flocking.SeparationSteering();
    }

    private void Move(Vector3 steering)
    {
        steering = Vector3.ClampMagnitude(steering, maxSteering * Time.deltaTime);
        _velocity = Vector3.ClampMagnitude(_velocity + steering, maxSpeed);
        _velocity.y = 0f;

        transform.position += _velocity * Time.deltaTime;
        if (flockArea != null)
            transform.position = flockArea.Wrap(transform.position);

        flocking.ResolveOverlap();
        FaceVelocity();
    }

    private void FaceVelocity()
    {
        if (_velocity.sqrMagnitude <= FacingThreshold) return;

        Quaternion look = Quaternion.LookRotation(_velocity.normalized);
        transform.rotation = turnSpeed <= 0f
            ? look
            : Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, evadeRadius);
    }
}
