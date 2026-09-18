using UnityEngine;

public class Cazador : Agent
{
    public enum EstadoCazador { Patrol, Attack, Gather }

    [Header("Movimiento")]
    public float maxSpeed = 4f;

    [Header("Patrol")]
    public Transform[] waypoints;
    public bool invertirAlFinal = true;

    [Header("Spawn de cebo")]
    public GameObject ceboPrefab;
    public float intervaloSpawn = 5f;
    public int maxCebosActivos = 3;

    [Header("Ataque")]
    public float rangoPercepcion = 8f;
    public float rangeAttackRadius = 4f;
    public float meleeAttackRadius = 1f;
    public float tba = 3f;

    [Header("Gather")]
    public float tiempoRecoleccion = 1.5f;

    [Header("Gizmos")]
    [SerializeField] private bool mostrarGizmos = true;

    private StateMachine sm;
    private Renderer[] renders;

    private void Awake()
    {
        renders = GetComponentsInChildren<Renderer>();

        sm = new StateMachine();
        sm.RegisterState(EstadoCazador.Patrol, new PatrolState(sm, this));
        sm.RegisterState(EstadoCazador.Attack, new AttackState(sm, this));
        sm.RegisterState(EstadoCazador.Gather, new GatherState(sm, this));
    }

    public void SetColor(Color c)
    {
        foreach (Renderer r in renders)
            r.material.color = c;
    }

    private void Start() => sm.ChangeState(EstadoCazador.Patrol);
    private void Update() => sm.Update();

    private void OnDrawGizmos()
    {
        if (!mostrarGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoPercepcion);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, rangeAttackRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRadius);
    }

    public class AttackState : State
    {
        private Cazador cazador;
        private Boid objetivo;
        private float cooldown = 0f;

        public AttackState(StateMachine sm, Cazador cazador) : base(sm) { this.cazador = cazador; }

        public override void Enter()
        {
            cazador.SetColor(Color.red);
            objetivo = Boid.BuscarObjetivoParaCazador(cazador.transform.position, cazador.rangoPercepcion);
            cooldown = 0f;
        }

        public override void Update()
        {
            bool objetivoInvalido = objetivo == null ||
                Vector3.Distance(cazador.transform.position, objetivo.transform.position) > cazador.rangoPercepcion;

            if (objetivoInvalido)
            {
                objetivo = Boid.BuscarObjetivoParaCazador(cazador.transform.position, cazador.rangoPercepcion);
                if (objetivo == null)
                {
                    StateMachine.ChangeState(EstadoCazador.Patrol);
                    return;
                }
            }

            Vector3 haciaObjetivo = objetivo.transform.position - cazador.transform.position;
            haciaObjetivo.y = 0f;
            float distancia = haciaObjetivo.magnitude;

            Vector3 direccion = distancia > 0.0001f ? haciaObjetivo.normalized : Vector3.zero;
            Vector3 velocidad = direccion * cazador.maxSpeed;
            cazador.SetVelocity(velocidad);

            float distanciaRestante = distancia - cazador.meleeAttackRadius;
            float paso = Mathf.Min(cazador.maxSpeed * Time.deltaTime, Mathf.Max(0f, distanciaRestante));
            cazador.transform.position += direccion * paso;

            if (direccion != Vector3.zero)
                cazador.transform.forward = direccion;

            cooldown += Time.deltaTime;
            bool alcanceEsteFrame = distanciaRestante <= cazador.maxSpeed * Time.deltaTime;
            if (alcanceEsteFrame && cooldown >= cazador.tba)
            {
                objetivo.RecibirAtaque();
                cooldown = 0f;

                if (Boid.BuscarInactivoParaCazador(cazador.transform.position, cazador.rangoPercepcion) != null)
                {
                    StateMachine.ChangeState(EstadoCazador.Gather);
                    return;
                }

                objetivo = Boid.BuscarObjetivoParaCazador(cazador.transform.position, cazador.rangoPercepcion);
                if (objetivo == null)
                    StateMachine.ChangeState(EstadoCazador.Patrol);
            }
        }

        public override void Exit()
        {
            objetivo = null;
            cooldown = 0f;
        }
    }

    public class GatherState : State
    {
        private Cazador cazador;
        private Boid objetivo;
        private float timerRecoleccion = 0f;

        public GatherState(StateMachine sm, Cazador cazador) : base(sm) { this.cazador = cazador; }

        public override void Enter()
        {
            cazador.SetColor(Color.blue);
            objetivo = Boid.BuscarInactivoParaCazador(cazador.transform.position, cazador.rangoPercepcion);
            timerRecoleccion = 0f;
        }

        public override void Update()
        {
            if (objetivo == null)
            {
                objetivo = Boid.BuscarInactivoParaCazador(cazador.transform.position, cazador.rangoPercepcion);

                if (objetivo == null)
                {
                    if (Boid.BuscarObjetivoParaCazador(cazador.transform.position, cazador.rangoPercepcion) != null)
                    {
                        StateMachine.ChangeState(EstadoCazador.Attack);
                        return;
                    }

                    StateMachine.ChangeState(EstadoCazador.Patrol);
                    return;
                }
            }

            Vector3 haciaObjetivo = objetivo.transform.position - cazador.transform.position;
            haciaObjetivo.y = 0f;
            float distancia = haciaObjetivo.magnitude;

            if (distancia <= cazador.meleeAttackRadius)
            {
                cazador.SetVelocity(Vector3.zero);
                timerRecoleccion += Time.deltaTime;

                if (timerRecoleccion >= cazador.tiempoRecoleccion)
                {
                    objetivo.Recolectar();
                    objetivo = Boid.BuscarInactivoParaCazador(cazador.transform.position, cazador.rangoPercepcion);
                    timerRecoleccion = 0f;
                }
                return;
            }

            Vector3 velocidad = distancia > 0.0001f
                ? haciaObjetivo.normalized * cazador.maxSpeed
                : Vector3.zero;

            cazador.SetVelocity(velocidad);
            cazador.transform.position += velocidad * Time.deltaTime;
            if (velocidad != Vector3.zero)
                cazador.transform.forward = velocidad.normalized;
        }

        public override void Exit()
        {
            objetivo = null;
            timerRecoleccion = 0f;
        }
    }
}
