namespace Content.Shared.Silicons.Borgs.Components;

/// <summary>
/// This is used for an entity that is linked to an MMI.
/// Mostly for receiving events.
/// </summary>
[RegisterComponent]
public sealed class MMILinkedComponent : Component
{
    /// <summary>
    /// The MMI this entity is linked to.
    /// </summary>
    [DataField("linkedMMI")]
    public EntityUid? LinkedMMI;
}
