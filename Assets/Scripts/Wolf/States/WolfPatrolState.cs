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

        if (!wolf.Route.HasWaypoints)
        {
            wolf.Mover.Stop();
            return;
        }

        Vector3 waypoint = wolf.Route.CurrentWaypoint;
        wolf.Mover.MoveToTarget(waypoint, wolf.PatrolSpeed);

        if (wolf.Mover.HasArrived(waypoint))
        {
            wolf.Route.Advance();
            wolf.Mover.ClearPath();
        }
    }
}
