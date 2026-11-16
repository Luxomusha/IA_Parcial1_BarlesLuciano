using UnityEngine;

public class PatrolState : HunterStateBase
{
    private const float WaypointReachedDistance = 0.2f;

    private float spawnTimer;

    public PatrolState(StateMachine stateMachine, Hunter hunter) : base(stateMachine, hunter) { }

    public override void Enter()
    {
        base.Enter();
        hunter.SetColor(Color.white);
    }

    public override void Update()
    {
        if (TryGather() || TryAttack()) return;

        FollowWaypoints();
        SpawnBaitOverTime();
    }

    private void FollowWaypoints()
    {
        if (!hunter.Route.HasWaypoints) return;

        float remaining = hunter.Mover.MoveDirectly(hunter.Route.CurrentWaypoint, hunter.MaxSpeed);
        if (remaining <= WaypointReachedDistance)
            hunter.Route.Advance();
    }

    private void SpawnBaitOverTime()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer < hunter.Baits.SpawnInterval) return;

        hunter.Baits.TrySpawn(hunter.transform.position);
        spawnTimer = 0f;
    }
}
