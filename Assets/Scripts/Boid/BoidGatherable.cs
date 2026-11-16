using UnityEngine;

[RequireComponent(typeof(Boid))]
public class BoidGatherable : MonoBehaviour
{
    [Tooltip("Scale the boid shrinks to while the Hunter is gathering it.")]
    [SerializeField] private float gatheredMinScale = 0.3f;
    [SerializeField] private AudioClip painSound;

    private Boid owner;
    private AgentAudio agentAudio;
    private Vector3 originalScale;

    public bool IsHooked { get; private set; }
    public bool IsDown { get; private set; }

    private void Awake()
    {
        owner = GetComponent<Boid>();
        agentAudio = GetComponent<AgentAudio>();
        originalScale = transform.localScale;
    }

    public void Hook()
    {
        IsHooked = true;
        owner.Stop();
    }

    public void ReceiveAttack()
    {
        if (IsDown) return;

        IsHooked = false;
        IsDown = true;
        owner.Stop();
        if (agentAudio != null) agentAudio.PlayOneShot(painSound);

        transform.Rotate(0f, 90f, 90f, Space.Self);
    }

    public void SetGatherProgress(float progress)
    {
        transform.localScale = originalScale * Mathf.Lerp(1f, gatheredMinScale, progress);
    }

    public void Respawn()
    {
        IsDown = false;
        IsHooked = false;
        transform.localScale = originalScale;
        transform.rotation = Quaternion.identity;

        owner.Relaunch();
    }
}
