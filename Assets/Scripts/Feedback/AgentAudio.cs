using UnityEngine;

public class AgentAudio : MonoBehaviour
{
    [Tooltip("If empty, the AudioSource on this GameObject is used.")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Long clips are cut after this many seconds (0 = play the whole clip).")]
    [SerializeField, Min(0f)] private float maxSoundDuration = 3f;
    [Tooltip("Random pitch variation so several agents don't sound the same.")]
    [SerializeField, Range(0f, 0.5f)] private float pitchVariation = 0.1f;

    private float soundTimer;

    public bool IsPlaying => audioSource != null && audioSource.isPlaying;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        CutLongSound();
    }

    public void PlayRandom(AudioClip[] clips, float volume)
    {
        if (clips == null || clips.Length == 0) return;
        Play(clips[Random.Range(0, clips.Length)], volume);
    }

    public void Play(AudioClip clip, float volume)
    {
        if (audioSource == null || clip == null) return;

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        audioSource.Play();
        soundTimer = maxSoundDuration;
    }

    private void CutLongSound()
    {
        if (!IsPlaying || maxSoundDuration <= 0f) return;

        soundTimer -= Time.deltaTime;
        if (soundTimer <= 0f)
            audioSource.Stop();
    }
}
