using UnityEngine;

public class PatrolState : HunterStateBase
{
    private const float WaypointReachedDistance = 0.2f;

    private int waypointIndex;
    private int direction = 1;
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

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= hunter.SpawnInterval)
        {
            hunter.TrySpawnBait();
            spawnTimer = 0f;
        }
    }

    private void FollowWaypoints()
    {
        Transform[] waypoints = hunter.Waypoints;
        if (waypoints == null || waypoints.Length == 0)
            return;

        float remaining = hunter.MoveTowards(waypoints[waypointIndex].position);
        if (remaining > WaypointReachedDistance || waypoints.Length == 1)
            return;

        waypointIndex += direction;
        if (waypointIndex >= 0 && waypointIndex < waypoints.Length)
            return;

        if (hunter.PingPongWaypoints)
        {
            direction *= -1;
            waypointIndex += direction * 2;
        }
        else
        {
            waypointIndex = 0;
        }
    }
}
