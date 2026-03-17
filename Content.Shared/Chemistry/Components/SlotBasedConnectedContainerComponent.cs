using Content.Shared.Containers;
using Content.Shared.Inventory;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Component for marking linked container in character slot, to which entity is bound.
/// </summary>
[RegisterComponent, Access(typeof(SlotBasedConnectedContainerSystem)), NetworkedComponent]
public sealed partial class SlotBasedConnectedContainerComponent : Component
{
    /// <summary>
    /// The slot in which target container should be.
    /// </summary>
    [DataField(required: true)]
    public SlotFlags TargetSlot;

    /// <summary>
    /// A whitelist for determining whether container is valid or not .
    /// </summary>
    [DataField]
    public EntityWhitelist? ContainerWhitelist;
}
