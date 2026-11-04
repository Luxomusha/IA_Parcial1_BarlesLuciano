using UnityEngine;

public abstract class WolfStateBase : State
{
    protected readonly Wolf wolf;

    public Wolf.WolfState Key { get; set; }

    protected WolfStateBase(StateMachine stateMachine, Wolf wolf) : base(stateMachine)
    {
        this.wolf = wolf;
    }

    public override void Enter()
    {
        wolf.NotifyStateEntered(Key);
        wolf.ClearPath();
    }

    protected void ChangeState(Wolf.WolfState state) => StateMachine.ChangeState(state);

    protected bool TryCatch()
    {
        if (!wolf.IsHunterInCaptureRange()) return false;

        wolf.CatchHunter();
        wolf.ForgetTarget();
        ChangeState(Wolf.WolfState.Patrol);
        return true;
    }

    protected bool TryChase()
    {
        if (!wolf.CanSeeHunter()) return false;

        wolf.SetLastKnownPosition(wolf.Target.transform.position);
        ChangeState(Wolf.WolfState.Chase);
        return true;
    }

    protected bool TryAlert()
    {
        if (!wolf.TryConsumeAlert(out Vector3 position)) return false;

        wolf.SetLastKnownPosition(position);
        ChangeState(Wolf.WolfState.Investigate);
        return true;
    }

    protected bool TryLoseTarget()
    {
        if (wolf.IsHunterAvailable) return false;

        wolf.ForgetTarget();
        ChangeState(Wolf.WolfState.Patrol);
        return true;
    }
}
