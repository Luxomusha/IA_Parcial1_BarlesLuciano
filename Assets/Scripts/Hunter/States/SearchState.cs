using UnityEngine;

public class SearchState : HunterStateBase
{
    private bool hasWanderTarget;
    private Vector3 wanderTarget;
    private float spawnTimer;

    public SearchState(StateMachine stateMachine, Hunter hunter) : base(stateMachine, hunter) { }

    public override void Enter()
    {
        base.Enter();
        hunter.SetColor(Color.white);
        hasWanderTarget = false;
    }

    public override void Update()
    {
        if (TryHigherPriority()) return;

        if (TryGather() || TryAttack()) return;

        if (!hunter.IsInHuntingGround && !hasWanderTarget)
        {
            hunter.Mover.FollowPathTo(hunter.HuntingGroundCenter, hunter.MaxSpeed);
            return;
        }

        if (!hasWanderTarget)
            PickWanderTarget();

        if (hunter.Mover.FollowPathTo(wanderTarget, hunter.MaxSpeed) || hunter.Mover.PathFailed)
            PickWanderTarget();

        if (!hunter.IsInHuntingGround) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= hunter.Baits.SpawnInterval)
        {
            hunter.Baits.TrySpawn(hunter.RandomHuntingGroundPoint());
            spawnTimer = 0f;
        }
    }

    private void PickWanderTarget()
    {
        wanderTarget = hunter.RandomHuntingGroundPoint();
        hasWanderTarget = true;
        hunter.Mover.ClearPath();
    }
}
