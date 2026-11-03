using UnityEngine;

// Common base for the Hunter states. Priority, highest first:
// Dead > Flee > Exit > Gather > Attack > Search.
// Each state checks the transitions above its own level before running its own logic,
// so every transition is still decided inside the FSM.
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

    // Dead: a wolf reached the Hunter.
    protected bool TryDead()
    {
        if (!hunter.IsCaught) return false;
        ChangeState(Hunter.HunterState.Dead);
        return true;
    }

    // Flee: a wolf was perceived (distance + line of sight).
    protected bool TryFlee()
    {
        if (!hunter.CanFlee || hunter.DetectClosestWolf() == null) return false;
        ChangeState(Hunter.HunterState.Flee);
        return true;
    }

    // Exit: RequiredGathers reached.
    protected bool TryExit()
    {
        if (!hunter.HasCompletedGathers) return false;
        ChangeState(Hunter.HunterState.Exit);
        return true;
    }

    // Shortcut for the states below Exit (Search, Attack, Gather).
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
