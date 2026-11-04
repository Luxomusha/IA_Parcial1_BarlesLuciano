using UnityEngine;

// Goes to the last known position of the Hunter (from its own sight or from a howl),
// using A* when it isn't in line of sight. New howls update the target.
public class WolfInvestigateState : WolfStateBase
{
    private static readonly Color InvestigateColor = new Color(1f, 0.55f, 0f);

    public WolfInvestigateState(StateMachine stateMachine, Wolf wolf) : base(stateMachine, wolf) { }

    public override void Enter()
    {
        base.Enter();
        wolf.SetColor(InvestigateColor);
    }

    public override void Update()
    {
        if (TryLoseTarget() || TryCatch() || TryChase()) return;

        // A newer howl replaces the target position.
        if (wolf.TryConsumeAlert(out Vector3 alertPosition))
        {
            wolf.SetLastKnownPosition(alertPosition);
            wolf.ClearPath();
        }

        Vector3 target = wolf.LastKnownPosition;
        wolf.MoveToTarget(target, wolf.InvestigateSpeed);

        if (wolf.HasArrived(target) || wolf.PathFailed)
            ChangeState(Wolf.WolfState.Search);
    }
}
