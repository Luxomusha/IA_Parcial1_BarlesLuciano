using UnityEngine;

public class ObjetoInteres : MonoBehaviour
{
    [SerializeField] private float duracion = 4f;

    [Header("Gizmos")]
    [SerializeField] private bool mostrarGizmos = true;
    [SerializeField] private float radioGizmo = 0.5f; // solo visual: referencia del interactRadius del Boid

    private void Update()
    {
        TiempoDeExposicion(Time.deltaTime);
    }

    private void OnDrawGizmos()
    {
        if (!mostrarGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioGizmo);
    }

    public void TiempoDeExposicion(float deltaTime)
    {
        duracion -= deltaTime;
        if (duracion <= 0f)
            Destroy(gameObject);
    }

    public void Atrapar(Boid boid)
    {
        boid.Engancharse();
        Destroy(gameObject);
    }
}