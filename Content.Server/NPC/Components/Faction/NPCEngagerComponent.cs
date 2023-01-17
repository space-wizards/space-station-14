/// <summary>
/// Inverse of combat target, this is added when we are trying to kill something.
/// </summary>
[RegisterComponent]
public sealed class NPCEngagerComponent : Component
{
    public TimeSpan Decay = TimeSpan.FromSeconds(7);

    public TimeSpan? RemoveWhen;

    /// <summary>
    /// Which entities we are trying to kill right now...
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> EngagedEnemies = new();
}
