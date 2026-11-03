using UnityEngine;

// Initial state (replaces Patrol). Goes to the Hunting ground with A*, then wanders
// between random nodes of the Hunting ground and spawns baits inside it.
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

        // Order defined by the brief: Gather first, then Attack.
        if (TryGather() || TryAttack()) return;

        if (!hunter.IsInHuntingGround && !hasWanderTarget)
        {
            hunter.FollowPathTo(hunter.HuntingGroundCenter);
            return;
        }

        if (!hasWanderTarget)
            PickWanderTarget();

        // New random node when this one is reached or can't be reached (e.g. it fell on a wall).
        if (hunter.FollowPathTo(wanderTarget) || hunter.PathFailed)
            PickWanderTarget();

        if (!hunter.IsInHuntingGround) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= hunter.SpawnInterval)
        {
            hunter.TrySpawnBait(hunter.RandomHuntingGroundPoint());
            spawnTimer = 0f;
        }
    }

    private void PickWanderTarget()
    {
        wanderTarget = hunter.RandomHuntingGroundPoint();
        hasWanderTarget = true;
        hunter.ClearPath();
    }
}
