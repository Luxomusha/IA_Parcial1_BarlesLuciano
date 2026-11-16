using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class HUDGatherProgress
{
    private static readonly Color EmptySegment = new Color(0.2f, 0.25f, 0.3f);

    private readonly RectTransform segmentsRoot;
    private readonly Text counter;
    private readonly int maximumSegments;
    private readonly Color filledSegment;
    private readonly List<Image> segments = new List<Image>();

    private int lastCount = -1;
    private int lastRequired = -1;

    public HUDGatherProgress(RectTransform segmentsRoot, Text counter, int maximumSegments, Color filledSegment)
    {
        this.segmentsRoot = segmentsRoot;
        this.counter = counter;
        this.maximumSegments = maximumSegments;
        this.filledSegment = filledSegment;

        foreach (Image image in segmentsRoot.GetComponentsInChildren<Image>(true)) segments.Add(image);
    }

    public void Refresh(int count, int required)
    {
        if (count == lastCount && required == lastRequired) return;

        lastCount = count;
        lastRequired = required;
        counter.text = required > 0 ? count + " / " + required : "—";

        int visible = Mathf.Min(required, maximumSegments);
        EnsureSegments(visible);
        PaintSegments(count, required, visible);
    }

    private void EnsureSegments(int visible)
    {
        while (segments.Count < visible)
            segments.Add(Object.Instantiate(segments[0], segmentsRoot));
    }

    private void PaintSegments(int count, int required, int visible)
    {
        float completion = required > 0 ? (float)count / required * visible : 0f;

        for (int i = 0; i < segments.Count; i++)
        {
            segments[i].gameObject.SetActive(i < visible);
            segments[i].color = completion >= i + 1 ? filledSegment : EmptySegment;
        }
    }
}
