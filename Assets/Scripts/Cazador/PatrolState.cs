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

        // TODO 1: moverse hacia cazador.waypoints[waypointIndex] (mismo patrón que Base/PatrolAgent.cs)
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

        // TODO 2: al llegar (distancia chica), avanzar waypointIndex += direccion;
        //         si te pasás del array: si cazador.invertirAlFinal, invertir direccion (*-1),
        //         si no, volver waypointIndex a 0
        if (haciaDestino.magnitude < 0.2f)
        {
            waypointIndex += direccion;
            if (waypointIndex >= cazador.waypoints.Length || waypointIndex < 0)
            {
                if (cazador.invertirAlFinal)
                {
                    direccion *= -1;
                    waypointIndex += direccion * 2; // deshace el paso inválido y avanza en la nueva dirección
                }
                else
                {
                    waypointIndex = 0;
                }
            }
        }

        // TODO 3: spawnTimer += Time.deltaTime; si spawnTimer >= cazador.intervaloSpawn
        //         y hay menos de cazador.maxCebosActivos objetos activos
        //         (FindObjectsByType<ObjetoInteres>(...).Length), Instantiate(cazador.ceboPrefab, ...)
        //         y reiniciar spawnTimer = 0
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= cazador.intervaloSpawn)
        {
            int activos = Object.FindObjectsByType<ObjetoInteres>(FindObjectsSortMode.None).Length;
            if (activos < cazador.maxCebosActivos && cazador.ceboPrefab != null && FlockArea.Instance != null)
                Object.Instantiate(cazador.ceboPrefab, cazador.transform.position, Quaternion.identity);

            spawnTimer = 0f;
        }

        // TODO 4 (transición a Attack): usar Boid.BuscarObjetivoParaCazador(...) —
        //         lo agregamos ahora a Boid.cs, miralo abajo — y si hay un boid en
        //         cazador.rangoPercepcion, StateMachine.ChangeState(Cazador.EstadoCazador.Attack)
        if (Boid.BuscarObjetivoParaCazador(cazador.transform.position, cazador.rangoPercepcion) != null)
        {
            StateMachine.ChangeState(Cazador.EstadoCazador.Attack);
            return;
        }

        // TODO 5 (transición a Gather): si Boid.inactivos tiene alguno dentro de
        //         cazador.rangoPercepcion, StateMachine.ChangeState(Cazador.EstadoCazador.Gather)
        if (Boid.BuscarInactivoParaCazador(cazador.transform.position, cazador.rangoPercepcion) != null)
        {
            StateMachine.ChangeState(Cazador.EstadoCazador.Gather);
            return;
        }
    }

}