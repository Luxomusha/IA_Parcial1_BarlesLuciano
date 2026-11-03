using UnityEngine;

// Bait spawned by the Hunter. Boids only notice it when its collider (Bait layer)
// enters their detection radius.
[RequireComponent(typeof(Collider))]
public class Bait : MonoBehaviour
{
    [SerializeField] private float lifetime = 4f;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private float gizmoRadius = 0.5f;

    private bool consumed;

    public bool IsAvailable => !consumed;

    private void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
            Destroy(gameObject);
    }

    public void Consume(Boid boid)
    {
        if (consumed) return;

        consumed = true;
        boid.Hook();
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);
    }
}
