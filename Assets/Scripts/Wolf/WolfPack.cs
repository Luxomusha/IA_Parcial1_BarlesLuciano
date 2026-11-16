using System;
using UnityEngine;

[RequireComponent(typeof(Wolf))]
public class WolfPack : MonoBehaviour
{
    private const float FeedbackDuration = 1f;

    public static event Action<Wolf, Hunter, Vector3> Howled;

    [Tooltip("Minimum time between two howls of the same wolf.")]
    [SerializeField, Min(0f)] private float howlCooldown = 2f;

    [Header("Sound")]
    [Tooltip("One of these is played at random when this wolf howls.")]
    [SerializeField] private AudioClip[] howlClips;
    [SerializeField, Range(0f, 1f)] private float howlVolume = 1f;
    [Tooltip("Optional: played when this wolf receives the howl of another wolf.")]
    [SerializeField] private AudioClip alertClip;
    [SerializeField, Range(0f, 1f)] private float alertVolume = 0.5f;

    private Wolf owner;
    private AgentAudio agentAudio;

    private float lastHowlTime = float.NegativeInfinity;
    private float howlFeedbackTimer;
    private float alertFeedbackTimer;

    private bool hasPendingAlert;
    private Vector3 pendingAlertPosition;
    private Hunter pendingAlertHunter;

    public bool IsHowling => howlFeedbackTimer > 0f;
    public bool JustReceivedAlert => alertFeedbackTimer > 0f;
    public Wolf LastAlertSender { get; private set; }

    private void Awake()
    {
        owner = GetComponent<Wolf>();
        agentAudio = GetComponent<AgentAudio>();
    }

    private void OnEnable() => Howled += OnPackHowl;
    private void OnDisable() => Howled -= OnPackHowl;

    private void Update()
    {
        howlFeedbackTimer -= Time.deltaTime;
        alertFeedbackTimer -= Time.deltaTime;
    }

    public bool Howl(Hunter seenHunter, Vector3 position)
    {
        if (Time.time - lastHowlTime < howlCooldown) return false;

        lastHowlTime = Time.time;
        howlFeedbackTimer = FeedbackDuration;
        if (agentAudio != null) agentAudio.PlayRandom(howlClips, howlVolume);

        Howled?.Invoke(owner, seenHunter, position);
        return true;
    }

    public void ReceiveAlert(Vector3 position, Hunter seenHunter, Wolf sender)
    {
        hasPendingAlert = true;
        pendingAlertPosition = position;
        pendingAlertHunter = seenHunter;
        LastAlertSender = sender;
        alertFeedbackTimer = FeedbackDuration;

        if (agentAudio != null && !agentAudio.IsPlaying)
            agentAudio.Play(alertClip, alertVolume);
    }

    public bool TryConsumeAlert(out Vector3 position, out Hunter seenHunter)
    {
        position = pendingAlertPosition;
        seenHunter = pendingAlertHunter;
        if (!hasPendingAlert) return false;

        hasPendingAlert = false;
        return true;
    }

    public void ClearAlert()
    {
        hasPendingAlert = false;
        pendingAlertHunter = null;
    }

    private void OnPackHowl(Wolf sender, Hunter seenHunter, Vector3 position)
    {
        if (sender == owner) return;
        ReceiveAlert(position, seenHunter, sender);
    }
}
