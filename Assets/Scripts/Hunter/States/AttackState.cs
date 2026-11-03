using UnityEngine;

public class AttackState : State
{
    private const float ReachTolerance = 0.01f;

    private readonly Hunter hunter;
    private Boid target;
    private float cooldown;

    public AttackState(StateMachine stateMachine, Hunter hunter) : base(stateMachine)
    {
        this.hunter = hunter;
    }

    public override void Enter()
    {
        hunter.SetColor(Color.red);
        target = hunter.FindClosestActiveBoid();
        cooldown = 0f;
    }

    public override void Update()
    {
        if (!IsValidTarget(target))
        {
            target = hunter.FindClosestActiveBoid();
            if (target == null)
            {
                StateMachine.ChangeState(Hunter.HunterState.Patrol);
                return;
            }
        }

        float remaining = hunter.MoveTowards(target.transform.position, hunter.MeleeAttackRadius);

        cooldown += Time.deltaTime;
        bool inMeleeRange = remaining <= hunter.MeleeAttackRadius + ReachTolerance;
        if (!inMeleeRange || cooldown < hunter.TimeBetweenAttacks)
            return;

        target.ReceiveAttack();
        cooldown = 0f;

        if (hunter.FindClosestDownBoid() != null)
        {
            StateMachine.ChangeState(Hunter.HunterState.Gather);
            return;
        }

        target = hunter.FindClosestActiveBoid();
        if (target == null)
            StateMachine.ChangeState(Hunter.HunterState.Patrol);
    }

    public override void Exit()
    {
        target = null;
        cooldown = 0f;
    }

    private bool IsValidTarget(Boid boid) => boid != null && !boid.IsDown && hunter.IsInPerception(boid);
}
