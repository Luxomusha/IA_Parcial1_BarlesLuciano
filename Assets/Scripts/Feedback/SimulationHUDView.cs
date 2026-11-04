using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class SimulationHUDView : MonoBehaviour
{
    [Header("Simulation")]
    [SerializeField] private Hunter hunter;
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
    [SerializeField] private UnityEngine.UI.Text progressCounter;
    [SerializeField] private UnityEngine.UI.Text packCount;
    [SerializeField] private RectTransform legend;
    [SerializeField] private RectTransform debugPanel;
    [SerializeField] private UnityEngine.UI.Text debugText;
    [SerializeField] private UnityEngine.UI.Button legendButton;
    [SerializeField] private UnityEngine.UI.Button debugButton;
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

    private Item _hunterRow;
    private Item _hunterBadge;
    private readonly Dictionary<int, WolfView> _active = new Dictionary<int, WolfView>();
    private readonly Dictionary<int, string> _identifiers = new Dictionary<int, string>();
    private readonly List<WolfView> _pool = new List<WolfView>();
    private readonly List<int> _removed = new List<int>();
    private readonly List<UnityEngine.UI.Image> _segments = new List<UnityEngine.UI.Image>();
    private readonly StringBuilder _telemetry = new StringBuilder(512);
    private bool _debug;
    private bool _legend;
    private int _nextId = 1;
    private int _lastGather = -1;
    private int _lastRequired = -1;
    private int _lastPackCount = -1;
    private float _nextTelemetry;
    private float _scale = 1f;
    private Color _emptySegment = new Color(0.2f, 0.25f, 0.3f);

    private readonly List<GameObject> _packSection = new List<GameObject>();
    private float _expandedPanelHeight;
    private float _expandedButtonsY;
    private float _compactButtonsY;
    private bool _hasCompactLayout;

    private UnityEngine.UI.CanvasScaler _scaler;
    private Vector2 _referenceResolution = new Vector2(1920f, 1080f);
    private float _matchWidthOrHeight = 0.5f;

    public bool DebugVisible => _debug;
    public bool LegendVisible => _legend;
    public int DisplayedWolfCount => _active.Count;

    private sealed class Item
    {
        public readonly RectTransform Root;
        public readonly UnityEngine.UI.Image StateIcon;
        private readonly UnityEngine.UI.Image _halo;
        private readonly UnityEngine.UI.Image _howl;
        private readonly UnityEngine.UI.Image _alert;
        private readonly UnityEngine.UI.Image _path;
        private readonly UnityEngine.UI.Text _id;
        private readonly UnityEngine.UI.Text _name;
        private readonly UnityEngine.UI.Text _detail;
        private readonly bool _floating;
        private HUDStateVisuals.Symbol _lastSymbol;
        private bool _hasSymbol;
        private bool _howling;
        private bool _alerted;
        private bool _debug;
        private float _pulse;
        private Color _accent;

        public Item(RectTransform root, bool floating)
        {
            Root = root;
            _floating = floating;
            foreach (var image in root.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                switch (image.name)
                {
                    case "StateIcon": StateIcon = image; break;
                    case "Halo": _halo = image; break;
                    case "HowlIcon": _howl = image; break;
                    case "AlertIcon": _alert = image; break;
                    case "PathIcon": _path = image; break;
                }
            }
            foreach (var text in root.GetComponentsInChildren<UnityEngine.UI.Text>(true))
            {
                switch (text.name)
                {
                    case "Identifier": _id = text; break;
                    case "StateName": _name = text; break;
                    case "Detail": _detail = text; break;
                }
            }
        }

        public void SetIdentifier(string value)
        {
            if (_id != null && _id.text != value) _id.text = value;
        }

        public void Update(HUDStateVisuals data, HUDStateVisuals.Symbol symbol, string state,
            bool howling, bool alerted, bool path, bool debug, float duration)
        {
            if (!_hasSymbol || _lastSymbol != symbol)
            {
                _hasSymbol = true;
                _lastSymbol = symbol;
                _accent = data.GetColor(symbol);
                if (StateIcon != null) StateIcon.sprite = data.GetSprite(symbol);
                if (_name != null) _name.text = state;
                _pulse = duration;
            }
            if ((howling && !_howling) || (alerted && !_alerted)) _pulse = duration;
            _howling = howling;
            _alerted = alerted;
            SetActive(_howl, howling);
            SetActive(_alert, alerted);
            SetActive(_path, debug && path);
            if (_name != null) SetActive(_name, debug);
            if (_id != null && _floating) SetActive(_id, debug);
            if (_detail != null)
            {
                SetActive(_detail, debug && !_floating);
                string detail = path ? "A* activo" : "";
                if (_detail.text != detail) _detail.text = detail;
            }
            if (!_floating && _debug != debug)
            {
                PositionEvent(_howl, debug ? 181 : 156, debug ? -31 : -17, debug ? 16 : 20);
                PositionEvent(_alert, debug ? 205 : 185, debug ? -31 : -17, debug ? 16 : 20);
                PositionEvent(_path, debug ? 229 : 214, debug ? -31 : -17, debug ? 16 : 20);
            }
            _debug = debug;
            if (_pulse > 0f)
            {
                _pulse = Mathf.Max(0f, _pulse - Time.unscaledDeltaTime);
                float amount = _pulse / duration;
                if (_halo != null)
                {
                    _halo.rectTransform.localScale = Vector3.one * (1f + amount * 0.18f);
                    _halo.color = new Color(_accent.r, _accent.g, _accent.b, _floating ? 0.92f : 0.18f + amount * 0.13f);
                }
                if (StateIcon != null) StateIcon.color = Color.Lerp(Color.white, _accent, amount * 0.4f);
            }
        }

        public void Missing()
        {
            if (StateIcon != null) StateIcon.enabled = false;
            if (_name != null) _name.text = "Sin cazador";
        }

        public void Restore() { if (StateIcon != null) StateIcon.enabled = true; }
        private static void SetActive(Component component, bool value)
        {
            if (component != null && component.gameObject.activeSelf != value) component.gameObject.SetActive(value);
        }
        private static void PositionEvent(UnityEngine.UI.Image image, float x, float y, float size)
        {
            if (image == null) return;
            image.rectTransform.anchoredPosition = new Vector2(x, y);
            image.rectTransform.sizeDelta = new Vector2(size, size);
        }
    }

    private sealed class WolfView
    {
        public Wolf Agent;
        public Item Row;
        public Item Badge;
        public RectTransform Marker;
        public UnityEngine.UI.Text MarkerId;
        public bool Seen;
    }

    private void Awake()
    {
        if (visuals == null || hudCanvas == null || hunterRow == null || badgeTemplate == null ||
            rowTemplate == null || markerTemplate == null || packContent == null || worldBadges == null)
        {
            Debug.LogError("SimulationHUDView: missing UI references.", this);
            enabled = false;
            return;
        }
        _hunterRow = new Item(hunterRow, false);
        _hunterRow.SetIdentifier("C01");
        _hunterBadge = new Item(Instantiate(badgeTemplate, worldBadges), true);
        _hunterBadge.Root.name = "HunterBadge";
        _hunterBadge.SetIdentifier("C01");
        foreach (Transform child in packContent)
        {
            if (child == rowTemplate) continue;
            var view = CreateWolfView((RectTransform)child);
            view.Row.Root.gameObject.SetActive(false);
            _pool.Add(view);
        }
        foreach (var image in progressSegments.GetComponentsInChildren<UnityEngine.UI.Image>(true)) _segments.Add(image);
        legend.gameObject.SetActive(false);
        debugPanel.gameObject.SetActive(false);
        SetupCompactLayout();
        ApplyPackVisibility();
        SetupScaler();
    }

    private void SetupScaler()
    {
        _scaler = hudCanvas.GetComponentInParent<UnityEngine.UI.CanvasScaler>();
        if (_scaler == null) return;

        if (_scaler.referenceResolution.x > 0f && _scaler.referenceResolution.y > 0f)
            _referenceResolution = _scaler.referenceResolution;
        _matchWidthOrHeight = _scaler.matchWidthOrHeight;
        _scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize;
    }

    private void SetupCompactLayout()
    {
        RectTransform heading = null;
        foreach (Transform child in panel)
        {
            if (child.name == "PackHeading" || child.name == "PackCount" || child.name == "PackScroll")
                _packSection.Add(child.gameObject);
            if (child.name == "PackHeading") heading = (RectTransform)child;
        }

        if (heading == null || legendButton == null || debugButton == null) return;

        _expandedPanelHeight = panel.sizeDelta.y;
        _expandedButtonsY = ((RectTransform)legendButton.transform).anchoredPosition.y;
        _compactButtonsY = heading.anchoredPosition.y;
        _hasCompactLayout = true;
    }

    private void ApplyPackVisibility()
    {
        bool visible = showPackInNormalView || _debug;
        foreach (GameObject part in _packSection) part.SetActive(visible);
        if (!_hasCompactLayout) return;

        float buttonsY = visible ? _expandedButtonsY : _compactButtonsY;
        SetY((RectTransform)legendButton.transform, buttonsY);
        SetY((RectTransform)debugButton.transform, buttonsY);

        float height = visible ? _expandedPanelHeight : _expandedPanelHeight - (_compactButtonsY - _expandedButtonsY);
        panel.sizeDelta = new Vector2(panel.sizeDelta.x, height);
    }

    private static void SetY(RectTransform rect, float y) =>
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);

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

    public void ToggleLegend()
    {
        _legend = !_legend;
        legend.gameObject.SetActive(_legend);
        TintButton(legendButton, _legend);
    }

    public void ToggleDebug()
    {
        _debug = !_debug;
        debugPanel.gameObject.SetActive(_debug);
        TintButton(debugButton, _debug);
        ApplyPackVisibility();
        _nextTelemetry = 0f;
    }

    private static void TintButton(UnityEngine.UI.Button button, bool selected)
    {
        if (button != null && button.targetGraphic != null)
            button.targetGraphic.color = selected ? new Color(0.18f, 0.38f, 0.49f) : new Color(0.16f, 0.21f, 0.28f);
    }

    private void LateUpdate()
    {
        AdaptLayout();
        SynchronizePack();
        if (hunter != null)
        {
            _hunterRow.Restore();
            var symbol = visuals.GetSymbol(hunter.CurrentState);
            string state = HunterStateName(hunter.CurrentState);
            bool alert = hunter.DetectedWolf != null;
            _hunterRow.Update(visuals, symbol, state, false, alert, false, _debug, transitionDuration);
            _hunterBadge.Update(visuals, symbol, state, false, alert, false, _debug, transitionDuration);
            Project(_hunterBadge.Root, hunter.transform.position + Vector3.up * hunterBadgeHeight,
                hunter.isActiveAndEnabled && !hunter.IsDead);
        }
        else
        {
            _hunterRow.Missing();
            _hunterBadge.Root.gameObject.SetActive(false);
        }
        UpdateProgress();
        foreach (var pair in _active)
        {
            var v = pair.Value;
            var wolf = v.Agent;
            if (wolf == null) continue;
            var symbol = visuals.GetSymbol(wolf.CurrentState);
            string state = WolfStateName(wolf.CurrentState);
            v.Row.Update(visuals, symbol, state, wolf.IsHowling, wolf.JustReceivedAlert, wolf.IsUsingPathfinding, _debug, transitionDuration);
            v.Badge.Update(visuals, symbol, state, wolf.IsHowling, wolf.JustReceivedAlert, wolf.IsUsingPathfinding, _debug, transitionDuration);
            Project(v.Badge.Root, wolf.transform.position + Vector3.up * wolfBadgeHeight, wolf.isActiveAndEnabled);
            Project(v.Marker, wolf.LastKnownPosition, _debug && wolf.HasLastKnownPosition);
        }
        if (_debug && Time.unscaledTime >= _nextTelemetry)
        {
            UpdateTelemetry();
            _nextTelemetry = Time.unscaledTime + telemetryInterval;
        }
    }

    private WolfView CreateWolfView(RectTransform row)
    {
        var badge = Instantiate(badgeTemplate, worldBadges);
        var marker = Instantiate(markerTemplate, worldBadges);
        var view = new WolfView { Row = new Item(row, false), Badge = new Item(badge, true), Marker = marker };
        foreach (var text in marker.GetComponentsInChildren<UnityEngine.UI.Text>(true)) view.MarkerId = text;
        badge.gameObject.SetActive(false);
        marker.gameObject.SetActive(false);
        return view;
    }

    private void SynchronizePack()
    {
        foreach (var pair in _active) pair.Value.Seen = false;
        foreach (var wolf in Wolf.Pack)
        {
            if (wolf == null || !wolf.isActiveAndEnabled) continue;
            int id = wolf.GetInstanceID();
            if (!_active.TryGetValue(id, out var view))
            {
                if (!_identifiers.TryGetValue(id, out string label))
                {
                    label = "L" + _nextId.ToString("00");
                    _nextId++;
                    _identifiers.Add(id, label);
                }
                if (_pool.Count > 0)
                {
                    view = _pool[_pool.Count - 1];
                    _pool.RemoveAt(_pool.Count - 1);
                }
                else view = CreateWolfView(Instantiate(rowTemplate, packContent));
                view.Agent = wolf;
                view.Row.SetIdentifier(label);
                view.Badge.SetIdentifier(label);
                view.MarkerId.text = label;
                view.Row.Root.name = label + "Row";
                view.Badge.Root.name = label + "Badge";
                view.Marker.name = label + "LastPosition";
                view.Row.Root.SetAsLastSibling();
                view.Row.Root.gameObject.SetActive(true);
                _active.Add(id, view);
            }
            view.Seen = true;
        }
        _removed.Clear();
        foreach (var pair in _active) if (!pair.Value.Seen) _removed.Add(pair.Key);
        foreach (int id in _removed)
        {
            var view = _active[id];
            view.Row.Root.gameObject.SetActive(false);
            view.Badge.Root.gameObject.SetActive(false);
            view.Marker.gameObject.SetActive(false);
            view.Agent = null;
            _pool.Add(view);
            _active.Remove(id);
        }
        if (_lastPackCount != _active.Count)
        {
            _lastPackCount = _active.Count;
            packCount.text = _lastPackCount.ToString("00");
        }
    }

    private void UpdateProgress()
    {
        int count = hunter != null ? hunter.GatherCount : 0;
        int required = hunter != null ? Mathf.Max(1, hunter.RequiredGathers) : 0;
        if (count == _lastGather && required == _lastRequired) return;
        _lastGather = count;
        _lastRequired = required;
        progressCounter.text = required > 0 ? count.ToString() + " / " + required.ToString() : "—";
        int visible = Mathf.Min(required, maximumSegments);
        while (_segments.Count < visible)
        {
            var image = Instantiate(_segments[0], progressSegments);
            _segments.Add(image);
        }
        for (int i = 0; i < _segments.Count; i++)
        {
            _segments[i].gameObject.SetActive(i < visible);
            float completion = required > 0 ? (float)count / required * visible : 0f;
            _segments[i].color = completion >= i + 1 ? visuals.GetColor(HUDStateVisuals.Symbol.Gather) : _emptySegment;
        }
    }

    private void AdaptLayout()
    {
        float canvasScale = Mathf.Max(0.01f, Mathf.Max(ScreenScale(), minimumPixelScale));
        if (_scaler != null && !Mathf.Approximately(_scaler.scaleFactor, canvasScale))
            _scaler.scaleFactor = canvasScale;

        _scale = 1f;
        legend.localScale = debugPanel.localScale = Vector3.one;
        panel.localScale = Vector3.one * panelScale;
        panel.anchoredPosition = new Vector2(16f / canvasScale, -16f / canvasScale);
        legend.anchoredPosition = new Vector2(-16f / canvasScale, -16f / canvasScale);
        float debugY = (16f + panel.sizeDelta.y * panelScale * canvasScale + 12f) / canvasScale;
        debugPanel.anchoredPosition = new Vector2(16f / canvasScale, -debugY);
    }

    private float ScreenScale()
    {
        float widthScale = Mathf.Log(Screen.width / _referenceResolution.x, 2f);
        float heightScale = Mathf.Log(Screen.height / _referenceResolution.y, 2f);
        return Mathf.Pow(2f, Mathf.Lerp(widthScale, heightScale, _matchWidthOrHeight));
    }

    private void Project(RectTransform rect, Vector3 world, bool visible)
    {
        if (targetCamera == null) visible = false;
        Vector3 point = visible ? targetCamera.WorldToViewportPoint(world) : Vector3.zero;
        visible &= point.z > 0f && point.x > 0.02f && point.x < 0.98f && point.y > 0.04f && point.y < 0.96f;
        if (rect.gameObject.activeSelf != visible) rect.gameObject.SetActive(visible);
        if (!visible) return;
        Vector2 screen = new Vector2(point.x * Screen.width, point.y * Screen.height);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(worldBadges, screen, null, out Vector2 local);
        rect.localPosition = new Vector3(local.x, local.y, 0f);
        rect.localScale = Vector3.one * _scale;
    }

    private void UpdateTelemetry()
    {
        _telemetry.Clear();
        if (hunter == null) _telemetry.AppendLine("C01: sin referencia");
        else
        {
            _telemetry.Append("C01: ").Append(HunterStateName(hunter.CurrentState));
            if (hunter.DetectedWolf != null) _telemetry.Append(" | ve: ").Append(hunter.DetectedWolf.name);
            if (hunter.ChosenSafeZone != null) _telemetry.Append(" | destino: ").Append(hunter.ChosenSafeZone.name);
            _telemetry.AppendLine();
        }
        foreach (var pair in _active)
        {
            Wolf wolf = pair.Value.Agent;
            if (wolf == null) continue;
            _telemetry.Append(_identifiers[pair.Key]).Append(": ").Append(WolfStateName(wolf.CurrentState));
            if (wolf.IsUsingPathfinding) _telemetry.Append(" | A*");
            if (wolf.IsHowling) _telemetry.Append(" | aullido");
            if (wolf.JustReceivedAlert)
            {
                _telemetry.Append(" | alerta");
                if (wolf.LastAlertSender != null && _identifiers.TryGetValue(wolf.LastAlertSender.GetInstanceID(), out string sender))
                    _telemetry.Append(" de ").Append(sender);
            }
            _telemetry.AppendLine();
        }
        string value = _telemetry.ToString().TrimEnd();
        if (debugText.text != value) debugText.text = value;
    }

    private static string HunterStateName(Hunter.HunterState state) => state switch
    {
        Hunter.HunterState.Search => "Search", Hunter.HunterState.Attack => "Attack",
        Hunter.HunterState.Gather => "Gather", Hunter.HunterState.Flee => "Flee",
        Hunter.HunterState.Exit => "Exit", Hunter.HunterState.Dead => "Dead", _ => "Patrol"
    };
    private static string WolfStateName(Wolf.WolfState state) => state switch
    {
        Wolf.WolfState.Chase => "Chase", Wolf.WolfState.Investigate => "Investigate",
        Wolf.WolfState.Search => "Search", _ => "Patrol"
    };
}
