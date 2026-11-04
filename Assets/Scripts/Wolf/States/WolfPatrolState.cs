using UnityEngine;

public class WolfPatrolState : WolfStateBase
{
    private static readonly Color PatrolColor = new Color(0.55f, 0.55f, 0.6f);

    public WolfPatrolState(StateMachine stateMachine, Wolf wolf) : base(stateMachine, wolf) { }

    public override void Enter()
    {
        base.Enter();
        wolf.SetColor(PatrolColor);
    }

    public override void Update()
    {
        if (TryCatch() || TryChase() || TryAlert()) return;

        if (!wolf.HasWaypoints)
        {
            wolf.Stop();
            return;
        }

        Vector3 waypoint = wolf.CurrentWaypoint;
        wolf.MoveToTarget(waypoint, wolf.PatrolSpeed);

        if (wolf.HasArrived(waypoint))
        {
            wolf.AdvanceWaypoint();
            wolf.ClearPath();
        }
    }
}
