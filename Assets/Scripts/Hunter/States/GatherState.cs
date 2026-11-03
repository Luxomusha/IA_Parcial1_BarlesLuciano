using UnityEngine;

public class GatherState : State
{
    private const float ReachTolerance = 0.01f;

    private static readonly Color MovingColor = Color.blue;
    private static readonly Color GatheringColor = Color.cyan;

    private readonly Hunter hunter;
    private Boid target;
    private float gatherTimer;

    public GatherState(StateMachine stateMachine, Hunter hunter) : base(stateMachine)
    {
        this.hunter = hunter;
    }

    public override void Enter()
    {
        hunter.SetColor(MovingColor);
        SelectNextTarget();
    }

    public override void Update()
    {
        if (target == null || !target.IsDown)
        {
            SelectNextTarget();
            if (target == null)
            {
                bool boidInSight = hunter.FindClosestActiveBoid() != null;
                StateMachine.ChangeState(boidInSight ? Hunter.HunterState.Attack : Hunter.HunterState.Patrol);
                return;
            }
        }

        float remaining = hunter.MoveTowards(target.transform.position, hunter.MeleeAttackRadius);
        if (remaining > hunter.MeleeAttackRadius + ReachTolerance)
        {
            hunter.SetColor(MovingColor);
            hunter.HideGatherBeam();
            return;
        }

        hunter.Stop();
        gatherTimer += Time.deltaTime;

        // Feedback: the boid shrinks, the Hunter shifts color and a beam links both while gathering.
        float progress = Mathf.Clamp01(gatherTimer / hunter.GatherDuration);
        target.SetGatherProgress(progress);
        hunter.SetColor(Color.Lerp(MovingColor, GatheringColor, Mathf.PingPong(gatherTimer * 4f, 1f)));
        hunter.ShowGatherBeam(target.transform.position);

        if (progress >= 1f)
        {
            target.Respawn();
            hunter.HideGatherBeam();
            SelectNextTarget();
        }
    }

    public override void Exit()
    {
        if (target != null && target.IsDown)
            target.SetGatherProgress(0f);

        hunter.HideGatherBeam();
        target = null;
        gatherTimer = 0f;
    }

    private void SelectNextTarget()
    {
        target = hunter.FindClosestDownBoid();
        gatherTimer = 0f;
    }
}
