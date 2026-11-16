using UnityEngine;

public class GatherState : HunterStateBase
{
    private const float ReachTolerance = 0.01f;

    private static readonly Color MovingColor = Color.blue;
    private static readonly Color GatheringColor = Color.cyan;

    private Boid target;
    private float gatherTimer;

    public GatherState(StateMachine stateMachine, Hunter hunter) : base(stateMachine, hunter) { }

    public override void Enter()
    {
        base.Enter();
        hunter.SetColor(MovingColor);
        target = hunter.FindClosestDownBoid();
        gatherTimer = 0f;
    }

    public override void Update()
    {
        if (TryHigherPriority()) return;

        if (target == null || !target.IsDown)
        {
            ChangeState(hunter.DefaultState);
            return;
        }

        float remaining = hunter.Mover.MoveToTarget(target.transform.position, hunter.MaxSpeed, hunter.Combat.MeleeAttackRadius);
        if (remaining > hunter.Combat.MeleeAttackRadius + ReachTolerance)
        {
            hunter.SetColor(MovingColor);
            hunter.Gathering.HideBeam();
            return;
        }

        hunter.Mover.Stop();
        gatherTimer += Time.deltaTime;

        float progress = Mathf.Clamp01(gatherTimer / hunter.Gathering.GatherDuration);
        target.SetGatherProgress(progress);
        hunter.SetColor(Color.Lerp(MovingColor, GatheringColor, Mathf.PingPong(gatherTimer * 4f, 1f)));
        hunter.Gathering.ShowBeam(target.transform.position);

        if (progress < 1f) return;

        target.Respawn();
        target = null;
        hunter.Gathering.AddGather();

        if (!TryExit())
            ChangeState(hunter.DefaultState);
    }

    public override void Exit()
    {
        if (target != null && target.IsDown)
            target.SetGatherProgress(0f);

        hunter.Gathering.HideBeam();
        target = null;
        gatherTimer = 0f;
    }
}
