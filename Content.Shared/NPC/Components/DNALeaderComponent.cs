namespace Content.Shared.NPC.Components;

/// <summary>
/// Component for people who have been imprinted as a leader.
/// </summary>
[RegisterComponent]
public sealed partial class DNALeaderComponent : Component
{
    /// <summary>
    /// List of imprinted followers.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> Followers = new();
}
