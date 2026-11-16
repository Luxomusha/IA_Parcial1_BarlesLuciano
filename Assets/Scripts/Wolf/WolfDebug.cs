using UnityEngine;

[RequireComponent(typeof(Wolf))]
public class WolfDebug : MonoBehaviour
{
    [Tooltip("Logs every time this wolf detects the Hunter.")]
    [SerializeField] private bool logDetections = true;
    [Tooltip("Pauses the Editor (Debug.Break) the moment this wolf detects the Hunter, to inspect the frame.")]
    [SerializeField] private bool pauseOnDetection;
    [Tooltip("Transform used by the context menu \"Debug/Go to debug target\".")]
    [SerializeField] private Transform debugTarget;
    [Tooltip("Play mode: tick it to send this wolf to Debug Target. It unticks itself.")]
    [SerializeField] private bool goToDebugTarget;

    private Wolf wolf;

    private void Awake() => wolf = GetComponent<Wolf>();

    private void OnEnable() => wolf.HunterDetected += OnHunterDetected;

    private void OnDisable() => wolf.HunterDetected -= OnHunterDetected;

    private void Update()
    {
        if (!goToDebugTarget) return;

        goToDebugTarget = false;
        GoToDebugTarget();
    }

    [ContextMenu("Debug/Go to debug target")]
    private void GoToDebugTarget()
    {
        if (!Application.isPlaying || debugTarget == null)
        {
            Debug.LogWarning($"{name}: enter Play mode and assign Debug Target first.", this);
            return;
        }

        wolf.Pack.ReceiveAlert(debugTarget.position, null, null);
        Debug.Log($"{name}: debug alert to {debugTarget.position}.", this);
    }

    private void OnHunterDetected(Hunter hunter)
    {
        if (logDetections)
            Debug.Log($"{name} SEES the Hunter: distance {Vector3.Distance(transform.position, hunter.transform.position):0.00}, " +
                      $"wolf {transform.position}, hunter {hunter.transform.position}", this);

        if (pauseOnDetection) Debug.Break();
    }
}
