using Robust.Shared.GameStates;

namespace Content.Shared.Nuke;

/// <summary>
/// Used for tracking the nuke disk - isn't a tag for pinpointer purposes.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class NukeDiskComponent : Component
{
    // Not synced on purpose.
    [DataField("attachedStation")]
    public EntityUid? AttachedStation = null;
}
