using UnityEngine;

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

        Vector3 hunterPosition = wolf.Target.transform.position;
        wolf.SetLastKnownPosition(hunterPosition);
        wolf.MoveToTarget(hunterPosition, wolf.ChaseSpeed);

        wolf.TryConsumeAlert(out _);
    }
}
