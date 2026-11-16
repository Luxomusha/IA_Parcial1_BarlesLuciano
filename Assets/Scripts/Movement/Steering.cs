using UnityEngine;

public static class Steering
{
    private const float Epsilon = 0.0001f;

    public static Vector3 Evade(Vector3 position, Vector3 velocity, float maxSpeed, Vector3 threatPosition, Vector3 threatVelocity)
    {
        float prediction = (threatPosition - position).magnitude / (maxSpeed + threatVelocity.magnitude);
        Vector3 futurePosition = threatPosition + threatVelocity * prediction;

        Vector3 desired = position - futurePosition;
        desired.y = 0f;
        if (desired.sqrMagnitude < Epsilon) return Vector3.zero;

        return desired.normalized * maxSpeed - velocity;
    }

    public static Vector3 Arrive(Vector3 position, Vector3 velocity, float maxSpeed, Vector3 target, float slowRadius)
    {
        Vector3 toTarget = target - position;
        float distance = toTarget.magnitude;
        if (distance < Epsilon) return Vector3.zero;

        float speed = distance < slowRadius ? maxSpeed * (distance / slowRadius) : maxSpeed;
        return toTarget.normalized * speed - velocity;
    }

    public static Vector3 Wander(ref Vector3 wanderDirection, Vector3 velocity, float maxSpeed, float jitter)
    {
        wanderDirection += new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * jitter * Time.deltaTime;
        wanderDirection = wanderDirection.normalized;

        return wanderDirection * maxSpeed - velocity;
    }

    public static Vector3 RandomPlanarDirection()
    {
        Vector2 direction = Random.insideUnitCircle.normalized;
        return new Vector3(direction.x, 0f, direction.y);
    }
}
