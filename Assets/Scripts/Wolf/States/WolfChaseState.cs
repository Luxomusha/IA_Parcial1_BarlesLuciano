using UnityEngine;

// The wolf sees the Hunter: howls once on Enter and chases it while it stays
// inside its FOV. Losing sight sends it to the last known position.
public class WolfChaseState : WolfStateBase
{
    public WolfChaseState(StateMachine stateMachine, Wolf wolf) : base(stateMachine, wolf) { }

    public override void Enter()
    {
        base.Enter();
        wolf.SetColor(Color.red);
        wolf.ReportDetection();
        wolf.Howl();
    }

    public override void Update()
    {
        if (TryLoseTarget() || TryCatch()) return;

        if (!wolf.CanSeeHunter())
        {
            ChangeState(Wolf.WolfState.Investigate);
            return;
        }

        // While in sight the wolf keeps its own memory updated (no howl spam).
        Vector3 hunterPosition = wolf.Target.transform.position;
        wolf.SetLastKnownPosition(hunterPosition);
        wolf.MoveToTarget(hunterPosition, wolf.ChaseSpeed);

        // Alerts from other wolves are useless while we see the Hunter ourselves.
        wolf.TryConsumeAlert(out _);
    }
}
