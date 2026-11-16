using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class HUDWolfView
{
    public Wolf Agent;
    public string Label;
    public HUDAgentItem Row;
    public HUDAgentItem Badge;
    public RectTransform Marker;
    public Text MarkerId;
    public bool Seen;
}

public sealed class HUDPackList
{
    private readonly RectTransform rowTemplate;
    private readonly RectTransform badgeTemplate;
    private readonly RectTransform markerTemplate;
    private readonly RectTransform packContent;
    private readonly RectTransform worldBadges;
    private readonly Text packCount;

    private readonly Dictionary<int, HUDWolfView> active = new Dictionary<int, HUDWolfView>();
    private readonly Dictionary<int, string> labels = new Dictionary<int, string>();
    private readonly List<HUDWolfView> pool = new List<HUDWolfView>();
    private readonly List<int> removed = new List<int>();
    private int nextId = 1;
    private int lastCount = -1;

    public int Count => active.Count;
    public Dictionary<int, HUDWolfView>.ValueCollection Views => active.Values;

    public HUDPackList(RectTransform rowTemplate, RectTransform badgeTemplate, RectTransform markerTemplate,
        RectTransform packContent, RectTransform worldBadges, Text packCount)
    {
        this.rowTemplate = rowTemplate;
        this.badgeTemplate = badgeTemplate;
        this.markerTemplate = markerTemplate;
        this.packContent = packContent;
        this.worldBadges = worldBadges;
        this.packCount = packCount;

        PoolExistingRows();
    }

    public bool TryGetLabel(Wolf wolf, out string label)
    {
        label = null;
        return wolf != null && labels.TryGetValue(wolf.GetInstanceID(), out label);
    }

    public void Synchronize(Wolf[] wolves)
    {
        foreach (HUDWolfView view in active.Values) view.Seen = false;

        foreach (Wolf wolf in wolves)
        {
            if (wolf == null || !wolf.isActiveAndEnabled) continue;

            int id = wolf.GetInstanceID();
            if (!active.TryGetValue(id, out HUDWolfView view))
                view = Show(wolf, id);
            view.Seen = true;
        }

        ReleaseUnseen();
        RefreshCount();
    }

    private void PoolExistingRows()
    {
        foreach (Transform child in packContent)
        {
            if (child == rowTemplate) continue;

            HUDWolfView view = CreateView((RectTransform)child);
            view.Row.Root.gameObject.SetActive(false);
            pool.Add(view);
        }
    }

    private HUDWolfView CreateView(RectTransform row)
    {
        RectTransform badge = Object.Instantiate(badgeTemplate, worldBadges);
        RectTransform marker = Object.Instantiate(markerTemplate, worldBadges);
        var view = new HUDWolfView
        {
            Row = new HUDAgentItem(row, false),
            Badge = new HUDAgentItem(badge, true),
            Marker = marker
        };

        foreach (Text text in marker.GetComponentsInChildren<Text>(true)) view.MarkerId = text;
        badge.gameObject.SetActive(false);
        marker.gameObject.SetActive(false);
        return view;
    }

    private HUDWolfView Show(Wolf wolf, int id)
    {
        string label = GetOrCreateLabel(id);
        HUDWolfView view = TakeFromPool();

        view.Agent = wolf;
        view.Label = label;
        view.Row.SetIdentifier(label);
        view.Badge.SetIdentifier(label);
        view.MarkerId.text = label;
        view.Row.Root.name = label + "Row";
        view.Badge.Root.name = label + "Badge";
        view.Marker.name = label + "LastPosition";
        view.Row.Root.SetAsLastSibling();
        view.Row.Root.gameObject.SetActive(true);

        active.Add(id, view);
        return view;
    }

    private string GetOrCreateLabel(int id)
    {
        if (labels.TryGetValue(id, out string label)) return label;

        label = "L" + nextId.ToString("00");
        nextId++;
        labels.Add(id, label);
        return label;
    }

    private HUDWolfView TakeFromPool()
    {
        if (pool.Count == 0) return CreateView(Object.Instantiate(rowTemplate, packContent));

        HUDWolfView view = pool[pool.Count - 1];
        pool.RemoveAt(pool.Count - 1);
        return view;
    }

    private void ReleaseUnseen()
    {
        removed.Clear();
        foreach (KeyValuePair<int, HUDWolfView> pair in active)
            if (!pair.Value.Seen) removed.Add(pair.Key);

        foreach (int id in removed)
        {
            HUDWolfView view = active[id];
            view.Row.Root.gameObject.SetActive(false);
            view.Badge.Root.gameObject.SetActive(false);
            view.Marker.gameObject.SetActive(false);
            view.Agent = null;
            pool.Add(view);
            active.Remove(id);
        }
    }

    private void RefreshCount()
    {
        if (lastCount == active.Count) return;

        lastCount = active.Count;
        packCount.text = lastCount.ToString("00");
    }
}
