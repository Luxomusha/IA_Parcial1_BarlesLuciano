using UnityEngine;

public class PatrolState : State
{
    private Cazador cazador;
    private int waypointIndex = 0;
    private int direccion = 1;
    private float spawnTimer = 0f;

    public PatrolState(StateMachine sm, Cazador cazador) : base(sm)
    {
        this.cazador = cazador;
    }

    public override void Enter()
    {
        cazador.SetColor(Color.white);
    }

    public override void Update()
    {
        if (cazador.waypoints == null || cazador.waypoints.Length == 0)
            return;

        Transform destino = cazador.waypoints[waypointIndex];
        Vector3 haciaDestino = destino.position - cazador.transform.position;
        haciaDestino.y = 0f;

        Vector3 velocidad = haciaDestino.sqrMagnitude > 0.0001f
            ? haciaDestino.normalized * cazador.maxSpeed
            : Vector3.zero;

        cazador.SetVelocity(velocidad);
        cazador.transform.position += velocidad * Time.deltaTime;
        if (velocidad != Vector3.zero)
            cazador.transform.forward = velocidad.normalized;

        if (haciaDestino.magnitude < 0.2f)
        {
            waypointIndex += direccion;
            if (waypointIndex >= cazador.waypoints.Length || waypointIndex < 0)
            {
                if (cazador.invertirAlFinal)
                {
                    direccion *= -1;
                    waypointIndex += direccion * 2;
                }
                else
                {
                    waypointIndex = 0;
                }
            }
        }

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= cazador.intervaloSpawn)
        {
            int activos = Object.FindObjectsByType<ObjetoInteres>(FindObjectsSortMode.None).Length;
            if (activos < cazador.maxCebosActivos && cazador.ceboPrefab != null && FlockArea.Instance != null)
                Object.Instantiate(cazador.ceboPrefab, cazador.transform.position, Quaternion.identity);

            spawnTimer = 0f;
        }

        if (Boid.BuscarObjetivoParaCazador(cazador.transform.position, cazador.rangoPercepcion) != null)
        {
            StateMachine.ChangeState(Cazador.EstadoCazador.Attack);
            return;
        }

        if (Boid.BuscarInactivoParaCazador(cazador.transform.position, cazador.rangoPercepcion) != null)
        {
            StateMachine.ChangeState(Cazador.EstadoCazador.Gather);
            return;
        }
    }

}
