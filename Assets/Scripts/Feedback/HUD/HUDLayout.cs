using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class HUDLayout
{
    private const float ScreenMargin = 16f;
    private const float DebugPanelGap = 12f;

    private readonly RectTransform panel;
    private readonly RectTransform legend;
    private readonly RectTransform debugPanel;
    private readonly RectTransform legendButton;
    private readonly RectTransform debugButton;
    private readonly float minimumPixelScale;
    private readonly float panelScale;

    private readonly List<GameObject> packSection = new List<GameObject>();
    private float expandedPanelHeight;
    private float expandedButtonsY;
    private float compactButtonsY;
    private bool hasCompactLayout;

    private CanvasScaler scaler;
    private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    private float matchWidthOrHeight = 0.5f;

    public HUDLayout(Canvas canvas, RectTransform panel, RectTransform legend, RectTransform debugPanel,
        Button legendButton, Button debugButton, float minimumPixelScale, float panelScale)
    {
        this.panel = panel;
        this.legend = legend;
        this.debugPanel = debugPanel;
        this.legendButton = legendButton != null ? (RectTransform)legendButton.transform : null;
        this.debugButton = debugButton != null ? (RectTransform)debugButton.transform : null;
        this.minimumPixelScale = minimumPixelScale;
        this.panelScale = panelScale;

        SetupCompactLayout();
        SetupScaler(canvas);
    }

    public void SetPackVisible(bool visible)
    {
        foreach (GameObject part in packSection) part.SetActive(visible);
        if (!hasCompactLayout) return;

        float buttonsY = visible ? expandedButtonsY : compactButtonsY;
        SetY(legendButton, buttonsY);
        SetY(debugButton, buttonsY);

        float height = visible ? expandedPanelHeight : expandedPanelHeight - (compactButtonsY - expandedButtonsY);
        panel.sizeDelta = new Vector2(panel.sizeDelta.x, height);
    }

    public void Adapt()
    {
        float canvasScale = Mathf.Max(0.01f, Mathf.Max(ScreenScale(), minimumPixelScale));
        if (scaler != null && !Mathf.Approximately(scaler.scaleFactor, canvasScale))
            scaler.scaleFactor = canvasScale;

        legend.localScale = debugPanel.localScale = Vector3.one;
        panel.localScale = Vector3.one * panelScale;

        float margin = ScreenMargin / canvasScale;
        panel.anchoredPosition = new Vector2(margin, -margin);
        legend.anchoredPosition = new Vector2(-margin, -margin);

        float debugY = (ScreenMargin + panel.sizeDelta.y * panelScale * canvasScale + DebugPanelGap) / canvasScale;
        debugPanel.anchoredPosition = new Vector2(margin, -debugY);
    }

    private void SetupCompactLayout()
    {
        RectTransform heading = null;
        foreach (Transform child in panel)
        {
            if (child.name == "PackHeading" || child.name == "PackCount" || child.name == "PackScroll")
                packSection.Add(child.gameObject);
            if (child.name == "PackHeading") heading = (RectTransform)child;
        }

        if (heading == null || legendButton == null || debugButton == null) return;

        expandedPanelHeight = panel.sizeDelta.y;
        expandedButtonsY = legendButton.anchoredPosition.y;
        compactButtonsY = heading.anchoredPosition.y;
        hasCompactLayout = true;
    }

    private void SetupScaler(Canvas canvas)
    {
        scaler = canvas.GetComponentInParent<CanvasScaler>();
        if (scaler == null) return;

        if (scaler.referenceResolution.x > 0f && scaler.referenceResolution.y > 0f)
            referenceResolution = scaler.referenceResolution;
        matchWidthOrHeight = scaler.matchWidthOrHeight;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
    }

    private float ScreenScale()
    {
        float widthScale = Mathf.Log(Screen.width / referenceResolution.x, 2f);
        float heightScale = Mathf.Log(Screen.height / referenceResolution.y, 2f);
        return Mathf.Pow(2f, Mathf.Lerp(widthScale, heightScale, matchWidthOrHeight));
    }

    private static void SetY(RectTransform rect, float y) =>
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
}
