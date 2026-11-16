using UnityEngine;

public class HunterGathering : MonoBehaviour
{
    [SerializeField, Min(1)] private int requiredGathers = 3;
    [SerializeField] private float gatherDuration = 1.5f;
    [Tooltip("Optional beam drawn from the Hunter to the boid it is gathering.")]
    [SerializeField] private LineRenderer gatherBeam;

    public int GatherCount { get; private set; }
    public int RequiredGathers => requiredGathers;
    public float GatherDuration => gatherDuration;
    public bool HasCompleted => GatherCount >= requiredGathers;

    private void Awake()
    {
        HideBeam();
    }

    public void AddGather() => GatherCount++;

    public void ResetCount() => GatherCount = 0;

    public void ShowBeam(Vector3 target)
    {
        if (gatherBeam == null) return;

        gatherBeam.enabled = true;
        gatherBeam.SetPosition(0, transform.position);
        gatherBeam.SetPosition(1, target);
    }

    public void HideBeam()
    {
        if (gatherBeam != null) gatherBeam.enabled = false;
    }
}
