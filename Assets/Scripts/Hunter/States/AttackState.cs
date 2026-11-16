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

        if (distance <= hunter.Combat.MeleeAttackRadius)
        {
            Attack(ranged: false);
            return;
        }

        if (distance <= hunter.Combat.RangeAttackRadius)
        {
            Attack(ranged: true);
            return;
        }

        hunter.Mover.MoveToTarget(PredictPosition(distance), hunter.MaxSpeed);
    }

    public override void Exit()
    {
        target = null;
        hunter.Mover.Stop();
    }

    private void Attack(bool ranged)
    {
        hunter.Mover.Stop();
        hunter.Attack(target, ranged);
        ChangeState(hunter.DefaultState);
    }

    private Vector3 PredictPosition(float distance)
    {
        float prediction = distance / Mathf.Max(hunter.MaxSpeed, 0.01f);
        return target.transform.position + target.Velocity * prediction;
    }
}
