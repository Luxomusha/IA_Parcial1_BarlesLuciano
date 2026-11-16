using System;
using UnityEngine;

[RequireComponent(typeof(FieldOfView), typeof(PathMover), typeof(PatrolRoute))]
[RequireComponent(typeof(HunterCombat), typeof(HunterGathering), typeof(BaitSpawner))]
public class Hunter : Agent
{
    public enum HunterState { Patrol, Search, Attack, Gather, Flee, Exit, Dead }

    private const float SafeZoneCheckRadius = 0.1f;
    private const float HuntingPointClearance = 0.5f;
    private const int HuntingPointAttempts = 10;

    [SerializeField] private float maxSpeed = 4f;

    [Header("Zones (Parcial 2)")]
    [SerializeField] private FlockArea huntingGround;
    [SerializeField] private Transform startZone;
    [SerializeField] private Transform exitZone;
    [SerializeField] private Transform[] safeZones;
    [SerializeField] private LayerMask safeZoneMask;

    [Header("Objective")]
    [SerializeField, Min(0f)] private float respawnTime = 5f;
    [SerializeField, Min(0f)] private float safeZoneWaitTime = 3f;

    [Header("Detection")]
    [SerializeField] private LayerMask boidMask;
    [SerializeField] private LayerMask wolfMask;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;

    private static readonly Predicate<Boid> IsActiveBoid = boid => !boid.IsDown;
    private static readonly Predicate<Boid> IsDownBoid = boid => boid.IsDown;

    private FieldOfView fieldOfView;
    private StateMachine stateMachine;
    private Renderer[] renderers;
    private Collider[] colliders;

    public PathMover Mover { get; private set; }
    public PatrolRoute Route { get; private set; }
    public HunterCombat Combat { get; private set; }
    public HunterGathering Gathering { get; private set; }
    public BaitSpawner Baits { get; private set; }

    public float MaxSpeed => maxSpeed;
    public float RespawnTime => respawnTime;
    public float SafeZoneWaitTime => safeZoneWaitTime;

    public bool HasNavigation => Mover != null && Mover.HasPathfinder;
    public HunterState DefaultState => HasNavigation ? HunterState.Search : HunterState.Patrol;
    public HunterState CurrentState { get; private set; }

    public int GatherCount => Gathering != null ? Gathering.GatherCount : 0;
    public int RequiredGathers => Gathering != null ? Gathering.RequiredGathers : 0;
    public bool HasCompletedGathers => HasNavigation && exitZone != null && Gathering.HasCompleted;

    public bool IsCaught { get; private set; }
    public bool IsDead => CurrentState == HunterState.Dead;
    public bool IsInSafeZone => Physics.CheckSphere(transform.position, SafeZoneCheckRadius, safeZoneMask, QueryTriggerInteraction.Collide);
    public bool IsInHuntingGround => huntingGround != null && huntingGround.Contains(transform.position);

    public bool CanFlee => HasNavigation && safeZones != null && safeZones.Length > 0;
    public Transform[] SafeZones => safeZones;
    public Transform ChosenSafeZone { get; set; }
    public Transform DetectedWolf { get; private set; }

    public Vector3 HuntingGroundCenter => huntingGround != null ? huntingGround.transform.position : transform.position;
    public Vector3 ExitPosition => exitZone != null ? exitZone.position : transform.position;

    private void Awake()
    {
        CacheComponents();
        CreateStateMachine();
    }

    private void Start()
    {
        if (HasNavigation && startZone != null)
            PlaceAt(startZone.position);

        stateMachine.ChangeState(DefaultState);
    }

    private void Update() => stateMachine.Update();

    private void CacheComponents()
    {
        fieldOfView = GetComponent<FieldOfView>();
        Mover = GetComponent<PathMover>();
        Route = GetComponent<PatrolRoute>();
        Combat = GetComponent<HunterCombat>();
        Gathering = GetComponent<HunterGathering>();
        Baits = GetComponent<BaitSpawner>();
        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider>();
    }

    private void CreateStateMachine()
    {
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

    public void NotifyStateEntered(HunterState state) => CurrentState = state;

    #region Perception

    public bool CanSee(Boid boid) => boid != null && fieldOfView.CanSee(boid.transform.position);

    public Boid FindClosestActiveBoid() => fieldOfView.FindClosestVisible(boidMask, IsActiveBoid);

    public Boid FindClosestDownBoid() => fieldOfView.FindClosestVisible(boidMask, IsDownBoid);

    public Transform DetectClosestWolf()
    {
        Wolf wolf = fieldOfView.FindClosestVisible<Wolf>(wolfMask);
        DetectedWolf = wolf != null ? wolf.transform : null;
        return DetectedWolf;
    }

    public void Attack(Boid target, bool ranged) => Combat.PerformAttack(target, fieldOfView.EyePosition, ranged);

    #endregion

    #region Zones

    public Vector3 RandomHuntingGroundPoint()
    {
        if (huntingGround == null) return transform.position;

        Vector3 point = huntingGround.RandomPoint();
        for (int i = 0; i < HuntingPointAttempts && Mover.IsInsideObstacle(point, HuntingPointClearance); i++)
            point = huntingGround.RandomPoint();
        return point;
    }

    [ContextMenu("Debug/Catch (test Dead)")]
    public void Catch()
    {
        if (!IsDead) IsCaught = true;
    }

    public void RespawnAtStart()
    {
        IsCaught = false;
        ChosenSafeZone = null;
        Gathering.ResetCount();
        Combat.ResetCooldown();
        Mover.ClearPath();
        Mover.Stop();

        if (startZone != null)
            PlaceAt(startZone.position);
    }

    private void PlaceAt(Vector3 position)
    {
        transform.position = new Vector3(position.x, transform.position.y, position.z);
        Physics.SyncTransforms();
    }

    #endregion

    #region Visuals

    public void SetColor(Color color)
    {
        foreach (Renderer r in renderers)
        {
            if (!(r is LineRenderer))
                r.material.color = color;
        }
    }

    public void SetVisible(bool visible)
    {
        foreach (Renderer r in renderers)
        {
            if (!(r is LineRenderer))
                r.enabled = visible;
        }

        foreach (Collider c in colliders)
            c.enabled = visible;

        if (visible) return;

        Gathering.HideBeam();
        Combat.HideBeam();
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos || !Application.isPlaying) return;

        if (DetectedWolf != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(fieldOfView.EyePosition, DetectedWolf.position);
        }

        if (ChosenSafeZone != null && CurrentState == HunterState.Flee)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(ChosenSafeZone.position + Vector3.up * 0.5f, new Vector3(4f, 1f, 4f));
        }

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"{CurrentState}  {GatherCount}/{RequiredGathers}");
#endif
    }

    #endregion
}
