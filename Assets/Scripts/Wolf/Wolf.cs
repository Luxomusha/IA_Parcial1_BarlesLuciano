using System.Collections.Generic;
using UnityEngine;

public class Wolf : Agent
{
    public enum WolfState { Patrol, Chase, Investigate, Search }
    public enum PatrolMode { PingPong, Loop }

    private const float PathWaypointTolerance = 0.05f;
    private const float RepathDistance = 0.75f;
    private const float ArriveDistance = 0.3f;

    private static readonly List<Wolf> pack = new List<Wolf>();

    [Header("References")]
    [Tooltip("If empty, the first Hunter in the scene is used.")]
    [SerializeField] private Hunter hunter;
    [Tooltip("If empty, the first Pathfinder in the scene is used.")]
    [SerializeField] private Pathfinder pathfinder;
    [SerializeField] private Animator animator;

    [Header("Speeds (always slower than the Hunter)")]
    [SerializeField, Min(0f)] private float patrolSpeed = 2.5f;
    [SerializeField, Min(0f)] private float investigateSpeed = 4f;
    [SerializeField, Min(0f)] private float chaseSpeed = 5.5f;
    [SerializeField, Min(0f)] private float turnSpeed = 540f;

    [Header("Patrol")]
    [SerializeField] private Transform[] waypoints;
    [Tooltip("PingPong: walk the waypoints back in reverse order. Loop: go back to the first one.")]
    [SerializeField] private PatrolMode patrolMode = PatrolMode.PingPong;

    [Header("Perception (FOV / LOS)")]
    [SerializeField, Min(0f)] private float viewRadius = 7f;
    [SerializeField, Range(1f, 360f)] private float viewAngle = 110f;
    [SerializeField] private float eyeHeight = 0.4f;
    [Tooltip("Layers that block movement and line of sight (level walls).")]
    [SerializeField] private LayerMask obstacleMask;
    [Tooltip("Layer of the Safe zone floors: wolves never step on them.")]
    [SerializeField] private LayerMask safeZoneMask;
    [SerializeField, Min(0.05f)] private float bodyRadius = 0.25f;

    [Header("Catch")]
    [SerializeField, Min(0f)] private float captureRadius = 0.8f;

    [Header("Howl")]
    [Tooltip("Minimum time between two howls of the same wolf.")]
    [SerializeField, Min(0f)] private float howlCooldown = 2f;

    [Header("Sound")]
    [Tooltip("If empty, the AudioSource on this GameObject is used.")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("One of these is played at random when this wolf howls.")]
    [SerializeField] private AudioClip[] howlClips;
    [SerializeField, Range(0f, 1f)] private float howlVolume = 1f;
    [Tooltip("Optional: played when this wolf receives the howl of another wolf.")]
    [SerializeField] private AudioClip alertClip;
    [SerializeField, Range(0f, 1f)] private float alertVolume = 0.5f;
    [Tooltip("Long clips are cut after this many seconds (0 = play the whole clip).")]
    [SerializeField, Min(0f)] private float maxSoundDuration = 3f;
    [Tooltip("Random pitch variation so the wolves don't all sound the same.")]
    [SerializeField, Range(0f, 0.5f)] private float pitchVariation = 0.1f;

    [Header("Search")]
    [Tooltip("Time spent looking around the last known position before going back to Patrol.")]
    [SerializeField, Min(0f)] private float searchDuration = 5f;
    [SerializeField, Min(0f)] private float searchRadius = 3f;

    [Header("Gizmos / Debug")]
    [SerializeField] private bool drawGizmos = true;
    [Tooltip("Pauses the Editor (Debug.Break) the moment this wolf detects the Hunter, to inspect the frame.")]
    [SerializeField] private bool pauseOnDetection;
    [Tooltip("Debug: Transform used by the context menu \"Debug/Go to debug target\".")]
    [SerializeField] private Transform debugTarget;
    [Tooltip("Debug (Play mode): tick it to send this wolf to Debug Target. It unticks itself.")]
    [SerializeField] private bool goToDebugTarget;

    private StateMachine stateMachine;
    private Renderer[] renderers;

    private List<Vector3> path = new List<Vector3>();
    private int pathIndex;
    private Vector3 pathGoal;
    private bool hasPathRequest;

    private int waypointIndex;
    private int waypointDirection = 1;

    private float lastHowlTime = float.NegativeInfinity;
    private bool hasPendingAlert;
    private Vector3 pendingAlertPosition;

    private float howlFeedbackTimer;
    private float alertFeedbackTimer;
    private Wolf lastAlertSender;

    private float soundTimer;

    public WolfState CurrentState { get; private set; }
    public Hunter Target => hunter;

    public bool HasLastKnownPosition { get; private set; }
    public Vector3 LastKnownPosition { get; private set; }

    public bool IsUsingPathfinding => hasPathRequest && path.Count > 0 && pathIndex < path.Count;

    public bool IsHowling => howlFeedbackTimer > 0f;
    public bool JustReceivedAlert => alertFeedbackTimer > 0f;
    public Wolf LastAlertSender => lastAlertSender;
    public static IReadOnlyList<Wolf> Pack => pack;

    public float PatrolSpeed => patrolSpeed;
    public float InvestigateSpeed => investigateSpeed;
    public float ChaseSpeed => chaseSpeed;
    public float SearchDuration => searchDuration;
    public float SearchRadius => searchRadius;
    public bool HasWaypoints => waypoints != null && waypoints.Length > 0;

    private void Awake()
    {
        if (hunter == null) hunter = FindFirstObjectByType<Hunter>();
        if (pathfinder == null) pathfinder = FindFirstObjectByType<Pathfinder>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        renderers = GetComponentsInChildren<Renderer>();

        ClampSpeedsBelowHunter();

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

    private void OnEnable() => pack.Add(this);
    private void OnDisable() => pack.Remove(this);

    private void Start() => stateMachine.ChangeState(WolfState.Patrol);

    private void Update()
    {
        if (goToDebugTarget)
        {
            goToDebugTarget = false;
            DebugGoToTarget();
        }

        stateMachine.Update();
        UpdateAnimation();
        UpdateSound();

        howlFeedbackTimer -= Time.deltaTime;
        alertFeedbackTimer -= Time.deltaTime;
    }

    public void NotifyStateEntered(WolfState state) => CurrentState = state;

    private void ClampSpeedsBelowHunter()
    {
        if (hunter == null) return;

        float limit = hunter.MaxSpeed * 0.95f;
        if (patrolSpeed < limit && investigateSpeed < limit && chaseSpeed < limit) return;

        Debug.LogWarning($"{name}: wolf speeds must be lower than the Hunter's ({hunter.MaxSpeed}). Clamped to {limit:0.##}.", this);
        patrolSpeed = Mathf.Min(patrolSpeed, limit);
        investigateSpeed = Mathf.Min(investigateSpeed, limit);
        chaseSpeed = Mathf.Min(chaseSpeed, limit);
    }

    #region Perception

    private Vector3 EyePosition => transform.position + Vector3.up * eyeHeight;

    private Vector3 HunterEyePosition => hunter.transform.position + Vector3.up * eyeHeight;

    public bool IsHunterAvailable =>
        hunter != null && hunter.isActiveAndEnabled && !hunter.IsDead && !hunter.IsInSafeZone;

    public bool CanSeeHunter()
    {
        if (!IsHunterAvailable) return false;

        Vector3 toHunter = hunter.transform.position - transform.position;
        toHunter.y = 0f;
        if (toHunter.sqrMagnitude > viewRadius * viewRadius) return false;
        if (toHunter.sqrMagnitude > 0.0001f && Vector3.Angle(transform.forward, toHunter) > viewAngle * 0.5f) return false;

        return HasLineOfSight(hunter.transform.position);
    }

    public bool HasLineOfSight(Vector3 point)
    {
        Vector3 from = EyePosition;
        Vector3 to = new Vector3(point.x, from.y, point.z);
        return !Physics.Linecast(from, to, obstacleMask, QueryTriggerInteraction.Ignore);
    }

    public bool HasClearPath(Vector3 point)
    {
        Vector3 toTarget = point - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;
        if (distance < 0.0001f) return true;

        return !Physics.SphereCast(EyePosition, bodyRadius, toTarget / distance, out _, distance, obstacleMask, QueryTriggerInteraction.Ignore);
    }

    public void ReportDetection()
    {
        Vector3 toHunter = hunter.transform.position - transform.position;
        toHunter.y = 0f;
        bool eyeInsideWall = Physics.CheckSphere(EyePosition, 0.05f, obstacleMask, QueryTriggerInteraction.Ignore);
        bool hunterInsideWall = Physics.CheckSphere(HunterEyePosition, 0.05f, obstacleMask, QueryTriggerInteraction.Ignore);

        string message = $"{name} SEES the Hunter: distance {toHunter.magnitude:0.00}/{viewRadius}, " +
                         $"angle {Vector3.Angle(transform.forward, toHunter):0}/{viewAngle * 0.5f:0}, " +
                         $"wolf {transform.position}, hunter {hunter.transform.position}";
        if (eyeInsideWall || hunterInsideWall)
            Debug.LogWarning($"{message}. SUSPICIOUS: eye inside wall = {eyeInsideWall}, hunter inside wall = {hunterInsideWall}.", this);
        else
            Debug.Log(message, this);

        if (pauseOnDetection) Debug.Break();
    }

    public bool IsHunterInCaptureRange()
    {
        if (!IsHunterAvailable) return false;
        return PlanarDistance(hunter.transform.position) <= captureRadius;
    }

    public void CatchHunter()
    {
        hunter.Catch();
        Debug.Log($"{name} caught the Hunter.", this);
    }

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
        hasPendingAlert = false;
    }

    #endregion

    #region Howl

    public void Howl()
    {
        if (Time.time - lastHowlTime < howlCooldown) return;
        lastHowlTime = Time.time;
        howlFeedbackTimer = 1f;

        Vector3 position = hunter.transform.position;
        Debug.Log($"{name} HOWLS: Hunter seen at {position}.", this);
        if (howlClips != null && howlClips.Length > 0)
            PlaySound(howlClips[Random.Range(0, howlClips.Length)], howlVolume);

        foreach (Wolf wolf in pack)
        {
            if (wolf != this)
                wolf.ReceiveAlert(position, this);
        }
    }

    private void ReceiveAlert(Vector3 position, Wolf sender)
    {
        hasPendingAlert = true;
        pendingAlertPosition = position;
        lastAlertSender = sender;
        alertFeedbackTimer = 1f;

        if (audioSource == null || !audioSource.isPlaying)
            PlaySound(alertClip, alertVolume);
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (audioSource == null || clip == null) return;

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        audioSource.Play();
        soundTimer = maxSoundDuration;
    }

    private void UpdateSound()
    {
        if (audioSource == null || !audioSource.isPlaying || maxSoundDuration <= 0f) return;

        soundTimer -= Time.deltaTime;
        if (soundTimer <= 0f)
            audioSource.Stop();
    }

    [ContextMenu("Debug/Go to debug target")]
    private void DebugGoToTarget()
    {
        if (!Application.isPlaying || debugTarget == null)
        {
            Debug.LogWarning($"{name}: enter Play mode and assign Debug Target first.", this);
            return;
        }

        ReceiveAlert(debugTarget.position, null);
        Debug.Log($"{name}: debug alert to {debugTarget.position}.", this);
    }

    public bool TryConsumeAlert(out Vector3 position)
    {
        position = pendingAlertPosition;
        if (!hasPendingAlert) return false;

        hasPendingAlert = false;
        return true;
    }

    #endregion

    #region Patrol waypoints

    public Vector3 CurrentWaypoint => waypoints[waypointIndex].position;

    public void AdvanceWaypoint()
    {
        if (waypoints.Length <= 1) return;

        if (patrolMode == PatrolMode.Loop)
        {
            waypointIndex = (waypointIndex + 1) % waypoints.Length;
            return;
        }

        if (waypointIndex + waypointDirection < 0 || waypointIndex + waypointDirection >= waypoints.Length)
            waypointDirection = -waypointDirection;
        waypointIndex += waypointDirection;
    }

    #endregion

    #region Movement

    public void SetColor(Color color)
    {
        foreach (Renderer r in renderers)
            r.material.color = color;
    }

    public float MoveToTarget(Vector3 target, float speed)
    {
        if (HasClearPath(target))
        {
            ClearPath();
            return MoveTowards(target, speed);
        }

        FollowPathTo(target, speed);
        return PlanarDistance(target);
    }

    public bool FollowPathTo(Vector3 goal, float speed)
    {
        if (pathfinder == null) return MoveTowards(goal, speed) <= PathWaypointTolerance;

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

        if (MoveTowards(path[pathIndex], speed) <= PathWaypointTolerance)
            pathIndex++;

        return pathIndex >= path.Count;
    }

    public bool PathFailed => hasPathRequest && path.Count == 0;

    public void ClearPath()
    {
        path.Clear();
        pathIndex = 0;
        hasPathRequest = false;
    }

    public bool IsReachable(Vector3 point) =>
        pathfinder != null
        && !Physics.CheckSphere(new Vector3(point.x, EyePosition.y, point.z), 0.4f, obstacleMask, QueryTriggerInteraction.Ignore)
        && pathfinder.FindPath(transform.position, point, excludeSafeZones: true).Count > 0;

    private void RequestPath(Vector3 goal)
    {
        path = pathfinder.FindPath(transform.position, goal, excludeSafeZones: true);
        pathGoal = goal;
        hasPathRequest = true;

        pathIndex = path.Count > 1 && HasClearPath(path[1]) ? 1 : 0;
    }

    private float MoveTowards(Vector3 target, float speed)
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
        float step = Mathf.Min(speed * Time.deltaTime, distance);

        Vector3 next = transform.position + direction * step;
        if (Physics.CheckSphere(next, bodyRadius, safeZoneMask, QueryTriggerInteraction.Collide))
            step = 0f;

        SetVelocity(Time.deltaTime > 0f ? direction * (step / Time.deltaTime) : Vector3.zero);
        transform.position += direction * step;
        Face(direction);

        return distance - step;
    }

    public void Face(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion look = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
    }

    public void Turn(float degrees) => transform.Rotate(0f, degrees, 0f);

    public void Stop() => SetVelocity(Vector3.zero);

    public float PlanarDistance(Vector3 target)
    {
        Vector3 delta = target - transform.position;
        delta.y = 0f;
        return delta.magnitude;
    }

    public bool HasArrived(Vector3 target) => PlanarDistance(target) <= ArriveDistance;

    private void UpdateAnimation()
    {
        if (animator == null) return;

        string clip = Velocity.sqrMagnitude > 0.01f ? "root_Walk" : "root_Idle";
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        if (!info.IsName(clip))
            animator.CrossFade(clip, 0.1f);

        animator.speed = Velocity.sqrMagnitude > 0.01f ? Mathf.Max(1f, Velocity.magnitude / patrolSpeed) : 1f;
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Vector3 eye = EyePosition;

        Color fovColor = Application.isPlaying && CurrentState == WolfState.Chase ? Color.red : new Color(1f, 0.6f, 0f);
        Gizmos.color = fovColor;
        Vector3 left = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.forward;
        Gizmos.DrawLine(eye, eye + left * viewRadius);
        Gizmos.DrawLine(eye, eye + right * viewRadius);

        const int segments = 20;
        Vector3 previous = eye + left * viewRadius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = -viewAngle * 0.5f + viewAngle * i / segments;
            Vector3 point = eye + Quaternion.Euler(0f, angle, 0f) * transform.forward * viewRadius;
            Gizmos.DrawLine(previous, point);
            previous = point;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, captureRadius);

        if (HasWaypoints)
        {
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.6f);
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;
                Gizmos.DrawWireSphere(waypoints[i].position, 0.2f);
                int next = i + 1;
                if (next == waypoints.Length)
                {
                    if (patrolMode == PatrolMode.PingPong) break;
                    next = 0;
                }
                if (waypoints[next] != null)
                    Gizmos.DrawLine(waypoints[i].position, waypoints[next].position);
            }
        }

        if (!Application.isPlaying) return;

        if (IsUsingPathfinding)
        {
            Gizmos.color = Color.cyan;
            for (int i = Mathf.Max(pathIndex, 1); i < path.Count; i++)
                Gizmos.DrawLine(path[i - 1] + Vector3.up * 0.2f, path[i] + Vector3.up * 0.2f);
            Gizmos.DrawLine(transform.position + Vector3.up * 0.2f, path[pathIndex] + Vector3.up * 0.2f);
        }

        if (CurrentState == WolfState.Chase && hunter != null && CanSeeHunter())
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(eye, HunterEyePosition);
        }

        if (HasLastKnownPosition)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(LastKnownPosition, 0.4f);
            Gizmos.DrawLine(eye, LastKnownPosition);
        }

        if (howlFeedbackTimer > 0f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, (1f - howlFeedbackTimer) * 6f + 0.5f);
        }

        if (alertFeedbackTimer > 0f && lastAlertSender != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(eye, lastAlertSender.EyePosition);
        }

#if UNITY_EDITOR
        string label = CurrentState.ToString();
        if (howlFeedbackTimer > 0f) label += "  AUUU!";
        else if (alertFeedbackTimer > 0f) label += "  (alert)";
        if (IsUsingPathfinding) label += "  [A*]";
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.2f, label);
#endif
    }

    #endregion
}
