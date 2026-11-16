public static class HUDStateNames
{
    public static string Of(Hunter.HunterState state) => state switch
    {
        Hunter.HunterState.Search => "Search",
        Hunter.HunterState.Attack => "Attack",
        Hunter.HunterState.Gather => "Gather",
        Hunter.HunterState.Flee => "Flee",
        Hunter.HunterState.Exit => "Exit",
        Hunter.HunterState.Dead => "Dead",
        _ => "Patrol"
    };

    public static string Of(Wolf.WolfState state) => state switch
    {
        Wolf.WolfState.Chase => "Chase",
        Wolf.WolfState.Investigate => "Investigate",
        Wolf.WolfState.Search => "Search",
        _ => "Patrol"
    };
}
