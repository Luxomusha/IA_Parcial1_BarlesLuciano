using UnityEngine;

[CreateAssetMenu(menuName = "Simulation/HUD State Visuals")]
public class HUDStateVisuals : ScriptableObject
{
    public enum Symbol { Patrol, Search, Attack, Gather, Flee, Exit, Dead, Chase, Investigate, Howl, Alert, Path, Track }

    [SerializeField] private Sprite[] symbols = new Sprite[13];
    [SerializeField] private Sprite background;
    [SerializeField] private Font font;
    [Header("State accents")]
    [SerializeField] private Color neutral = new Color(0.59f, 0.69f, 0.79f);
    [SerializeField] private Color danger = new Color(1f, 0.36f, 0.39f);
    [SerializeField] private Color warning = new Color(1f, 0.67f, 0.28f);
    [SerializeField] private Color searching = new Color(0.97f, 0.85f, 0.36f);
    [SerializeField] private Color gathering = new Color(0.34f, 0.89f, 0.65f);
    [SerializeField] private Color exiting = new Color(0.31f, 0.81f, 0.95f);

    public Sprite Background => background;
    public Font Font => font;
    public Sprite GetSprite(Symbol symbol) => symbols != null && (int)symbol < symbols.Length ? symbols[(int)symbol] : null;
    public Symbol GetSymbol(Hunter.HunterState state) => state switch
    {
        Hunter.HunterState.Search => Symbol.Search,
        Hunter.HunterState.Attack => Symbol.Attack,
        Hunter.HunterState.Gather => Symbol.Gather,
        Hunter.HunterState.Flee => Symbol.Flee,
        Hunter.HunterState.Exit => Symbol.Exit,
        Hunter.HunterState.Dead => Symbol.Dead,
        _ => Symbol.Patrol
    };
    public Symbol GetSymbol(Wolf.WolfState state) => state switch
    {
        Wolf.WolfState.Chase => Symbol.Chase,
        Wolf.WolfState.Investigate => Symbol.Investigate,
        Wolf.WolfState.Search => Symbol.Search,
        _ => Symbol.Patrol
    };
    public Color GetColor(Symbol symbol) => symbol switch
    {
        Symbol.Attack => danger,
        Symbol.Chase => danger,
        Symbol.Flee => warning,
        Symbol.Investigate => warning,
        Symbol.Search => searching,
        Symbol.Gather => gathering,
        Symbol.Exit => exiting,
        Symbol.Dead => new Color(0.46f, 0.5f, 0.55f),
        _ => neutral
    };
    public string GetCaption(Symbol symbol) => symbol switch
    {
        Symbol.Patrol => "Patrullar",
        Symbol.Search => "Buscar",
        Symbol.Attack => "Atacar",
        Symbol.Gather => "Recolectar",
        Symbol.Flee => "Huir",
        Symbol.Exit => "Salir",
        Symbol.Dead => "Muerto",
        Symbol.Chase => "Perseguir",
        Symbol.Investigate => "Investigar",
        Symbol.Howl => "Aullido",
        Symbol.Alert => "Alerta recibida",
        Symbol.Path => "Ruta A* (debug)",
        _ => "Última pista (debug)"
    };
}
