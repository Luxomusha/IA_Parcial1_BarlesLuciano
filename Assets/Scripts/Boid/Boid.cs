using System.Collections.Generic;
using UnityEngine;

public class Boid : Agent
{
    [Header("Movement")]
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float maxSteering = 10f;

    [Header("Perception radii")]
    [SerializeField] private float separationRadius = 1.5f;
    [SerializeField] private float neighborRadius = 4f;

    [Header("Weights")]
    [SerializeField] private float separationWeight = 1.5f;
    [SerializeField] private float alignmentWeight = 1f;
    [SerializeField] private float cohesionWeight = 1f;

    [Header("Wander (keeps the flock moving)")]
    [SerializeField] private float wanderWeight = 0.5f;
    [SerializeField] private float wanderJitter = 2f;

    [Header("Evade")]
    [SerializeField] private float evadeRadius = 6f;

    [Header("Bait")]
    [SerializeField] private float baitDetectionRadius = 10f;
    [SerializeField] private float arriveSlowRadius = 2f;
    [SerializeField] private float interactRadius = 0.5f;

    [Header("Detection")]
    [SerializeField] private LayerMask boidMask;
    [SerializeField] private LayerMask hunterMask;
    [SerializeField] private LayerMask baitMask;
    [SerializeField] private int maxDetectedColliders = 32;

    [Header("Gather feedback")]
    [Tooltip("Scale the boid shrinks to while the Hunter is gathering it.")]
    [SerializeField] private float gatheredMinScale = 0.3f;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;

    [Header("Audio")]
    [SerializeField] private AudioClip painSound;

    private AudioSource audioSource;
    private Vector3 wanderDirection;
    private Vector3 originalScale;
    private float spawnHeight;

    private Collider[] detectionBuffer;
    private readonly List<Boid> neighbors = new List<Boid>();
    private readonly List<Boid> closeNeighbors = new List<Boid>();
    private readonly List<Boid> overlappingBoids = new List<Boid>();

    public bool IsHooked { get; private set; }
    public bool IsDown { get; private set; }
    private bool IsFlocking => !IsHooked && !IsDown;

    private void Awake()
    {
        detectionBuffer = new Collider[maxDetectedColliders];
        originalScale = transform.localScale;
        spawnHeight = transform.position.y;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        wanderDirection = RandomPlanarDirection();
        _velocity = wanderDirection * maxSpeed * 0.5f;
    }

    private void Update()
    {
        if (!IsFlocking)
            return;

        DetectNearbyBoids();

        Vector3 steering;
        Agent hunter = DetectHunter();

        if (hunter != null)
        {
            steering = Evade(hunter);
        }
        else if (DetectClosestBait() is Bait bait)
        {
            float distance = Vector3.Distance(transform.position, bait.transform.position);
            if (distance <= interactRadius)
            {
                bait.Consume(this);
                return;
            }

            steering = Arrive(bait.transform.position) + Separation() * separationWeight;
        }
        else
        {
            steering = Separation() * separationWeight
                     + Alignment() * alignmentWeight
                     + Cohesion() * cohesionWeight
                     + Wander() * wanderWeight;
        }

        steering = Vector3.ClampMagnitude(steering, maxSteering * Time.deltaTime);
        _velocity = Vector3.ClampMagnitude(_velocity + steering, maxSpeed);
        _velocity.y = 0f;

        transform.position += _velocity * Time.deltaTime;
        if (FlockArea.Instance != null)
            transform.position = FlockArea.Instance.Wrap(transform.position);

        ResolveOverlap();

        if (_velocity.sqrMagnitude > 0.01f)
            transform.forward = _velocity.normalized;
    }

    #region Detection

    private int Detect(float radius, LayerMask mask) =>
        Physics.OverlapSphereNonAlloc(transform.position, radius, detectionBuffer, mask, QueryTriggerInteraction.Collide);

    private void DetectNearbyBoids()
    {
        neighbors.Clear();
        closeNeighbors.Clear();
        overlappingBoids.Clear();

        float sqrNeighborRadius = neighborRadius * neighborRadius;
        float sqrSeparationRadius = separationRadius * separationRadius;

        int count = Detect(Mathf.Max(neighborRadius, separationRadius), boidMask);
        for (int i = 0; i < count; i++)
        {
            Boid other = detectionBuffer[i].GetComponentInParent<Boid>();
            if (other == null || other == this) continue;

            float sqrDistance = (other.transform.position - transform.position).sqrMagnitude;

            if (sqrDistance <= sqrSeparationRadius)
                overlappingBoids.Add(other);

            if (!other.IsFlocking) continue;

            if (sqrDistance <= sqrNeighborRadius)
                neighbors.Add(other);
            if (sqrDistance <= sqrSeparationRadius)
                closeNeighbors.Add(other);
        }
    }

    private Agent DetectHunter()
    {
        int count = Detect(evadeRadius, hunterMask);
        for (int i = 0; i < count; i++)
        {
            Hunter hunter = detectionBuffer[i].GetComponentInParent<Hunter>();
            if (hunter != null)
                return hunter;
        }

        return null;
    }

    private Bait DetectClosestBait()
    {
        Bait closest = null;
        float bestSqrDistance = float.MaxValue;

        int count = Detect(baitDetectionRadius, baitMask);
        for (int i = 0; i < count; i++)
        {
            Bait bait = detectionBuffer[i].GetComponentInParent<Bait>();
            if (bait == null || !bait.IsAvailable) continue;

            float sqrDistance = (bait.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                closest = bait;
            }
        }

        return closest;
    }

    #endregion

    #region Steering

    private Vector3 Separation()
    {
        Vector3 steer = Vector3.zero;

        foreach (Boid other in closeNeighbors)
        {
            Vector3 away = transform.position - other.transform.position;
            float distance = away.magnitude;
            if (distance > 0.0001f)
                steer += away.normalized / distance;
        }

        return steer;
    }

    private Vector3 Alignment()
    {
        if (neighbors.Count == 0)
            return Vector3.zero;

        Vector3 averageVelocity = Vector3.zero;
        foreach (Boid other in neighbors)
            averageVelocity += other.Velocity;

        return averageVelocity / neighbors.Count;
    }

    private Vector3 Cohesion()
    {
        if (neighbors.Count == 0)
            return Vector3.zero;

        Vector3 center = Vector3.zero;
        foreach (Boid other in neighbors)
            center += other.transform.position;
        center /= neighbors.Count;

        Vector3 desired = center - transform.position;
        if (desired.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        return desired.normalized * maxSpeed - _velocity;
    }

    private Vector3 Wander()
    {
        wanderDirection += new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * wanderJitter * Time.deltaTime;
        wanderDirection = wanderDirection.normalized;

        return wanderDirection * maxSpeed - _velocity;
    }

    private Vector3 Evade(Agent hunter)
    {
        Vector3 toHunter = hunter.transform.position - transform.position;
        float prediction = toHunter.magnitude / (maxSpeed + hunter.Velocity.magnitude);
        Vector3 futurePosition = hunter.transform.position + hunter.Velocity * prediction;

        Vector3 desired = transform.position - futurePosition;
        desired.y = 0f;
        if (desired.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        return desired.normalized * maxSpeed - _velocity;
    }

    private Vector3 Arrive(Vector3 target)
    {
        Vector3 toTarget = target - transform.position;
        float distance = toTarget.magnitude;
        if (distance < 0.0001f)
            return Vector3.zero;

        float speed = distance < arriveSlowRadius ? maxSpeed * (distance / arriveSlowRadius) : maxSpeed;
        return toTarget.normalized * speed - _velocity;
    }

    private void ResolveOverlap()
    {
        foreach (Boid other in overlappingBoids)
        {
            Vector3 away = transform.position - other.transform.position;
            away.y = 0f;
            float distance = away.magnitude;

            if (distance < separationRadius && distance > 0.0001f)
                transform.position += away.normalized * (separationRadius - distance);
        }
    }

    #endregion

    #region Hunter interaction

    public void Hook()
    {
        IsHooked = true;
        _velocity = Vector3.zero;
    }

    public void ReceiveAttack()
    {
        if (IsDown) return;

        IsHooked = false;
        IsDown = true;
        _velocity = Vector3.zero;

        if (painSound != null)
            audioSource.PlayOneShot(painSound);

        transform.Rotate(0f, 90f, 90f, Space.Self);
    }

    public void SetGatherProgress(float progress)
    {
        transform.localScale = originalScale * Mathf.Lerp(1f, gatheredMinScale, progress);
    }

    public void Respawn()
    {
        IsDown = false;
        IsHooked = false;

        transform.localScale = originalScale;
        transform.rotation = Quaternion.identity;
        _velocity = RandomPlanarDirection() * maxSpeed * 0.5f;

        if (FlockArea.Instance != null)
        {
            Vector3 point = FlockArea.Instance.RandomPoint();
            point.y = spawnHeight;
            transform.position = point;
        }
    }

    #endregion

    private static Vector3 RandomPlanarDirection()
    {
        Vector2 direction = Random.insideUnitCircle.normalized;
        return new Vector3(direction.x, 0f, direction.y);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, neighborRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, evadeRadius);

    }
}
