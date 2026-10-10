namespace Content.Server.NPC.Components;

/// <summary>
/// Component for people who have been imprinted as a leader.
/// </summary>
[RegisterComponent]
public sealed partial class DNALeaderComponent : Component
{
    /// <summary>
    /// List of imprinted followers.
    /// </summary>
    [ViewVariables]
    public readonly HashSet<EntityUid> Followers = new();
}
