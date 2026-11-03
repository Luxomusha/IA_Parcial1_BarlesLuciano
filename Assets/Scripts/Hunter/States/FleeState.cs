using System.Collections.Generic;
using UnityEngine;

// A wolf was perceived. Picks the closest Safe zone by A* path length, runs there,
// waits SafeZoneWaitTime and then goes back to Exit or Search.
public class FleeState : HunterStateBase
{
    private Transform targetZone;
    private bool arrived;
    private float waitTimer;

    public FleeState(StateMachine stateMachine, Hunter hunter) : base(stateMachine, hunter) { }

    public override void Enter()
    {
        base.Enter();
        hunter.SetColor(Color.yellow);
        targetZone = ChooseClosestSafeZone();
        hunter.ChosenSafeZone = targetZone;
        arrived = false;
        waitTimer = 0f;
    }

    public override void Update()
    {
        if (TryDead()) return;

        if (targetZone == null)
        {
            // No reachable Safe zone: nothing to run to, resume the normal activity.
            Resume();
            return;
        }

        if (!arrived)
        {
            arrived = hunter.FollowPathTo(targetZone.position);
            return;
        }

        hunter.Stop();
        waitTimer += Time.deltaTime;
        if (waitTimer >= hunter.SafeZoneWaitTime)
            Resume();
    }

    public override void Exit()
    {
        hunter.ChosenSafeZone = null;
        targetZone = null;
    }

    // Exit if the objective is already complete, otherwise Search. If a wolf is still
    // in sight, Search/Exit send the Hunter straight back here.
    private void Resume()
    {
        if (!TryExit())
            ChangeState(hunter.DefaultState);
    }

    private Transform ChooseClosestSafeZone()
    {
        Transform best = null;
        int bestLength = int.MaxValue;

        foreach (Transform zone in hunter.SafeZones)
        {
            if (zone == null) continue;

            List<Vector3> path = hunter.FindPath(zone.position);
            if (path.Count > 0 && path.Count < bestLength)
            {
                bestLength = path.Count;
                best = zone;
            }
        }

        return best;
    }
}
