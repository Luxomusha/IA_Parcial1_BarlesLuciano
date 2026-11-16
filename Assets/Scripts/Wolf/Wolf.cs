using System;
using UnityEngine;

[RequireComponent(typeof(FieldOfView), typeof(PathMover), typeof(PatrolRoute))]
[RequireComponent(typeof(WolfPack))]
public class Wolf : Agent
{
    public enum WolfState { Patrol, Chase, Investigate, Search }

    private const float SpeedLimitFactor = 0.95f;

    private static readonly Color IdleFovColor = new Color(1f, 0.6f, 0f);

    [Header("Speeds (always slower than the Hunter)")]
    [SerializeField, Min(0f)] private float patrolSpeed = 2.5f;
    [SerializeField, Min(0f)] private float investigateSpeed = 4f;
    [SerializeField, Min(0f)] private float chaseSpeed = 5.5f;

    [Header("Target")]
    [Tooltip("Layer of the Hunter: the wolf only knows it once it enters its perception.")]
    [SerializeField] private LayerMask hunterMask;
    [SerializeField, Min(0f)] private float captureRadius = 0.8f;

    [Header("Search")]
    [Tooltip("Time spent looking around the last known position before going back to Patrol.")]
    [SerializeField, Min(0f)] private float searchDuration = 5f;
    [SerializeField, Min(0f)] private float searchRadius = 3f;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;

    public event Action<Hunter> HunterDetected;

    private static readonly Predicate<Hunter> IsAvailable = IsHunterAvailable;

    private FieldOfView fieldOfView;
    private StateMachine stateMachine;
    private Renderer[] renderers;
    private Hunter hunter;
    private bool speedsChecked;

    public PathMover Mover { get; private set; }
    public PatrolRoute Route { get; private set; }
    public WolfPack Pack { get; private set; }

    public WolfState CurrentState { get; private set; }
    public Hunter Target => hunter;
    public bool HasLastKnownPosition { get; private set; }
    public Vector3 LastKnownPosition { get; private set; }

    public bool IsUsingPathfinding => Mover != null && Mover.IsUsingPathfinding;
    public bool IsHowling => Pack != null && Pack.IsHowling;
    public bool JustReceivedAlert => Pack != null && Pack.JustReceivedAlert;
    public Wolf LastAlertSender => Pack != null ? Pack.LastAlertSender : null;

    public float PatrolSpeed => patrolSpeed;
    public float InvestigateSpeed => investigateSpeed;
    public float ChaseSpeed => chaseSpeed;
    public float SearchDuration => searchDuration;
    public float SearchRadius => searchRadius;

    public bool HasLostTarget => hunter != null && !IsHunterAvailable(hunter);

    private void Awake()
    {
        CacheComponents();
        CreateStateMachine();
    }

    private void Start() => stateMachine.ChangeState(WolfState.Patrol);

    private void Update() => stateMachine.Update();

    private void CacheComponents()
    {
        fieldOfView = GetComponent<FieldOfView>();
        Mover = GetComponent<PathMover>();
        Route = GetComponent<PatrolRoute>();
        Pack = GetComponent<WolfPack>();
        renderers = GetComponentsInChildren<Renderer>();
    }

    private void CreateStateMachine()
    {
        stateMachine = new StateMachine();
        Register(WolfState.Patrol, new WolfPatrolState(stateMachine, this));
        Register(WolfState.Chase, new WolfChaseState(stateMachine, this));
        Register(WolfState.Investigate, new WolfInvestigateState(stateMachine, this));
        Register(WolfState.Search, new WolfSearchState(stateMachine, this));
    }

    private void Register(WolfState key, WolfStateBase state)
    {
        state.Key = key;
        stateMachine.RegisterState(key, state);
    }

    public void NotifyStateEntered(WolfState state)
    {
        CurrentState = state;
        fieldOfView.GizmoColor = state == WolfState.Chase ? Color.red : IdleFovColor;
    }

    #region Target perception

    private static bool IsHunterAvailable(Hunter candidate) =>
        candidate != null && candidate.isActiveAndEnabled && !candidate.IsDead && !candidate.IsInSafeZone;

    public bool CanSeeHunter() => TrySetTarget(fieldOfView.FindVisible(hunterMask, IsAvailable));

    public bool CanTrackHunter() => TrySetTarget(fieldOfView.FindInSight(hunterMask, IsAvailable));

    public bool IsHunterInCaptureRange()
    {
        Hunter perceived = fieldOfView.FindInRadius(captureRadius, hunterMask, IsAvailable);
        return TrySetTarget(perceived) && Mover.PlanarDistance(perceived.transform.position) <= captureRadius;
    }

    public void CatchHunter()
    {
        hunter.Catch();
        Debug.Log($"{name} caught the Hunter.", this);
    }

    private bool TrySetTarget(Hunter perceived)
    {
        if (perceived == null) return false;

        hunter = perceived;
        ClampSpeedsBelowHunter();
        return true;
    }

    private void ClampSpeedsBelowHunter()
    {
        if (speedsChecked) return;
        speedsChecked = true;

        float limit = hunter.MaxSpeed * SpeedLimitFactor;
        if (patrolSpeed < limit && investigateSpeed < limit && chaseSpeed < limit) return;

        Debug.LogWarning($"{name}: wolf speeds must be lower than the Hunter's ({hunter.MaxSpeed}). Clamped to {limit:0.##}.", this);
        patrolSpeed = Mathf.Min(patrolSpeed, limit);
        investigateSpeed = Mathf.Min(investigateSpeed, limit);
        chaseSpeed = Mathf.Min(chaseSpeed, limit);
    }

    public void ReportDetection() => HunterDetected?.Invoke(hunter);

    #endregion

    #region Target memory

    public void SetLastKnownPosition(Vector3 position)
    {
        LastKnownPosition = new Vector3(position.x, transform.position.y, position.z);
        HasLastKnownPosition = true;
    }

    public void ForgetTarget()
    {
        HasLastKnownPosition = false;
        Pack.ClearAlert();
        hunter = null;
    }

    public void Howl()
    {
        Vector3 position = hunter.transform.position;
        if (Pack.Howl(hunter, position))
            Debug.Log($"{name} HOWLS: Hunter seen at {position}.", this);
    }

    public bool TryConsumeAlert(out Vector3 position)
    {
        if (!Pack.TryConsumeAlert(out position, out Hunter reported)) return false;

        TrySetTarget(reported);
        return true;
    }

    #endregion

    #region Visuals

    public void SetColor(Color color)
    {
        foreach (Renderer r in renderers)
            r.material.color = color;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, captureRadius);

        if (!Application.isPlaying) return;

        DrawTargetGizmos();
        DrawPackGizmos();
        DrawStateLabel();
    }

    private void DrawTargetGizmos()
    {
        Vector3 eye = fieldOfView != null ? fieldOfView.EyePosition : transform.position;

        if (CurrentState == WolfState.Chase && hunter != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(eye, hunter.transform.position + Vector3.up * (eye.y - transform.position.y));
        }

        if (HasLastKnownPosition)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(LastKnownPosition, 0.4f);
            Gizmos.DrawLine(eye, LastKnownPosition);
        }
    }

    private void DrawPackGizmos()
    {
        if (IsHowling)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 3f);
        }

        if (JustReceivedAlert && LastAlertSender != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position + Vector3.up * 0.4f, LastAlertSender.transform.position + Vector3.up * 0.4f);
        }
    }

    private void DrawStateLabel()
    {
#if UNITY_EDITOR
        string label = CurrentState.ToString();
        if (IsHowling) label += "  AUUU!";
        else if (JustReceivedAlert) label += "  (alert)";
        if (IsUsingPathfinding) label += "  [A*]";
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.2f, label);
#endif
    }

    #endregion
}
