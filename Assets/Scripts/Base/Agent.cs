using UnityEngine;

public class Agent : MonoBehaviour, IVelocityProvider
{
    public Vector3 Velocity => _velocity;
    protected Vector3 _velocity;

    public void SetVelocity(Vector3 velocity) => _velocity = velocity;
}