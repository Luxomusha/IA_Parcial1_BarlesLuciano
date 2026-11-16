using UnityEngine;

public class WalkAnimator : MonoBehaviour
{
    private const float MovingThreshold = 0.01f;

    [Tooltip("If empty, the first Animator in the children is used.")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkClip = "root_Walk";
    [SerializeField] private string idleClip = "root_Idle";
    [Tooltip("Speed at which the walk animation plays at normal rate. Faster movement speeds it up.")]
    [SerializeField, Min(0.01f)] private float referenceSpeed = 2.5f;
    [SerializeField, Min(0f)] private float crossFade = 0.1f;

    private IVelocityProvider velocityProvider;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        velocityProvider = GetComponent<IVelocityProvider>();
    }

    private void LateUpdate()
    {
        if (animator == null || velocityProvider == null) return;

        Vector3 velocity = velocityProvider.Velocity;
        bool moving = velocity.sqrMagnitude > MovingThreshold;

        PlayClip(moving ? walkClip : idleClip);
        animator.speed = moving ? Mathf.Max(1f, velocity.magnitude / referenceSpeed) : 1f;
    }

    private void PlayClip(string clip)
    {
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(clip))
            animator.CrossFade(clip, crossFade);
    }
}
