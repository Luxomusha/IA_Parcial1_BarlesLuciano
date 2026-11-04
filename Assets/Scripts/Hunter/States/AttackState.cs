using UnityEngine;

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
        hunter.PerformAttack(target, ranged);
        ChangeState(hunter.DefaultState);
    }

    private Vector3 PredictPosition(float distance)
    {
        float prediction = distance / Mathf.Max(hunter.MaxSpeed, 0.01f);
        return target.transform.position + target.Velocity * prediction;
    }
}
