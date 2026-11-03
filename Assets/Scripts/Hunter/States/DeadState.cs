using UnityEngine;

// Caught by a wolf: hidden and inactive during RespawnTime, then back to the
// Start zone with the gather counter reset.
public class DeadState : HunterStateBase
{
    private float timer;

    public DeadState(StateMachine stateMachine, Hunter hunter) : base(stateMachine, hunter) { }

    public override void Enter()
    {
        base.Enter();
        hunter.Stop();
        hunter.SetVisible(false);
        timer = 0f;
    }

    public override void Update()
    {
        timer += Time.deltaTime;
        if (timer < hunter.RespawnTime) return;

        hunter.RespawnAtStart();
        ChangeState(hunter.DefaultState);
    }

    public override void Exit()
    {
        hunter.SetVisible(true);
    }
}
