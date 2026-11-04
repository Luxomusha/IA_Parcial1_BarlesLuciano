using UnityEngine;

public abstract class HunterStateBase : State
{
    protected readonly Hunter hunter;

    public Hunter.HunterState Key { get; set; }

    protected HunterStateBase(StateMachine stateMachine, Hunter hunter) : base(stateMachine)
    {
        this.hunter = hunter;
    }

    public override void Enter()
    {
        hunter.NotifyStateEntered(Key);
        hunter.ClearPath();
    }

    protected void ChangeState(Hunter.HunterState state) => StateMachine.ChangeState(state);

    protected bool TryDead()
    {
        if (!hunter.IsCaught) return false;
        ChangeState(Hunter.HunterState.Dead);
        return true;
    }

    protected bool TryFlee()
    {
        if (!hunter.CanFlee || hunter.DetectClosestWolf() == null) return false;
        ChangeState(Hunter.HunterState.Flee);
        return true;
    }

    protected bool TryExit()
    {
        if (!hunter.HasCompletedGathers) return false;
        ChangeState(Hunter.HunterState.Exit);
        return true;
    }

    protected bool TryHigherPriority() => TryDead() || TryFlee() || TryExit();

    protected bool TryGather()
    {
        if (hunter.FindClosestDownBoid() == null) return false;
        ChangeState(Hunter.HunterState.Gather);
        return true;
    }

    protected bool TryAttack()
    {
        if (!hunter.IsAttackReady || hunter.FindClosestActiveBoid() == null) return false;
        ChangeState(Hunter.HunterState.Attack);
        return true;
    }

    protected float PlanarDistance(Vector3 a, Vector3 b)
    {
        Vector3 delta = a - b;
        delta.y = 0f;
        return delta.magnitude;
    }
}
