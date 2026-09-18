using UnityEngine;

public class FlockArea : MonoBehaviour
{
    public static FlockArea Instance { get; private set; }

    [Header("Tamaño de la zona (cercado)")]
    [SerializeField] private Vector3 size = new Vector3(30f, 15f, 30f);

    private void OnEnable() => Instance = this;

    private void OnDisable()
    {
        if (Instance == this)
            Instance = null;
    }

    // Efecto Pac-Man: si se sale por un lado, reaparece del lado opuesto.
    public Vector3 Wrap(Vector3 position)
    {
        Vector3 center = transform.position;
        Vector3 half = size * 0.5f;
        Vector3 local = position - center;

        if (local.x > half.x) local.x -= size.x;
        else if (local.x < -half.x) local.x += size.x;

        if (local.y > half.y) local.y -= size.y;
        else if (local.y < -half.y) local.y += size.y;

        if (local.z > half.z) local.z -= size.z;
        else if (local.z < -half.z) local.z += size.z;

        return center + local;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, size);
    }
    [SerializeField] private float alturaSuelo = 0f;

    public Vector3 PuntoAleatorio()
    {
        Vector3 half = size * 0.5f;
        return new Vector3(
            transform.position.x + Random.Range(-half.x, half.x),
            alturaSuelo,
            transform.position.z + Random.Range(-half.z, half.z));
    }
}
