namespace Content.Shared.RPGoals;

public sealed class RPGoalSession
{
    public string UserId { get; init; } = default!;
    public string RoleId { get; init; } = default!;
    public int RerollsRemaining { get; set; } = 2;
    public bool Finalized { get; set; }
    public string? SelectedGoalId { get; set; }
    public HashSet<string> SeenGoalIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<RPGoalOption> CurrentOptions { get; } = new();
}
