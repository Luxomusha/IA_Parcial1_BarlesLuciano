using UnityEngine;

public class PatrolState : State
{
    private const float WaypointReachedDistance = 0.2f;

    private readonly Hunter hunter;
    private int waypointIndex;
    private int direction = 1;
    private float spawnTimer;

    public PatrolState(StateMachine stateMachine, Hunter hunter) : base(stateMachine)
    {
        this.hunter = hunter;
    }

    public override void Enter()
    {
        hunter.SetColor(Color.white);
    }

    public override void Update()
    {
        if (hunter.FindClosestActiveBoid() != null)
        {
            StateMachine.ChangeState(Hunter.HunterState.Attack);
            return;
        }

        if (hunter.FindClosestDownBoid() != null)
        {
            StateMachine.ChangeState(Hunter.HunterState.Gather);
            return;
        }

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
