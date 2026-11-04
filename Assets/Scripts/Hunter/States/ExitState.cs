using UnityEngine;

public class ExitState : HunterStateBase
{
    public ExitState(StateMachine stateMachine, Hunter hunter) : base(stateMachine, hunter) { }

    public override void Enter()
    {
        base.Enter();
        hunter.SetColor(Color.green);
    }

    public override void Update()
    {
        if (TryDead() || TryFlee()) return;

        if (!hunter.FollowPathTo(hunter.ExitPosition)) return;

        hunter.RespawnAtStart();
        ChangeState(hunter.DefaultState);
    }
}
