using UnityEngine;

// Entered only with the TBA finished and a live boid in sight.
// Melee inside MeleeAttackRadius, ranged inside RangeAttackRadius,
// otherwise Pursuit (predicting the boid's future position).
public class AttackState : HunterStateBase
{
    private Boid target;

    public AttackState(StateMachine stateMachine, Hunter hunter) : base(stateMachine, hunter) { }

    public override void Enter()
    {
        base.Enter();
        hunter.SetColor(Color.red);
        target = hunter.FindClosestActiveBoid();
    }

    public override void Update()
    {
        if (TryHigherPriority()) return;

        // Target lost (out of sight or already down): back to Search, TBA untouched.
        if (target == null || target.IsDown || !hunter.CanSee(target))
        {
            ChangeState(hunter.DefaultState);
            return;
        }

        float distance = PlanarDistance(target.transform.position, hunter.transform.position);

        if (distance <= hunter.MeleeAttackRadius)
        {
            Attack(ranged: false);
            return;
        }

        if (distance <= hunter.RangeAttackRadius)
        {
            Attack(ranged: true);
            return;
        }

        hunter.MoveToTarget(PredictPosition(distance));
    }

    public override void Exit()
    {
        target = null;
        hunter.Stop();
    }

    private void Attack(bool ranged)
    {
        hunter.Stop();
        hunter.PerformAttack(target, ranged); // also restarts the TBA
        ChangeState(hunter.DefaultState);
    }

    // Pursuit: aim where the boid will be by the time the Hunter gets there.
    private Vector3 PredictPosition(float distance)
    {
        float prediction = distance / Mathf.Max(hunter.MaxSpeed, 0.01f);
        return target.transform.position + target.Velocity * prediction;
    }
}
