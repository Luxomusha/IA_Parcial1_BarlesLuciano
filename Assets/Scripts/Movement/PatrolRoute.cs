using UnityEngine;

public class PatrolRoute : MonoBehaviour
{
    public enum Mode { PingPong, Loop }

    [SerializeField] private Transform[] waypoints;
    [Tooltip("PingPong: walk the waypoints back in reverse order. Loop: go back to the first one.")]
    [SerializeField] private Mode mode = Mode.PingPong;
    [SerializeField] private bool drawGizmos = true;

    private int index;
    private int direction = 1;

    public bool HasWaypoints => waypoints != null && waypoints.Length > 0;
    public Vector3 CurrentWaypoint => waypoints[index].position;

    public void Advance()
    {
        if (waypoints.Length <= 1) return;

        if (mode == Mode.Loop)
        {
            index = (index + 1) % waypoints.Length;
            return;
        }

        if (index + direction < 0 || index + direction >= waypoints.Length)
            direction = -direction;
        index += direction;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos || !HasWaypoints) return;

        Gizmos.color = new Color(1f, 0.6f, 0f, 0.6f);
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;

            Gizmos.DrawWireSphere(waypoints[i].position, 0.2f);
            DrawSegment(i);
        }
    }

    private void DrawSegment(int from)
    {
        int to = from + 1;
        if (to == waypoints.Length)
        {
            if (mode == Mode.PingPong) return;
            to = 0;
        }

        if (waypoints[to] != null)
            Gizmos.DrawLine(waypoints[from].position, waypoints[to].position);
    }
}
