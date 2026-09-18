using System.Collections.Generic;
using UnityEngine;

public class Boid : Agent
{
    [Header("Movimiento")]
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float maxSteering = 10f;

    [Header("Radios de percepción")]
    [SerializeField] private float separationRadius = 1.5f;
    [SerializeField] private float neighborRadius = 4f;

    [Header("Pesos")]
    [SerializeField] private float separationWeight = 1.5f;
    [SerializeField] private float alignmentWeight = 1f;
    [SerializeField] private float cohesionWeight = 1f;

    [Header("Wander (para que nunca queden quietos)")]
    [SerializeField] private float wanderWeight = 0.5f;
    [SerializeField] private float wanderJitter = 2f;
    private Vector3 wanderDireccion;

    [Header("Evade")]
    [SerializeField] private float evadeRadius = 6f;
    [SerializeField] private string cazadorTag = "Cazador";
    private Transform cazador;

    [Header("Objeto de interés (cebo)")]
    [SerializeField] private float arriveSlowRadius = 2f;
    [SerializeField] private float interactRadius = 0.5f;
    private bool enganchado = false;

    [Header("Gizmos")]
    [SerializeField] private bool mostrarGizmos = true;

    [Header("Audio")]
    [SerializeField] private AudioClip sonidoDolor;
    private AudioSource audioSource;

    private static readonly List<Boid> allBoids = new List<Boid>();
    public static readonly List<Boid> enganchados = new List<Boid>();
    private bool estaInactivo = false;
    private static readonly List<Boid> inactivos = new List<Boid>();

    private void OnEnable() => allBoids.Add(this);
    private void OnDisable() => allBoids.Remove(this);

    private void Start()
    {
        GameObject cazadorObj = GameObject.FindWithTag(cazadorTag);
        if (cazadorObj != null) cazador = cazadorObj.transform;

        Vector2 dirAleatoria = Random.insideUnitCircle.normalized;
        wanderDireccion = new Vector3(dirAleatoria.x, 0f, dirAleatoria.y);
        _velocity = wanderDireccion * maxSpeed * 0.5f;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void OnDrawGizmos()
    {
        if (!mostrarGizmos) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, neighborRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, evadeRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, arriveSlowRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }

    private void Update()
    {
        if (estaInactivo)
            return;

        if (enganchado)
            return;

        Vector3 steering;
        bool cazadorCerca = cazador != null &&
            (cazador.position - transform.position).sqrMagnitude <= evadeRadius * evadeRadius;

        if (cazadorCerca)
        {
            steering = Evade();
        }
        else if (BuscarObjetoMasCercano() is ObjetoInteres cebo)
        {
            float dist = Vector3.Distance(transform.position, cebo.transform.position);
            if (dist <= interactRadius)
            {
                cebo.Atrapar(this);
                return;
            }

            List<Boid> closeNeighbors = GetNeighbors(separationRadius);
            steering = Arrive(cebo.transform.position) + Separation(closeNeighbors) * separationWeight;
        }
        else
        {
            List<Boid> neighbors = GetNeighbors(neighborRadius);
            List<Boid> closeNeighbors = GetNeighbors(separationRadius);

            Vector3 separation = Separation(closeNeighbors);
            Vector3 alignment = Alignment(neighbors);
            Vector3 cohesion = Cohesion(neighbors);
            Vector3 wander = Wander();

            steering = separation * separationWeight
                     + alignment * alignmentWeight
                     + cohesion * cohesionWeight
                     + wander * wanderWeight;
        }

        steering = Vector3.ClampMagnitude(steering, maxSteering * Time.deltaTime);
        _velocity = Vector3.ClampMagnitude(_velocity + steering, maxSpeed);
        _velocity.y = 0f;

        transform.position += _velocity * Time.deltaTime;
        if (FlockArea.Instance != null)
            transform.position = FlockArea.Instance.Wrap(transform.position);

        ResolverSuperposicion();

        if (_velocity.sqrMagnitude > 0.01f)
            transform.forward = _velocity.normalized;
    }

    private void ResolverSuperposicion()
    {
        foreach (Boid other in GetAllNearby(separationRadius))
        {
            Vector3 diff = transform.position - other.transform.position;
            diff.y = 0f;
            float dist = diff.magnitude;

            if (dist < separationRadius && dist > 0.0001f)
                transform.position += diff.normalized * (separationRadius - dist);
        }
    }

    public void Engancharse()
    {
        enganchado = true;
        _velocity = Vector3.zero;
        enganchados.Add(this);
    }

    private List<Boid> GetNeighbors(float radius)
    {
        List<Boid> result = new List<Boid>();
        float sqrRadius = radius * radius;
        foreach (Boid other in allBoids)
        {
            if (other == this) continue;
            if (other.enganchado || other.estaInactivo) continue;
            float sqrDist = (other.transform.position - transform.position).sqrMagnitude;
            if (sqrDist <= sqrRadius)
                result.Add(other);
        }

        return result;
    }

    private List<Boid> GetAllNearby(float radius)
    {
        List<Boid> result = new List<Boid>();
        float sqrRadius = radius * radius;
        foreach (Boid other in allBoids)
        {
            if (other == this) continue;
            float sqrDist = (other.transform.position - transform.position).sqrMagnitude;
            if (sqrDist <= sqrRadius)
                result.Add(other);
        }

        return result;
    }

    private Vector3 Separation(List<Boid> closeNeighbors)
    {
        Vector3 steer = Vector3.zero;

        foreach (Boid other in closeNeighbors)
        {
            Vector3 diff = transform.position - other.transform.position;
            float dist = diff.magnitude;
            if (dist > 0.0001f)
                steer += diff.normalized / dist;
        }

        return steer;
    }

    private Vector3 Alignment(List<Boid> neighbors)
    {
        if (neighbors.Count == 0)
            return Vector3.zero;

        Vector3 avgVelocity = Vector3.zero;
        foreach (Boid other in neighbors)
            avgVelocity += other.Velocity;

        avgVelocity /= neighbors.Count;

        return avgVelocity;
    }

    private Vector3 Cohesion(List<Boid> neighbors)
    {
        if (neighbors.Count == 0)
            return Vector3.zero;

        Vector3 center = Vector3.zero;
        foreach (Boid other in neighbors)
            center += other.transform.position;
        center /= neighbors.Count;

        Vector3 desired = center - transform.position;
        if (desired.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        desired = desired.normalized * maxSpeed;
        Vector3 steer = desired - _velocity;

        return steer;
    }

    private Vector3 Wander()
    {
        wanderDireccion += new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * wanderJitter * Time.deltaTime;
        wanderDireccion = wanderDireccion.normalized;

        Vector3 desired = wanderDireccion * maxSpeed;
        return desired - _velocity;
    }

    private Vector3 Evade()
    {
        Vector3 desired = transform.position - cazador.position;
        if (desired.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        desired = desired.normalized * maxSpeed;
        Vector3 steer = desired - _velocity;

        return steer;
    }

    private ObjetoInteres BuscarObjetoMasCercano()
    {
        ObjetoInteres[] objetos = FindObjectsByType<ObjetoInteres>(FindObjectsSortMode.None);
        if (objetos.Length == 0)
            return null;

        ObjetoInteres masCercano = null;
        float mejorSqrDist = float.MaxValue;

        foreach (ObjetoInteres obj in objetos)
        {
            float sqrDist = (obj.transform.position - transform.position).sqrMagnitude;
            if (sqrDist < mejorSqrDist)
            {
                mejorSqrDist = sqrDist;
                masCercano = obj;
            }
        }

        return masCercano;
    }

    private Vector3 Arrive(Vector3 target)
    {
        Vector3 toTarget = target - transform.position;
        float dist = toTarget.magnitude;
        if (dist < 0.0001f)
            return Vector3.zero;

        float speed = dist < arriveSlowRadius ? maxSpeed * (dist / arriveSlowRadius) : maxSpeed;
        Vector3 desired = toTarget.normalized * speed;

        return desired - _velocity;
    }

    public void RecibirAtaque()
    {
        Debug.Log($"{name}: entró a RecibirAtaque()");
        if (estaInactivo) return;

        enganchado = false;
        enganchados.Remove(this);

        estaInactivo = true;
        inactivos.Add(this);

        if (sonidoDolor != null && audioSource != null)
            audioSource.PlayOneShot(sonidoDolor);

        transform.Rotate(0f, 90f, 90f, Space.Self);
    }

    public void Recolectar()
    {
        inactivos.Remove(this);
        estaInactivo = false;

        transform.rotation = Quaternion.identity;

        Vector2 dirAleatoria = Random.insideUnitCircle.normalized;
        _velocity = new Vector3(dirAleatoria.x, 0f, dirAleatoria.y) * maxSpeed * 0.5f;

        transform.position = FlockArea.Instance.PuntoAleatorio();
    }
    public static Boid BuscarObjetivoParaCazador(Vector3 origen, float radio)
    {
        Boid masCercano = null;
        float mejorSqrDist = float.MaxValue;

        foreach (Boid b in allBoids)
        {
            if (b.estaInactivo) continue;
            float sqrDist = (b.transform.position - origen).sqrMagnitude;
            if (sqrDist <= radio * radio && sqrDist < mejorSqrDist)
            {
                mejorSqrDist = sqrDist;
                masCercano = b;
            }
        }

        return masCercano;
    }

    public static Boid BuscarInactivoParaCazador(Vector3 origen, float radio)
    {
        Boid masCercano = null;
        float mejorSqrDist = float.MaxValue;

        foreach (Boid b in inactivos)
        {
            float sqrDist = (b.transform.position - origen).sqrMagnitude;
            if (sqrDist <= radio * radio && sqrDist < mejorSqrDist)
            {
                mejorSqrDist = sqrDist;
                masCercano = b;
            }
        }

        return masCercano;
    }

}
