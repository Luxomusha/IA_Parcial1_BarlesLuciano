using UnityEngine;

public class HunterCombat : MonoBehaviour
{
    private static readonly Color MeleeBeamColor = Color.red;
    private static readonly Color RangedBeamColor = new Color(1f, 0.5f, 0f);

    [SerializeField] private float meleeAttackRadius = 1f;
    [SerializeField] private float rangeAttackRadius = 5f;
    [Tooltip("TBA: time between attacks.")]
    [SerializeField] private float timeBetweenAttacks = 3f;

    [Header("Feedback")]
    [Tooltip("Optional line flashed from the Hunter to the boid it hits.")]
    [SerializeField] private LineRenderer attackBeam;
    [SerializeField] private float attackBeamDuration = 0.2f;
    [SerializeField] private bool drawGizmos = true;

    private float lastAttackTime = float.NegativeInfinity;
    private float attackBeamTimer;

    public float MeleeAttackRadius => meleeAttackRadius;
    public float RangeAttackRadius => rangeAttackRadius;
    public bool IsAttackReady => Time.time - lastAttackTime >= timeBetweenAttacks;

    private void Awake()
    {
        HideBeam();
    }

    private void Update()
    {
        UpdateBeam();
    }

    public void PerformAttack(Boid target, Vector3 origin, bool ranged)
    {
        target.Gatherable.ReceiveAttack();
        lastAttackTime = Time.time;
        ShowBeam(origin, target.transform.position, ranged);
    }

    public void ResetCooldown()
    {
        lastAttackTime = float.NegativeInfinity;
    }

    public void HideBeam()
    {
        if (attackBeam != null) attackBeam.enabled = false;
    }

    private void ShowBeam(Vector3 from, Vector3 to, bool ranged)
    {
        if (attackBeam == null) return;

        attackBeam.enabled = true;
        attackBeam.startColor = attackBeam.endColor = ranged ? RangedBeamColor : MeleeBeamColor;
        attackBeam.SetPosition(0, from);
        attackBeam.SetPosition(1, to);
        attackBeamTimer = attackBeamDuration;
    }

    private void UpdateBeam()
    {
        if (attackBeam == null || !attackBeam.enabled) return;

        attackBeamTimer -= Time.deltaTime;
        if (attackBeamTimer <= 0f)
            HideBeam();
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = RangedBeamColor;
        Gizmos.DrawWireSphere(transform.position, rangeAttackRadius);
        Gizmos.color = MeleeBeamColor;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRadius);
    }
}
