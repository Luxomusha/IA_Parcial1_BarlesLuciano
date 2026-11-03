using System.Collections.Generic;
using UnityEngine;

public class Hunter : Agent
{
    public enum HunterState { Patrol, Attack, Gather }

    [Header("Movement")]
    [SerializeField] private float maxSpeed = 4f;

    [Header("Patrol")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private bool pingPongWaypoints = true;

    [Header("Bait spawning")]
    [SerializeField] private Bait baitPrefab;
    [Tooltip("Parent for the spawned baits, keeps the Hierarchy clean.")]
    [SerializeField] private Transform baitContainer;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private int maxActiveBaits = 3;

    [Header("Attack")]
    [SerializeField] private float perceptionRadius = 8f;
    [SerializeField] private float meleeAttackRadius = 1f;
    [SerializeField] private float timeBetweenAttacks = 3f;

    [Header("Gather")]
    [SerializeField] private float gatherDuration = 1.5f;
    [Tooltip("Optional beam drawn from the Hunter to the boid it is gathering.")]
    [SerializeField] private LineRenderer gatherBeam;

    [Header("Detection")]
    [SerializeField] private LayerMask boidMask;
    [SerializeField] private int maxDetectedColliders = 32;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;

    private StateMachine stateMachine;
    private Renderer[] renderers;
    private Collider[] detectionBuffer;
    private readonly List<Bait> activeBaits = new List<Bait>();

    public Transform[] Waypoints => waypoints;
    public bool PingPongWaypoints => pingPongWaypoints;
    public float SpawnInterval => spawnInterval;
    public float MeleeAttackRadius => meleeAttackRadius;
    public float PerceptionRadius => perceptionRadius;
    public float TimeBetweenAttacks => timeBetweenAttacks;
    public float GatherDuration => gatherDuration;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        detectionBuffer = new Collider[maxDetectedColliders];
        HideGatherBeam();

        stateMachine = new StateMachine();
        stateMachine.RegisterState(HunterState.Patrol, new PatrolState(stateMachine, this));
        stateMachine.RegisterState(HunterState.Attack, new AttackState(stateMachine, this));
        stateMachine.RegisterState(HunterState.Gather, new GatherState(stateMachine, this));
    }

    private void Start() => stateMachine.ChangeState(HunterState.Patrol);
    private void Update() => stateMachine.Update();

    public void SetColor(Color color)
    {
        foreach (Renderer r in renderers)
        {
            if (r != gatherBeam)
                r.material.color = color;
        }
    }

    // Moves on the XZ plane towards target, stopping at stopDistance. Returns the remaining distance.
    public float MoveTowards(Vector3 target, float stopDistance = 0f)
    {
        Vector3 toTarget = target - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        if (distance < 0.0001f)
        {
            SetVelocity(Vector3.zero);
            return 0f;
        }

        Vector3 direction = toTarget / distance;
        float step = Mathf.Min(maxSpeed * Time.deltaTime, Mathf.Max(0f, distance - stopDistance));

        SetVelocity(step > 0f ? direction * maxSpeed : Vector3.zero);
        transform.position += direction * step;
        transform.forward = direction;

        return distance - step;
    }

    public void Stop() => SetVelocity(Vector3.zero);

    #region Detection

    public Boid FindClosestActiveBoid() => FindClosestBoid(wantDown: false);
    public Boid FindClosestDownBoid() => FindClosestBoid(wantDown: true);

    public bool IsInPerception(Boid boid) =>
        boid != null && (boid.transform.position - transform.position).sqrMagnitude <= perceptionRadius * perceptionRadius;

    // Only boids whose colliders are inside the perception radius can be found.
    private Boid FindClosestBoid(bool wantDown)
    {
        Boid closest = null;
        float bestSqrDistance = float.MaxValue;

        int count = Physics.OverlapSphereNonAlloc(transform.position, perceptionRadius, detectionBuffer, boidMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            Boid boid = detectionBuffer[i].GetComponentInParent<Boid>();
            if (boid == null || boid.IsDown != wantDown) continue;

            float sqrDistance = (boid.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                closest = boid;
            }
        }

        return closest;
    }

    #endregion

    #region Bait

    public void TrySpawnBait()
    {
        activeBaits.RemoveAll(bait => bait == null);

        if (baitPrefab == null || activeBaits.Count >= maxActiveBaits)
            return;

        Bait bait = Instantiate(baitPrefab, transform.position, Quaternion.identity, baitContainer);
        activeBaits.Add(bait);
    }

    #endregion

    #region Gather feedback

    public void ShowGatherBeam(Vector3 target)
    {
        if (gatherBeam == null) return;

        gatherBeam.enabled = true;
        gatherBeam.SetPosition(0, transform.position);
        gatherBeam.SetPosition(1, target);
    }

    public void HideGatherBeam()
    {
        if (gatherBeam != null)
            gatherBeam.enabled = false;
    }

    #endregion

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, perceptionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRadius);
    }
}
