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

        float remaining = hunter.MoveToTarget(target.transform.position, hunter.MeleeAttackRadius);
        if (remaining > hunter.MeleeAttackRadius + ReachTolerance)
        {
            hunter.SetColor(MovingColor);
            hunter.HideGatherBeam();
            return;
        }

        hunter.Stop();
        gatherTimer += Time.deltaTime;

        float progress = Mathf.Clamp01(gatherTimer / hunter.GatherDuration);
        target.SetGatherProgress(progress);
        hunter.SetColor(Color.Lerp(MovingColor, GatheringColor, Mathf.PingPong(gatherTimer * 4f, 1f)));
        hunter.ShowGatherBeam(target.transform.position);

        if (progress < 1f) return;

        target.Respawn();
        target = null;
        hunter.AddGather();

        if (!TryExit())
            ChangeState(hunter.DefaultState);
    }

    public override void Exit()
    {
        if (target != null && target.IsDown)
            target.SetGatherProgress(0f);

        hunter.HideGatherBeam();
        target = null;
        gatherTimer = 0f;
    }
}
