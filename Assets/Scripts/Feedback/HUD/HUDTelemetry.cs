using System.Text;
using UnityEngine;
using UnityEngine.UI;

public sealed class HUDTelemetry
{
    private readonly Text output;
    private readonly float interval;
    private readonly StringBuilder builder = new StringBuilder(512);
    private float nextRefresh;

    public HUDTelemetry(Text output, float interval)
    {
        this.output = output;
        this.interval = interval;
    }

    public void RefreshNow() => nextRefresh = 0f;

    public void Tick(Hunter hunter, HUDPackList pack)
    {
        if (Time.unscaledTime < nextRefresh) return;

        nextRefresh = Time.unscaledTime + interval;
        builder.Clear();
        AppendHunter(hunter);
        foreach (HUDWolfView view in pack.Views) AppendWolf(view, pack);

        string value = builder.ToString().TrimEnd();
        if (output.text != value) output.text = value;
    }

    private void AppendHunter(Hunter hunter)
    {
        if (hunter == null)
        {
            builder.AppendLine("C01: sin referencia");
            return;
        }

        builder.Append("C01: ").Append(HUDStateNames.Of(hunter.CurrentState));
        if (hunter.DetectedWolf != null) builder.Append(" | ve: ").Append(hunter.DetectedWolf.name);
        if (hunter.ChosenSafeZone != null) builder.Append(" | destino: ").Append(hunter.ChosenSafeZone.name);
        builder.AppendLine();
    }

    private void AppendWolf(HUDWolfView view, HUDPackList pack)
    {
        Wolf wolf = view.Agent;
        if (wolf == null) return;

        builder.Append(view.Label).Append(": ").Append(HUDStateNames.Of(wolf.CurrentState));
        if (wolf.IsUsingPathfinding) builder.Append(" | A*");
        if (wolf.IsHowling) builder.Append(" | aullido");
        if (wolf.JustReceivedAlert)
        {
            builder.Append(" | alerta");
            if (pack.TryGetLabel(wolf.LastAlertSender, out string sender))
                builder.Append(" de ").Append(sender);
        }
        builder.AppendLine();
    }
}
