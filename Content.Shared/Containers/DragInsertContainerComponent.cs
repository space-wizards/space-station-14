using Robust.Shared.GameStates;

namespace Content.Shared.Containers;

/// <summary>
/// This is used for a container that can have entities inserted into it via a
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(DragInsertContainerSystem))]
public sealed partial class DragInsertContainerComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string ContainerId;

    // todo: doafter support
    // [DataField, ViewVariables(VVAccess.ReadWrite)]
    // public TimeSpan InsertDelay = TimeSpan.Zero;
}
