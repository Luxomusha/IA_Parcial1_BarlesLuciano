using UnityEngine;
using UnityEngine.UI;

public class SimulationHUDView : MonoBehaviour
{
    private const string HunterLabel = "C01";

    private static readonly Color SelectedButton = new Color(0.18f, 0.38f, 0.49f);
    private static readonly Color IdleButton = new Color(0.16f, 0.21f, 0.28f);

    [Header("Simulation")]
    [SerializeField] private Hunter hunter;
    [SerializeField] private Wolf[] wolves;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private HUDStateVisuals visuals;
    [Header("Canvas references")]
    [SerializeField] private Canvas hudCanvas;
    [SerializeField] private RectTransform panel;
    [SerializeField] private RectTransform worldBadges;
    [SerializeField] private RectTransform hunterRow;
    [SerializeField] private RectTransform rowTemplate;
    [SerializeField] private RectTransform badgeTemplate;
    [SerializeField] private RectTransform markerTemplate;
    [SerializeField] private RectTransform packContent;
    [SerializeField] private RectTransform progressSegments;
    [SerializeField] private Text progressCounter;
    [SerializeField] private Text packCount;
    [SerializeField] private RectTransform legend;
    [SerializeField] private RectTransform debugPanel;
    [SerializeField] private Text debugText;
    [SerializeField] private Button legendButton;
    [SerializeField] private Button debugButton;
    [Header("Readability")]
    [SerializeField, Min(0.1f)] private float minimumPixelScale = 0.9f;
    [Tooltip("Extra scale for the main panel only (badges, legend and debug panel keep their size).")]
    [SerializeField, Range(0.4f, 1f)] private float panelScale = 0.75f;
    [Tooltip("Show the MANADA list in the normal view. Off: it only appears with Debug (the badges already show each wolf).")]
    [SerializeField] private bool showPackInNormalView = false;
    [SerializeField, Min(0.1f)] private float transitionDuration = 0.3f;
    [SerializeField] private float hunterBadgeHeight = 1.5f;
    [SerializeField] private float wolfBadgeHeight = 1.2f;
    [SerializeField, Min(1)] private int maximumSegments = 12;
    [SerializeField, Min(0.05f)] private float telemetryInterval = 0.1f;

    private HUDAgentItem hunterRowItem;
    private HUDAgentItem hunterBadge;
    private HUDPackList pack;
    private HUDGatherProgress progress;
    private HUDLayout layout;
    private HUDWorldProjector projector;
    private HUDTelemetry telemetry;
    private bool debugVisible;
    private bool legendVisible;

    public bool DebugVisible => debugVisible;
    public bool LegendVisible => legendVisible;
    public int DisplayedWolfCount => pack != null ? pack.Count : 0;

    private void Awake()
    {
        if (!HasRequiredReferences())
        {
            Debug.LogError("SimulationHUDView: missing UI references.", this);
            enabled = false;
            return;
        }

        CreateHunterItems();
        CreateParts();
        legend.gameObject.SetActive(false);
        debugPanel.gameObject.SetActive(false);
        ApplyPackVisibility();
    }

    private void OnEnable()
    {
        if (legendButton != null) legendButton.onClick.AddListener(ToggleLegend);
        if (debugButton != null) debugButton.onClick.AddListener(ToggleDebug);
        if (worldBadges != null) worldBadges.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        if (legendButton != null) legendButton.onClick.RemoveListener(ToggleLegend);
        if (debugButton != null) debugButton.onClick.RemoveListener(ToggleDebug);
        if (worldBadges != null) worldBadges.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        layout.Adapt();
        pack.Synchronize(wolves);
        UpdateHunter();
        UpdateProgress();
        UpdateWolves();
        if (debugVisible) telemetry.Tick(hunter, pack);
    }

    public void ToggleLegend()
    {
        legendVisible = !legendVisible;
        legend.gameObject.SetActive(legendVisible);
        TintButton(legendButton, legendVisible);
    }

    public void ToggleDebug()
    {
        debugVisible = !debugVisible;
        debugPanel.gameObject.SetActive(debugVisible);
        TintButton(debugButton, debugVisible);
        ApplyPackVisibility();
        telemetry.RefreshNow();
    }

    private bool HasRequiredReferences() =>
        visuals != null && hudCanvas != null && hunterRow != null && badgeTemplate != null &&
        rowTemplate != null && markerTemplate != null && packContent != null && worldBadges != null;

    private void CreateHunterItems()
    {
        hunterRowItem = new HUDAgentItem(hunterRow, false);
        hunterRowItem.SetIdentifier(HunterLabel);

        hunterBadge = new HUDAgentItem(Instantiate(badgeTemplate, worldBadges), true);
        hunterBadge.Root.name = "HunterBadge";
        hunterBadge.SetIdentifier(HunterLabel);
    }

    private void CreateParts()
    {
        pack = new HUDPackList(rowTemplate, badgeTemplate, markerTemplate, packContent, worldBadges, packCount);
        progress = new HUDGatherProgress(progressSegments, progressCounter, maximumSegments,
            visuals.GetColor(HUDStateVisuals.Symbol.Gather));
        layout = new HUDLayout(hudCanvas, panel, legend, debugPanel, legendButton, debugButton, minimumPixelScale, panelScale);
        projector = new HUDWorldProjector(targetCamera, worldBadges);
        telemetry = new HUDTelemetry(debugText, telemetryInterval);
    }

    private void ApplyPackVisibility() => layout.SetPackVisible(showPackInNormalView || debugVisible);

    private void UpdateHunter()
    {
        if (hunter == null)
        {
            hunterRowItem.Missing();
            hunterBadge.Root.gameObject.SetActive(false);
            return;
        }

        hunterRowItem.Restore();
        HUDStateVisuals.Symbol symbol = visuals.GetSymbol(hunter.CurrentState);
        string state = HUDStateNames.Of(hunter.CurrentState);
        bool alert = hunter.DetectedWolf != null;

        hunterRowItem.Update(visuals, symbol, state, false, alert, false, debugVisible, transitionDuration);
        hunterBadge.Update(visuals, symbol, state, false, alert, false, debugVisible, transitionDuration);
        projector.Project(hunterBadge.Root, hunter.transform.position + Vector3.up * hunterBadgeHeight,
            hunter.isActiveAndEnabled && !hunter.IsDead);
    }

    private void UpdateProgress()
    {
        int count = hunter != null ? hunter.GatherCount : 0;
        int required = hunter != null ? Mathf.Max(1, hunter.RequiredGathers) : 0;
        progress.Refresh(count, required);
    }

    private void UpdateWolves()
    {
        foreach (HUDWolfView view in pack.Views)
        {
            if (view.Agent != null) UpdateWolf(view);
        }
    }

    private void UpdateWolf(HUDWolfView view)
    {
        Wolf wolf = view.Agent;
        HUDStateVisuals.Symbol symbol = visuals.GetSymbol(wolf.CurrentState);
        string state = HUDStateNames.Of(wolf.CurrentState);

        view.Row.Update(visuals, symbol, state, wolf.IsHowling, wolf.JustReceivedAlert, wolf.IsUsingPathfinding, debugVisible, transitionDuration);
        view.Badge.Update(visuals, symbol, state, wolf.IsHowling, wolf.JustReceivedAlert, wolf.IsUsingPathfinding, debugVisible, transitionDuration);
        projector.Project(view.Badge.Root, wolf.transform.position + Vector3.up * wolfBadgeHeight, wolf.isActiveAndEnabled);
        projector.Project(view.Marker, wolf.LastKnownPosition, debugVisible && wolf.HasLastKnownPosition);
    }

    private static void TintButton(Button button, bool selected)
    {
        if (button != null && button.targetGraphic != null)
            button.targetGraphic.color = selected ? SelectedButton : IdleButton;
    }
}
