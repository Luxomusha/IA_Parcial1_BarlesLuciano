using UnityEngine;
using UnityEngine.UI;

public sealed class HUDAgentItem
{
    public readonly RectTransform Root;
    public readonly Image StateIcon;

    private readonly Image halo;
    private readonly Image howlIcon;
    private readonly Image alertIcon;
    private readonly Image pathIcon;
    private readonly Text identifier;
    private readonly Text stateName;
    private readonly Text detail;
    private readonly bool floating;

    private HUDStateVisuals.Symbol lastSymbol;
    private bool hasSymbol;
    private bool howling;
    private bool alerted;
    private bool debug;
    private float pulse;
    private Color accent;

    public HUDAgentItem(RectTransform root, bool floating)
    {
        Root = root;
        this.floating = floating;

        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            switch (image.name)
            {
                case "StateIcon": StateIcon = image; break;
                case "Halo": halo = image; break;
                case "HowlIcon": howlIcon = image; break;
                case "AlertIcon": alertIcon = image; break;
                case "PathIcon": pathIcon = image; break;
            }
        }

        foreach (Text text in root.GetComponentsInChildren<Text>(true))
        {
            switch (text.name)
            {
                case "Identifier": identifier = text; break;
                case "StateName": stateName = text; break;
                case "Detail": detail = text; break;
            }
        }
    }

    public void SetIdentifier(string value)
    {
        if (identifier != null && identifier.text != value) identifier.text = value;
    }

    public void Update(HUDStateVisuals visuals, HUDStateVisuals.Symbol symbol, string state,
        bool isHowling, bool isAlerted, bool usesPath, bool showDebug, float duration)
    {
        UpdateSymbol(visuals, symbol, state, duration);
        UpdateEvents(isHowling, isAlerted, usesPath, showDebug, duration);
        UpdateDebugTexts(usesPath, showDebug);
        UpdateEventLayout(showDebug);
        UpdatePulse(duration);
    }

    public void Missing()
    {
        if (StateIcon != null) StateIcon.enabled = false;
        if (stateName != null) stateName.text = "Sin cazador";
    }

    public void Restore()
    {
        if (StateIcon != null) StateIcon.enabled = true;
    }

    private void UpdateSymbol(HUDStateVisuals visuals, HUDStateVisuals.Symbol symbol, string state, float duration)
    {
        if (hasSymbol && lastSymbol == symbol) return;

        hasSymbol = true;
        lastSymbol = symbol;
        accent = visuals.GetColor(symbol);
        if (StateIcon != null) StateIcon.sprite = visuals.GetSprite(symbol);
        if (stateName != null) stateName.text = state;
        pulse = duration;
    }

    private void UpdateEvents(bool isHowling, bool isAlerted, bool usesPath, bool showDebug, float duration)
    {
        if ((isHowling && !howling) || (isAlerted && !alerted)) pulse = duration;

        howling = isHowling;
        alerted = isAlerted;
        SetActive(howlIcon, isHowling);
        SetActive(alertIcon, isAlerted);
        SetActive(pathIcon, showDebug && usesPath);
    }

    private void UpdateDebugTexts(bool usesPath, bool showDebug)
    {
        if (stateName != null) SetActive(stateName, showDebug);
        if (identifier != null && floating) SetActive(identifier, showDebug);
        if (detail == null) return;

        SetActive(detail, showDebug && !floating);
        string value = usesPath ? "A* activo" : "";
        if (detail.text != value) detail.text = value;
    }

    private void UpdateEventLayout(bool showDebug)
    {
        if (!floating && debug != showDebug)
        {
            PositionEvent(howlIcon, showDebug ? 181 : 156, showDebug ? -31 : -17, showDebug ? 16 : 20);
            PositionEvent(alertIcon, showDebug ? 205 : 185, showDebug ? -31 : -17, showDebug ? 16 : 20);
            PositionEvent(pathIcon, showDebug ? 229 : 214, showDebug ? -31 : -17, showDebug ? 16 : 20);
        }

        debug = showDebug;
    }

    private void UpdatePulse(float duration)
    {
        if (pulse <= 0f) return;

        pulse = Mathf.Max(0f, pulse - Time.unscaledDeltaTime);
        float amount = pulse / duration;

        if (halo != null)
        {
            halo.rectTransform.localScale = Vector3.one * (1f + amount * 0.18f);
            halo.color = new Color(accent.r, accent.g, accent.b, floating ? 0.92f : 0.18f + amount * 0.13f);
        }

        if (StateIcon != null) StateIcon.color = Color.Lerp(Color.white, accent, amount * 0.4f);
    }

    private static void SetActive(Component component, bool value)
    {
        if (component != null && component.gameObject.activeSelf != value)
            component.gameObject.SetActive(value);
    }

    private static void PositionEvent(Image image, float x, float y, float size)
    {
        if (image == null) return;

        image.rectTransform.anchoredPosition = new Vector2(x, y);
        image.rectTransform.sizeDelta = new Vector2(size, size);
    }
}
