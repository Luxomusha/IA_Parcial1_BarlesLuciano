using UnityEngine;

// Common base for the wolf states. Every transition is decided by the states
// themselves from what the wolf perceives (or the howls it received).
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

    // Close enough to the Hunter: it goes to Dead and the wolf forgets it.
    protected bool TryCatch()
    {
        if (!wolf.IsHunterInCaptureRange()) return false;

        wolf.CatchHunter();
        wolf.ForgetTarget();
        ChangeState(Wolf.WolfState.Patrol);
        return true;
    }

    // The Hunter is inside the FOV: chase it (Chase howls on Enter).
    protected bool TryChase()
    {
        if (!wolf.CanSeeHunter()) return false;

        wolf.SetLastKnownPosition(wolf.Target.transform.position);
        ChangeState(Wolf.WolfState.Chase);
        return true;
    }

    // Another wolf howled: go to the position it reported.
    protected bool TryAlert()
    {
        if (!wolf.TryConsumeAlert(out Vector3 position)) return false;

        wolf.SetLastKnownPosition(position);
        ChangeState(Wolf.WolfState.Investigate);
        return true;
    }

    // The Hunter entered a Safe zone or died: drop the target and go back to Patrol.
    protected bool TryLoseTarget()
    {
        if (wolf.IsHunterAvailable) return false;

        wolf.ForgetTarget();
        ChangeState(Wolf.WolfState.Patrol);
        return true;
    }
}
