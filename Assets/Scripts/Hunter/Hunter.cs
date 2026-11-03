using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Hunter : Agent
{
    // Patrol is only used in the Parcial 1 scene (no graph assigned).
    public enum HunterState { Patrol, Search, Attack, Gather, Flee, Exit, Dead }

    private const float PathWaypointTolerance = 0.05f;
    private const float RepathDistance = 0.75f;

    [Header("Movement")]
    [SerializeField] private float maxSpeed = 4f;
    [Tooltip("Layers that block movement and line of sight, such as the level walls.")]
    [SerializeField] private LayerMask obstacleMask;
    [Tooltip("Small world-space clearance kept between the capsule and walls.")]
    [SerializeField, Min(0f)] private float collisionSkin = 0.01f;

    [Header("Navigation (Parcial 2)")]
    [Tooltip("Leave empty to run the Parcial 1 behaviour (Patrol, no zones).")]
    [SerializeField] private Pathfinder pathfinder;
    [SerializeField] private FlockArea huntingGround;
    [SerializeField] private Transform startZone;
    [SerializeField] private Transform exitZone;
    [SerializeField] private Transform[] safeZones;
    [SerializeField] private LayerMask safeZoneMask;

    [Header("Objective")]
    [SerializeField, Min(1)] private int requiredGathers = 3;
    [SerializeField, Min(0f)] private float respawnTime = 5f;
    [SerializeField, Min(0f)] private float safeZoneWaitTime = 3f;

    [Header("Patrol (Parcial 1)")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private bool pingPongWaypoints = true;

    [Header("Bait spawning")]
    [SerializeField] private Bait baitPrefab;
    [Tooltip("Parent for the spawned baits, keeps the Hierarchy clean.")]
    [SerializeField] private Transform baitContainer;
    [SerializeField] private float spawnInterval = 5f;
    [Tooltip("The brief allows at most 5 active baits at the same time.")]
    [SerializeField, Range(1, 5)] private int maxActiveBaits = 3;

    [Header("Attack")]
    [FormerlySerializedAs("perceptionRadius")]
    [SerializeField] private float visionRadius = 8f;
    [SerializeField] private float meleeAttackRadius = 1f;
    [SerializeField] private float rangeAttackRadius = 5f;
    [Tooltip("TBA: time between attacks.")]
    [SerializeField] private float timeBetweenAttacks = 3f;
    [Tooltip("Optional line flashed from the Hunter to the boid it hits.")]
    [SerializeField] private LineRenderer attackBeam;
    [SerializeField] private float attackBeamDuration = 0.2f;

    [Header("Gather")]
    [SerializeField] private float gatherDuration = 1.5f;
    [Tooltip("Optional beam drawn from the Hunter to the boid it is gathering.")]
    [SerializeField] private LineRenderer gatherBeam;

    [Header("Detection")]
    [SerializeField] private LayerMask boidMask;
    [SerializeField] private LayerMask wolfMask;
    [SerializeField] private int maxDetectedColliders = 32;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;

    private StateMachine stateMachine;
    private Renderer[] renderers;
    private Collider[] colliders;
    private CapsuleCollider movementCollider;
    private Collider[] detectionBuffer;
    private readonly List<Bait> activeBaits = new List<Bait>();

    private List<Vector3> path = new List<Vector3>();
    private int pathIndex;
    private Vector3 pathGoal;
    private bool hasPathRequest;
    private bool warnedNoPath;

    private float lastAttackTime = float.NegativeInfinity;
    private float attackBeamTimer;

    public Transform[] Waypoints => waypoints;
    public bool PingPongWaypoints => pingPongWaypoints;
    public float SpawnInterval => spawnInterval;
    public float VisionRadius => visionRadius;
    public float MeleeAttackRadius => meleeAttackRadius;
    public float RangeAttackRadius => rangeAttackRadius;
    public float GatherDuration => gatherDuration;
    public float RespawnTime => respawnTime;
    public float SafeZoneWaitTime => safeZoneWaitTime;
    public float MaxSpeed => maxSpeed;

    public bool HasNavigation => pathfinder != null;
    public HunterState DefaultState => HasNavigation ? HunterState.Search : HunterState.Patrol;
    public HunterState CurrentState { get; private set; }

    public int GatherCount { get; private set; }
    public int RequiredGathers => requiredGathers;
    public bool HasCompletedGathers => HasNavigation && exitZone != null && GatherCount >= requiredGathers;

    public bool IsAttackReady => Time.time - lastAttackTime >= timeBetweenAttacks;
    public bool IsCaught { get; private set; }
    public bool IsDead => CurrentState == HunterState.Dead;
    public bool IsInSafeZone => Physics.CheckSphere(transform.position, 0.1f, safeZoneMask, QueryTriggerInteraction.Collide);
    public bool IsInHuntingGround => huntingGround != null && huntingGround.Contains(transform.position);

    public Transform ChosenSafeZone { get; set; }
    public Transform DetectedWolf { get; private set; }

    private void Awake()
    {
        movementCollider = GetComponentInChildren<CapsuleCollider>();
        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider>();
        detectionBuffer = new Collider[maxDetectedColliders];
        HideGatherBeam();
        if (attackBeam != null) attackBeam.enabled = false;

        stateMachine = new StateMachine();
        Register(HunterState.Patrol, new PatrolState(stateMachine, this));
        Register(HunterState.Search, new SearchState(stateMachine, this));
        Register(HunterState.Attack, new AttackState(stateMachine, this));
        Register(HunterState.Gather, new GatherState(stateMachine, this));
        Register(HunterState.Flee, new FleeState(stateMachine, this));
        Register(HunterState.Exit, new ExitState(stateMachine, this));
        Register(HunterState.Dead, new DeadState(stateMachine, this));
    }

    private void Register(HunterState key, HunterStateBase state)
    {
        state.Key = key;
        stateMachine.RegisterState(key, state);
    }

    private void Start()
    {
        if (HasNavigation && startZone != null)
            PlaceAt(startZone.position);

        stateMachine.ChangeState(DefaultState);
    }

    private void Update()
    {
        stateMachine.Update();
        UpdateAttackBeam();
    }

    // Called by the states on Enter, so CurrentState always mirrors the FSM.
    public void NotifyStateEntered(HunterState state) => CurrentState = state;

    #region Movement

    public void SetColor(Color color)
    {
        foreach (Renderer r in renderers)
        {
            if (r != gatherBeam && r != attackBeam)
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

        if (step > 0f && movementCollider != null)
        {
            GetCapsule(out Vector3 bottom, out Vector3 top, out float radius);

            // Sweeping the whole step also prevents tunnelling on slow frames.
            if (Physics.CheckCapsule(bottom, top, radius, obstacleMask, QueryTriggerInteraction.Ignore))
                step = 0f;
            else if (Physics.CapsuleCast(bottom, top, radius, direction, out RaycastHit hit,
                         step + collisionSkin, obstacleMask, QueryTriggerInteraction.Ignore))
                step = Mathf.Min(step, Mathf.Max(0f, hit.distance - collisionSkin));
        }

        SetVelocity(Time.deltaTime > 0f ? direction * (step / Time.deltaTime) : Vector3.zero);
        transform.position += direction * step;
        transform.forward = direction;

        return distance - step;
    }

    public void Stop() => SetVelocity(Vector3.zero);

    // Final stretch towards a target: straight steering when the way is clear,
    // A* otherwise. Returns the remaining planar distance to the target.
    public float MoveToTarget(Vector3 target, float stopDistance = 0f)
    {
        if (!HasNavigation || HasClearPath(target))
        {
            ClearPath();
            return MoveTowards(target, stopDistance);
        }

        FollowPathTo(target);
        return PlanarDistance(target);
    }

    // Walks the A* path to goal. Returns true once the end of the path is reached.
    public bool FollowPathTo(Vector3 goal)
    {
        if (!HasNavigation)
            return MoveTowards(goal) <= PathWaypointTolerance;

        if (!hasPathRequest || (goal - pathGoal).sqrMagnitude > RepathDistance * RepathDistance)
            RequestPath(goal);

        if (path.Count == 0)
        {
            Stop();
            return false;
        }

        if (pathIndex >= path.Count)
        {
            Stop();
            return true;
        }

        if (MoveTowards(path[pathIndex]) <= PathWaypointTolerance)
            pathIndex++;

        return pathIndex >= path.Count;
    }

    // True when the last request found no path (blocked or unreachable goal).
    public bool PathFailed => hasPathRequest && path.Count == 0;

    public void ClearPath()
    {
        path.Clear();
        pathIndex = 0;
        hasPathRequest = false;
    }

    public List<Vector3> FindPath(Vector3 goal) =>
        HasNavigation ? pathfinder.FindPath(transform.position, goal) : new List<Vector3>();

    private void RequestPath(Vector3 goal)
    {
        path = pathfinder.FindPath(transform.position, goal);
        pathGoal = goal;
        hasPathRequest = true;

        // Skip our own node when the next one can be reached directly,
        // so the Hunter doesn't step back to the center of its cell.
        pathIndex = path.Count > 1 && HasClearPath(path[1]) ? 1 : 0;

        if (path.Count == 0 && !warnedNoPath)
        {
            Debug.LogWarning($"Hunter: no path from {transform.position} to {goal}.", this);
            warnedNoPath = true;
        }
        else if (path.Count > 0)
        {
            warnedNoPath = false;
        }
    }

    private void GetCapsule(out Vector3 bottom, out Vector3 top, out float radius)
    {
        UnityEngine.Bounds bounds = movementCollider.bounds;
        radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
        float halfSegment = Mathf.Max(0f, bounds.extents.y - radius);
        bottom = bounds.center - Vector3.up * halfSegment;
        top = bounds.center + Vector3.up * halfSegment;
    }

    private float PlanarDistance(Vector3 target)
    {
        Vector3 delta = target - transform.position;
        delta.y = 0f;
        return delta.magnitude;
    }

    #endregion

    #region Perception

    private Vector3 EyePosition => movementCollider != null ? movementCollider.bounds.center : transform.position + Vector3.up * 0.5f;

    // Line of sight: no wall between the Hunter and the point.
    public bool HasLineOfSight(Vector3 point)
    {
        Vector3 from = EyePosition;
        Vector3 to = new Vector3(point.x, from.y, point.z);
        return !Physics.Linecast(from, to, obstacleMask, QueryTriggerInteraction.Ignore);
    }

    // Like line of sight but as wide as the capsule: the Hunter can walk straight there.
    public bool HasClearPath(Vector3 point)
    {
        if (movementCollider == null) return HasLineOfSight(point);

        GetCapsule(out Vector3 bottom, out Vector3 top, out float radius);
        Vector3 toTarget = point - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;
        if (distance < 0.0001f) return true;

        return !Physics.CapsuleCast(bottom, top, radius, toTarget / distance, distance, obstacleMask, QueryTriggerInteraction.Ignore);
    }

    public bool CanSee(Boid boid) =>
        boid != null
        && (boid.transform.position - transform.position).sqrMagnitude <= visionRadius * visionRadius
        && HasLineOfSight(boid.transform.position);

    public Boid FindClosestActiveBoid() => FindClosestBoid(wantDown: false);
    public Boid FindClosestDownBoid() => FindClosestBoid(wantDown: true);

    // Only boids inside the vision radius and not hidden behind a wall can be found.
    private Boid FindClosestBoid(bool wantDown)
    {
        Boid closest = null;
        float bestSqrDistance = float.MaxValue;

        int count = Physics.OverlapSphereNonAlloc(transform.position, visionRadius, detectionBuffer, boidMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            Boid boid = detectionBuffer[i].GetComponentInParent<Boid>();
            if (boid == null || boid.IsDown != wantDown) continue;
            if (!HasLineOfSight(boid.transform.position)) continue;

            float sqrDistance = (boid.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                closest = boid;
            }
        }

        return closest;
    }

    // Wolf perception: max distance + line of sight (no view angle needed).
    public Transform DetectClosestWolf()
    {
        DetectedWolf = null;
        float bestSqrDistance = float.MaxValue;

        int count = Physics.OverlapSphereNonAlloc(transform.position, visionRadius, detectionBuffer, wolfMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            Transform wolf = detectionBuffer[i].transform;
            if (!HasLineOfSight(wolf.position)) continue;

            float sqrDistance = (wolf.position - transform.position).sqrMagnitude;
            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                DetectedWolf = wolf;
            }
        }

        return DetectedWolf;
    }

    public bool CanFlee => HasNavigation && safeZones != null && safeZones.Length > 0;
    public Transform[] SafeZones => safeZones;

    #endregion

    #region Zones

    public Vector3 HuntingGroundCenter => huntingGround != null ? huntingGround.transform.position : transform.position;
    // Random point of the Hunting ground that is not on a wall (same check the graph uses).
    public Vector3 RandomHuntingGroundPoint()
    {
        if (huntingGround == null) return transform.position;

        Vector3 point = huntingGround.RandomPoint();
        for (int i = 0; i < 10 && Physics.CheckSphere(point, 0.5f, obstacleMask, QueryTriggerInteraction.Ignore); i++)
            point = huntingGround.RandomPoint();
        return point;
    }
    public Vector3 ExitPosition => exitZone != null ? exitZone.position : transform.position;

    public void AddGather() => GatherCount++;

    // Called by the wolves when they reach the Hunter. The FSM reacts on its next update.
    [ContextMenu("Debug/Catch (test Dead)")]
    public void Catch()
    {
        if (!IsDead) IsCaught = true;
    }

    public void SetVisible(bool visible)
    {
        foreach (Renderer r in renderers)
        {
            if (r != gatherBeam && r != attackBeam)
                r.enabled = visible;
        }
        foreach (Collider c in colliders)
            c.enabled = visible;

        if (!visible)
        {
            HideGatherBeam();
            if (attackBeam != null) attackBeam.enabled = false;
        }
    }

    // Back to the Start zone with the gather counter reset (after Dead or Exit).
    public void RespawnAtStart()
    {
        IsCaught = false;
        GatherCount = 0;
        lastAttackTime = float.NegativeInfinity;
        ChosenSafeZone = null;
        ClearPath();
        Stop();

        if (startZone != null)
            PlaceAt(startZone.position);
    }

    private void PlaceAt(Vector3 position)
    {
        transform.position = new Vector3(position.x, transform.position.y, position.z);
        Physics.SyncTransforms();
    }

    #endregion

    #region Attack

    public void PerformAttack(Boid target, bool ranged)
    {
        target.ReceiveAttack();
        lastAttackTime = Time.time;

        if (attackBeam == null) return;

        attackBeam.enabled = true;
        attackBeam.startColor = attackBeam.endColor = ranged ? new Color(1f, 0.5f, 0f) : Color.red;
        attackBeam.SetPosition(0, EyePosition);
        attackBeam.SetPosition(1, target.transform.position);
        attackBeamTimer = attackBeamDuration;
    }

    private void UpdateAttackBeam()
    {
        if (attackBeam == null || !attackBeam.enabled) return;

        attackBeamTimer -= Time.deltaTime;
        if (attackBeamTimer <= 0f)
            attackBeam.enabled = false;
    }

    #endregion

    #region Bait

    public void TrySpawnBait() => TrySpawnBait(transform.position);

    public void TrySpawnBait(Vector3 position)
    {
        activeBaits.RemoveAll(bait => bait == null);

        if (baitPrefab == null || activeBaits.Count >= maxActiveBaits)
            return;

        Bait bait = Instantiate(baitPrefab, position, Quaternion.identity, baitContainer);
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
        Gizmos.DrawWireSphere(transform.position, visionRadius);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, rangeAttackRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRadius);

        if (!Application.isPlaying) return;

        // Current A* path.
        if (path != null && path.Count > 0)
        {
            Gizmos.color = Color.white;
            for (int i = Mathf.Max(pathIndex, 1); i < path.Count; i++)
                Gizmos.DrawLine(path[i - 1] + Vector3.up * 0.15f, path[i] + Vector3.up * 0.15f);
            if (pathIndex < path.Count)
                Gizmos.DrawLine(transform.position + Vector3.up * 0.15f, path[pathIndex] + Vector3.up * 0.15f);
        }

        if (DetectedWolf != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(EyePosition, DetectedWolf.position);
        }

        if (ChosenSafeZone != null && CurrentState == HunterState.Flee)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(ChosenSafeZone.position + Vector3.up * 0.5f, new Vector3(4f, 1f, 4f));
        }

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
            $"{CurrentState}  {GatherCount}/{requiredGathers}");
#endif
    }
}
