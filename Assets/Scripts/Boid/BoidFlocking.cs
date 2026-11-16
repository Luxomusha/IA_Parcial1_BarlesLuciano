using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Boid))]
public class BoidFlocking : MonoBehaviour
{
    private const float Epsilon = 0.0001f;

    [Header("Perception radii")]
    [SerializeField] private float separationRadius = 1.5f;
    [SerializeField] private float neighborRadius = 4f;
    [Tooltip("Minimum distance kept between two boids. Closer than this they are pushed apart (separationRadius only steers).")]
    [SerializeField, Min(0.01f)] private float minDistance = 0.5f;
    [SerializeField] private LayerMask boidMask;
    [SerializeField] private int maxDetectedColliders = 32;

    [Header("Weights")]
    [SerializeField] private float separationWeight = 1.5f;
    [SerializeField] private float alignmentWeight = 1f;
    [SerializeField] private float cohesionWeight = 1f;

    [Header("Wander (keeps the flock moving)")]
    [SerializeField] private float wanderWeight = 0.5f;
    [SerializeField] private float wanderJitter = 2f;

    [SerializeField] private bool drawGizmos = true;

    private Boid owner;
    private Collider[] buffer;
    private Vector3 wanderDirection;
    private readonly List<Boid> neighbors = new List<Boid>();
    private readonly List<Boid> closeNeighbors = new List<Boid>();
    private readonly List<Boid> overlapping = new List<Boid>();

    public Vector3 WanderDirection => wanderDirection;

    private void Awake()
    {
        owner = GetComponent<Boid>();
        buffer = new Collider[maxDetectedColliders];
        wanderDirection = Steering.RandomPlanarDirection();
    }

    public void DetectNeighbors()
    {
        neighbors.Clear();
        closeNeighbors.Clear();
        overlapping.Clear();

        float sqrNeighborRadius = neighborRadius * neighborRadius;
        float sqrSeparationRadius = separationRadius * separationRadius;
        float detectionRadius = Mathf.Max(neighborRadius, separationRadius, minDistance);
        int count = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, buffer, boidMask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            Boid other = buffer[i].GetComponentInParent<Boid>();
            if (other == null || other == owner) continue;

            Classify(other, (other.transform.position - transform.position).sqrMagnitude, sqrNeighborRadius, sqrSeparationRadius);
        }
    }

    public Vector3 FlockSteering(Vector3 velocity, float maxSpeed)
    {
        return SeparationSteering()
             + Alignment() * alignmentWeight
             + Cohesion(velocity, maxSpeed) * cohesionWeight
             + Steering.Wander(ref wanderDirection, velocity, maxSpeed, wanderJitter) * wanderWeight;
    }

    public Vector3 SeparationSteering() => Separation() * separationWeight;

    public void ResolveOverlap()
    {
        foreach (Boid other in overlapping)
        {
            Vector3 away = transform.position - other.transform.position;
            away.y = 0f;
            float distance = away.magnitude;
            if (distance >= minDistance || distance <= Epsilon) continue;

            float share = other.IsFlocking ? 0.5f : 1f;
            transform.position += away / distance * ((minDistance - distance) * share);
        }
    }

    private void Classify(Boid other, float sqrDistance, float sqrNeighborRadius, float sqrSeparationRadius)
    {
        if (sqrDistance <= minDistance * minDistance)
            overlapping.Add(other);

        if (!other.IsFlocking) return;

        if (sqrDistance <= sqrNeighborRadius)
            neighbors.Add(other);
        if (sqrDistance <= sqrSeparationRadius)
            closeNeighbors.Add(other);
    }

    private Vector3 Separation()
    {
        Vector3 steer = Vector3.zero;

        foreach (Boid other in closeNeighbors)
        {
            Vector3 away = transform.position - other.transform.position;
            float distance = away.magnitude;
            if (distance > Epsilon)
                steer += away.normalized / distance;
        }

        return steer;
    }

    private Vector3 Alignment()
    {
        if (neighbors.Count == 0) return Vector3.zero;

        Vector3 averageVelocity = Vector3.zero;
        foreach (Boid other in neighbors)
            averageVelocity += other.Velocity;

        return averageVelocity / neighbors.Count;
    }

    private Vector3 Cohesion(Vector3 velocity, float maxSpeed)
    {
        if (neighbors.Count == 0) return Vector3.zero;

        Vector3 center = Vector3.zero;
        foreach (Boid other in neighbors)
            center += other.transform.position;
        center /= neighbors.Count;

        Vector3 desired = center - transform.position;
        if (desired.sqrMagnitude < Epsilon) return Vector3.zero;

        return desired.normalized * maxSpeed - velocity;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, separationRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, neighborRadius);
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, minDistance);
    }
}
