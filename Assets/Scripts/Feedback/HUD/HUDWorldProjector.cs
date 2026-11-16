using UnityEngine;

public sealed class HUDWorldProjector
{
    private const float HorizontalMargin = 0.02f;
    private const float VerticalMargin = 0.04f;

    private readonly Camera camera;
    private readonly RectTransform container;

    public HUDWorldProjector(Camera camera, RectTransform container)
    {
        this.camera = camera;
        this.container = container;
    }

    public void Project(RectTransform rect, Vector3 world, bool visible)
    {
        Vector3 viewport = Vector3.zero;
        if (camera == null) visible = false;
        else if (visible) viewport = camera.WorldToViewportPoint(world);

        visible &= IsOnScreen(viewport);
        if (rect.gameObject.activeSelf != visible) rect.gameObject.SetActive(visible);
        if (!visible) return;

        Vector2 screen = new Vector2(viewport.x * Screen.width, viewport.y * Screen.height);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(container, screen, null, out Vector2 local);
        rect.localPosition = new Vector3(local.x, local.y, 0f);
        rect.localScale = Vector3.one;
    }

    private static bool IsOnScreen(Vector3 viewport) =>
        viewport.z > 0f
        && viewport.x > HorizontalMargin && viewport.x < 1f - HorizontalMargin
        && viewport.y > VerticalMargin && viewport.y < 1f - VerticalMargin;
}
