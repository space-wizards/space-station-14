namespace Content.Server.NPC;

/// <summary>
/// Cached data for the faction prototype. Can be modified at runtime.
/// </summary>
public sealed class FactionData
{
    [ViewVariables]
    public HashSet<string> Friendly = new();

    [ViewVariables]
    public HashSet<string> Hostile = new();
}
