namespace Content.Shared.RPGoals;

public static class RPGoalUnsafePolicy
{
    public static readonly HashSet<string> DefaultForbiddenTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "AntagOnly",
        "Violence",
        "Theft",
        "Sabotage",
        "Powergaming",
        "Metagame",
        "ERP",
        "CommandBreach"
    };
}
