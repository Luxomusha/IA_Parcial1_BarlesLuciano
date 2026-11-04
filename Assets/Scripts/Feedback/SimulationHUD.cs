using UnityEngine;

public class SimulationHUD : MonoBehaviour
{
    [SerializeField] private Hunter hunter;
    [Tooltip("If empty, Camera.main is used.")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool showPanel = true;
    [SerializeField] private bool showLabels = true;
    [SerializeField] private bool showLastKnownPositions = true;
    [SerializeField, Range(10, 30)] private int fontSize = 14;

    private GUIStyle panelStyle;
    private GUIStyle labelStyle;
    private Texture2D panelBackground;

    private void Awake()
    {
        if (hunter == null) hunter = FindFirstObjectByType<Hunter>();
    }

    private void OnDestroy()
    {
        if (panelBackground != null) Destroy(panelBackground);
    }

    private void OnGUI()
    {
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        EnsureStyles();

        if (showPanel) DrawPanel();
        if (cam == null) return;

        if (showLastKnownPositions)
        {
            foreach (Wolf wolf in Wolf.Pack)
            {
                if (wolf.HasLastKnownPosition)
                    DrawWorldLabel(cam, wolf.LastKnownPosition, $"X  ({ShortName(wolf)})", Color.magenta);
            }
        }

        if (!showLabels) return;

        if (hunter != null && hunter.isActiveAndEnabled && !hunter.IsDead)
            DrawWorldLabel(cam, hunter.transform.position + Vector3.up * 1.5f, HunterLabel(), HunterColor());

        foreach (Wolf wolf in Wolf.Pack)
            DrawWorldLabel(cam, wolf.transform.position + Vector3.up * 1.2f, WolfLabel(wolf), WolfColor(wolf));
    }

    private void DrawPanel()
    {
        var text = new System.Text.StringBuilder();

        if (hunter != null && hunter.isActiveAndEnabled)
        {
            text.AppendLine($"<b>CAZADOR</b>  {hunter.CurrentState}   recolecciones {hunter.GatherCount}/{hunter.RequiredGathers}");
            if (hunter.DetectedWolf != null)
                text.AppendLine($"   <color=#ff6060>ve a un lobo: {hunter.DetectedWolf.name}</color>");
            if (hunter.CurrentState == Hunter.HunterState.Flee && hunter.ChosenSafeZone != null)
                text.AppendLine($"   <color=#ff80ff>huye a: {hunter.ChosenSafeZone.name}</color>");
            text.AppendLine();
        }

        text.AppendLine("<b>LOBOS</b>");
        foreach (Wolf wolf in Wolf.Pack)
        {
            string line = $"{ShortName(wolf)}: {wolf.CurrentState}";
            if (wolf.IsUsingPathfinding) line += "  <color=#60ffff>[A*]</color>";
            if (wolf.IsHowling) line += "  <color=#ff6060>AULLA</color>";
            else if (wolf.JustReceivedAlert)
                line += $"  <color=#ffff60>alerta de {ShortName(wolf.LastAlertSender)}</color>";
            text.AppendLine(line);
        }

        GUIContent content = new GUIContent(text.ToString().TrimEnd());
        Vector2 size = panelStyle.CalcSize(content);
        GUI.Box(new Rect(10f, 10f, size.x, size.y), content, panelStyle);
    }

    private void DrawWorldLabel(Camera cam, Vector3 worldPosition, string text, Color color)
    {
        Vector3 screen = cam.WorldToScreenPoint(worldPosition);
        if (screen.z <= 0f) return;

        GUIContent content = new GUIContent(text);
        Vector2 size = labelStyle.CalcSize(content);
        Rect rect = new Rect(screen.x - size.x * 0.5f, Screen.height - screen.y - size.y, size.x, size.y);

        labelStyle.normal.textColor = Color.black;
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), content, labelStyle);
        labelStyle.normal.textColor = color;
        GUI.Label(rect, content, labelStyle);
    }

    private string HunterLabel()
    {
        string label = $"{hunter.CurrentState} {hunter.GatherCount}/{hunter.RequiredGathers}";
        if (hunter.DetectedWolf != null) label += "  !";
        return label;
    }

    private Color HunterColor() => hunter.CurrentState switch
    {
        Hunter.HunterState.Flee => Color.yellow,
        Hunter.HunterState.Attack => new Color(1f, 0.4f, 0.4f),
        Hunter.HunterState.Gather => Color.green,
        Hunter.HunterState.Exit => Color.cyan,
        _ => Color.white,
    };

    private static string WolfLabel(Wolf wolf)
    {
        string label = wolf.CurrentState.ToString();
        if (wolf.IsHowling) label = "AUUU!  " + label;
        else if (wolf.JustReceivedAlert) label = "?  " + label;
        if (wolf.IsUsingPathfinding) label += " [A*]";
        return label;
    }

    private static Color WolfColor(Wolf wolf) => wolf.CurrentState switch
    {
        Wolf.WolfState.Chase => new Color(1f, 0.35f, 0.35f),
        Wolf.WolfState.Investigate => new Color(1f, 0.6f, 0.1f),
        Wolf.WolfState.Search => new Color(1f, 0.95f, 0.3f),
        _ => new Color(0.85f, 0.85f, 0.9f),
    };

    private static string ShortName(Wolf wolf) =>
        wolf == null ? "?" : wolf.name.Replace("Wolf_", "");

    private void EnsureStyles()
    {
        if (panelStyle != null && panelStyle.fontSize == fontSize) return;

        if (panelBackground == null)
        {
            panelBackground = new Texture2D(1, 1);
            panelBackground.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.65f));
            panelBackground.Apply();
        }

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = fontSize,
            alignment = TextAnchor.UpperLeft,
            richText = true,
            padding = new RectOffset(10, 10, 8, 8),
        };
        panelStyle.normal.background = panelBackground;
        panelStyle.normal.textColor = Color.white;

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            richText = false,
        };
    }
}
