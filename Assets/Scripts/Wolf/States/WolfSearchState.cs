using UnityEngine;

// Reached the last known position without seeing the Hunter: looks around nearby
// reachable points for SearchDuration, then forgets the target and patrols again.
public class WolfSearchState : WolfStateBase
{
    private static readonly Color SearchColor = new Color(1f, 0.9f, 0.2f);
    private const float LookAroundTime = 0.8f;
    private const float LookAroundTurnSpeed = 180f;

    private float timer;
    private float lookTimer;
    private bool hasSearchPoint;
    private Vector3 searchPoint;

    public WolfSearchState(StateMachine stateMachine, Wolf wolf) : base(stateMachine, wolf) { }

    public override void Enter()
    {
        base.Enter();
        wolf.SetColor(SearchColor);
        timer = 0f;
        lookTimer = LookAroundTime;
        hasSearchPoint = false;
    }

    public override void Update()
    {
        if (TryLoseTarget() || TryCatch() || TryChase() || TryAlert()) return;

        timer += Time.deltaTime;
        if (timer >= wolf.SearchDuration)
        {
            wolf.ForgetTarget();
            ChangeState(Wolf.WolfState.Patrol);
            return;
        }

        // Turn on the spot for a moment so the FOV sweeps the area.
        if (lookTimer > 0f)
        {
            lookTimer -= Time.deltaTime;
            wolf.Stop();
            wolf.Turn(LookAroundTurnSpeed * Time.deltaTime);
            return;
        }

        if (!hasSearchPoint)
            hasSearchPoint = PickSearchPoint();

        if (!hasSearchPoint)
        {
            lookTimer = LookAroundTime;
            return;
        }

        wolf.MoveToTarget(searchPoint, wolf.InvestigateSpeed);
        if (wolf.HasArrived(searchPoint) || wolf.PathFailed)
        {
            hasSearchPoint = false;
            lookTimer = LookAroundTime;
            wolf.ClearPath();
        }
    }

    // Random point around the last known position that the wolf can reach with A*.
    private bool PickSearchPoint()
    {
        for (int i = 0; i < 6; i++)
        {
            Vector2 offset = Random.insideUnitCircle * wolf.SearchRadius;
            Vector3 point = wolf.LastKnownPosition + new Vector3(offset.x, 0f, offset.y);
            if (wolf.IsReachable(point))
            {
                searchPoint = point;
                wolf.ClearPath();
                return true;
            }
        }
        return false;
    }
}
