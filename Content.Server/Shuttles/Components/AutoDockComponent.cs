namespace Content.Server.Shuttles.Components;

/// <summary>
/// Added to entities when they are actively trying to dock with something else.
/// We track it because checking every dock constantly would be expensive.
/// </summary>
[RegisterComponent]
public sealed partial class AutoDockComponent : Component
{
    /// <summary>
    /// Track who has requested autodocking so we can know when to be removed.
    /// </summary>
    public HashSet<EntityUid> Requesters = new();
}
